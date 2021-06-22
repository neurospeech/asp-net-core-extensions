using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroSpeech.Eternity.Mocks
{
    public class MockEmailService
    {

        public List<(string emailAddress, string code, DateTimeOffset extra)> Emails 
            = new List<(string emailAddress, string code, DateTimeOffset extra)>();

    }
}
