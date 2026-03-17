using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using Serilog;
using Domain = ToolExecution.Domain.Models;
using ToolExecution.API.Middleware;
using ToolExecution.Application.Contracts;
using ToolExecution.Application.Services;
using ToolExecution.Application.Validators;
using ToolExecution.Domain.Models;
using ToolExecution.Infrastructure.Clients;
using ToolExecution.Infrastructure.Policies;
using ToolExecution.Infrastructure.Registries;
using ToolExecution.Infrastructure.SampleTools;
using ToolExecution.Infrastructure.Tools;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => 
    lc.WriteTo.Console());

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Dependency Injection

// Tool Engine
builder.Services.AddSingleton<Domain.IToolRegistry, InMemoryToolRegistry>();

// NEW: Register the generic ToolExecutor service
builder.Services.AddScoped<IToolExecutor, ToolExecutor>();

// LEGACY: Keep ToolExecutionService for backward compatibility with IToolExecutorService
builder.Services.AddScoped<IToolExecutorService, ToolExecutorService>();

// Kubernetes Client
builder.Services.AddSingleton<PolicyProvider>();

// DI swap: use mock or real Kubernetes client based on configuration
var kubernetesClient = builder.Configuration["KUBERNETES_CLIENT"] ?? "mock";
if (kubernetesClient == "real")
    builder.Services.AddSingleton<IKubernetesClient, KubernetesClient>();
else
    builder.Services.AddSingleton<IKubernetesClient, MockKubernetesClient>();

// FluentValidation Registration
// Assembly scanning is not used; validators are explicitly registered below
// builder.Services.AddValidatorsFromAssemblyContaining<GetPodLogsArgumentsValidator>(ServiceLifetime.Singleton);

// Specifically register each validator
builder.Services.AddSingleton<IValidator<GetPodLogsArguments>, GetPodLogsArgumentsValidator>();
builder.Services.AddSingleton<IValidator<ListPodsArguments>, ListPodsArgumentsValidator>();
builder.Services.AddSingleton<IValidator<GetDeploymentsArguments>, GetDeploymentsArgumentsValidator>();
builder.Services.AddSingleton<IValidator<GetResourceUsageArguments>, GetResourceUsageArgumentsValidator>();
builder.Services.AddSingleton<IValidator<ExecuteCommandArguments>, ExecuteCommandArgumentsValidator>();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();
    });

var app = builder.Build();

// Initialize Tool Engine: Register all tools
var toolRegistry = app.Services.GetRequiredService<Domain.IToolRegistry>();
var kubernetesClientInstance = app.Services.GetRequiredService<IKubernetesClient>();

// Register sample tools
toolRegistry.Register(new EchoTool());
toolRegistry.Register(new MathAddTool());

// Register Kubernetes tools
toolRegistry.Register(new ListPodsTool(kubernetesClientInstance));
toolRegistry.Register(new GetPodLogsTool(kubernetesClientInstance));
toolRegistry.Register(new GetDeploymentsTool(kubernetesClientInstance));
toolRegistry.Register(new GetResourceUsageTool(kubernetesClientInstance));
toolRegistry.Register(new ExecuteCommandTool(kubernetesClientInstance));

app.Logger.LogInformation(
    "Tool Engine initialized with {ToolCount} registered tools",
    toolRegistry.Count);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseSerilogRequestLogging();

// Add validation middleware
app.UseMiddleware<RequestValidationMiddleware>();

app.MapControllers();

app.Run();