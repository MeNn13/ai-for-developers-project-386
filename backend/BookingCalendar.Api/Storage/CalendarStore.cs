using BookingCalendar.Application.State;
using BookingCalendar.Domain.Rules;
namespace BookingCalendar.Api.Storage;

public readonly record struct SlotSnapshot(int DurationMinutes, DateTimeOffset Now, IReadOnlyList<OccupiedInterval> Occupied);
internal sealed class CalendarStore(TimeProvider timeProvider, CalendarState state)
{
    public SlotSnapshot? ReadSlotSnapshot(string meetingTypeId)
    {
        lock (state.Gate)
        {
            var now = timeProvider.GetUtcNow();
            var type = state.MeetingTypes.Find(type => string.Equals(type.Id.Value, meetingTypeId, StringComparison.Ordinal));
            if (type is null) { return null; }
            return new(type.Duration.Value, now, state.Calendar.Meetings.Select(meeting => new OccupiedInterval(meeting.Start, meeting.End)).ToArray());
        }
    }
}
