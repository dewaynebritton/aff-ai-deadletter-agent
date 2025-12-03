namespace Affinity.Deadletter.Agent.Models;

public sealed class DeadletterRecord
{
    public int Id { get; set; }
    public string? CustomCorrelationId { get; set; }
    public DateTime InsertedDateUtc { get; set; }
    public string? RawPayloadJson { get; set; }
}
