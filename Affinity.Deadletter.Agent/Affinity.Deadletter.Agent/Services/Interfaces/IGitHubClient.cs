using Affinity.Deadletter.Agent.Models;
using Affinity.Deadletter.Agent.Services.Interfaces;

namespace Affinity.Deadletter.Agent.Services;

public interface IGitHubClient
{
    Task<CodeContext?> ResolveCodeContextFromStackTraceAsync(ExceptionInfo exception, CancellationToken ct = default);
}