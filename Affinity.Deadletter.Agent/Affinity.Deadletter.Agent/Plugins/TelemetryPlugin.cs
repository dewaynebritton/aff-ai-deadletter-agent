using Microsoft.SemanticKernel;

namespace Affinity.Deadletter.Agent.Plugins;

public class TelemetryPlugin
{
    [KernelFunction("get_telemetry_by_correlation")]
    public Task<string> GetTelemetryAsync(string correlationId)
    {
        // TODO: Replace with LogsQueryClient Kusto lookup
        return Task.FromResult($$"""
        {
            "OperationId": "operation-444",
            "TraceMessage": "Timeout exceeded in Functions.SendCertEmail",
            "Timestamp": "2025-10-24T20:11:00Z"
        }
        """);
    }

    [KernelFunction("get_exception_for_operation")]
    public Task<string> GetExceptionAsync(string operationId)
    {
        // TODO: Replace with real App Insights exception query
        return Task.FromResult("""
        {
          "ExceptionType": "TaskCanceledException",
          "Message": "Timeout value exceeded",
          "StackTrace": "at Affinity.API.Email.SendCertEmail.Send() in /src/.../Email.cs:line 122"
        }
        """);
    }
}
