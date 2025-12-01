using Affinity.Deadletter.Agent.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;


public class DeadletterSkAgent
{
    private readonly Kernel _kernel;
    private readonly ILogger<DeadletterSkAgent> _logger;

    public DeadletterSkAgent(Kernel kernel, ILogger<DeadletterSkAgent> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task RunForDeadletterAsync(DeadletterRecord dl, CancellationToken ct = default)
    {
        //var planner = new StepwisePlanner(_kernel);

        var goal = $@"
You are an Affinity deadletter diagnostic agent.

Investigate this Service Bus deadletter and notify the Affinity dev team with a concise summary and suggested fix.

Deadletter:
- Id: {dl.Id}
- CustomCorrelationId: {dl.CustomCorrelationId}
- InsertedDateUtc: {dl.InsertedDateUtc:o}
- RawPayloadJson: {Truncate(dl.RawPayloadJson, 2000)}

You can:
- Get traces by correlation id
- Get exceptions by operation id
- Resolve GitHub code context from stack trace
- Notify developers via Teams
- Mark deadletters as processed

Plan:
1. If correlation id exists, fetch latest trace.
2. Use its operation id to fetch exception.
3. Use exception stack trace to locate code in GitHub.
4. Create a Markdown summary: root cause, likely fix, and any follow-up logging suggestions.
5. Call notification tool to send summary.
6. Mark the deadletter as processed.
Stop when you are done.";

        var args = new KernelArguments
        {
            ["deadletterId"] = dl.Id.ToString(),
            ["deadletterCorrelationId"] = dl.CustomCorrelationId ?? string.Empty,
            ["deadletterRawPayload"] = dl.RawPayloadJson ?? string.Empty
        };

        _logger.LogInformation("Starting SK agent for deadletter Id={Id}", dl.Id);

        //var plan = await planner.CreatePlanAsync(goal);
        //var result = await plan.InvokeAsync(_kernel, args, ct);

        //_logger.LogInformation("Agent completed for deadletter Id={Id}. Result: {Result}",
        //    dl.Id, result.ToString());
    }

    private static string? Truncate(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Length <= max ? s : s[..max] + "...[truncated]";
    }
}

