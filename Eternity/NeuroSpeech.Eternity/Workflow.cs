using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity
{
    internal interface IWorkflow
    {
        void Init(string id, EternityContext context, DateTimeOffset start);

    }

    public abstract class Workflow<TWorkflow,TInput,TOutput>: IWorkflow
        where TWorkflow: Workflow<TWorkflow,TInput,TOutput>
    {

        private static MethodInfo runAsync;


        public static Task<string>  CreateAsync(EternityContext context, TInput input)
        {

            runAsync ??= typeof(TWorkflow).GetMethod(nameof(RunAsync));
            return context.CreateAsync(ClrHelper.Instance.GetDerived(typeof(TWorkflow)), runAsync, input);
        }


        public string ID { get; private set; }

        private EternityContext Context;

        public DateTimeOffset CurrentUtc { get; private set; }


        public abstract Task<TOutput> RunAsync(TInput input);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<TActivityOutput> ScheduleAsync<TActivityOutput>(TimeSpan after, MethodInfo method, params object[] input)
        {
            return Context.ScheduleAsync<TWorkflow,TActivityOutput>(ID, this.CurrentUtc.Add(after), method, input);
        }

        void IWorkflow.Init(string id, EternityContext context, DateTimeOffset start)
        {
            this.ID = id;
            this.Context = context;
            this.CurrentUtc = start;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Task<string> WaitForExternalEventsAsync(string[] names, TimeSpan delay, CancellationToken cancellationToken)
        {
            return Context.WaitForExternalEventsAsync(names, delay, cancellationToken);
        }
    }
}
