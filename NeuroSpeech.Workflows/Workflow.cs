#nullable enable
using NeuroSpeech.Workflows.Impl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.Workflows
{

    public abstract class Workflow<TWorkflow, TInput, TOutput>: BaseWorkflow<TInput, TOutput>
        where TWorkflow: Workflow<TWorkflow, TInput, TOutput>
    {
        
        public static async Task<string> CreateInstanceAsync(BaseWorkflowService context, string instanceId, TInput input)
        {
            var o = await context.client.CreateOrchestrationInstanceAsync(typeof(TWorkflow), instanceId, input);
            return o.InstanceId;
        }

        public static async Task<string> CreateInstanceAsync(BaseWorkflowService context, string instanceId, TInput input, DateTime at)
        {
            var o = await context.client.CreateScheduledOrchestrationInstanceAsync(typeof(TWorkflow), instanceId, input, at);
            return o.InstanceId;
        }

        public static async Task<string> CreateInstanceAsync(BaseWorkflowService context, TInput input)
        {
            var o = await context.client.CreateOrchestrationInstanceAsync(typeof(TWorkflow), input);
            return o.InstanceId;
        }

        public static async Task<string> CreateInstanceAsync(BaseWorkflowService context, TInput input, DateTime at)
        {
            var o = await context.client.CreateScheduledOrchestrationInstanceAsync(typeof(TWorkflow), input, at);
            return o.InstanceId;
        }

        public static async Task CancelAsync(BaseWorkflowService context, string id)
        {
            var ctx = await context.client.GetOrchestrationStateAsync(id);
            await context.client.RaiseEventAsync(ctx.OrchestrationInstance, "__CANCEL", "cancel");
        }

        public static async Task<WorkflowResult<TOutput>> GetResultAsync(BaseWorkflowService context, string id)
        {
            var ctx = await context.client.GetOrchestrationStateAsync(id);
            return new WorkflowResult<TOutput>(ctx);
        }

        private readonly Dictionary<string, EventQueue> events = new Dictionary<string, EventQueue>();

        /// <summary>
        /// Invokes another Workflow in the same Orchestration Context,
        /// this will not create a new Orchestration, instead it will
        /// just call the workflow and use the methods as they were inside of
        /// this workflow
        /// </summary>
        /// <param name="workflow"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Task<TOutput> RunInAsync<TI, TO>(BaseWorkflow<TI,TO> workflow, TInput input)
        {
            if (workflow.context == null)
                throw new InvalidOperationException($"Cannot run workflow within an activity");
            var w = (ClrHelper.Instance.Build(typeof(TWorkflow).FullName, workflow.serviceProvider) as IWorkflowExecutor<TOutput>)!;
            return w.RunAsync(workflow.context, input!);
        }

        internal override void OnEvent(string name, string input)
        {
            GetEvent(name)
                .SetEvent(input);
        }

        /// <summary>
        /// Creates a waitable timer for given delay
        /// </summary>
        /// <param name="wait"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task Delay(TimeSpan wait, CancellationToken token = default)
        {
            if (context == null)
                throw new InvalidOperationException($"You cannot call create a timer from activity");
            if (wait.TotalMilliseconds <= 0)
                throw new ArgumentOutOfRangeException($"Cannot create timer for time in the past");
            try
            {
                await context.CreateTimer(context.CurrentUtcDateTime.Add(wait), true, token);
            }catch (TaskCanceledException) {
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected Task<TR> CallTaskAsync<TI, TActivity, TR>(TI input)
        {
            if (context == null)
                throw new InvalidOperationException($"You cannot call another activity from activity");
            return context.ScheduleTask<TR>(typeof(TActivity), input);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected Task<TR> CallTupleAsync2<T1, T2, TActivity, TR>(T1 i1, T2 i2)
        {
            if (context == null)
                throw new InvalidOperationException($"You cannot call another activity from activity");
            return context.ScheduleTask<TR>(typeof(TActivity), Tuple.Create(i1, i2));
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected Task<TR> CallTupleAsync3<T1, T2, T3, TActivity, TR>(T1 i1, T2 i2, T3 i3)
        {
            if (context == null)
                throw new InvalidOperationException($"You cannot call another activity from activity");
            return context.ScheduleTask<TR>(typeof(TActivity), Tuple.Create(i1, i2, i3));
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected Task<TR> CallTupleAsync4<T1, T2, T3, T4, TActivity, TR>(T1 i1, T2 i2, T3 i3, T4 i4)
        {
            if (context == null)
                throw new InvalidOperationException($"You cannot call another activity from activity");
            return context.ScheduleTask<TR>(typeof(TActivity), Tuple.Create(i1, i2, i3, i4));
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected Task<TR> CallTupleAsync5<T1, T2, T3, T4, T5, TActivity, TR>(T1 i1, T2 i2, T3 i3, T4 i4, T5 i5)
        {
            if (context == null)
                throw new InvalidOperationException($"You cannot call another activity from activity");
            return context.ScheduleTask<TR>(typeof(TActivity), Tuple.Create(i1, i2, i3, i4, i5));
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        protected Task<TR> CallTupleAsync6<T1, T2, T3, T4, T5, T6, TActivity, TR>(T1 i1, T2 i2, T3 i3, T4 i4, T5 i5, T6 i6)
        {
            if (context == null)
                throw new InvalidOperationException($"You cannot call another activity from activity");
            return context.ScheduleTask<TR>(typeof(TActivity), Tuple.Create(i1, i2, i3, i4, i5, i6));
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected Task<TR> CallTupleAsync7<T1, T2, T3, T4, T5, T6, T7, TActivity, TR>(T1 i1, T2 i2, T3 i3, T4 i4, T5 i5, T6 i6, T7 i7)
        {
            if (context == null)
                throw new InvalidOperationException($"You cannot call another activity from activity");
            return context.ScheduleTask<TR>(typeof(TActivity), Tuple.Create(i1, i2, i3, i4, i5, i6, i7));
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected Task<TR> CallTupleAsync8<T1, T2, T3, T4, T5, T6, T7, T8, TActivity, TR>(T1 i1, T2 i2, T3 i3, T4 i4, T5 i5, T6 i6, T7 i7, T8 i8)
        {
            if (context == null)
                throw new InvalidOperationException($"You cannot call another activity from activity");
            return context.ScheduleTask<TR>(typeof(TActivity), new Tuple<T1,T2,T3,T4,T5,T6,T7,T8>(i1, i2, i3, i4, i5, i6, i7, i8));
        }

        private EventQueue GetEvent(string name)
        {
            lock (this)
            {
                if (!this.events.TryGetValue(name, out var ve))
                {
                    ve = new EventQueue();
                    this.events[name] = ve;
                }
                return ve;
            }
        }

        public async Task<(string? Name, string? Result)> WaitForEventsAsync(
            TimeSpan maxWait, 
            params string[] eventNames) {
            if (context == null)
                throw new InvalidOperationException($"You cannot wait for event in the activity");
            if (maxWait.TotalMilliseconds <= 0)
                throw new ArgumentOutOfRangeException($"maxWait cannot be equal to or less than zero");
            List<EventQueue> list = new List<EventQueue>(eventNames.Length);
            try
            {

                CancellationTokenSource c = new CancellationTokenSource();
                List<Task> tasks = new List<Task>(eventNames.Length + 1) {};
                foreach (var m in eventNames)
                {
                    var e = GetEvent(m);
                    list.Add(e);
                    var (we, ct) = e.Request();
                    ct.Register(() => c.Cancel());
                    tasks.Add(we);
                }

                var timer = this.context.CreateTimer(this.context.CurrentUtcDateTime.Add(maxWait), "", c.Token);

                tasks.Add(timer);

                string? firedEvent = null;
                string? result = null;


                await Task.WhenAny(tasks);

                if (timer.IsCompleted)
                {
                    // timed out...
                    return (null, null);
                }

                c.Cancel();

                result = tasks.OfType<Task<string>>()
                    .First(x => x.IsCompleted)
                    .Result;

                return (firedEvent, result);
            } finally
            {
                foreach (var e in list)
                    e.Reset();
            }

        }


    }
}
