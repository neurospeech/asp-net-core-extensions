# Eternity Framework

Long running workflows with ability to suspend and replay the workflow in future.

## Features

### Strongly Typed API

Lets assume following businesss logic

1. User enters email address to signup
2. A unique verification code is generated and it is sent in an email
3. We will wait for 15 minutes, if we receive response to resend email, we will resend the verification email again and wait again for 15 minutes
4. If we get verification code within total of 45 minutes, we will break the loop and end the workflow by returning `Verified`
5. Or else, we will end the workflow by returning `NotVerified`, here we can delete the user from database
6. When user enters the code, we will call `RaiseEventAsync`

```c#

// create new workflow and execute now
var id = await SignupWorkflow.CreateAsync(context, "sample@gmail.com");

// raise an event...
await context.RaiseEventAsync(id, SignupWorkflow.Verify, verificationCode);

public class SignupWorkflow : Workflow<SignupWorkflow, string, string>
{
    public const string Resend = nameof(Resend);

    public const string Verify = nameof(Verify);

    public override async Task<string> RunAsync(string input)
    {
        var maxWait = TimeSpan.FromMinutes(15);
        var code = (this.CurrentUtc.Ticks & 0xF).ToString();
        await SendEmailAsync(input, code);
        for (int i = 0; i < 3; i++)
        {
            var (name, result) = await WaitForExternalEventsAsync(maxWait, Resend, Verify);
            switch(name)
            {
                case Verify:
                    if(result == code)
                    {
                        return "Verified";
                    }
                    break;
                case Resend:
                    await SendEmailAsync(input, code, i);
                    break;
            }
        }
        return "NotVerified";
    }

    [Activity]
    public virtual async Task<string> SendEmailAsync(
        string emailAddress, 
        string code, 
        int attempt = -1,
        [Inject] MockEmailService emailService = null) {
        await Task.Delay(100);
        emailService.Emails.Add((emailAddress, code, CurrentUtc));
        return $"{emailService.Emails.Count-1}";
    }
}
```

### Dependency Injection

Inbuilt dependency injection, you can inject services into activity methods. Each activity is executed in separate scope.
While calling the method you can pass null, it will be replaced with actual dependency while actually running it.

### Activities are C# Methods

Activities are methods of the same class marked with `[Activity]` attribute and methods must be public and virtual.

Activities can also be scheduled in future by passing a parameter marked with `[Schedule]` attribute as shown below.

```c#

public class RenewMembershipWorkflow: Workflow<RenewMembershipWorkflow,long,string> {
    
    public async Task<string> RunAsync(long id) {

        var at = TimeSpan.FromDays(364);
        
        // at this time, this workflow will be suspended and removed from the execution
        // internally it will throw `ActivitySuspendedException` and it will start
        // just before the given timespan

        for(int i = 0; i<3; i++) {
            var success = await RewewAsync(id, at);
            if(success) {

                // restart the same workflow
                await RenewMembershipWorkflow.CreateAsync(this.Context, id);

                return "Done";
            }

            // try after 3 days again...
            at = TimeSpan.FromDays(3);
        }

        // renewal failed...
        return "Failed";

    }

    [Activity]
    public virtual async Task<bool> RenewAsync(
        long id, 
        [Schedule] TimeSpan at, 
        [Inject] IPaymentService paymentService = null,
        [Inject] IEmailService emailService = null
        ) {

        var result = await paymentService.ChargeAsync(id);
        if(result.Success) {
            return true;
        }
        await emailService.SendFailedRenewalAsync(id);
        return false;
    }
    
}

```

### Wait for External Event

You can wait for an external event by specifying names of the event expected along with the maximum wait time. The method will return an `EventResult` which will contain the name of event fired and the paramter that came along.

### Really very very large workflows

You can create workflows that span from days to months, as workflows are suspended if it needs to wait more than 15 seconds. Storage is independent of the execution engine.

### Exception Handling

> You should never catch `ActivitySuspendedException`, it will create deadlocks and it will eat up all resources.

#### Exception with Filter

Always use an exception filter as specified below

```c#
    try {
       ... your code
    } catch (Exception ex) when (!(ex is ActivitySuspendedException)) {
       ... log or do something with ex
    }
```

### Recursive Activities or Nested Activities Not Supported

You cannot call an activity within an activity, as current executing activity is locked for single execution, it may lead to deadlock. However you can use normal c# recursive methods inside `RunAsync` of `Workflow`.

```c#

    public class RecursiveWorkflow: Workflow<RecursiveWorkflow,long,long> {
   
        public Task<string> RunAsync(string folder) {
            // You can call any recursive method here...
            return RecursiveMethod(folder);
        }

        // You can call recursive methods inside `RunAsync`
        public async Task<string> RecursiveMethod(string folder) {
            foreach(var dir in Directory.EnumerateDirectories(folder)) {
                await RecursiveMethod(dir);
            }
            foreach(Var file in Directory.EnumerateFiles(folder)) {
                await NonRecursiveActivity(file);
            }
        }

        [Activity]
        public virtual async Task NonRecursiveActivity(string file) {
            // do something...

            // no recursive or no activity can be called here..
        }
    }

```

### Storage

Currently we are supporting Azure Table storage and Blob storage. You can implement your own storage easily. Azure Queue has limitation of maximum of 7 days visibility time, it is not suitable for eternity framework. Instead we have created queue using Table storage which offers same functionality.

### Unit Testing

We have created `NeuroSpeech.Eternity.Mocks` package which provides Mock engine and storage. You can create unit tests easily as shown below.

```c#
    public class SignupWorkflow : Workflow<SignupWorkflow, string, string>
    {
        public const string Resend = nameof(Resend);

        public const string Verify = nameof(Verify);

        public override async Task<string> RunAsync(string input)
        {
            var maxWait = TimeSpan.FromMinutes(15);
            var code = (this.CurrentUtc.Ticks & 0xF).ToString();
            await SendEmailAsync(input, code);
            for (int i = 0; i < 3; i++)
            {
                var (name, value) = await WaitForExternalEventsAsync(maxWait, Resend, Verify);
                switch(name)
                {
                    case Verify:
                        if(value == code)
                        {
                            return "Verified";
                        }
                        break;
                    case Resend:
                        await SendEmailAsync(input, code, i);
                        break;
                }
            }
            return "NotVerified";
        }

        [Activity]
        public virtual async Task<string> SendEmailAsync(
            string emailAddress, 
            string code, 
            int attempt = -1,
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
            var id = await SignupWorkflow.CreateAsync(context, "sample@gmail.com");

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

            Assert.AreEqual(0, engine.Storage.QueueSize);
        }

        [TestMethod]
        public async Task ResendAsync()
        {
            var engine = new MockEngine();
            var emailService = engine.EmailService;
            var context = engine.Resolve<EternityContext>();

            // send email..
            var id = await SignupWorkflow.CreateAsync(context, "sample@gmail.com");

            engine.Clock.UtcNow += TimeSpan.FromMinutes(5);

            await context.ProcessMessagesOnceAsync();

            // check if we received the email..
            Assert.IsTrue(emailService.Emails.Any());

            var code = emailService.Emails[0].code;

            // fire event..
            await context.RaiseEventAsync(id, SignupWorkflow.Resend, null);

            engine.Clock.UtcNow += TimeSpan.FromMinutes(1);

            await context.ProcessMessagesOnceAsync();

            Assert.AreEqual(2, emailService.Emails.Count);

            await context.RaiseEventAsync(id, SignupWorkflow.Verify, code);

            engine.Clock.UtcNow += TimeSpan.FromMinutes(1);

            await context.ProcessMessagesOnceAsync();

            var status = await engine.Storage.GetWorkflowAsync(id);

            Assert.AreEqual(status.Status, ActivityStatus.Completed);

            Assert.AreEqual(status.Result, "\"Verified\"");

            Assert.AreEqual(0, engine.Storage.QueueSize);

        }

        [TestMethod]
        public async Task TimedOut()
        {
            var engine = new MockEngine();
            var emailService = engine.EmailService;
            var context = engine.Resolve<EternityContext>();

            // send email..
            var id = await SignupWorkflow.CreateAsync(context, "sample@gmail.com");

            engine.Clock.UtcNow += TimeSpan.FromMinutes(5);

            await context.ProcessMessagesOnceAsync();

            // check if we received the email..
            Assert.IsTrue(emailService.Emails.Any());

            engine.Clock.UtcNow += TimeSpan.FromMinutes(20);

            await context.ProcessMessagesOnceAsync();


            engine.Clock.UtcNow += TimeSpan.FromMinutes(20);

            await context.ProcessMessagesOnceAsync();

            engine.Clock.UtcNow += TimeSpan.FromMinutes(20);

            await context.ProcessMessagesOnceAsync();

            var status = await engine.Storage.GetWorkflowAsync(id);

            Assert.AreEqual(status.Status, ActivityStatus.Completed);

            Assert.AreEqual(status.Result, "\"NotVerified\"");

            Assert.AreEqual(0, engine.Storage.QueueSize);

        }

    }
 ```

# Internals

1. When workflow execution begins, it will create a proxy, in which all virtual activity methods will be overriden and it will be replaced with schedule activity logic.
2. In schedule activity, it will create a new step in a history with `ETA` when it is supposed to execute, along with it, it will put a message in queue.
3. If activity needs to be executed after 15 seconds, execution will be aborted and workflow will stay in suspended mode.
4. When time has arrived to execute the activity, it will actually proceed with execution and will call the original c# code of the workflow.
5. The engine will ensure that only one and only one computer will execute the activity, even if multiple computers are trying to execute many workflows, queue will ensure concurency, however execution engine will still try to acquire lock to execute. If while it was waiting, if another machine has finished the activity, it will proceed to next execution.

# Comparison with Durable Tasks
1. Eternity framework allows non deterministric workflows. Activity replay is only dependent on parameters passed to the activity. 
2. Inside a workflow, a method name with same set of parameters at the same timeline will never be executed again. 
3. You can also mark activity as `unique parameters` to make activity unique throughtout the execution.
4. An event can only be raised if workflow is waiting for an event, unlike Durable Tasks. You can choose to fire an exception if workflow is not waiting for an event.
5. You can wait for an event from days to months. You can easily create workflows for membership renewals etc.
6. We have created Mock Storage, you can unit test your workflows spanning days/months easily against in memory storage.
7. Storage engine interface is small, you can easily use it to create storage even on mobile devices. We will soon offer Sqlite storage.
