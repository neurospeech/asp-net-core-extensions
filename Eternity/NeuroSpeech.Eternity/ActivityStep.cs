using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace NeuroSpeech.Eternity
{
    public enum ActivityType
    {
        Workflow,
        Activity,
        Delay,
        Event
    }

    public class ActivityStep
    {
        public string WorkflowType { get; set; }

        public string Method { get; set; }

        public string OutputType { get; set; }

        public string ID { get; set; }

        public ActivityType ActivityType { get; set; }

        public long SequenceID { get; set; }

        public DateTimeOffset DateCreated { get; set; }

        public DateTimeOffset LastUpdated { get; set; }

        public DateTimeOffset ETA { get; set; }

        // public string ParametersHash { get; set; }

        public string Key => $"{ID}-{ActivityType}-{DateCreated.Ticks}-{Parameters}";

        public string KeyHash => Convert.ToBase64String( sha.ComputeHash( System.Text.Encoding.UTF8.GetBytes(Key) ) );

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

        public static ActivityStep Delay(
            string id,
            Type workflowType,
            DateTimeOffset eta,
            DateTimeOffset now)
        {
            var step = new ActivityStep();
            step.ActivityType = ActivityType.Activity;
            step.WorkflowType = workflowType.AssemblyQualifiedName;
            step.ActivityType = ActivityType.Delay;
            step.ID = id;
            step.Parameters = JsonSerializer.Serialize(eta.Ticks);
            // step.ParametersHash = Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(step.Parameters)));
            step.ETA = eta;
            step.DateCreated = now;
            step.LastUpdated = now;
            step.Status = ActivityStatus.Running;
            return step;
        }

        public static ActivityStep Event(
            string id,
            Type workflowType,
            string[] events,
            DateTimeOffset eta,
            DateTimeOffset now)
        {
            var step = new ActivityStep();
            step.ActivityType = ActivityType.Activity;
            step.WorkflowType = workflowType.AssemblyQualifiedName;
            step.ActivityType = ActivityType.Event;
            step.ID = id;
            step.Parameters = JsonSerializer.Serialize(events);
            // step.ParametersHash = Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(step.Parameters)));
            step.ETA = eta;
            step.DateCreated = now;
            step.LastUpdated = now;
            step.Status = ActivityStatus.Running;
            return step;
        }


        public static ActivityStep Activity(
            string id, 
            Type workflowType, 
            MethodInfo method, 
            object[] parameters, 
            DateTimeOffset eta,
            DateTimeOffset now,
            JsonSerializerOptions options = default)
        {
            var step = new ActivityStep();
            step.ActivityType = ActivityType.Activity;
            step.WorkflowType = workflowType.AssemblyQualifiedName;
            step.OutputType = method.ReturnType.AssemblyQualifiedName;
            step.ID = id;
            step.Method = method.Name;
            step.Parameters = JsonSerializer.Serialize(parameters.Select(x => JsonSerializer.Serialize(x, options) ), options);
            // step.ParametersHash = Convert.ToBase64String(sha.ComputeHash( System.Text.Encoding.UTF8.GetBytes(step.Parameters)));
            step.ETA = eta;
            step.DateCreated = now;
            step.LastUpdated = now;
            step.Status = ActivityStatus.Running;
            return step;
        }


        public static ActivityStep Workflow(
            string id,
            Type workflowType,
            Type outputType,
            object input,
            DateTimeOffset eta,
            DateTimeOffset now,
            JsonSerializerOptions options = default)
        {
            var step = new ActivityStep();
            step.ActivityType = ActivityType.Workflow;
            step.WorkflowType = workflowType.AssemblyQualifiedName;
            step.OutputType = outputType.AssemblyQualifiedName;
            step.ID = id;
            step.Parameters = JsonSerializer.Serialize(input, options);
            // step.ParametersHash = Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(step.Parameters)));
            step.ETA = eta;
            step.DateCreated = now;
            step.LastUpdated = now;
            step.Status = ActivityStatus.Running;
            return step;
        }
    }
}
