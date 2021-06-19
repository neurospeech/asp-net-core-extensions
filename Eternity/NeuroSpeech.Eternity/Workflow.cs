using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity
{

    /// <summary>
    /// Base class for Eternity Workflow
    /// </summary>
    /// <typeparam name="TWorkflow">Workflow itself</typeparam>
    /// <typeparam name="TInput">Type of input</typeparam>
    /// <typeparam name="TOutput">Type of output</typeparam>
    public abstract class Workflow<TWorkflow,TInput,TOutput>: IWorkflow
        where TWorkflow: Workflow<TWorkflow,TInput,TOutput>
    {

        /// <summary>
        /// Creates a new workflow, which will be executed immediately
        /// </summary>
        /// <param name="context">Eternity Context</param>
        /// <param name="input">Input</param>
        /// <returns></returns>
        public static Task<string> CreateAsync(EternityContext context, TInput input)
        {
            // this will force verification..
            ClrHelper.Instance.GetDerived(typeof(TWorkflow));
            return context.CreateAsync<TInput, TOutput>(typeof(TWorkflow), input);
        }

        /// <summary>
        /// Creates a new workflow, which will be executed immediately with given ID, 
        /// ID must be unique, if workflow with same ID exists, it will throw an error
        /// </summary>
        /// <param name="context">Eternity Context</param>
        /// <param name="id">Workflow ID</param>
        /// <param name="input">Input</param>
        /// <returns></returns>
        public static Task<string> CreateAsync(EternityContext context, string id, TInput input)
        {
            // this will force verification..
            ClrHelper.Instance.GetDerived(typeof(TWorkflow));
            return context.CreateAsync<TInput, TOutput>(typeof(TWorkflow), input, id);
        }


        /// <summary>
        /// Creates a new workflow, which will be at specified time
        /// </summary>
        /// <param name="context">Eternity Context</param>
        /// <param name="input">Input</param>
        /// <param name="at">Start on this time</param>
        /// <returns></returns>
        public static Task<string> CreateAtAsync(EternityContext context, TInput input, DateTimeOffset at)
        {
            // this will force verification..
            ClrHelper.Instance.GetDerived(typeof(TWorkflow));
            return context.CreateAtAsync<TInput, TOutput>(typeof(TWorkflow), input, at);
        }

        /// <summary>
        /// Creates a new workflow, which will be at specified time with given ID, 
        /// ID must be unique, if workflow with same ID exists, it will throw an error
        /// </summary>
        /// <param name="context">Eternity Context</param>
        /// <param name="id">Workflow ID</param>
        /// <param name="input">Input</param>
        /// <param name="at">Start on this time</param>
        /// <returns></returns>
        public static Task<string> CreateAtAsync(EternityContext context, string id, TInput input, DateTimeOffset at)
        {
            // this will force verification..
            ClrHelper.Instance.GetDerived(typeof(TWorkflow));
            return context.CreateAtAsync<TInput, TOutput>(typeof(TWorkflow), input, at, id);
        }

        /// <summary>
        /// Retrieve status of the workflow
        /// </summary>
        /// <param name="context"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Task<WorkflowStatus<TOutput>> GetStatusAsync(EternityContext context, string id)
        {
            return context.GetStatusAsync<TOutput>(id);
        }

        /// <summary>
        /// Returns if the control is inside an activity
        /// </summary>
        public bool IsActivityRunning { get; internal set; }

        /// <summary>
        /// Workflow ID associated with current execution
        /// </summary>
        public string ID { get; private set; }

        /// <summary>
        /// Set this to true to delete entire history of replay after successful or failed execution
        /// </summary>
        public bool DeleteHistory { get; set;  }


        public EternityContext Context { get; private set; }

        /// <summary>
        /// Time associated with current execution, it will not be same as current date time when it is replayed
        /// </summary>
        public DateTimeOffset CurrentUtc { get; private set; }

        Type IWorkflow.InputType => typeof(TInput);

        bool IWorkflow.IsActivityRunning { get => IsActivityRunning; set => IsActivityRunning = value; }

        IList<string> IWorkflow.QueueItemList { get; } = new List<string>();

        public abstract Task<TOutput> RunAsync(TInput input);

        void IWorkflow.Init(string id, EternityContext context, DateTimeOffset start)
        {
            this.ID = id;
            this.Context = context;
            this.CurrentUtc = start;
        }

        /// <summary>
        /// Wait for an external event upto given timespan, timespan cannot be infinite, and cannot be zero or negative
        /// </summary>
        /// <param name="maxWait"></param>
        /// <param name="names">Names of expected events</param>
        /// <returns></returns>
        public Task<(string name, string value)> WaitForExternalEventsAsync(TimeSpan maxWait,params string[] names)
        {
            if(maxWait.TotalMilliseconds <= 0)
            {
                throw new ArgumentException($"{nameof(maxWait)} cannot be in the past");
            }
            if(names.Length == 0)
            {
                throw new ArgumentException($"{nameof(names)} cannot be empty");
            }
            return Context.WaitForExternalEventsAsync(this, ID, names, CurrentUtc.Add(maxWait));
        }

        /// <summary>
        /// Pause the execution for given time span, it cannot be zero or negative and it cannot be infinite
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public Task Delay(TimeSpan timeout)
        {
            if (timeout.TotalMilliseconds <= 0)
            {
                throw new ArgumentException($"{nameof(timeout)} cannot be in the past");
            }
            return Context.Delay(this, ID, CurrentUtc.Add(timeout));
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<T> ScheduleResultAsync<T>(string method, params object[] items)
        {
            var fx = typeof(TWorkflow).GetMethod(method);
            var unique = fx.GetCustomAttribute<ActivityAttribute>();
            return Context.ScheduleAsync<T>(this, unique.UniqueParameters, ID, CurrentUtc, fx, items);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public async Task ScheduleAsync(string method, params object[] items)
        {
            var fx = typeof(TWorkflow).GetMethod(method);
            var unique = fx.GetCustomAttribute<ActivityAttribute>();
            await Context.ScheduleAsync<object>(this, unique.UniqueParameters, ID, CurrentUtc, fx, items);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<T> ScheduleAtResultAsync<T>(DateTimeOffset at, string method, params object[] items)
        {
            if (at <= CurrentUtc)
            {
                throw new ArgumentException($"{nameof(at)} cannot be in the past");
            }
            var fx = typeof(TWorkflow).GetMethod(method);
            var unique = fx.GetCustomAttribute<ActivityAttribute>();
            return Context.ScheduleAsync<T>(this, unique.UniqueParameters, ID, at, fx, items);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public async Task ScheduleAtAsync(DateTimeOffset at, string method, params object[] items)
        {
            if (at <= CurrentUtc)
            {
                throw new ArgumentException($"{nameof(at)} cannot be in the past");
            }
            var fx = typeof(TWorkflow).GetMethod(method);
            var unique = fx.GetCustomAttribute<ActivityAttribute>();
            await Context.ScheduleAsync<object>(this, unique.UniqueParameters, ID, at, fx, items);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<T> ScheduleAfterResultAsync<T>(TimeSpan at, string method, params object[] items)
        {
            if (at.TotalMilliseconds <= 0)
            {
                throw new ArgumentException($"{nameof(at)} cannot be in the past");
            }
            var fx = typeof(TWorkflow).GetMethod(method);
            var unique = fx.GetCustomAttribute<ActivityAttribute>();
            return Context.ScheduleAsync<T>(this, unique.UniqueParameters, ID, CurrentUtc.Add(at), fx, items);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public async Task ScheduleAfterAsync(TimeSpan at, string method, params object[] items)
        {
            if (at.TotalMilliseconds <= 0)
            {
                throw new ArgumentException($"{nameof(at)} cannot be in the past");
            }
            var fx = typeof(TWorkflow).GetMethod(method);
            var unique = fx.GetCustomAttribute<ActivityAttribute>();
            await Context.ScheduleAsync<object>(this, unique.UniqueParameters, ID, CurrentUtc.Add(at), fx, items);
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
