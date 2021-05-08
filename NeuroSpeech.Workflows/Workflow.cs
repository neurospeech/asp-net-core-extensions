#nullable enable
using DurableTask.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.Workflows
{

    public class EventResult<T>
    {
        public readonly T? Result;
        public readonly bool TimedOut;

        public EventResult(T result)
        {
            this.Result = result;
            TimedOut = false;
        }

        private EventResult(T? result, bool timedout)
        {
            this.Result = result;
            this.TimedOut = timedout;
        }


        internal static readonly EventResult<T> TimedOutValue = new EventResult<T>(default, true);

        public static readonly Task<EventResult<T>> Empty = Task.FromResult(new EventResult<T>(default!));
    }

    public abstract class Workflow<TInput, TOutput>
    {

        public static Task<OrchestrationInstance> Queue<T>(TaskHubClient client, TInput input)
        {
            return client.CreateOrchestrationInstanceAsync(typeof(T), input);
        }

        public OrchestrationContext? context;

        private Dictionary<string, TaskCompletionSource<string>> waitTasks 
            = new Dictionary<string, TaskCompletionSource<string>>();

        internal TaskCompletionSource<string> GetTaskCompletionSource(string name)
        {
            if (waitTasks.TryGetValue(name, out var value))
                return value;
            value = new TaskCompletionSource<string>();
            waitTasks[name] = value;
            return value;
        }

        public abstract Task<TOutput> RunTask(TInput input);

        /// <summary>
        /// Creates a waitable timer for given delay
        /// </summary>
        /// <param name="wait"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task Delay(TimeSpan wait, CancellationToken token = default)
        {
            if (wait.TotalMilliseconds <= 0)
                throw new ArgumentOutOfRangeException($"Cannot create timer for time in the past");
            try
            {
                await context!.CreateTimer(context.CurrentUtcDateTime.Add(wait), true, token);
            }catch (TaskCanceledException) {
            }
        }

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
            return context.ScheduleTask<TR>(typeof(TActivity), Tuple.Create(i1, i2, i3, i4, i5, i6, i7, i8));
        }

        protected async Task<(string? result, bool timedOut)> WaitForEventStringAsync(string name, TimeSpan delay)
        {
            if (context == null)
                throw new InvalidOperationException("You can not wait for an event inside an activity");
            TaskCompletionSource<string> wait;
            try
            {
                if (delay.TotalMilliseconds <= 0)
                {
                    return (await GetTaskCompletionSource(name).Task, false);
                }
                wait = GetTaskCompletionSource(name);
                if (wait.Task.IsCompleted)
                    return (await wait.Task, false);

                var ct = new System.Threading.CancellationTokenSource();
                var timer = context.CreateTimer<bool>(context.CurrentUtcDateTime.Add(delay), true, ct.Token);

                await Task.WhenAny(timer, wait.Task);

                if (timer.IsCompleted)
                    return (default, true);
                ct.Cancel();
            } catch (TaskCanceledException)
            {
                return (default, true);
            }
            return (await wait.Task, false);
        }


        protected async Task<EventResult<TR>> WaitForEventDelayAsync<TR>(string name, TimeSpan delay) {
            var (result, timedOut) = await WaitForEventStringAsync(name, delay);
            if (timedOut)
                return EventResult<TR>.TimedOutValue;
            return new EventResult<TR>(JsonConvert.DeserializeObject<TR>(result));
        }

        protected Task<EventResult<TR>> WaitForEventAsync<TR>(string name)
        {
            return WaitForEventDelayAsync<TR>(name, TimeSpan.Zero);
        }

    }
}
