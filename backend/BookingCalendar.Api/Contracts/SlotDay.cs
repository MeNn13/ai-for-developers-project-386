namespace BookingCalendar.Api.Contracts;

public sealed record SlotDay(DateOnly Date, IReadOnlyList<Slot> Slots);
