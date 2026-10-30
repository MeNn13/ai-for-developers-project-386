using BookingCalendar.Api.Domain;
using BookingCalendar.Api.Storage;

namespace BookingCalendar.Api;

// Слоты выбранного типа встречи на всё окно записи: один снимок на запрос.
// now читается один раз, правила слота — чистые (Domain.SlotRules).
internal static class SlotEndpoints
{
    public static IEndpointRouteBuilder MapSlotEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/slots", ListSlots);
        return endpoints;
    }

    private static IResult ListSlots(
        string? meetingTypeId,
        CalendarStore store,
        TimeProvider timeProvider,
        HttpRequest request)
    {
        var instance = request.Path.ToString();

        if (string.IsNullOrWhiteSpace(meetingTypeId))
        {
            return Problems.ValidationFailed(
                instance,
                "Параметр meetingTypeId обязателен.",
                new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["meetingTypeId"] = ["Параметр «meetingTypeId» обязателен."],
                });
        }

        var now = timeProvider.GetUtcNow();
        var meetingType = store.FindMeetingType(meetingTypeId);
        if (meetingType is null)
        {
            return Problems.MeetingTypeNotFound(instance);
        }

        // ADR 0001: снимок встреч читается под тем же lock, что и типы встреч.
        var occupied = store.ListOccupiedIntervals();
        var response = SlotRules.BuildResponse(meetingType.DurationMinutes, now, occupied);
        return Results.Json(response);
    }
}
