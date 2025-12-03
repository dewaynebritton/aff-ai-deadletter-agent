using Affinity.Deadletter.Agent.Services.Interfaces;
using Affinity.DeadletterAgent.Agent;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Affinity.DeadletterAgent.Function;

public sealed class DeadletterAgentFunction
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

        var oneHourAgo = DateTime.UtcNow.AddHours(-1);

        var deadletters = await _sql.GetUnprocessedDeadlettersSinceAsync(oneHourAgo, cancellationToken);
        if (deadletters.Count == 0)
        {
            _logger.LogInformation("No new Service Bus deadletters found in the last hour.");
            return;
        }

        var groups = deadletters
            .Where(dl => !string.IsNullOrWhiteSpace(dl.CustomCorrelationId))
            .GroupBy(dl => dl.CustomCorrelationId!)
            .ToList();

        foreach (var group in groups)
        {
            var correlationId = group.Key;
            var count = group.Count();

            if (count < 10)
            {
                _logger.LogInformation(
                    "CorrelationId {CorrelationId} has {Count} deadletters (<10); skipping incident.",
                    correlationId, count);
                continue;
            }

            var groupList = group.ToList();

            _logger.LogInformation(
                "CorrelationId {CorrelationId} has {Count} deadletters; raising single incident.",
                correlationId, count);

            try
            {
                await _agent.RunForCorrelationAsync(correlationId, groupList, oneHourAgo, cancellationToken);

                // Optionally also mark processed from here; or let the SK tool do it.
                await _sql.MarkGroupAsProcessedAsync(correlationId, oneHourAgo, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error running agent for correlationId={CorrelationId} with {Count} deadletters.",
                    correlationId, count);
            }
        }
    }
}
