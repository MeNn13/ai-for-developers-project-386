using BookingCalendar.Api.Contracts;

namespace BookingCalendar.Api.Storage;

// ADR 0001: все обращения к данным идут под единым lock на объекте хранилища.
// Внутри критической секции — только работа с памятью, без await и ввода-вывода.
public sealed class CalendarStore
{
    private readonly List<MeetingType> _meetingTypes = [];

    public IReadOnlyList<MeetingType> ListMeetingTypes()
    {
        lock (this)
        {
            return [.. _meetingTypes];
        }
    }

    public void Add(MeetingType meetingType)
    {
        lock (this)
        {
            _meetingTypes.Add(meetingType);
        }
    }
}
