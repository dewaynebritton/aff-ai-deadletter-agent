using Microsoft.SemanticKernel;

namespace Affinity.Deadletter.Agent.Plugins;

public class DeadLetterPlugin
{
    [KernelFunction("get_new_deadletter")]
    public Task<string> GetNewDeadLetterAsync()
    {
        // TODO: Replace with SQL or DLQ query
        return Task.FromResult("""
    {
      "MessageId": "abc123",
      "CorrelationId": "785212a0-3829-4424-93aa-efeec3b31203",
      "Error": "Function XYZ failed due to timeout"
    }
    """);
    }
}
