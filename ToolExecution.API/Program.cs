using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ToolExecution.Application.Contracts;
using ToolExecution.Application.Services;
using ToolExecution.Infrastructure.Clients;
using ToolExecution.Infrastructure.Policies;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc.WriteTo.Console());

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DI
builder.Services.AddSingleton<PolicyProvider>();
builder.Services.AddSingleton<IKubernetesClient, KubernetesClient>();
builder.Services.AddScoped<IToolExecutionOrchestratorService, ToolExecutionOrchestratorService>();

// OpenTelemetry minimal setup
builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
{
    tracerProviderBuilder.AddAspNetCoreInstrumentation();
    tracerProviderBuilder.AddHttpClientInstrumentation();
    tracerProviderBuilder.AddConsoleExporter();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseSerilogRequestLogging();
app.UseEndpoints(endpoints => endpoints.MapControllers());

app.Run();
