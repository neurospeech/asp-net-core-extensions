using DurableTask.Core;
using NeuroSpeech.Workflows;
using NeuroSpeech.Workflows.Impl;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NeuroSpeech.Workflows
{
    public static class TaskHubExtensions
    {

        public static void Register(this TaskHubWorker worker, IServiceProvider sp, Assembly assembly)
        {
            foreach(var t in assembly.GetExportedTypes())
            {
                var w = t.GetCustomAttribute<WorkflowAttribute>();
                if (w == null)
                    continue;

                var (factory, name, activities) = ClrHelper.Instance.Factory(t);
                worker.AddTaskOrchestrations(new WFactory<TaskOrchestration>(name, sp, factory));

                foreach(var a in activities)
                {
                    worker.AddTaskActivities(new WFactory<TaskActivity>(a.Name,sp, factory));
                }
            }
        }

        public class WFactory<T> : ObjectCreator<T>
        {
            private readonly IServiceProvider sp;
            private readonly Func<string, IServiceProvider, object> factory;

            public WFactory(string name, IServiceProvider sp, Func<string,IServiceProvider,object> factory)
            {
                this.Name = name;
                this.sp = sp;
                this.factory = factory;
            }
            public override T Create()
            {
                return (T)factory(Name, sp);
            }
        }

    }
}
