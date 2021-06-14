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
        private readonly System.Text.Json.JsonSerializerOptions options;
        

        public EternityContext(IEternityStorage storage, IServiceProvider services)
        {
            this.storage = storage;
            this.services = services;
            this.options = new JsonSerializerOptions()
            {
                AllowTrailingCommas = true,
                IgnoreReadOnlyProperties = true,
                IgnoreReadOnlyFields = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

        }

        internal Task<string> CreateAsync<TInput>(Type type, MethodInfo runAsync, TInput input)
        {
            throw new NotImplementedException();
        }

        public async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                var items = await storage.GetScheduledActivitiesAsync();
                await Task.WhenAll(items.Select(RunActivityAsync));
            }
        }

        internal Task<string> WaitForExternalEventsAsync(string[] names, TimeSpan delay, CancellationToken cancellationToken)
        {

            // create cancellation method... and decide which one will win...



            throw new NotImplementedException();
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
        public async Task ScheduleAsync<T>(
            string ID,
            DateTimeOffset after,
            MethodInfo method,
            params object[] input)
        {
            var key = ActivityStep.Create(ID, typeof(T), method, input, after, options);

            while (true)
            {

                // has result...
                var task = await GetActivityResultAsync(key);

                switch (task.Status)
                {
                    case ActivityStatus.Failed:
                        throw new ActivityFailedException(task.Error);
                    case ActivityStatus.Completed:
                        return;
                    case ActivityStatus.None:
                        // lets schedule...
                        await storage.ScheduleActivityAsync(key);
                        if ((after - DateTimeOffset.UtcNow).TotalMinutes > 1)
                        {
                            throw new ActivitySuspendedException();
                        }
                        break;
                    case ActivityStatus.Running:
                        if ((task.ETA - DateTimeOffset.UtcNow).TotalMinutes > 1)
                            throw new ActivitySuspendedException();
                        break;
                }

                await RunActivityAsync(task);
            }


        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public async Task<TActivityOutput> ScheduleAsync<T, TActivityOutput>(
            string ID, 
            DateTimeOffset after, 
            MethodInfo method, 
            params object[] input)
        {
            var key = ActivityStep.Create(ID, typeof(T), method, input, after, options);

            while (true)
            {

                // has result...
                var task = await GetActivityResultAsync(key);

                switch (task.Status)
                {
                    case ActivityStatus.Failed:
                        throw new ActivityFailedException(task.Error);
                    case ActivityStatus.Completed:
                        return task.AsResult<TActivityOutput>(options);
                    case ActivityStatus.None:
                        // lets schedule...
                        await storage.ScheduleActivityAsync(key);
                        if ((after - DateTimeOffset.UtcNow).TotalMinutes > 1)
                        {
                            throw new ActivitySuspendedException();
                        }
                        break;
                    case ActivityStatus.Running:
                        if ((task.ETA - DateTimeOffset.UtcNow).TotalMinutes > 1)
                            throw new ActivitySuspendedException();
                        break;
                }

                await RunActivityAsync(task);
            }


        }

        internal async Task RunActivityAsync(ActivityStep key)
        {
            using var scope = services.CreateScope();

            var task = await GetActivityResultAsync(key);

            var sequenceId = task.SequenceID;

            var type = Type.GetType(key.WorkflowType);

            var method = type.GetMethod(key.Method);

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
                        return;
                    case ActivityStatus.Failed:
                        throw new ActivityFailedException(task.Error);
                }

                try
                {
                    var parameters = BuildParameters(method, key.InputType, key.Parameters, scope.ServiceProvider);

                    var result = await method.RunAsync(instance, parameters.ToArray(), options);
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

        private List<object> BuildParameters(MethodInfo method, string inputType, string parameters, IServiceProvider serviceProvider)
        {
            var result = new List<object>();
            var pas = method.GetParameters();
            var input = Type.GetType(inputType);
            var tuple = JsonSerializer.Deserialize(parameters, input, options);
            for (int i = 0; i < pas.Length; i++)
            {
                var pa = pas[i];
                if(pa.GetCustomAttribute<InjectAttribute>() == null)
                {
                    result.Add(tuple.GetTupleValue(input, i+1));
                    continue;
                }
                result.Add( serviceProvider.GetRequiredService(pa.ParameterType) );
            }
            return result;
        }

        private IWorkflow GetWorkflowInstance(Type type, string id, DateTimeOffset eta)
        {
            var w = Activator.CreateInstance(type) as IWorkflow;
            w.Init(id, this, eta);
            return w;
        }

        internal Task<long> AcquireLockAsync(long sequenceId)
        {
            return storage.AcquireLockAsync(sequenceId);
        }

        public string Serialize<TActivityOutput>(TActivityOutput result)
        {
            return JsonSerializer.Serialize<TActivityOutput>(result);
        }

        internal Task FreeLockAsync(long executionLock)
        {
            return storage.FreeLockAsync(executionLock);
        }
    }

}
