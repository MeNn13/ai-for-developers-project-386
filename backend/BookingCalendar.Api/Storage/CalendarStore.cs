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

// Снимок для правил слотов: длительность выбранного типа, момент чтения и
// занятые интервалы. Все части прочитаны под одним lock (ADR 0001).
public readonly record struct SlotSnapshot(
    int DurationMinutes,
    DateTimeOffset Now,
    IReadOnlyList<OccupiedInterval> Occupied);

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

    // Снимок слотов одним чтением: тип встречи, now и занятость под одним lock,
    // иначе ответ мог бы смешать состояния до и после параллельного бронирования.
    public SlotSnapshot? ReadSlotSnapshot(string meetingTypeId)
    {
        lock (this)
        {
            var now = timeProvider.GetUtcNow();
            var meetingType = _meetingTypes.Find(type =>
                string.Equals(type.Id, meetingTypeId, StringComparison.Ordinal));
            if (meetingType is null)
            {
                return null;
            }

            var occupied = _meetings
                .Select(meeting => new OccupiedInterval(meeting.Start, meeting.End))
                .ToArray();
            return new SlotSnapshot(meetingType.DurationMinutes, now, occupied);
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
}
