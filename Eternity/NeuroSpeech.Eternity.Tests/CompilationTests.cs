using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity.Tests
{
    [TestClass]
    public class CompilationTests
    {

        [TestMethod]
        public void Test()
        {

        }

    }

    public class TestWorkflow : Workflow<TestWorkflow, string, string>
    {
        public override async Task<string> RunAsync(string input)
        {
            await SendEmailAsync("a", "b");
            return "ok";
        }

        [Activity]
        public async Task<string> SendEmailAsync(
            string emailAddress, 
            string content)
        {
            await Task.Delay(1000);
            return $"{emailAddress}: {content}";
        }
    }
}
