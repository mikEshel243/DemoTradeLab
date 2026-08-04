using System.Text.Json;
using System.Text.Json.Serialization;
using DemoTradeLab.Core.Trades;
using DemoTradeLab.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DemoTradeLab")
    ?? throw new InvalidOperationException(
        "Connection string 'DemoTradeLab' is not configured.");

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
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<TradeService>();

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
