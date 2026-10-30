namespace BookingCalendar.Api.Contracts;

public sealed record Slot(DateTimeOffset Start, DateTimeOffset End, bool Available);
