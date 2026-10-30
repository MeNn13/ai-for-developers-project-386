using System.Net;
using System.Text;
using System.Text.Json;

using Json.Schema;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace BookingCalendar.Api.Tests;

// Conformance «контракт <-> бэкенд» по contract/openapi/openapi.yaml.
// Прямое направление: каждая операция контракта с валидным примером из контракта
// отвечает не-404, а тело ответа проходит JSON-схему объявленного успеха.
// Обратное направление: маршруты /api приложения из EndpointDataSource совпадают
// с путями контракта (catch-all с ** в шаблонах не считается маршрутом контракта).
public class ContractConformanceTests(TestApp app) : IClassFixture<TestApp>
{
    [Theory]
    [MemberData(nameof(ContractOperations))]
    public async Task EveryContractOperationAnswersAndMatchesSchema(string path, string method)
    {
        using var client = app.CreateClient();
        using var response = await SendOperationAsync(client, path, method);

        var status = (int)response.StatusCode;
        Assert.NotEqual((int)HttpStatusCode.NotFound, status);
        Assert.True(response.IsSuccessStatusCode, $"{method} {path} ответил {status}.");

        Assert.Contains(status, OpenApiContract.Instance.ResponseStatusCodes(path, method));
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var result = OpenApiContract.Instance.ResponseSchema(path, method, status)
            .Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });

        Assert.True(
            result.IsValid,
            $"Тело {method} {path} не соответствует схеме контракта.\n{result}\n{body}");
    }

    [Fact]
    public void ApplicationApiRoutesMatchContractPaths()
    {
        var dataSource = app.Services.GetRequiredService<EndpointDataSource>();

        var appRoutes = dataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText!)
            .Where(template =>
                template.StartsWith("/api", StringComparison.Ordinal)
                && !template.Contains("**", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(template => template, StringComparer.Ordinal)
            .ToArray();

        string[] contractPaths = [.. OpenApiContract.Instance.PathMethods.Keys
            .OrderBy(path => path, StringComparer.Ordinal)];

        Assert.Equal(contractPaths, appRoutes);
    }

    public static TheoryData<string, string> ContractOperations
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var (path, methods) in OpenApiContract.Instance.PathMethods)
            {
                foreach (var method in methods)
                {
                    data.Add(path, method);
                }
            }

            return data;
        }
    }

    private static async Task<HttpResponseMessage> SendOperationAsync(
        HttpClient client,
        string path,
        string method) => method switch
        {
            "get" => await client.GetAsync(path),
            "post" => await client.PostAsync(path, ContractRequestContent(path, method)),
            _ => throw new NotSupportedException($"Метод «{method}» не поддерживается тестом."),
        };

    // Тело POST-запроса берётся из example схемы запроса в контракте.
    private static StringContent ContractRequestContent(string path, string method)
    {
        var example = OpenApiContract.Instance
            .RequestExample(OpenApiContract.Instance.RequestSchemaName(path, method));
        return new StringContent(example.ToJsonString(), Encoding.UTF8, "application/json");
    }
}
