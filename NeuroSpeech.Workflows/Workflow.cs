﻿#nullable enable
using DurableTask.Core;
using NeuroSpeech.Workflows.Impl;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            if(events.TryGetValue(name, out var we))
            {
                we.SetEvent(input);
            }
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

        /// <summary>
        /// Returns EventResult after the event was fired or timedout
        /// </summary>
        /// <param name="event"></param>
        /// <param name="maxWait">Can be zero or Positive only</param>
        /// <returns></returns>
        protected async Task<EventResult> WaitForEvent(WorkflowEvent @event, TimeSpan maxWait)
        {
            if (context == null)
                throw new InvalidOperationException($"You cannot wait for event in the activity");
            if (maxWait.TotalMilliseconds < 0)
                throw new ArgumentOutOfRangeException($"maxWait cannot be negative");
            var (task, cancel) = @event.Request();
            try
            {
                if (task.IsCompleted)
                    return EventResult.From(task);
                if (maxWait.TotalMilliseconds == 0)
                {
                    await task;
                    return EventResult.From(task);
                }

                var timer = context.CreateTimer(context.CurrentUtcDateTime.Add(maxWait), true, cancel);

                await Task.WhenAny(timer, task);

                return EventResult.From(task);
            } finally
            {
                @event.Reset();
            }
        }

        /// <summary>
        /// Returns EventResult after any of two events was fired or timedout
        /// </summary>
        /// <param name="event"></param>
        /// <param name="maxWait">Can be zero or Positive only</param>
        /// <returns></returns>
        protected async Task<(EventResult Event1, EventResult Event2)> WaitForEvents(WorkflowEvent e1, WorkflowEvent e2, TimeSpan maxWait)
        {
            if (context == null)
                throw new InvalidOperationException($"You cannot wait for event in the activity");
            if (maxWait.TotalMilliseconds < 0)
                throw new ArgumentOutOfRangeException($"maxWait cannot be negative");
            var (t1, c1) = e1.Request();
            var (t2, c2) = e2.Request();
            try
            {
                if (t1.IsCompleted || t2.IsCompleted)
                {
                    return (EventResult.From(t1), EventResult.From(t2));
                }
                if (maxWait.TotalMilliseconds == 0)
                {
                    await Task.WhenAny(t1, t2);
                    return (EventResult.From(t1), EventResult.From(t2));
                }
                var c = new CancellationTokenSource();
                c1.Register(() => c.Cancel());
                c2.Register(() => c.Cancel());
                var timer = context.CreateTimer(context.CurrentUtcDateTime.Add(maxWait), true);
                await Task.WhenAny(t1, t2, timer);
                if (timer.IsCompleted)
                {
                    return (EventResult.TimedOutValue, EventResult.TimedOutValue);
                }
                var r1 = EventResult.From(t1);
                var r2 = EventResult.From(t2);
                return (r1, r2);
            }
            finally {
                e1.Reset();
                e2.Reset();
            }

        }

        /// <summary>
        /// Returns EventResult after any of three events was fired or timedout
        /// </summary>
        /// <param name="event"></param>
        /// <param name="maxWait">Can be zero or Positive only</param>
        /// <returns></returns>
        protected async Task<(EventResult Event1, EventResult Event2, EventResult Event3)> 
            WaitForEvents<T1, T2, T3>(WorkflowEvent e1, WorkflowEvent e2, WorkflowEvent e3, TimeSpan maxWait)
        {
            if (context == null)
                throw new InvalidOperationException($"You cannot wait for event in the activity");
            if (maxWait.TotalMilliseconds < 0)
                throw new ArgumentOutOfRangeException($"maxWait cannot be negative");
            var (t1, c1) = e1.Request();
            var (t2, c2) = e2.Request();
            var (t3, c3) = e3.Request();
            try
            {
                if (t1.IsCompleted || t2.IsCompleted || t3.IsCompleted)
                {
                    return (EventResult.From(t1), EventResult.From(t2), EventResult.From(t3));
                }
                if (maxWait.TotalMilliseconds == 0)
                {
                    await Task.WhenAny(t1, t2);
                    return (EventResult.From(t1), EventResult.From(t2), EventResult.From(t3));
                }
                var c = new CancellationTokenSource();
                c1.Register(() => c.Cancel());
                c2.Register(() => c.Cancel());
                c3.Register(() => c.Cancel());
                var timer = context.CreateTimer(context.CurrentUtcDateTime.Add(maxWait), true);
                await Task.WhenAny(t1, t2, t3, timer);
                if (timer.IsCompleted)
                {
                    return (EventResult.From(t1), EventResult.From(t2), EventResult.From(t3));
                }
                var r1 = EventResult.From(t1);
                var r2 = EventResult.From(t2);
                var r3 = EventResult.From(t3);
                return (r1, r2, r3);
            } finally {
                e1.Reset();
                e2.Reset();
                e3.Reset();
            }
        }

        private Dictionary<string, WorkflowEvent> events = new Dictionary<string, WorkflowEvent>();

        internal override void SetupEvents()
        {
            foreach(var f in typeof(TWorkflow).GetFields(System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (f.GetCustomAttribute<EventAttribute>() == null)
                    continue;
                var we = Activator.CreateInstance(f.FieldType) as WorkflowEvent;
                if (we == null)
                {
                    throw new TypeAccessException($"Event field types must be of type WorkflowEvent<T> only");
                }
                f.SetValue(this, we);
                events[f.Name] = we;
            }
        }

        //protected async Task<(string? result, bool timedOut)> WaitForEventStringAsync(string name, TimeSpan delay)
        //{
        //    if (context == null)
        //        throw new InvalidOperationException("You can not wait for an event inside an activity");
        //    try
        //    {
        //        var eq = events[name];
        //        gather?.Add(eq);
        //        if (delay.TotalMilliseconds == 0)
        //        {
        //            return (await eq.Wait().task, false);
        //        }
        //        var (wait, cancellation) = eq.Wait();
        //        if (wait.IsCompleted)
        //            return (await wait, false);

        //        var timer = context.CreateTimer<bool>(context.CurrentUtcDateTime.Add(delay), true, cancellation);
        //        if (timer.IsCompleted)
        //        {

        //            // if this is playing
        //            // it may already be timed out
        //            eq.Timeout();
        //            return (default, true);
        //        }
        //        await Task.WhenAny(timer, wait);

        //        if (timer.IsCompleted)
        //        {
        //            eq.Timeout();
        //            return (default, true);
        //        }
        //        return (await wait, false);
        //    } catch (TaskCanceledException)
        //    {
        //        return (default, true);
        //    }
        //}


        //protected async Task<EventResult<TR>> WaitForEventDelayAsync<TR>(string name, TimeSpan delay) {
        //    var (result, timedOut) = await WaitForEventStringAsync(name, delay);
        //    if (timedOut)
        //        return EventResult<TR>.TimedOutValue;
        //    return new EventResult<TR>(JsonConvert.DeserializeObject<TR>(result));
        //}

        //protected Task<EventResult<TR>> WaitForEventAsync<TR>(string name)
        //{
        //    return WaitForEventDelayAsync<TR>(name, TimeSpan.Zero);
        //}

    }
}
