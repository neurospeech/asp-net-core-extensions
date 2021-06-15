using System;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity
{
    internal interface IWorkflow
    {
        void Init(string id, EternityContext context, DateTimeOffset start);
        void SetCurrentTime(DateTimeOffset time);

        Type InputType { get; }

        Task<object> RunAsync(object input);
    }
}
