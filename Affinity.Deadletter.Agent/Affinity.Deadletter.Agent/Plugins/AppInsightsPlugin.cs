using Affinity.Deadletter.Agent.Models;
using Affinity.Deadletter.Agent.Services.Interfaces;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Affinity.Deadletter.Agent.Plugins;

public class AppInsightsPlugin
{
    private readonly IAppInsightsClient _client;

    public AppInsightsPlugin(IAppInsightsClient client)
    {
        _client = client;
    }

    [KernelFunction]
    [Description("Get the most recent trace entry for a given AffinityCorrelationId.")]
    public Task<TraceInfo?> GetLatestTraceByCorrelationIdAsync(
        [Description("The AffinityCorrelationId from customDimensions.")] string correlationId)
        => _client.GetLatestTraceByCorrelationIdAsync(correlationId);

    [KernelFunction]
    [Description("Get the most recent exception entry for a given operation id.")]
    public Task<ExceptionInfo?> GetExceptionByOperationIdAsync(
        [Description("The operation id from trace telemetry.")] string operationId)
        => _client.GetExceptionByOperationIdAsync(operationId);
}

