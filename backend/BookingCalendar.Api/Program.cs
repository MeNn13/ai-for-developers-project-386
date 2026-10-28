using BookingCalendar.Api.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();

app.MapGet("/api/health", () => new HealthResponse("ok"));

app.Run();

#pragma warning disable CA1050 // Program остаётся в глобальном namespace для WebApplicationFactory.
public partial class Program { }
#pragma warning restore CA1050
