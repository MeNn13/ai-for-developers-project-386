namespace BookingCalendar.Api.Contracts;

public sealed record SlotsResponse(string Timezone, SlotWindow Window, IReadOnlyList<SlotDay> Days);
