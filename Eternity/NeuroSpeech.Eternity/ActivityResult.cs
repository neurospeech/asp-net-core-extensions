using System;

namespace NeuroSpeech.Eternity
{
    public class ActivityResult<T>
    {
        public readonly long SequenceID;
        public readonly T Result;
        public readonly ActivityStatus Status;
        public readonly string Error;
        public readonly DateTimeOffset ETA;

        public ActivityResult(
            long executionId,
            T result, 
            ActivityStatus status, 
            string error, 
            DateTimeOffset eta)
        {
            this.SequenceID = executionId;
            this.Result = result;
            this.Status = status;
            this.Error = error;
            this.ETA = eta;
        }
    }
}
