using System;

namespace NeuroSpeech.Eternity
{
    public interface IEternityClock
    {
        DateTimeOffset UtcNow { get; }
    }

}
