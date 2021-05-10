using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.Workflows.Impl
{
    internal class EventQueueSet
    {
        private Dictionary<string, EventQueue> events = new Dictionary<string, EventQueue>();

        public EventQueue this[string name]
        {
            get
            {
                lock (this)
                {
                    if (events.TryGetValue(name, out var eq))
                        return eq;
                    eq = new EventQueue();
                    events[name] = eq;
                    return eq;
                }
            }
        }
    }

    internal class EventQueue
    {

        private Queue<string> pending = new Queue<string>();
        private TaskCompletionSource<string> current = new TaskCompletionSource<string>();
        private CancellationTokenSource source = new CancellationTokenSource();

        public EventQueue()
        {

        }

        public void Fire(string input)
        {
            lock (this)
            {
                if (current.Task.IsCompleted)
                {
                    pending.Enqueue(input);
                    return;
                }
                source.Cancel();
                current.TrySetResult(input);
            }
        }

        /// <summary>
        /// This must be called if timer was cancelled...
        /// </summary>
        public void Timeout()
        {
            lock (this)
            {
                current = new TaskCompletionSource<string>();
                source = new CancellationTokenSource();
            }
        }

        public (Task<string> task, CancellationToken token) Wait()
        {
            lock (this)
            {
                if (pending.Count > 0)
                {
                    // dequeue one...
                    var first = pending.Dequeue();
                    return (Task.FromResult(first), default);
                }
                return (current.Task, source.Token);
            }
        }
    }
}
