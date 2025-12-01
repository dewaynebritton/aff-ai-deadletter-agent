using Affinity.Deadletter.Agent.Models;

namespace Affinity.Deadletter.Agent.Services.Interfaces;

public interface IAppInsightsClient
{
    Task<TraceInfo?> GetLatestTraceByCorrelationIdAsync(string correlationId, CancellationToken ct = default);
    Task<ExceptionInfo?> GetExceptionByOperationIdAsync(string operationId, CancellationToken ct = default);
}

