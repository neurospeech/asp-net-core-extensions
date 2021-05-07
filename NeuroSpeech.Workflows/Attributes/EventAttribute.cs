using System;

namespace NeuroSpeech.Workflows
{
    [AttributeUsage(AttributeTargets.Method)]
    public class EventAttribute: Attribute
    {
        public readonly string Name;

        public EventAttribute(string name = null)
        {
            this.Name = name;
        }


    }
}
