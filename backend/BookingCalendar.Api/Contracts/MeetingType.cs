namespace BookingCalendar.Api.Contracts;

public sealed record MeetingType(string Id, string Title, string Description, int DurationMinutes);
