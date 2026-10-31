using BookingCalendar.Domain.Aggregates;

namespace BookingCalendar.Application.State;

// Shared with the temporary API CalendarStore until booking and availability migrate.
internal sealed class CalendarState
{
    internal List<StoredMeeting> Meetings { get; } = [];
    internal object Gate { get; } = new();
    internal List<MeetingType> MeetingTypes { get; } = [];
}
