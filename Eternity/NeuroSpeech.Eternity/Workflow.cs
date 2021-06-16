using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity
{
    public class EventResult
    {
        public string EventName { get; set; }
        public string Value { get; set; }
    }

    public abstract class Workflow<TWorkflow,TInput,TOutput>: IWorkflow
        where TWorkflow: Workflow<TWorkflow,TInput,TOutput>
    {

        public static Task<string> CreateAsync(EternityContext context, TInput input)
        {
            return context.CreateAsync<TInput, TOutput>(typeof(TWorkflow), input);
        }


        public string ID { get; private set; }

        public EternityContext Context { get; private set; }

        public DateTimeOffset CurrentUtc { get; private set; }

        Type IWorkflow.InputType => typeof(TInput);

        public abstract Task<TOutput> RunAsync(TInput input);

        void IWorkflow.Init(string id, EternityContext context, DateTimeOffset start)
        {
            this.ID = id;
            this.Context = context;
            this.CurrentUtc = start;
        }

        public Task<EventResult> WaitForExternalEventsAsync(TimeSpan maxWait,params string[] names)
        {
            if(maxWait.TotalMilliseconds <= 0)
            {
                throw new ArgumentException($"{nameof(maxWait)} cannot be in the past");
            }
            if(names.Length == 0)
            {
                throw new ArgumentException($"{nameof(names)} cannot be empty");
            }
            return Context.WaitForExternalEventsAsync(this, typeof(TWorkflow), ID, names, CurrentUtc.Add( maxWait));
        }

        public Task Delay(TimeSpan timeout)
        {
            if (timeout.TotalMilliseconds <= 0)
            {
                throw new ArgumentException($"{nameof(timeout)} cannot be in the past");
            }
            return Context.Delay(this, typeof(TWorkflow), ID, CurrentUtc.Add(timeout));
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<T> ScheduleResultAsync<T>(string method, params object[] items)
        {
            var fx = typeof(TWorkflow).GetMethod(method);
            return Context.ScheduleAsync<TWorkflow, T>(this, ID, CurrentUtc, fx, items);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task ScheduleAsync(string method, params object[] items)
        {
            var fx = typeof(TWorkflow).GetMethod(method);
            return Context.ScheduleAsync<TWorkflow>(this, ID, CurrentUtc, fx, items);
        }

        void IWorkflow.SetCurrentTime(DateTimeOffset time)
        {
            this.CurrentUtc = time;
        }

        async Task<object> IWorkflow.RunAsync(object input)
        {
            var result = await RunAsync((TInput)input);
            return result;
        }
    }
}
