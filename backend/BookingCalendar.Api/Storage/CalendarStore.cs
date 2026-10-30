using BookingCalendar.Api.Contracts;
using BookingCalendar.Api.Domain;

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

// ADR 0001: все обращения к данным идут под единым lock на объекте хранилища.
// Внутри критической секции — только работа с памятью, без await и ввода-вывода.
public sealed class CalendarStore(TimeProvider timeProvider)
{
    private readonly List<MeetingType> _meetingTypes = [];
    private readonly List<Meeting> _meetings = [];

    public IReadOnlyList<MeetingType> ListMeetingTypes()
    {
        lock (this)
        {
            return [.. _meetingTypes];
        }
    }

    public MeetingType? FindMeetingType(string id)
    {
        lock (this)
        {
            return _meetingTypes.Find(type => string.Equals(type.Id, id, StringComparison.Ordinal));
        }
    }

    public bool TryAdd(MeetingType meetingType)
    {
        lock (this)
        {
            foreach (var existing in _meetingTypes)
            {
                if (string.Equals(existing.Id, meetingType.Id, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            _meetingTypes.Add(meetingType);
            return true;
        }
    }

    // ADR 0001: под одним lock — одно чтение now, повторная проверка правил слота,
    // проверка пересечения [start, end) и вставка встречи.
    public BookingResult TryBook(string meetingTypeId, DateTimeOffset start, string guestName)
    {
        lock (this)
        {
            var now = timeProvider.GetUtcNow();

            var meetingType = _meetingTypes.Find(type =>
                string.Equals(type.Id, meetingTypeId, StringComparison.Ordinal));
            if (meetingType is null)
            {
                return new BookingResult(BookingOutcome.MeetingTypeNotFound, null);
            }

            if (!SlotRules.IsBookable(start, meetingType.DurationMinutes, now))
            {
                return new BookingResult(BookingOutcome.SlotNotBookable, null);
            }

            var end = start.AddMinutes(meetingType.DurationMinutes);
            foreach (var existing in _meetings)
            {
                if (SlotRules.Overlaps(start, end, existing.Start, existing.End))
                {
                    return new BookingResult(BookingOutcome.SlotTaken, null);
                }
            }

            var meeting = new Meeting(
                Guid.NewGuid(),
                meetingType.Id,
                meetingType.Title,
                meetingType.DurationMinutes,
                start,
                end,
                guestName);
            _meetings.Add(meeting);
            return new BookingResult(BookingOutcome.Created, meeting);
        }
    }

    // Предстоящие встречи: идущая сейчас остаётся, потому что фильтр по end > now.
    public IReadOnlyList<Meeting> ListUpcomingMeetings()
    {
        lock (this)
        {
            var now = timeProvider.GetUtcNow();
            return [.. _meetings.Where(meeting => meeting.End > now).OrderBy(meeting => meeting.Start)];
        }
    }

    // Снимок занятости для правил слотов: интервалы встреч любых типов.
    public IReadOnlyList<OccupiedInterval> ListOccupiedIntervals()
    {
        lock (this)
        {
            return [.. _meetings.Select(meeting => new OccupiedInterval(meeting.Start, meeting.End))];
        }
    }
}
