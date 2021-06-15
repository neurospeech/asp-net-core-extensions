using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity
{

    public abstract class EternityContext
    {
        private readonly IEternityStorage storage;
        private readonly IServiceProvider services;
        private readonly IEternityClock clock;
        private readonly System.Text.Json.JsonSerializerOptions options;
        

        public EternityContext(
            IEternityStorage storage, 
            IServiceProvider services,
            IEternityClock clock)
        {
            this.storage = storage;
            this.services = services;
            this.clock = clock;
            this.options = new JsonSerializerOptions()
            {
                AllowTrailingCommas = true,
                IgnoreReadOnlyProperties = true,
                IgnoreReadOnlyFields = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

        }

        internal async Task<string> CreateAsync<TInput, TOutput>(Type type, TInput input, string id = null)
        {
            id ??= Guid.NewGuid().ToString("N");
            var utcNow = clock.UtcNow;
            var key = ActivityStep.Workflow(id, type, typeof(TOutput), input, utcNow, utcNow, options);
            await storage.ScheduleActivityAsync(key);
            return id;
        }

        public async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                var items = await storage.GetScheduledActivitiesAsync();
                await Task.WhenAll(items.Select(RunWorkflowAsync));
            }
        }

        private async Task RunWorkflowAsync(ActivityStep step)
        {
            if (step.ActivityType != ActivityType.Workflow)
                throw new InvalidOperationException();

            var workflowType = Type.GetType(step.WorkflowType);
            // we need to begin...
            var instance = GetWorkflowInstance(workflowType, step.ID, step.LastUpdated);
            var input = JsonSerializer.Deserialize(step.Parameters, instance.InputType, options);
            try
            {
                var result = await instance.RunAsync(input);
                step.Result = JsonSerializer.Serialize(result, options);
                step.LastUpdated = clock.UtcNow;
                step.Status = ActivityStatus.Completed;
            }
            catch (ActivitySuspendedException)
            {
                step.Status = ActivityStatus.Suspended;
                step.LastUpdated = clock.UtcNow;
            } catch(Exception ex)
            {
                step.Error = ex.ToString();
                step.Status = ActivityStatus.Failed;
                step.LastUpdated = clock.UtcNow;
            }
            await storage.UpdateAsync(step);
        }

        internal async Task Delay(IWorkflow workflow, Type type, string id, DateTimeOffset timeout)
        {
            var utcNow = clock.UtcNow;
            var key = ActivityStep.Delay(id, type, timeout, utcNow);
            var status = await GetActivityResultAsync(key);

            switch (status.Status)
            {
                case ActivityStatus.Completed:
                    workflow.SetCurrentTime(status.LastUpdated);
                    return;
                case ActivityStatus.Failed:
                    workflow.SetCurrentTime(status.LastUpdated);
                    throw new ActivityFailedException(status.Error);
                case ActivityStatus.None:
                    status = await storage.ScheduleActivityAsync(key);
                    break;
            }

            if(status.ETA <= utcNow)
            {
                // this was in the past...
                return;
            }

            var diff = status.ETA - utcNow;
            if (diff.TotalSeconds > 15)
                throw new ActivitySuspendedException();

            await Task.Delay(diff);
        }

        internal async Task<string> WaitForExternalEventsAsync(IWorkflow workflow, Type type, string id, string[] names, DateTimeOffset eta)
        {
            var utcNow = clock.UtcNow;
            var key = ActivityStep.Event(id, type, names, eta, utcNow);

            while (true)
            {

                var status = await GetActivityResultAsync(key);

                switch (status.Status)
                {
                    case ActivityStatus.Completed:
                        workflow.SetCurrentTime(status.LastUpdated);
                        return status.AsResult<string>(options);
                    case ActivityStatus.Failed:
                        workflow.SetCurrentTime(status.LastUpdated);
                        throw new ActivityFailedException(status.Error);
                    case ActivityStatus.None:
                        status = await storage.ScheduleActivityAsync(status);
                        break;
                }

                if (status.ETA <= utcNow)
                {
                    // this was in the past...
                    return status.Result;
                }

                var diff = status.ETA - utcNow;
                if (diff.TotalSeconds > 15)
                    throw new ActivitySuspendedException();

                await Task.Delay(diff);
            }
        }

        internal async Task<ActivityStep> GetActivityResultAsync(ActivityStep key)
        {
            
            return (await storage.GetStatusAsync(key)) ?? key;
        }

        public TActivityOutput Deserialize<TActivityOutput>(string result)
        {
            return JsonSerializer.Deserialize<TActivityOutput>(result);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal async Task ScheduleAsync<T>(IWorkflow workflow,
            string ID,
            DateTimeOffset after,
            MethodInfo method,
            params object[] input)
        {
            var utcNow = clock.UtcNow;
            var key = ActivityStep.Activity(ID, typeof(T), method, input, after, utcNow, options);

            while (true)
            {

                // has result...
                var task = await GetActivityResultAsync(key);

                switch (task.Status)
                {
                    case ActivityStatus.Failed:
                        workflow.SetCurrentTime(task.LastUpdated);
                        throw new ActivityFailedException(task.Error);
                    case ActivityStatus.Completed:
                        workflow.SetCurrentTime(task.LastUpdated);
                        return;
                    case ActivityStatus.None:
                        // lets schedule...
                        await storage.ScheduleActivityAsync(key);
                        if ((after - clock.UtcNow).TotalMinutes > 1)
                        {
                            throw new ActivitySuspendedException();
                        }
                        break;
                    case ActivityStatus.Suspended:
                    case ActivityStatus.Running:
                        if ((task.ETA - clock.UtcNow).TotalMinutes > 1)
                            throw new ActivitySuspendedException();
                        break;
                }

                await RunActivityAsync(task);
            }


        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal Task<TActivityOutput> ScheduleAsync<T, TActivityOutput>(IWorkflow workflow,
            string ID,
            DateTimeOffset after,
            MethodInfo method,
            params object[] input)
        {
            return ScheduleAsync<TActivityOutput>(workflow, typeof(T), ID, after, method, input);
        }


        [EditorBrowsable(EditorBrowsableState.Never)]
        internal async Task<TActivityOutput> ScheduleAsync<TActivityOutput>(
            IWorkflow workflow,
            Type type,
            string ID, 
            DateTimeOffset after, 
            MethodInfo method, 
            params object[] input)
        {
            var utcNow = clock.UtcNow;

            var key = ActivityStep.Activity(ID, type, method, input, after, utcNow, options);

            while (true)
            {

                // has result...
                var task = await GetActivityResultAsync(key);

                switch (task.Status)
                {
                    case ActivityStatus.Failed:
                        workflow.SetCurrentTime(task.LastUpdated);
                        throw new ActivityFailedException(task.Error);
                    case ActivityStatus.Completed:
                        workflow.SetCurrentTime(task.LastUpdated);
                        return task.AsResult<TActivityOutput>(options);
                    case ActivityStatus.None:
                        // lets schedule...
                        await storage.ScheduleActivityAsync(key);
                        if ((after - clock.UtcNow).TotalMinutes > 1)
                        {
                            throw new ActivitySuspendedException();
                        }
                        break;
                    case ActivityStatus.Suspended:
                    case ActivityStatus.Running:
                        if ((task.ETA - clock.UtcNow).TotalMinutes > 1)
                            throw new ActivitySuspendedException();
                        break;
                }

                await RunActivityAsync(task);
            }


        }

        internal async Task RunActivityAsync(ActivityStep key)
        {

            var task = await GetActivityResultAsync(key);

            var sequenceId = task.SequenceID;

            var type = Type.GetType(key.WorkflowType);

            var instance = GetWorkflowInstance(type, key.ID, key.ETA);

            // we are supposed to run this activity now...
            // acquire execution lock...
            var executionLock = await AcquireLockAsync(sequenceId);
            try
            {

                // requery that status...
                task = await GetActivityResultAsync(key);
                switch (task.Status)
                {
                    case ActivityStatus.Completed:
                    case ActivityStatus.Failed:
                        return;
                }

                using var scope = services.CreateScope();

                try
                {
                    var method = type.GetMethod(key.Method);

                    var parameters = BuildParameters(method, key.Parameters, scope.ServiceProvider);

                    var result = await method.RunAsync(instance, parameters, options);
                    key.Result = result;
                    await storage.UpdateAsync(key);
                    return;

                }
                catch (Exception ex) when (!(ex is ActivitySuspendedException))
                {
                    await storage.UpdateAsync(key);
                    throw new ActivityFailedException(ex.ToString());
                }


                // record the result here as well..
            }
            finally
            {
                await FreeLockAsync(executionLock);
            }
        }

        private object[] BuildParameters(MethodInfo method, string parameters, IServiceProvider serviceProvider)
        {
            var pas = method.GetParameters();
            var result = new object[pas.Length];
            var tuple = JsonSerializer.Deserialize<string[]>(parameters, options);
            for (int i = 0; i < pas.Length; i++)
            {
                var pa = pas[i];
                if(pa.GetCustomAttribute<InjectAttribute>() == null)
                {
                    var value = tuple[i];
                    result[i] = JsonSerializer.Deserialize(value, pa.ParameterType, options);
                    continue;
                }
                result[i] = serviceProvider.GetRequiredService(pa.ParameterType);
            }
            return result;
        }

        private IWorkflow GetWorkflowInstance(Type type, string id, DateTimeOffset eta)
        {
            var w = Activator.CreateInstance(type) as IWorkflow;
            w.Init(id, this, eta);
            return w;
        }

        internal Task<IEternityLock> AcquireLockAsync(long sequenceId)
        {
            return storage.AcquireLockAsync(sequenceId);
        }

        public string Serialize<TActivityOutput>(TActivityOutput result)
        {
            return JsonSerializer.Serialize<TActivityOutput>(result);
        }

        internal Task FreeLockAsync(IEternityLock executionLock)
        {
            return storage.FreeLockAsync(executionLock);
        }
    }

}
