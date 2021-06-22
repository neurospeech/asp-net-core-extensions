#nullable enable
using System.Threading.Tasks;

namespace NeuroSpeech.Workflows
{
    public class EventResult
    {
        /// <summary>
        /// Result if event was not timedout
        /// </summary>
        public readonly string? Result;

        /// <summary>
        /// True if event was timed out
        /// </summary>
        public readonly bool TimedOut;

        /// <summary>
        /// True if event was raised
        /// </summary>
        public readonly bool Raised;


        public EventResult(string result)
        {
            this.Result = result;
            TimedOut = false;
            Raised = !TimedOut;
        }

        private EventResult(string? result, bool timedout)
        {
            this.Result = result;
            this.TimedOut = timedout;
        }


        public static readonly EventResult TimedOutValue = new EventResult(default, true);

        public static readonly Task<EventResult> Empty = Task.FromResult(new EventResult(null!));

        public static EventResult From(Task<string> result)
            => result.IsCompleted ? new EventResult(result.Result) : EventResult.TimedOutValue;
    }
}
