using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using Serilog;
using ToolExecution.API.Middleware;
using ToolExecution.Application.Contracts;
using ToolExecution.Application.Services;
using ToolExecution.Application.Validators;
using ToolExecution.Domain.Models;
using ToolExecution.Infrastructure.Clients;
using ToolExecution.Infrastructure.Policies;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => 
    lc.WriteTo.Console());

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
});

// Dependency Injection
builder.Services.AddSingleton<PolicyProvider>();
builder.Services.AddSingleton<IKubernetesClient, KubernetesClient>();
builder.Services.AddScoped<IToolExecutorService, ToolExecutorService>();

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

app.Run();