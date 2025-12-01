using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using System.Net.Http.Json;

namespace Affinity.Deadletter.Agent.Plugins;

public class TeamsPlugin
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http = new();

    public TeamsPlugin(IConfiguration config)
    {
        _config = config;
    }

    [KernelFunction("send_teams_message")]
    public async Task<string> SendTeamsMessageAsync(string title, string body)
    {
        var webhook = _config["Teams:WebhookUrl"];

        var payload = new
        {
            title,
            text = body
        };

        await _http.PostAsJsonAsync(webhook, payload);

        return "Message sent";
    }
}

