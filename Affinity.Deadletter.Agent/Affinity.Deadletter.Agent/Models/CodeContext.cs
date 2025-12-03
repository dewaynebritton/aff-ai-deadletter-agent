namespace Affinity.Deadletter.Agent.Models;

public sealed class CodeContext
{
    public string? Repository { get; set; }
    public string? Branch { get; set; }
    public string? FilePath { get; set; }
    public int? LineNumber { get; set; }
    public string? CodeSnippet { get; set; }
    public string? GitHubUrl { get; set; }
}

