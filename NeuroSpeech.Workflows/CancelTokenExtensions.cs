using DurableTask.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NeuroSpeech.Workflows
{
    internal class CancelTokenRegistry
    {

        public static CancelTokenRegistry Instance = new CancelTokenRegistry();

        private ConcurrentDictionary<string, CancellationTokenSource> sources = new ConcurrentDictionary<string, CancellationTokenSource>();

        internal CancellationTokenSource Get(string id)
        {
            return sources.GetOrAdd(id, (k) => new CancellationTokenSource());
        }

        internal void Remove(string id)
        {
            sources.TryRemove(id, out var _);
        }

    }

    public static class CancelTokenExtensions
    {
        internal static CancellationTokenSource GetCancellationTokenSource(this OrchestrationContext context)
        {
            return CancelTokenRegistry.Instance.Get(context.OrchestrationInstance.InstanceId);
        }

        internal static void RemoveCancellationTokenSource(this OrchestrationContext context)
        {
            CancelTokenRegistry.Instance.Remove(context.OrchestrationInstance.InstanceId);
        }

        public static CancellationToken GetCancellationToken(this TaskContext context)
        {
            return CancelTokenRegistry.Instance.Get(context.OrchestrationInstance.InstanceId).Token;
        }

    }
}
