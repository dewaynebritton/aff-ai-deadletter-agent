using Affinity.Deadletter.Agent.Plugins;
using Azure.Core;
using Azure.Identity;
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

// ---- SEMANTIC KERNEL ----
builder.Services.AddSingleton<Kernel>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var kernel = Kernel.CreateBuilder()
        .AddAzureOpenAIChatCompletion(
            deploymentName: config["AzureOpenAI:Deployment"],
            endpoint: config["AzureOpenAI:Endpoint"],
            apiKey: config["AzureOpenAI:ApiKey"])
        .Build();
    // Register plugins
    kernel.Plugins.AddFromType<DeadLetterPlugin>("DeadLetter");
    kernel.Plugins.AddFromType<TelemetryPlugin>("Telemetry");
    kernel.Plugins.AddFromType<GitHubPlugin>("GitHub");
    kernel.Plugins.AddFromType<TeamsPlugin>("Teams");
    return kernel;
});

builder.Services.AddSingleton<TokenCredential, DefaultAzureCredential>();

builder.Build().Run();

