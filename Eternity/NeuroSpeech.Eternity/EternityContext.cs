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

    public class EternityContext
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
            var key = WorkflowStep.Workflow(id, type, input, utcNow, utcNow, options);
            key = await storage.InsertWorkflowAsync(key);
            await storage.QueueWorkflowAsync(key.ID, utcNow);
            return id;
        }

        public async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                await ProcessMessagesOnceAsync();
            }
        }

        public async Task ProcessMessagesOnceAsync()
        {
            var items = await storage.GetScheduledActivitiesAsync();
            await Task.WhenAll(items.Select(RunWorkflowAsync));

        }

        private async Task RunWorkflowAsync(WorkflowQueueItem queueItem)
        {
            var step = await storage.GetWorkflowAsync(queueItem.ID);
            if (step.Status == ActivityStatus.Completed || step.Status == ActivityStatus.Failed)
            {
                await storage.RemoveQueueAsync(queueItem.QueueToken);
                return;
            }

            var workflowType = ClrHelper.Instance.GetDerived(Type.GetType(step.WorkflowType));
            // we need to begin...
            var instance = GetWorkflowInstance(workflowType, step.ID, step.LastUpdated);
            instance.QueueItemList.Add(queueItem.QueueToken);
            var input = JsonSerializer.Deserialize(step.Parameter, instance.InputType, options);
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
                await storage.UpdateAsync(step);
                await storage.RemoveQueueAsync(queueItem.QueueToken);
                return;
            }
            catch (Exception ex)
            {
                step.Error = ex.ToString();
                step.Status = ActivityStatus.Failed;
                step.LastUpdated = clock.UtcNow;
            }
            await storage.UpdateAsync(step);
            await storage.RemoveQueueAsync(instance.QueueItemList.ToArray());
        }

        internal async Task Delay(IWorkflow workflow, Type type, string id, DateTimeOffset timeout)
        {
            
            var key = ActivityStep.Delay(id, timeout, workflow.CurrentUtc);
            var status = await GetActivityResultAsync(workflow, key);

            switch (status.Status)
            {
                case ActivityStatus.Completed:
                    workflow.SetCurrentTime(status.LastUpdated);
                    return;
                case ActivityStatus.Failed:
                    workflow.SetCurrentTime(status.LastUpdated);
                    throw new ActivityFailedException(status.Error);
            }

            var utcNow = clock.UtcNow;
            if (status.ETA <= utcNow)
            {
                // this was in the past...
                status.Status = ActivityStatus.Completed;
                status.Result = "null";
                await storage.UpdateAsync(status);
                return;
            }

            var diff = status.ETA - utcNow;
            if (diff.TotalSeconds > 15)
            {
                throw new ActivitySuspendedException();
            }

            await Task.Delay(diff);

            status.Status = ActivityStatus.Completed;
            status.Result = "null";
            await storage.UpdateAsync(status);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="eventName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task RaiseEventAsync(
            string id,
            string eventName,
            string value,
            bool throwIfNotFound = false)
        {
            value = value ?? "";
            var key = await storage.GetEventAsync(id, eventName);
            if (key == null)
            {
                if(throwIfNotFound)
                    throw new NotSupportedException();
                return;
            }
            key.Result = Serialize(new EventResult {
                EventName = eventName,
                Value = value
            });
            key.ETA = clock.UtcNow;
            key.Status = ActivityStatus.Completed;
            await storage.UpdateAsync(key);
            // we need to change queue token here...
            key.QueueToken = await storage.QueueWorkflowAsync(key.ID, key.ETA, key.QueueToken);
        }

        internal async Task<EventResult> WaitForExternalEventsAsync(IWorkflow workflow, string id, string[] names, DateTimeOffset eta)
        {
            var key = ActivityStep.Event(id, names, eta, workflow.CurrentUtc);

            var status = await GetActivityResultAsync(workflow, key);

            while (true)
            {

                switch (status.Status)
                {
                    case ActivityStatus.Completed:
                        workflow.SetCurrentTime(status.LastUpdated);
                        return status.AsResult<EventResult>(options);
                    case ActivityStatus.Failed:
                        workflow.SetCurrentTime(status.LastUpdated);
                        throw new ActivityFailedException(status.Error);
                }

                var diff = status.ETA - clock.UtcNow;
                if (diff.TotalSeconds > 15)
                {
                    throw new ActivitySuspendedException();
                }

                if (diff.TotalMilliseconds > 0)
                {
                    await Task.Delay(diff);
                }

                status = await GetActivityResultAsync(workflow, status);
                if(status.Status != ActivityStatus.Completed && status.Status != ActivityStatus.Failed)
                {
                    var timedout = new EventResult { };
                    status.Result = Serialize(timedout);
                    status.Status = ActivityStatus.Completed;
                    status.LastUpdated = clock.UtcNow;
                    await storage.UpdateAsync(status);
                    return timedout;
                }
            }
        }

        internal async Task<ActivityStep> GetActivityResultAsync(IWorkflow workflow, ActivityStep key)
        {
            var r = await storage.GetStatusAsync(key);
            if (r != null){
                return r;
            }
            key = await storage.InsertActivityAsync(key);
            var qi = await storage.QueueWorkflowAsync(key.ID, key.ETA);
            key.QueueToken = qi;
            workflow.QueueItemList.Add(qi);
            return key;
        }

        public TActivityOutput Deserialize<TActivityOutput>(string result)
        {
            return JsonSerializer.Deserialize<TActivityOutput>(result);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal Task<TActivityOutput> ScheduleAsync<T, TActivityOutput>(IWorkflow workflow,
            bool uniqueParameters,
            string ID,
            DateTimeOffset after,
            MethodInfo method,
            params object[] input)
        {
            return ScheduleAsync<TActivityOutput>(typeof(T), workflow, uniqueParameters, ID, after, method, input);
        }


        [EditorBrowsable(EditorBrowsableState.Never)]
        internal async Task<TActivityOutput> ScheduleAsync<TActivityOutput>(
            Type type,
            IWorkflow workflow,
            bool uniqueParameters,
            string ID, 
            DateTimeOffset after, 
            MethodInfo method, 
            params object[] input)
        {

            var key = ActivityStep.Activity(uniqueParameters, ID, method, input, after, workflow.CurrentUtc, options);

            while (true)
            {

                // has result...
                var task = await GetActivityResultAsync(workflow, key);
                var utcNow = clock.UtcNow;

                switch (task.Status)
                {
                    case ActivityStatus.Failed:
                        workflow.SetCurrentTime(task.LastUpdated);
                        throw new ActivityFailedException(task.Error);
                    case ActivityStatus.Completed:
                        workflow.SetCurrentTime(task.LastUpdated);
                        if (typeof(TActivityOutput) == typeof(object))
                            return (TActivityOutput)(object)"null";
                        return task.AsResult<TActivityOutput>(options);
                }

                var diff = task.ETA - clock.UtcNow;
                if(diff.TotalSeconds > 15)
                {
                    throw new ActivitySuspendedException();
                }

                await RunActivityAsync(workflow, task);
            }


        }

        internal async Task RunActivityAsync(IWorkflow workflow, ActivityStep key)
        {

            var task = await GetActivityResultAsync(workflow, key);

            var sequenceId = task.SequenceID;

            var type = workflow.GetType().BaseType;

            // we are supposed to run this activity now...
            // acquire execution lock...
            var executionLock = await storage.AcquireLockAsync(key.ID, sequenceId);
            try
            {

                // requery that status...
                task = await GetActivityResultAsync(workflow, key);
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

                    var result = await method.RunAsync(workflow, parameters, options);
                    key.Result = result;
                    key.Status = ActivityStatus.Completed;
                    key.LastUpdated = clock.UtcNow;
                    await storage.UpdateAsync(key);
                    return;

                }
                catch (Exception ex) when (!(ex is ActivitySuspendedException))
                {
                    key.Error = ex.ToString();
                    key.Status = ActivityStatus.Failed;
                    key.LastUpdated = clock.UtcNow;
                    await storage.UpdateAsync(key);
                    throw new ActivityFailedException(ex.ToString());
                }


                // record the result here as well..
            }
            finally
            {
                await storage.FreeLockAsync(executionLock);
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

        public string Serialize<TActivityOutput>(TActivityOutput result)
        {
            return JsonSerializer.Serialize(result);
        }
    }

}
