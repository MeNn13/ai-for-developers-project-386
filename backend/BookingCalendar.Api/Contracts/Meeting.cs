namespace BookingCalendar.Api.Contracts;

// Встреча хранит снимок названия и длительности типа на момент брони
// (спека #23, решение #3): правки типа не переписывают историю.
public sealed record Meeting(
    Guid Id,
    string MeetingTypeId,
    string MeetingTypeTitle,
    int DurationMinutes,
    DateTimeOffset Start,
    DateTimeOffset End,
    string GuestName);
