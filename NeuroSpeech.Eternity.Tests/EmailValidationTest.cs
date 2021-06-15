using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeuroSpeech.Eternity.Tests.Mocks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity.Tests
{
    public class SignupWorkflow : Workflow<SignupWorkflow, string, string>
    {
        public override Task<string> RunAsync(string input)
        {
            throw new NotImplementedException();
        }

        [Activity]
        public async Task<string> SendEmailAsync(
            string emailAddress, 
            string code, 
            int attempt,
            [Inject] MockEmailService emailService) {
            await Task.Delay(100);
            emailService.Emails.Add((emailAddress, code));
            return $"{emailService.Emails.Count-1}";
        }
    }

    [TestClass]
    public class EmailValidationTest
    {
    }
}
