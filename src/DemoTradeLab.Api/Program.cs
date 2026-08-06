using System.Text.Json;
using System.Text.Json.Serialization;
using DemoTradeLab.Api.Configuration;
using DemoTradeLab.Core.Analytics;
using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Trades;
using DemoTradeLab.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    "demo-environment.json",
    optional: false,
    reloadOnChange: false);

var connectionString = builder.Configuration.GetConnectionString("DemoTradeLab")
    ?? throw new InvalidOperationException(
        "Connection string 'DemoTradeLab' is not configured.");
var demoEnvironmentSection = builder.Configuration.GetRequiredSection(
    DemoEnvironmentOptions.SectionName);
var demoEnvironment = demoEnvironmentSection.Get<DemoEnvironmentOptions>()
    ?? throw new InvalidOperationException(
        $"Configuration section '{DemoEnvironmentOptions.SectionName}' is invalid.");

builder.Services.AddOptions<DemoEnvironmentOptions>()
    .Bind(demoEnvironmentSection)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter<TradeDirection>(
                JsonNamingPolicy.CamelCase,
                allowIntegerValues: false));
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter<TradeDataSource>(
                JsonNamingPolicy.CamelCase,
                allowIntegerValues: false));
    });
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(
    connectionString,
    demoEnvironment.ToSeedDefinitions());
builder.Services.AddScoped<DemoProfileService>();
builder.Services.AddScoped<TradeService>();
builder.Services.AddScoped<TradeAnalyticsService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program;
