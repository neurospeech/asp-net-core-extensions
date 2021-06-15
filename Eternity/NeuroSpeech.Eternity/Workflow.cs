using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity
{

    public abstract class Workflow<TWorkflow,TInput,TOutput>: IWorkflow
        where TWorkflow: Workflow<TWorkflow,TInput,TOutput>
    {

        public static Task<string> CreateAsync(EternityContext context, TInput input)
        {
            return context.CreateAsync<TInput, TOutput>(ClrHelper.Instance.GetDerived(typeof(TWorkflow)), input);
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

        public Task<string> WaitForExternalEventsAsync(string[] names, TimeSpan delay)
        {
            if(delay.TotalMilliseconds <= 0)
            {
                throw new NotSupportedException();
            }
            return Context.WaitForExternalEventsAsync(this, typeof(TWorkflow), ID, names, CurrentUtc.Add( delay));
        }

        public Task Delay(TimeSpan timeout)
        {
            return Context.Delay(this, typeof(TWorkflow), ID, CurrentUtc.Add(timeout));
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<T> ScheduleResultAsync<T>(MethodInfo fx, params object[] items)
        {
            return Context.ScheduleAsync<TWorkflow, T>(this, ID, CurrentUtc, fx, items);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task ScheduleAsync(MethodInfo fx, params object[] items)
        {
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
