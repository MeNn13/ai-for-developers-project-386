using BookingCalendar.Api.Contracts;
using BookingCalendar.Api.Domain;
using BookingCalendar.Application.State;

namespace BookingCalendar.Api.Storage;

public enum BookingOutcome
{
    Created,
    MeetingTypeNotFound,
    SlotNotBookable,
    SlotTaken,
}

// Результат бронирования: исход и созданная встреча (только у Created).
public readonly record struct BookingResult(BookingOutcome Outcome, Meeting? Meeting);

// Снимок для правил слотов: длительность выбранного типа, момент чтения и
// занятые интервалы. Все части прочитаны под одним lock (ADR 0001).
public readonly record struct SlotSnapshot(
    int DurationMinutes,
    DateTimeOffset Now,
    IReadOnlyList<OccupiedInterval> Occupied);

// ADR 0001: все обращения к данным идут под единым lock на объекте хранилища.
// Внутри критической секции — только работа с памятью, без await и ввода-вывода.
internal sealed class CalendarStore(TimeProvider timeProvider, CalendarState state)
{
    // Temporary bridge for #39 and #40; types and synchronization belong to Application.


    // Снимок слотов одним чтением: тип встречи, now и занятость под одним lock,
    // иначе ответ мог бы смешать состояния до и после параллельного бронирования.
    public SlotSnapshot? ReadSlotSnapshot(string meetingTypeId)
    {
        lock (state.Gate)
        {
            var now = timeProvider.GetUtcNow();
            var meetingType = state.MeetingTypes.Find(type =>
                string.Equals(type.Id.Value, meetingTypeId, StringComparison.Ordinal));
            if (meetingType is null)
            {
                return null;
            }

            var occupied = state.Meetings
                .Select(meeting => new OccupiedInterval(meeting.Start, meeting.End))
                .ToArray();
            return new SlotSnapshot(meetingType.Duration.Value, now, occupied);
        }
    }

    // ADR 0001: под одним lock — одно чтение now, повторная проверка правил слота,
    // проверка пересечения [start, end) и вставка встречи.
    public BookingResult TryBook(string meetingTypeId, DateTimeOffset start, string guestName)
    {
        lock (state.Gate)
        {
            var now = timeProvider.GetUtcNow();

            var meetingType = state.MeetingTypes.Find(type =>
                string.Equals(type.Id.Value, meetingTypeId, StringComparison.Ordinal));
            if (meetingType is null)
            {
                return new BookingResult(BookingOutcome.MeetingTypeNotFound, null);
            }

            if (!SlotRules.IsBookable(start, meetingType.Duration.Value, now))
            {
                return new BookingResult(BookingOutcome.SlotNotBookable, null);
            }

            var end = start.AddMinutes(meetingType.Duration.Value);
            foreach (var existing in state.Meetings)
            {
                if (SlotRules.Overlaps(start, end, existing.Start, existing.End))
                {
                    return new BookingResult(BookingOutcome.SlotTaken, null);
                }
            }

            var meeting = new StoredMeeting(
                Guid.NewGuid(),
                meetingType.Id.Value,
                meetingType.Title.Value,
                meetingType.Duration.Value,
                start,
                end,
                guestName);
            state.Meetings.Add(meeting);
            return new BookingResult(BookingOutcome.Created, ToContract(meeting));
        }
    }

    // Предстоящие встречи: идущая сейчас остаётся, потому что фильтр по end > now.
    public IReadOnlyList<Meeting> ListUpcomingMeetings()
    {
        lock (state.Gate)
        {
            var now = timeProvider.GetUtcNow();
            return [.. state.Meetings.Where(meeting => meeting.End > now).OrderBy(meeting => meeting.Start).Select(ToContract)];
        }
    }
    private static Meeting ToContract(StoredMeeting meeting) =>
        new(meeting.Id, meeting.MeetingTypeId, meeting.MeetingTypeTitle, meeting.DurationMinutes,
            meeting.Start, meeting.End, meeting.GuestName);
}
