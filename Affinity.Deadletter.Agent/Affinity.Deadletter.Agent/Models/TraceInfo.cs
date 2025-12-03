namespace Affinity.Deadletter.Agent.Models;

public sealed class TraceInfo
{
    public string OperationId { get; set; } = default!;
    public DateTime TimestampUtc { get; set; }
    public string Message { get; set; } = default!;
    public string? Severity { get; set; }
}



