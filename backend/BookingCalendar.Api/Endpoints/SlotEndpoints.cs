using BookingCalendar.Api.Contracts;
using BookingCalendar.Api.Http;
using BookingCalendar.Application.Abstractions;

namespace BookingCalendar.Api.Endpoints;

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
        IAvailabilityService service,
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
        var availability = service.List(meetingTypeId);
        if (availability is null)
        {
            return Problems.MeetingTypeNotFound(instance);
        }

        var response = new SlotsResponse(
            availability.Timezone,
            new SlotWindow(availability.From, availability.To),
            availability.Days.Select(day => new SlotDay(day.Date,
                day.Slots.Select(slot => new Slot(slot.Start, slot.End, slot.Available)).ToArray())).ToArray());
        return Results.Json(response);
    }
}
