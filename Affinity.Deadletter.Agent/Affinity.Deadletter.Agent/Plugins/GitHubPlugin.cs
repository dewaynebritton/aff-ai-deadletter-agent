using Affinity.Deadletter.Agent.Models;
using Affinity.Deadletter.Agent.Services;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Affinity.DeadletterAgent.Plugins;

public class GitHubPlugin
{
    private readonly IGitHubClient _github;

    public GitHubPlugin(IGitHubClient github)
    {
        _github = github;
    }

    [KernelFunction]
    [Description("Resolve the GitHub code context from an exception stack trace.")]
    public Task<CodeContext?> ResolveCodeFromStackTraceAsync(
        [Description("Full exception stack trace.")] string stackTrace)
    {
        var ex = new ExceptionInfo
        {
            StackTrace = stackTrace,
            OperationId = "N/A",
            Type = "N/A",
            Message = "N/A",
            TimestampUtc = DateTime.UtcNow
        };

        return _github.ResolveCodeContextFromStackTraceAsync(ex);
    }
}