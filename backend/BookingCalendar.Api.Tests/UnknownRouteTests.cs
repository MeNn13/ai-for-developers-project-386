using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;

namespace BookingCalendar.Api.Tests;

public class UnknownRouteTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task UnknownApiRouteReturnsNotFound()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
