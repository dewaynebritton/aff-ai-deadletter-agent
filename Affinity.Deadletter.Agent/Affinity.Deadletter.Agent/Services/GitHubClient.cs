using Affinity.Deadletter.Agent.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace Affinity.Deadletter.Agent.Services;

public sealed class GitHubClient : IGitHubClient
{
    private readonly ILogger<GitHubClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _owner;
    private readonly string _repo;
    private readonly string _branch;

    public GitHubClient(
        ILogger<GitHubClient> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration config)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("GitHub");

        var token = config["GitHub:Token"]
            ?? throw new InvalidOperationException("GitHub:Token not configured.");
        _owner = config["GitHub:Owner"] ?? "Lockton-Affinity";
        _repo = config["GitHub:Repo"] ?? "Affinity.API";
        _branch = config["GitHub:Branch"] ?? "main";

        _httpClient.BaseAddress = new Uri("https://api.github.com/");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AffinityDeadletterAgent");
    }

    public async Task<CodeContext?> ResolveCodeContextFromStackTraceAsync(ExceptionInfo exception, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(exception.StackTrace))
        {
            _logger.LogInformation("No stack trace provided; cannot resolve GitHub context.");
            return null;
        }

        // TODO: parse stack trace (e.g. find first frame with your namespace, extract file path + line)
        // For now, just return a stub; you can plug in real parsing logic.

        // Example stub:
        var path = "src/Affinity.API/SomeFile.cs";
        var line = 123;

        var url = $"https://github.com/{_owner}/{_repo}/blob/{_branch}/{path}#L{line}";

        return new CodeContext
        {
            Repository = _repo,
            Branch = _branch,
            FilePath = path,
            LineNumber = line,
            GitHubUrl = url,
            CodeSnippet = null // optionally call GitHub contents API to fetch a snippet
        };
    }
}

