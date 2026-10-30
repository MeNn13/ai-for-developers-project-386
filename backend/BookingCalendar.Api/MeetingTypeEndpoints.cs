using System.Text.Json;

using BookingCalendar.Api.Contracts;
using BookingCalendar.Api.Domain;
using BookingCalendar.Api.Storage;

namespace BookingCalendar.Api;

// Эндпоинты типов встреч: чтение списка и создание с валидацией по ограничениям
// контракта (contract/main.tsp). Проверка дубликата id и запись — в CalendarStore
// под единым lock (ADR 0001); в критической секции нет await и ввода-вывода.
internal static class MeetingTypeEndpoints
{
    private const int IdMinLength = 1;
    private const int IdMaxLength = 64;
    private const int TitleMinLength = 1;
    private const int TitleMaxLength = 120;
    private const int DescriptionMaxLength = 500;

    public static IEndpointRouteBuilder MapMeetingTypeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/meeting-types", (CalendarStore store) => store.ListMeetingTypes());
        endpoints.MapPost("/api/meeting-types", CreateMeetingTypeAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateMeetingTypeAsync(HttpRequest request, CalendarStore store)
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
            IdMinLength,
            IdMaxLength,
            requireIdPattern: true,
            errors);
        var title = ReadString(
            document.RootElement,
            "title",
            "Название",
            TitleMinLength,
            TitleMaxLength,
            requireIdPattern: false,
            errors);
        var description = ReadString(
            document.RootElement,
            "description",
            "Описание",
            minLength: 0,
            DescriptionMaxLength,
            requireIdPattern: false,
            errors);
        var duration = ReadDuration(document.RootElement, errors);

        if (errors.Count > 0)
        {
            return Problems.ValidationFailed(
                instance,
                "Проверьте правильность заполнения полей.",
                errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal));
        }

        var created = new MeetingType(id!, title!, description!, duration!.Value);
        if (!store.TryAdd(created))
        {
            return Problems.MeetingTypeExists(instance);
        }

        return Results.Json(created, statusCode: StatusCodes.Status201Created);
    }

    private static string? ReadString(
        JsonElement root,
        string name,
        string label,
        int minLength,
        int maxLength,
        bool requireIdPattern,
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
        if (text.Length < minLength || text.Length > maxLength)
        {
            var limit = minLength == 0
                ? $"не больше {maxLength} символов"
                : $"от {minLength} до {maxLength} символов";
            JsonRequest.AddError(errors, name, $"Длина поля «{label}» должна быть {limit}.");
            return null;
        }

        if (requireIdPattern && !IsAllowedId(text))
        {
            JsonRequest.AddError(
                errors,
                name,
                "Поле «Идентификатор» может содержать только строчные латинские буквы, цифры и дефис.");
            return null;
        }

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

        if (duration < MeetingTypeConstraints.DurationMinMinutes
            || duration > MeetingTypeConstraints.DurationMaxMinutes)
        {
            JsonRequest.AddError(
                errors,
                name,
                $"Длительность должна быть от {MeetingTypeConstraints.DurationMinMinutes} до {MeetingTypeConstraints.DurationMaxMinutes} минут.");
            return null;
        }

        if (duration % MeetingTypeConstraints.DurationStepMinutes != 0)
        {
            JsonRequest.AddError(
                errors,
                name,
                $"Длительность должна быть кратна {MeetingTypeConstraints.DurationStepMinutes} минутам.");
            return null;
        }

        return duration;
    }

    // Совпадает с шаблоном контракта ^[a-z0-9-]+$: ведущие и хвостовые дефисы допустимы.
    private static bool IsAllowedId(string value) =>
        value.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');
}
