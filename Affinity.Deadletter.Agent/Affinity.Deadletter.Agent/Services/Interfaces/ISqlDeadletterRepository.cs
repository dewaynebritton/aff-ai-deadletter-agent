using Affinity.Deadletter.Agent.Models;

namespace Affinity.Deadletter.Agent.Services.Interfaces;

public interface ISqlDeadletterRepository
{
    Task<IReadOnlyList<DeadletterRecord>> GetUnprocessedDeadlettersAsync(CancellationToken ct = default);
    //Task MarkAsProcessedAsync(int id, CancellationToken ct = default);
}

