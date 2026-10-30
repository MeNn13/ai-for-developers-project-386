using BookingCalendar.Api.Domain;
using BookingCalendar.Api.Storage;

namespace BookingCalendar.Api;

// Слоты выбранного типа встречи на всё окно записи: один снимок на запрос.
// Тип, now и занятость приходят из хранилища одним чтением под lock (ADR 0001),
// правила слота — чистые (Domain.SlotRules).
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

        // ADR 0001: тип встречи, now и занятость читаются одним снимком под lock.
        var snapshot = store.ReadSlotSnapshot(meetingTypeId);
        if (snapshot is null)
        {
            return Problems.MeetingTypeNotFound(instance);
        }

        var response = SlotRules.BuildResponse(
            snapshot.Value.DurationMinutes,
            snapshot.Value.Now,
            snapshot.Value.Occupied);
        return Results.Json(response);
    }
}
