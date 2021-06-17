using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeuroSpeech.Eternity.Tests.Mocks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity.Tests
{

    public class ScheduleAfter : Workflow<ScheduleAfter, string, string>
    {
        public override async Task<string> RunAsync(string input)
        {
            await RunAfter(TimeSpan.FromDays(1), "1", "a");
            await RunAfter(TimeSpan.FromDays(1), "2", "b");
            return "ok";
        }

        [Activity]
        public virtual Task RunAfter(
            [Schedule] TimeSpan next, 
            string p,
            string r,
            [Inject] MockBag bag = null)
        {
            bag[p] = r;
            return Task.CompletedTask;
        }
    }

    public class ScheduleAt : Workflow<ScheduleAt, string, string>
    {
        public override async Task<string> RunAsync(string input)
        {
            await RunAfter(CurrentUtc.AddDays(1), "1", "a");
            await RunAfter(CurrentUtc.AddDays(1), "2", "b");
            return "ok";
        }

        [Activity]
        public virtual Task RunAfter(
            [Schedule] DateTimeOffset next,
            string p,
            string r,
            [Inject] MockBag bag = null)
        {
            bag[p] = r;
            return Task.CompletedTask;
        }
    }


    [TestClass]
    public class ScheduleTest
    {

        [TestMethod]
        public async Task After()
        {
            var engine = new MockEngine();
            var context = engine.Resolve<EternityContext>();

            // send email..
            var id = await ScheduleAfter.CreateAsync(context, "sample@gmail.com");

            engine.Clock.UtcNow += TimeSpan.FromMinutes(5);

            await context.ProcessMessagesOnceAsync();

            // must be suspended...
            var status = await engine.Storage.GetWorkflowAsync(id);
            Assert.AreEqual(ActivityStatus.Suspended, status.Status);
            Assert.IsNull(engine.Bag["1"]);
            Assert.IsNull(engine.Bag["2"]);

            // next day...
            engine.Clock.UtcNow += TimeSpan.FromDays(1);

            await context.ProcessMessagesOnceAsync();

            status = await engine.Storage.GetWorkflowAsync(id);
            Assert.AreEqual(ActivityStatus.Suspended, status.Status);
            Assert.AreEqual("a", engine.Bag["1"]);
            Assert.IsNull(engine.Bag["2"]);

            engine.Clock.UtcNow += TimeSpan.FromDays(1);

            await context.ProcessMessagesOnceAsync();

            status = await engine.Storage.GetWorkflowAsync(id);
            Assert.AreEqual("a", engine.Bag["1"]);
            Assert.AreEqual("b", engine.Bag["2"]);
            Assert.AreEqual(ActivityStatus.Completed, status.Status);
            Assert.AreEqual("\"ok\"", status.Result);
            Assert.AreEqual(0, engine.Storage.QueueSize);
        }

        [TestMethod]
        public async Task At()
        {
            var engine = new MockEngine();
            var context = engine.Resolve<EternityContext>();

            // send email..
            var id = await ScheduleAt.CreateAsync(context, "sample@gmail.com");

            engine.Clock.UtcNow += TimeSpan.FromMinutes(5);

            await context.ProcessMessagesOnceAsync();

            // must be suspended...
            var status = await engine.Storage.GetWorkflowAsync(id);
            Assert.AreEqual(ActivityStatus.Suspended, status.Status);
            Assert.IsNull(engine.Bag["1"]);
            Assert.IsNull(engine.Bag["2"]);

            // next day...
            engine.Clock.UtcNow += TimeSpan.FromDays(1);

            await context.ProcessMessagesOnceAsync();

            status = await engine.Storage.GetWorkflowAsync(id);
            Assert.AreEqual(ActivityStatus.Suspended, status.Status);
            Assert.AreEqual("a", engine.Bag["1"]);
            Assert.IsNull(engine.Bag["2"]);

            engine.Clock.UtcNow += TimeSpan.FromDays(1);

            await context.ProcessMessagesOnceAsync();

            status = await engine.Storage.GetWorkflowAsync(id);
            Assert.AreEqual("a", engine.Bag["1"]);
            Assert.AreEqual("b", engine.Bag["2"]);
            Assert.AreEqual(ActivityStatus.Completed, status.Status);
            Assert.AreEqual("\"ok\"", status.Result);
            Assert.AreEqual(0, engine.Storage.QueueSize);
        }

    }
}
