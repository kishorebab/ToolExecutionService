using Polly;
using Polly.Retry;

namespace ToolExecution.Infrastructure.Policies;

public sealed class PolicyProvider
{
    public AsyncRetryPolicy DefaultRetryPolicy { get; }

    public PolicyProvider()
    {
        DefaultRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(200 * retryAttempt));
    }
}
