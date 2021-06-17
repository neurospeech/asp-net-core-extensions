# Eternity Framework

Long running workflows with ability to suspend and replay the workflow in future.

## Features

### Strongly Typed API

```c#

// create new workflow and execute now
var id = await SignupWorkflow.CreateAsync(context, "sample@gmail.com");

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
            var result = await WaitForExternalEventsAsync(maxWait, Resend, Verify);
            switch(result.EventName)
            {
                case Verify:
                    if(result.Value == code)
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

You can create workflows that span from days to months, as workflows are suspended if it needs to wait more than 15 seconds.

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

### Recursive Activities

We have no idea how and what will happen if you call activities recursively, we don't know whether we should support it or not.

### Nested Activities Not Supported

You cannot call an activity within an activity, by doing so, you will break the execution cycle. An Activity must return control back to the `RunAsync` method and only `RunAsync` method can schedule/call any other activity.

### Storage

Currently we are supporting Azure Queue/Table storage. You can implement your own storage easily.