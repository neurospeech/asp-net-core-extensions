using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeuroSpeech.Eternity.Tests.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity.Tests
{
    public class SignupWorkflow : Workflow<SignupWorkflow, string, string>
    {
        public const string Resend = nameof(Resend);

        public const string Verify = nameof(Verify);

        public override async Task<string> RunAsync(string input)
        {
            var maxWait = TimeSpan.FromMinutes(15);
            for (int i = 0; i < 3; i++)
            {
                var code = (this.CurrentUtc.Ticks & 0xF).ToString();
                await SendEmailAsync(input, code, i);
                var result = await WaitForExternalEventsAsync(maxWait, Resend, Verify);
                switch(result.EventName)
                {
                    case Verify:
                        if(result.Value == code)
                        {
                            return "Verified";
                        }
                        break;
                            
                }
            }
            return "NotVerified";
        }

        [Activity]
        public virtual async Task<string> SendEmailAsync(
            string emailAddress, 
            string code, 
            int attempt,
            [Inject] MockEmailService emailService = null) {
            await Task.Delay(100);
            emailService.Emails.Add((emailAddress, code, CurrentUtc));
            return $"{emailService.Emails.Count-1}";
        }
    }

    [TestClass]
    public class EmailValidationTest
    {

        [TestMethod]
        public async Task VerifyAsync()
        {
            var engine = new MockEngine();
            var emailService = engine.EmailService;
            var context = engine.Resolve<EternityContext>();

            // send email..
            var id = await SignupWorkflow.CreateAsync(context, "ackava@gmail.com");

            engine.Clock.UtcNow += TimeSpan.FromMinutes(5);

            await context.ProcessMessagesOnceAsync();

            // check if we received the email..
            Assert.IsTrue(emailService.Emails.Any());

            var code = emailService.Emails[0].code;

            // fire event..
            await context.RaiseEventAsync(id, SignupWorkflow.Verify, code);

            engine.Clock.UtcNow += TimeSpan.FromMinutes(1);

            await context.ProcessMessagesOnceAsync();

            var status = await engine.Storage.GetWorkflowAsync(id);

            Assert.AreEqual(status.Status, ActivityStatus.Completed);

            Assert.AreEqual(status.Result, "\"Verified\"");

        }

        [TestMethod]
        public async Task ResendAsync()
        {
            var engine = new MockEngine();
            var emailService = engine.EmailService;
            var context = engine.Resolve<EternityContext>();

            // send email..
            var id = await SignupWorkflow.CreateAsync(context, "ackava@gmail.com");

            engine.Clock.UtcNow += TimeSpan.FromMinutes(5);

            await context.ProcessMessagesOnceAsync();

            // check if we received the email..
            Assert.IsTrue(emailService.Emails.Any());

            var code = emailService.Emails[0].code;

            // fire event..
            await context.RaiseEventAsync(id, SignupWorkflow.Resend, null);

            engine.Clock.UtcNow += TimeSpan.FromMinutes(1);

            await context.ProcessMessagesOnceAsync();

            Assert.IsTrue(emailService.Emails.Count == 2);

            await context.RaiseEventAsync(id, SignupWorkflow.Verify, code);

            engine.Clock.UtcNow += TimeSpan.FromMinutes(1);

            await context.ProcessMessagesOnceAsync();

            var status = await engine.Storage.GetWorkflowAsync(id);

            Assert.AreEqual(status.Status, ActivityStatus.Completed);

            Assert.AreEqual(status.Result, "\"Verified\"");

        }

    }
}
