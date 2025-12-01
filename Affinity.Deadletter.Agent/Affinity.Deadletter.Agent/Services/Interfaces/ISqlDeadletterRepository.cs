using Affinity.Deadletter.Agent.Models;

namespace Affinity.Deadletter.Agent.Services.Interfaces;

public interface ISqlDeadletterRepository
{
    Task<IReadOnlyList<DeadletterRecord>> GetUnprocessedDeadlettersSinceAsync(
        DateTime sinceUtc,
        CancellationToken ct = default);

    Task MarkGroupAsProcessedAsync(
        string correlationId,
        DateTime sinceUtc,
        CancellationToken ct = default);
}


