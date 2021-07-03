using System;

namespace NeuroSpeech
{
    /// <summary>
    /// Registers current assembly with Assembly parts
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public class RegisterAssemblyAttribute : Attribute
    {
        public readonly bool RegisterParts;
        public readonly Type StartupType;

        public RegisterAssemblyAttribute(bool registerParts = true, Type startupType = null)
        {
            this.StartupType = startupType;
            this.RegisterParts = registerParts;
        }
    }
}
