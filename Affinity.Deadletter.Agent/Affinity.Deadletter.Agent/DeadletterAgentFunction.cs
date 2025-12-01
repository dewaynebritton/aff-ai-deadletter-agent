using Affinity.Deadletter.Agent.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Affinity.Deadletter.Agent;

public class DeadletterAgentFunction
{
    private readonly ILogger<DeadletterAgentFunction> _logger;
    private readonly ISqlDeadletterRepository _sql;
    private readonly DeadletterSkAgent _agent;

    public DeadletterAgentFunction(
        ILogger<DeadletterAgentFunction> logger,
        ISqlDeadletterRepository sql,
        DeadletterSkAgent agent)
    {
        _logger = logger;
        _sql = sql;
        _agent = agent;
    }

    [Function("AffinityDeadletterAgent")]
    public async Task RunAsync(
        [TimerTrigger("0 */1 * * * *")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("AffinityDeadletterAgent triggered at {Time}", DateTime.UtcNow);

        var deadletters = await _sql.GetUnprocessedDeadlettersAsync(cancellationToken);
        if (deadletters.Count == 0)
        {
            _logger.LogInformation("No new Service Bus deadletters found.");
            return;
        }

        foreach (var dl in deadletters)
        {
            try
            {
                await _agent.RunForDeadletterAsync(dl, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running agent for deadletter Id={Id}", dl.Id);
                // optional: store failure info in a separate table / column
            }
        }
    }
}
