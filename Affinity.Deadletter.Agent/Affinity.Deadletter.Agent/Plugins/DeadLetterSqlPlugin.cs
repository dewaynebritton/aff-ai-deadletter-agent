using Affinity.Deadletter.Agent.Services.Interfaces;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Affinity.Deadletter.Agent.Plugins;

public class DeadletterSqlPlugin
{
    private readonly ISqlDeadletterRepository _repo;

    public DeadletterSqlPlugin(ISqlDeadletterRepository repo)
    {
        _repo = repo;
    }

    [KernelFunction]
    [Description("Mark a Service Bus deadletter group as processed by correlation id and starting time.")]
    public async Task MarkDeadletterGroupAsProcessedAsync(
        [Description("The correlation id for the group.")] string correlationId,
        [Description("ISO 8601 UTC timestamp indicating the earliest time to include.")] string sinceUtcIso)
    {
        if (!DateTime.TryParse(sinceUtcIso, out var since))
        {
            throw new ArgumentException("Invalid sinceUtcIso", nameof(sinceUtcIso));
        }

        await _repo.MarkGroupAsProcessedAsync(correlationId, since, CancellationToken.None);
    }
}


