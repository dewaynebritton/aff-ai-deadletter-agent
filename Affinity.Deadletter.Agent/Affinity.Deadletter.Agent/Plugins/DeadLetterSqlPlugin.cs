using System.ComponentModel;
using Affinity.Deadletter.Agent.Models;
using Affinity.Deadletter.Agent.Services.Interfaces;
using Microsoft.SemanticKernel;

namespace Affinity.Deadletter.Agent.Plugins;

public class DeadletterSqlPlugin
{
    private readonly ISqlDeadletterRepository _repo;

    public DeadletterSqlPlugin(ISqlDeadletterRepository repo)
    {
        _repo = repo;
    }

    [KernelFunction]
    [Description("Get up to 50 new unprocessed Service Bus deadletter records from the Affinity SQL table.")]
    public async Task<IReadOnlyList<DeadletterRecord>> GetNewDeadlettersAsync()
    {
        return await _repo.GetUnprocessedDeadlettersAsync();
    }

    [KernelFunction]
    [Description("Mark a Service Bus deadletter record as processed by Id in the Affinity SQL table.")]
    public async Task MarkDeadletterAsProcessedAsync(
        [Description("The Id of the deadletter row.")] int id)
    {
        //await _repo.MarkAsProcessedAsync(id);
    }
}

