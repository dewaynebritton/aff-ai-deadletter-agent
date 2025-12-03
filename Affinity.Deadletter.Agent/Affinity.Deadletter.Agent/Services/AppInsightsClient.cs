using Affinity.Deadletter.Agent.Models;
using Affinity.Deadletter.Agent.Services.Interfaces;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Monitor.Query.Logs;
using Azure.Monitor.Query.Logs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Affinity.Deadletter.Agent.Services;

public sealed class AppInsightsClient : IAppInsightsClient
{
    private readonly ILogger<AppInsightsClient> _logger;
    private readonly LogsQueryClient _logsClient;
    private readonly string _workspaceId;
    private readonly TimeSpan _defaultTimeRange;

    public AppInsightsClient(IConfiguration config, ILogger<AppInsightsClient> logger)
    {
        _logger = logger;

        _workspaceId = config["AppInsights:WorkspaceId"]
            ?? throw new InvalidOperationException("AppInsights:WorkspaceId not configured.");

        var minutes = int.TryParse(config["AppInsights:QueryTimeRangeMinutes"], out var m)
            ? m
            : 60;
        _defaultTimeRange = TimeSpan.FromMinutes(minutes);

        TokenCredential credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeInteractiveBrowserCredential = true
        });

        _logsClient = new LogsQueryClient(credential);
    }

    public async Task<TraceInfo?> GetLatestTraceByCorrelationIdAsync(string correlationId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            _logger.LogWarning("GetLatestTraceByCorrelationIdAsync called with empty correlationId.");
            return null;
        }

        var escaped = EscapeKustoString(correlationId);

        var kql = $@"
traces
| where timestamp between (ago({_defaultTimeRange.TotalMinutes}m)..now())
| extend AffinityCorrelationId = tostring(customDimensions.AffinityCorrelationId)
| where AffinityCorrelationId == '{escaped}'
| project timestamp, message, severityLevel, operation_Id, customDimensions
| order by timestamp desc
| take 1
";

        var table = await ExecuteKustoAsync(kql, null, ct);
        if (table == null || table.Rows.Count == 0)
        {
            _logger.LogInformation("No trace found for AffinityCorrelationId={CorrelationId}", correlationId);
            return null;
        }

        var row = table.Rows[0];

        var timestamp = (DateTimeOffset)row["timestamp"];
        var message = row["message"]?.ToString() ?? string.Empty;
        var severity = row["severityLevel"]?.ToString();
        var opId = row["operation_Id"]?.ToString() ?? string.Empty;

        return new TraceInfo
        {
            OperationId = opId,
            TimestampUtc = timestamp.UtcDateTime,
            Message = message,
            Severity = severity
        };
    }

    public async Task<ExceptionInfo?> GetExceptionByOperationIdAsync(string operationId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            _logger.LogWarning("GetExceptionByOperationIdAsync called with empty operationId.");
            return null;
        }

        var escaped = EscapeKustoString(operationId);

        var kql = $@"
exceptions
| where timestamp between (ago({_defaultTimeRange.TotalMinutes}m)..now())
| where operation_Id == '{escaped}'
| order by timestamp desc
| take 1
| project timestamp,
          type,
          outerMessage,
          innermostMessage,
          stack = tostring(details[0].parsedStack),
          problemId,
          assembly,
          method,
          operation_Id
";

        var table = await ExecuteKustoAsync(kql, null, ct);
        if (table == null || table.Rows.Count == 0)
        {
            _logger.LogInformation("No exception found for operation_Id={OperationId}", operationId);
            return null;
        }

        var row = table.Rows[0];

        var timestamp = (DateTimeOffset)row["timestamp"];
        var type = row["type"]?.ToString() ?? string.Empty;
        var outerMsg = row["outerMessage"]?.ToString() ?? string.Empty;
        var innerMsg = row["innermostMessage"]?.ToString();
        var stack = row["stack"]?.ToString();
        var opId = row["operation_Id"]?.ToString() ?? operationId;

        var message = !string.IsNullOrWhiteSpace(innerMsg) ? innerMsg! : outerMsg;

        return new ExceptionInfo
        {
            OperationId = opId,
            TimestampUtc = timestamp.UtcDateTime,
            Type = type,
            Message = message,
            StackTrace = stack
        };
    }

    private async Task<LogsTable?> ExecuteKustoAsync(
        string kql,
        TimeSpan? timeRangeOverride = null,
        CancellationToken ct = default)
    {
        var timeRange = timeRangeOverride ?? _defaultTimeRange;

        _logger.LogDebug("Executing Kusto query against workspace {WorkspaceId} with timeRange {Minutes} minutes.",
            _workspaceId, timeRange.TotalMinutes);

        try
        {
            Response<LogsQueryResult> response =
                await _logsClient.QueryWorkspaceAsync(
                    _workspaceId,
                    kql,
                    timeRange,
                    cancellationToken: ct);

            if (response.Value.AllTables.Count == 0)
            {
                return null;
            }

            return response.Value.Table;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Kusto query:\n{Kql}", kql);
            throw;
        }
    }

    private static string EscapeKustoString(string value)
        => value.Replace("'", "''");
}



