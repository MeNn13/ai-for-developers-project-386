using System.Text.Json;

namespace BookingCalendar.Api.Tests;

public class HealthSmokeTests(TestApp app) : IClassFixture<TestApp>
{
    [Fact]
    public async Task HealthReturnsOk()
    {
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ok", document.RootElement.GetProperty("status").GetString());
    }
}
