using System;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity
{
    internal interface IWorkflow
    {
        void Init(string id, EternityContext context, DateTimeOffset start);
        void SetCurrentTime(DateTimeOffset time);

        Type InputType { get; }

        DateTimeOffset CurrentUtc { get; }

        Task<object> RunAsync(object input);
    }
}
