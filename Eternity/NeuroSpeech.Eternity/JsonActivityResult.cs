using System;

namespace NeuroSpeech.Eternity
{
    public class JsonActivityResult
    {
        public long ID { get; set; }

        public ActivityStatus Status { get; set; }

        public string Result { get; set; }

        public string Error { get; set; }

        public DateTimeOffset ETA { get; set; }
    }

}
