using System.Globalization;
using System.Text.Json;

using BookingCalendar.Api.Domain;
using BookingCalendar.Api.Http;
using BookingCalendar.Api.Storage;

namespace BookingCalendar.Api.Endpoints;

// Эндпоинты встреч: создание с повторной проверкой правил слота и чтение
// предстоящих встреч. Проверка и вставка — в CalendarStore под единым lock
// (ADR 0001); снаружи остаются только разбор запроса и сборка ответа.
internal static class MeetingEndpoints
{
    private const int MeetingTypeIdMinLength = 1;
    private const int MeetingTypeIdMaxLength = 64;
    private const int GuestNameMinLength = 1;
    private const int GuestNameMaxLength = 100;

    public static IEndpointRouteBuilder MapMeetingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/meetings", CreateMeetingAsync);
        endpoints.MapGet("/api/meetings", (CalendarStore store) => store.ListUpcomingMeetings());
        return endpoints;
    }

    private static async Task<IResult> CreateMeetingAsync(HttpRequest request, CalendarStore store)
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
        var meetingTypeId = ReadString(
            document.RootElement,
            "meetingTypeId",
            "Тип встречи",
            MeetingTypeIdMinLength,
            MeetingTypeIdMaxLength,
            errors);
        var start = ReadStart(document.RootElement, errors);
        var guestName = ReadString(
            document.RootElement,
            "guestName",
            "Ваше имя",
            GuestNameMinLength,
            GuestNameMaxLength,
            errors);

        if (errors.Count > 0)
        {
            return Problems.ValidationFailed(
                instance,
                "Проверьте правильность заполнения полей.",
                errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal));
        }

        var result = store.TryBook(meetingTypeId!, start!.Value, guestName!);
        return result.Outcome switch
        {
            BookingOutcome.Created => Results.Json(result.Meeting, statusCode: StatusCodes.Status201Created),
            BookingOutcome.MeetingTypeNotFound => Problems.MeetingTypeNotFound(instance),
            BookingOutcome.SlotNotBookable => Problems.SlotNotBookable(instance),
            BookingOutcome.SlotTaken => Problems.SlotTaken(instance),
            _ => throw new InvalidOperationException($"Неизвестный исход бронирования: {result.Outcome}."),
        };
    }

    private static string? ReadString(
        JsonElement root,
        string name,
        string label,
        int minLength,
        int maxLength,
        Dictionary<string, List<string>> errors)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            JsonRequest.AddError(errors, name, $"Поле «{label}» обязательно и должно быть строкой.");
            return null;
        }

        var text = value.GetString()!;
        if (text.Length < minLength || text.Length > maxLength)
        {
            JsonRequest.AddError(errors, name, $"Длина поля «{label}» должна быть от {minLength} до {maxLength} символов.");
            return null;
        }

        return text;
    }

    // Дата без времени или без смещения — не слот, а битое поле: 400 validation-failed.
    private static DateTimeOffset? ReadStart(JsonElement root, Dictionary<string, List<string>> errors)
    {
        const string name = "start";
        const string label = "Начало";

        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            JsonRequest.AddError(errors, name, $"Поле «{label}» обязательно и должно быть строкой.");
            return null;
        }

        var text = value.GetString()!;
        if (!HasOffset(text)
            || !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            JsonRequest.AddError(errors, name, $"Поле «{label}» должно быть в формате ISO 8601 со смещением.");
            return null;
        }

        return TimeZoneInfo.ConvertTime(parsed, SlotRules.TimeZone);
    }

    // ISO 8601 со смещением заканчивается на «Z» или на «±ЧЧ:ММ» / «±ЧЧММ».
    private static bool HasOffset(string value) =>
        value.EndsWith('Z')
        || (value.Length >= 6 && value[^6] is '+' or '-' && value[^3] == ':')
        || (value.Length >= 5 && value[^5] is '+' or '-');
}
