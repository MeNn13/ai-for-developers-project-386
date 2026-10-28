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

// An unknown API route answers 404 instead of falling through to the SPA
// fallback below, which would return HTML with 200.
app.Map("/api/{**rest}", (HttpContext context) =>
{
    context.Response.StatusCode = StatusCodes.Status404NotFound;
    return Task.CompletedTask;
});

// Deep links to the SPA (/book, /owner) have no file on disk, so the fallback
// returns index.html and the router takes over.
app.MapFallbackToFile("index.html");

app.Run();

#pragma warning disable CA1050 // Program остаётся в глобальном namespace для WebApplicationFactory.
public partial class Program { }
#pragma warning restore CA1050
