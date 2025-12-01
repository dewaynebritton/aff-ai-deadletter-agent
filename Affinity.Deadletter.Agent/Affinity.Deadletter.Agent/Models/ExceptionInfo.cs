namespace Affinity.Deadletter.Agent.Models;

public sealed class ExceptionInfo
{
    public string OperationId { get; set; } = default!;
    public DateTime TimestampUtc { get; set; }
    public string Type { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string? StackTrace { get; set; }
}
