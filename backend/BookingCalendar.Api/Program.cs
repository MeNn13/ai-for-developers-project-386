using BookingCalendar.Api.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

// In the container the built frontend is copied into wwwroot, so one process
// serves both the API and the pages. In development the folder does not exist
// and Vite serves the frontend instead.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapOpenApi();

app.MapGet("/api/health", () => new HealthResponse("ok"));

app.Run();

#pragma warning disable CA1050 // Program остаётся в глобальном namespace для WebApplicationFactory.
public partial class Program { }
#pragma warning restore CA1050
