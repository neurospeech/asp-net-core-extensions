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

    public class WorkflowStep
    {

        public string? WorkflowType { get; set; }
        public string? ID { get; set; }
        public string? Parameter { get; set; }
        public DateTimeOffset ETA { get; set; }
        public DateTimeOffset DateCreated { get; set; }
        public DateTimeOffset LastUpdated { get; set; }

        public ActivityStatus Status { get; set; }

        public string? Result { get; set; }

        public string? Error { get; set; }

        public static WorkflowStep Workflow(
            string id,
            Type workflowType,
            object input,
            DateTimeOffset eta,
            DateTimeOffset now,
            JsonSerializerOptions? options = default)
        {
            var step = new WorkflowStep();
            step.WorkflowType = workflowType.AssemblyQualifiedName;
            step.ID = id;
            step.Parameter = JsonSerializer.Serialize(input, options);
            // step.ParametersHash = Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(step.Parameters)));
            step.ETA = eta;
            step.DateCreated = now;
            step.LastUpdated = now;
            return step;
        }
    }

    public class ActivityStep
    {
        public string? Method { get; set; }

        public string? ID { get; set; }

        public ActivityType ActivityType { get; set; }

        public long SequenceID { get; set; }

        public DateTimeOffset DateCreated { get; set; }

        public DateTimeOffset LastUpdated { get; set; }

        public DateTimeOffset ETA { get; set; }

        // public string ParametersHash { get; set; }

        public string? Key { get; set; }

        public string KeyHash => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Key));

        public string? Parameters { get; set; }

        public ActivityStatus Status { get; set; }

        public string? Error { get; set; }

        public string? Result { get; set; }

        public string? QueueToken { get; set; }

        private static SHA256 sha = SHA256.Create();

        internal T? AsResult<T>(JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<T>(Result!, options);
        }

        public static ActivityStep Delay(
            string id,
            DateTimeOffset eta,
            DateTimeOffset now)
        {
            var step = new ActivityStep();
            step.ActivityType = ActivityType.Activity;
            step.ActivityType = ActivityType.Delay;
            step.ID = id;
            step.Parameters = JsonSerializer.Serialize(eta.Ticks);
            // step.ParametersHash = Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(step.Parameters)));
            step.ETA = eta;
            step.DateCreated = now;
            step.LastUpdated = now;
            step.Key = $"{step.ID}-{step.ActivityType}-{step.DateCreated.Ticks}-{step.Parameters}";
            return step;
        }

        public static ActivityStep Event(
            string id,
            string[] events,
            DateTimeOffset eta,
            DateTimeOffset now)
        {
            var step = new ActivityStep();
            step.ActivityType = ActivityType.Activity;
            step.ActivityType = ActivityType.Event;
            step.ID = id;
            step.Parameters = JsonSerializer.Serialize(events);
            // step.ParametersHash = Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(step.Parameters)));
            step.ETA = eta;
            step.DateCreated = now;
            step.LastUpdated = now;
            step.Key = $"{step.ID}-{step.ActivityType}-{step.DateCreated.Ticks}-{step.Parameters}";
            return step;
        }


        public static ActivityStep Activity(
            bool uniqueParameters,
            string id, 
            MethodInfo method, 
            object[] parameters, 
            DateTimeOffset eta,
            DateTimeOffset now,
            JsonSerializerOptions? options = default)
        {
            var step = new ActivityStep();
            step.ActivityType = ActivityType.Activity;
            step.ID = id;
            step.Method = method.Name;
            step.Parameters = JsonSerializer.Serialize(parameters.Select(x => JsonSerializer.Serialize(x, options) ), options);
            step.ETA = eta;
            step.DateCreated = now;
            step.LastUpdated = now;
            step.Key = uniqueParameters 
                ? $"{step.ID}-{step.ActivityType}-{step.Parameters}"
                : $"{step.ID}-{step.ActivityType}-{step.DateCreated.Ticks}-{step.Parameters}"; 
            // step.ParametersHash = Convert.ToBase64String(sha.ComputeHash( System.Text.Encoding.UTF8.GetBytes(step.Parameters)));
            return step;
        }
    }
}
