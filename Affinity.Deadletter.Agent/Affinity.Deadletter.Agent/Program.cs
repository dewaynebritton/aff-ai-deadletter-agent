using Affinity.Deadletter.Agent;
using Affinity.Deadletter.Agent.Plugins;
using Affinity.Deadletter.Agent.Services;
using Affinity.Deadletter.Agent.Services.Interfaces;
using Affinity.DeadletterAgent.Agent;
using Affinity.DeadletterAgent.Function;
using Affinity.DeadletterAgent.Plugins;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Add your extra configuration sources ON TOP of what the worker already set up
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// HttpClient
builder.Services.AddHttpClient();

// Domain services
builder.Services.AddSingleton<ISqlDeadletterRepository, SqlDeadletterRepository>();
builder.Services.AddSingleton<IAppInsightsClient, AppInsightsClient>();
builder.Services.AddSingleton<IGitHubClient, GitHubClient>();
builder.Services.AddSingleton<INotificationClient, NotificationClient>();

// ---- SEMANTIC KERNEL ----
builder.Services.AddSingleton<Kernel>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    // Azure OpenAI config
    var endpoint = config["AzureOpenAI:Endpoint"]
        ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured.");
    var deploymentName = config["AzureOpenAI:DeploymentName"]
        ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName not configured.");
    var apiKey = config["AzureOpenAI:ApiKey"]
        ?? throw new InvalidOperationException("AzureOpenAI:ApiKey not configured.");

    var kernel = Kernel.CreateBuilder();

    // Add Azure OpenAI chat completion service to the kernel
    // Overload from docs:
    // builder.AddAzureOpenAIChatCompletion(deploymentName, apiKey, endpoint, modelId: null, serviceId: null, httpClient: null); :contentReference[oaicite:0]{index=0}
    kernel.AddAzureOpenAIChatCompletion(
        deploymentName: deploymentName,
        endpoint: endpoint,
        apiKey: apiKey);

    // Logging + HTTP from host DI
    kernel.Services.AddLogging();
    kernel.Services.AddHttpClient();

    var builtKernel = kernel.Build();

    // Inject domain services into plugins
    var sql = sp.GetRequiredService<ISqlDeadletterRepository>();
    var ai = sp.GetRequiredService<IAppInsightsClient>();
    var github = sp.GetRequiredService<IGitHubClient>();
    var notifier = sp.GetRequiredService<INotificationClient>();

    builtKernel.Plugins.AddFromObject(new DeadletterSqlPlugin(sql), "DeadletterSql");
    builtKernel.Plugins.AddFromObject(new AppInsightsPlugin(ai), "AppInsights");
    builtKernel.Plugins.AddFromObject(new GitHubPlugin(github), "GitHub");
    builtKernel.Plugins.AddFromObject(new NotificationPlugin(notifier), "Notify");

    return builtKernel;
});

// Agent orchestrator
builder.Services.AddSingleton<DeadletterSkAgent>();

// Functions class
builder.Services.AddSingleton<DeadletterAgentFunction>();

builder.Build().Run();

