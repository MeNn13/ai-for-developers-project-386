namespace BookingCalendar.Application.State;

// Встреча хранит снимок названия и длительности типа на момент бронирования
// (спека #23, решение #3): правки типа не переписывают историю.
internal sealed record StoredMeeting(
    Guid Id,
    string MeetingTypeId,
    string MeetingTypeTitle,
    int DurationMinutes,
    DateTimeOffset Start,
    DateTimeOffset End,
    string GuestName);
