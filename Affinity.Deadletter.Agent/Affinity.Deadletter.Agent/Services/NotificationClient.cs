using Affinity.Deadletter.Agent.Models;
using Affinity.Deadletter.Agent.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Affinity.Deadletter.Agent.Services;

public sealed class NotificationClient : INotificationClient
{
    private readonly ILogger<NotificationClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _webhookUrl;

    public NotificationClient(
        ILogger<NotificationClient> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration config)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("Teams");

        _webhookUrl = config["Teams:WebhookUrl"]
            ?? throw new InvalidOperationException("Teams:WebhookUrl not configured.");
    }

    public async Task NotifyAsync(
        DeadletterRecord representative,
        int totalCount,
        TraceInfo? trace,
        ExceptionInfo? exceptionInfo,
        CodeContext? codeContext,
        AiAnalysisResult aiResult,
        CancellationToken ct = default)
    {
        var text = $@"
**Affinity Deadletter Incident**

- **Deadletter Id (sample):** {representative.Id}
- **CorrelationId:** {representative.CustomCorrelationId}
- **Total occurrences (last hour):** {totalCount}

{(trace is not null ? $"- **Trace operation_Id:** {trace.OperationId}\n" : "")}
{(exceptionInfo is not null ? $"- **Exception:** {exceptionInfo.Type} - {exceptionInfo.Message}\n" : "")}
{(codeContext?.GitHubUrl is not null ? $"- **Code location:** {codeContext.GitHubUrl}\n" : "")}

---

{aiResult.SummaryMarkdown}
";

        var payload = new { text };

        var response = await _httpClient.PostAsJsonAsync(_webhookUrl, payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Failed to send Teams notification. Status={StatusCode}, Body={Body}",
                response.StatusCode, body);
        }
    }
}

