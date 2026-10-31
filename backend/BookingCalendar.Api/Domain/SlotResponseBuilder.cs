using BookingCalendar.Api.Contracts;
using BookingCalendar.Domain.Rules;

using static BookingCalendar.Domain.Rules.SlotRules;
namespace BookingCalendar.Api.Domain;

internal static class SlotResponseBuilder
{
    public static SlotDay BuildDay(
        DateOnly date,
        int durationMinutes,
        DateTimeOffset now,
        IReadOnlyList<OccupiedInterval> meetings)
    {
        if (!IsWorkingDay(date))
        {
            return new SlotDay(date, []);
        }

        var slots = new List<Slot>();
        foreach (var start in DayStarts(date))
        {
            if (!IsBookable(start, durationMinutes, now))
            {
                continue;
            }

            var end = start.AddMinutes(durationMinutes);
            slots.Add(new Slot(start, end, IsAvailable(start, end, meetings)));
        }

        return new SlotDay(date, slots);
    }

    public static SlotsResponse BuildResponse(
        int durationMinutes,
        DateTimeOffset now,
        IReadOnlyList<OccupiedInterval> meetings)
    {
        var from = WindowStart(now);
        var days = new List<SlotDay>(WindowDays);
        for (var offset = 0; offset < WindowDays; offset++)
        {
            days.Add(BuildDay(from.AddDays(offset), durationMinutes, now, meetings));
        }

        return new SlotsResponse(TimeZoneId, new SlotWindow(from, from.AddDays(WindowDays - 1)), days);
    }

}
