using DurableTask.Core;
using System.Threading.Tasks;

namespace NeuroSpeech.Workflows
{

    public abstract class Workflow<TInput, TOutput>
    {
        public OrchestrationContext context;

        public abstract Task<TOutput> RunTask(TInput input);

        protected Task<TR> CallTaskAsync<TI, TActivity, TR>(TI input)
        {
            return context.ScheduleTask<TR>(typeof(TActivity), input);
        }
    }
}
