using System.Text.Json;

using BookingCalendar.Api.Contracts;
using BookingCalendar.Api.Http;
using BookingCalendar.Application.Abstractions;

namespace BookingCalendar.Api.Endpoints;

internal static class MeetingTypeEndpoints
{
    public static IEndpointRouteBuilder MapMeetingTypeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/meeting-types", (IMeetingTypeService service) => service.List().Select(ToContract));
        endpoints.MapPost("/api/meeting-types", CreateMeetingTypeAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateMeetingTypeAsync(HttpRequest request, IMeetingTypeService service)
    {
        var instance = request.Path.ToString();

        using var document = await JsonRequest.ParseBodyAsync(request);
        if (document is null)
        {
            return Problems.ValidationFailed(instance, "Тело запроса не является корректным JSON.");
        }

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return Problems.ValidationFailed(instance, "Тело запроса должно быть JSON-объектом.");
        }

        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var id = ReadString(
            document.RootElement,
            "id",
            "Идентификатор",
            errors);
        var title = ReadString(
            document.RootElement,
            "title",
            "Название",
            errors);
        var description = ReadString(
            document.RootElement,
            "description",
            "Описание",
            errors);
        var duration = ReadDuration(document.RootElement, errors);

        var result = service.Create(id, title, description, duration);
        foreach (var error in result.Errors) { errors[error.Key] = [.. error.Value]; }

        if (errors.Count > 0)
        {
            return Problems.ValidationFailed(
                instance,
                "Проверьте правильность заполнения полей.",
                errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal));
        }

        if (result.AlreadyExists)
        {
            return Problems.MeetingTypeExists(instance);
        }

        return Results.Json(ToContract(result.MeetingType!), statusCode: StatusCodes.Status201Created);
    }

    private static string? ReadString(
        JsonElement root,
        string name,
        string label,
        Dictionary<string, List<string>> errors)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            JsonRequest.AddError(errors, name, $"Поле «{label}» обязательно.");
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            JsonRequest.AddError(errors, name, $"Поле «{label}» должно быть строкой.");
            return null;
        }

        var text = value.GetString()!;
        return text;
    }

    private static int? ReadDuration(JsonElement root, Dictionary<string, List<string>> errors)
    {
        const string name = "durationMinutes";
        const string label = "Длительность";

        if (!root.TryGetProperty(name, out var value))
        {
            JsonRequest.AddError(errors, name, $"Поле «{label}» обязательно.");
            return null;
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var duration))
        {
            JsonRequest.AddError(errors, name, $"Поле «{label}» должно быть целым числом минут.");
            return null;
        }

        return duration;
    }

    private static MeetingType ToContract(BookingCalendar.Domain.Aggregates.MeetingType type) =>
        new(type.Id.Value, type.Title.Value, type.Description.Value, type.Duration.Value);
}
