using Affinity.Deadletter.Agent.Models;

namespace Affinity.Deadletter.Agent.Services.Interfaces;

public interface INotificationClient
{
    Task NotifyAsync(
        DeadletterRecord representative,
        int totalCount,
        TraceInfo? trace,
        ExceptionInfo? exceptionInfo,
        CodeContext? codeContext,
        AiAnalysisResult aiResult,
        CancellationToken ct = default);
}


