using Microsoft.SemanticKernel;

namespace Affinity.Deadletter.Agent.Plugins;
public class GitHubPlugin
{
    [KernelFunction("search_github_code")]
    public Task<string> SearchAsync(string keywords)
    {
        // TODO: Call GitHub REST or GraphQL
        return Task.FromResult("""
        {
            "Repo": "affinity/peo-utils",
            "FilePath": "src/Email/SendCertEmail.cs",
            "Line": 122,
            "Snippet": "await _emailSender.SendCertificateAsync(...);"
        }
        """);
    }
}
