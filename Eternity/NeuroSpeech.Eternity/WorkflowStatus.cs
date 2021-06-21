using System;

namespace NeuroSpeech.Eternity
{
    public class WorkflowStatus<T>
    {
        public ActivityStatus Status { get; set; }

        public T? Result { get; set; }

        public string? Error { get; set; }

        public DateTimeOffset DateCreated { get; set; }

        public DateTimeOffset LastUpdate { get; set; }
    }
}
