using Affinity.Deadletter.Agent.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace Affinity.DeadletterAgent.Agent;

public sealed class DeadletterSkAgent
{
    private readonly Kernel _kernel;
    private readonly ILogger<DeadletterSkAgent> _logger;

    public DeadletterSkAgent(Kernel kernel, ILogger<DeadletterSkAgent> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task RunForCorrelationAsync(
        string correlationId,
        IReadOnlyList<DeadletterRecord> group,
        DateTime windowStartUtc,
        CancellationToken ct = default)
    {
        var first = group.MinBy(g => g.InsertedDateUtc)!;
        var last = group.MaxBy(g => g.InsertedDateUtc)!;

        var goal = $@"
You are an Affinity deadletter diagnostic agent.

You are investigating a *group incident* for a correlation id that has failed multiple times.

CorrelationId: {correlationId}
Total deadletters in this incident: {group.Count}
Time window: {first.InsertedDateUtc:o} - {last.InsertedDateUtc:o}

Your job:

1. Use tools to:
   - Look up traces using the correlation id (AffinityCorrelationId in customDimensions).
   - From traces, identify the most relevant operation_Id.
   - Retrieve the exception for that operation_Id.
   - Use the exception's stack trace to locate code in GitHub (file and line), if possible.
2. Analyze the failure pattern (frequency, time window).
3. Produce a Markdown incident summary that includes:
   - Title and short summary
   - Reproduction clues (inputs, timing, context)
   - Likely root cause, including code location
   - Suggested code fix or mitigation
   - Any improvements to logging or telemetry.
4. Call the notification tool exactly once with:
   - A concise Markdown summary
   - The incident correlation id
   - The incident count
   - A representative deadletter id.
5. Optionally mark the deadletter group as processed, if it is safe to do so.

Think step-by-step and prefer using tools instead of guessing.
";

        // Enable auto function calling over all plugins
        var settings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var args = new KernelArguments(settings)
        {
            ["incidentCorrelationId"] = correlationId,
            ["incidentCount"] = group.Count.ToString(),
            ["incidentFirstSeen"] = first.InsertedDateUtc.ToString("o"),
            ["incidentLastSeen"] = last.InsertedDateUtc.ToString("o"),
            ["incidentWindowStartUtc"] = windowStartUtc.ToString("o"),
            ["incidentDeadlettersJson"] = JsonSerializer.Serialize(group)
        };

        _logger.LogInformation(
            "Starting SK agent for correlationId={CorrelationId} with {Count} deadletters.",
            correlationId, group.Count);

        var result = await _kernel.InvokePromptAsync(
            goal,
            args,
            cancellationToken: ct);

        _logger.LogInformation(
            "Agent completed for correlationId={CorrelationId}. Final result: {Result}",
            correlationId, result.ToString());
    }
}
