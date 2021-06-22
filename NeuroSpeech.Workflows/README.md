# Workflow?

Durable Tasks created by Azure team is great, but it requires too many classes. And for very small logic, the amount of code to be written is huge. We have created this package to reduce amount of code required to write Durable Functions easily in your own premises.

# NuGet

`NeuroSpeech.Workflows`

# Example

Lets assume, we want to write following logic.

1. After user sign ups we want to verify the email address.
2. User can enter the code or request for resending the email.
3. If verification fails for 3 times or times out within 45 minutes, we delete the user.

# Declare Workflow

```c#

[Workflow]
public class SignupVerification: Workflow<SignupVerification, SignupModel, string> {


    /// Event fired when user enters the code
    /// Event must be marked as a static
    [Event]
    public static WorkflowEvent UserCode;

   
    /// User request for resend
    [Event]
    public static WorkflowEvent Resend;

    /// This is the main method of workflow
    public async Task<string> RunAsync(SignupModel model) {
        string code = (context.CurrentDateTimeUtc.Ticks & 0xFFFF).ToString();

        await EmailUserAsync(model.EmailAddress, code);

        var maxWait = TimeSpan.FromMinutes(15);

        for(int i = 0; i< 3; i++) {
            var (resendEvent, codeEvent) = await this.WaitForEvents(Resend, UserCode, maxWait);
            if(resendEvent.Raised) {
                // i is important for durable task framework
                // to know that it is not same call, otherwise it will
                // not execute this method
                await EmailUserAsync(model.EmailAddress, code, i);
                continue;
            }
            if(codeEvent.Raised) {
                if(code == codeEvent.Result) {
                    await ActivateUserAsync(model.AccountID);
                    return "User Activated";
                }
            }
        }

        // failed for 3 attempts...
        await DeleteUserAsync(model.AccountID);

        return "User Deleted";
    }


    ///
    /// Activities must be marked with [Activity] attribute
    /// and they must be public virtual methods
    ///
    [Activity]
    public virtual async Task<string> EmailUserAsync(
        string emailAddress, 
        string code, 
        int attempt = -1,
        // dependency injection !!
        [Inject] MailService mailService) {
            ... send email....
        }

    [Activity]
    public virtual async Task<string> DeleteUserAsync(
        long userId,
        [Inject] AppDbContext db) {
            ... delete user...
    }

    [Activity]
    public virtual async Task<string> ActivateUserAsync(
        long userId,
        [Inject] AppDbContext db) {
            ... mark user as active ...
    }

}

```

# Declare WorkflowService

```c#

public class WorkflowService: BaseWorkflowService {
    public WorkflowService() {
        ... other setup...

        // this is important
        this.client = new TaskHubClient ......

        // this method basically registers all activities in the assembly
        this.client.Register(typeof(WorkflowService).Assembly);
    }
}

```

# Create New Instance of Workflow

```c#

   // CreateInstanceAsync will force you to enter correct input type
   // intellisense will show up correctly
   var id = await SignupVerification.CreteInstanceAsync(workflowService, new SignupModel {
       EmailAddress = ....
       AccountId = ...
   });

```

# Raise an Event
```c#

   SignupVerification.UserCode.RaiseAsync(workflowService, id, "data");

```

# Reuse Some other workflow
Often we have common activities we would want to use it in same workflow without creating sub orchestrations. You can easily do it as follow.

```c#   

   // you can only call this from "RunAsync" method only

   var result = await ForgotPassword.RunInAsync(this, new EmailRequest {
       EmailAddress = ...
   });

```

# Restrictions
1. You cannot wait for any event in the Activity
2. You cannot call activity from another activity
3. Activities must return the result back to `RunAsync` method.
4. You can call any other method within `RunAsync` and inside activity methods.

# How it works?

1. New class inherits `Workflow` class automatically and rewrites all virtual methods. Those methods basically calls Schedule on context and pass parameter encapsulating type.
2. More than one parameters of method are actually passed as Tuples internally.
3. You can wait on multiple events at same time with single timeout. Timers are destroyed instantly if any of the event fires successfully. Events are queued so replay works correctly.


