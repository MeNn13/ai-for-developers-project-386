using System.Text.Json;

using Json.Schema;

namespace BookingCalendar.Api.Tests;

// Единая проверка ошибок ProblemDetails: application/problem+json, соответствие
// схеме ProblemDetails из контракта, slug из enum контракта и совпадение status.
internal static class ProblemAssertions
{
    public static async Task AssertProblem(HttpResponseMessage response, string slug)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        var result = OpenApiContract.Instance.ProblemSchema.Evaluate(
            root,
            new EvaluationOptions { OutputFormat = OutputFormat.List });
        Assert.True(
            result.IsValid,
            $"Тело ошибки не соответствует схеме ProblemDetails из контракта.\n{result}\n{body}");

        Assert.Equal(slug, root.GetProperty("type").GetString());
        Assert.Contains(slug, OpenApiContract.Instance.ProblemTypeSlugs);
        Assert.Equal((int)response.StatusCode, root.GetProperty("status").GetInt32());
    }
}
