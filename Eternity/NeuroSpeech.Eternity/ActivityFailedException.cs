using System;

namespace NeuroSpeech.Eternity
{
    public class ActivityFailedException: Exception
    {
        public ActivityFailedException(string message): base(message)
        {

        }
    }
}
