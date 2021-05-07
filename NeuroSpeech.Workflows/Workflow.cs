#nullable enable
using DurableTask.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        protected Task<TR> CallTaskAsync<TI, TActivity, TR>(TI input)
        {
            return context!.ScheduleTask<TR>(typeof(TActivity), input);
        }

        protected async Task<(string? result, bool timedOut)> WaitForEventStringAsync(string name, TimeSpan delay)
        {
            TaskCompletionSource<string>? wait = null;
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
                var timer = context!.CreateTimer<bool>(context.CurrentUtcDateTime.Add(delay), true, ct.Token);

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
