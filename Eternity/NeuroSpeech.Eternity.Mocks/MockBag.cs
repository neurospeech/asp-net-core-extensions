using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroSpeech.Eternity.Mocks
{
    public class MockBag
    {

        private Dictionary<string, string> bag = new Dictionary<string, string>();

        public string this[string key]
        {
            get => bag.TryGetValue(key, out var value) ? value : null;
            set => bag[key] = value;
        }

    }
}
