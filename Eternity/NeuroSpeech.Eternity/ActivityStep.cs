using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace NeuroSpeech.Eternity
{
    public class ActivityStep
    {
        public string WorkflowType { get; set; }

        public string Method { get; set; }

        public string InputType { get; set; }

        public string OutputType { get; set; }

        public string ID { get; set; }

        public long SequenceID { get;set; }

        public DateTimeOffset DateCreated { get; set; }

        public DateTimeOffset LastUpdated { get; set; }

        public DateTimeOffset ETA { get; set; }

        public string ParametersHash { get; set; }

        public string Parameters { get; set; }

        public ActivityStatus Status { get; set; }

        public string Error { get; set; }

        public string Result { get; set; }

        public string ExtraData { get; set; }

        public string QueueToken { get; set; }

        private static SHA256 sha = SHA256.Create();

        internal T AsResult<T>(JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<T>(Result, options);
        }

        public static ActivityStep Create(
            string id, 
            Type workflowType, 
            MethodInfo method, 
            object[] parameters, 
            DateTimeOffset eta,
            JsonSerializerOptions options = default)
        {
            var step = new ActivityStep();
            step.WorkflowType = workflowType.AssemblyQualifiedName;
            step.OutputType = method.ReturnType.AssemblyQualifiedName;
            step.ID = id;
            step.Method = method.Name;
            var input = method.ToValueTuple(parameters);
            step.InputType = input.GetType().AssemblyQualifiedName;
            step.Parameters = JsonSerializer.Serialize(input, options);
            step.ParametersHash = Convert.ToBase64String(sha.ComputeHash( System.Text.Encoding.UTF8.GetBytes(step.Parameters)));
            step.ETA = eta;
            step.Status = ActivityStatus.Running;
            return step;
        }
    }
}
