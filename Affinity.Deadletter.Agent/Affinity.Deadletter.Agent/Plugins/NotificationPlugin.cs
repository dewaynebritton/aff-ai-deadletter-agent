using Affinity.Deadletter.Agent.Models;
using Affinity.Deadletter.Agent.Services.Interfaces;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Affinity.Deadletter.Agent.Plugins;

public class NotificationPlugin
{
    private readonly INotificationClient _notifier;

    public NotificationPlugin(INotificationClient notifier)
    {
        _notifier = notifier;
    }

    [KernelFunction]
    [Description("Notify developers about a grouped deadletter incident with a Markdown summary.")]
    public async Task NotifyDeveloperAsync(
        [Description("Representative deadletter Id.")] int representativeDeadletterId,
        [Description("Correlation id for the incident.")] string? correlationId,
        [Description("Total number of deadletters in this incident.")] int totalCount,
        [Description("Markdown summary of the incident and suggested fix.")] string analysisMarkdown)
    {
        var dl = new DeadletterRecord
        {
            Id = representativeDeadletterId,
            CustomCorrelationId = correlationId
        };

        var aiResult = new AiAnalysisResult { SummaryMarkdown = analysisMarkdown };

        await _notifier.NotifyAsync(dl, totalCount, null, null, null, aiResult, CancellationToken.None);
    }
}


