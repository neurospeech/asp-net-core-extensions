using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroSpeech.Eternity.Tests.Mocks
{
    public class MockClock : IEternityClock
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
    }
}
