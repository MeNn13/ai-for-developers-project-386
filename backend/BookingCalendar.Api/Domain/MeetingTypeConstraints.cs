namespace BookingCalendar.Api.Domain;

// Ограничения длительности типа встречи из контракта (MeetingTypeCreateRequest):
// 15–480 минут шагом 5. Одна точка для валидации API и правил слота.
public static class MeetingTypeConstraints
{
    public const int DurationMinMinutes = 15;
    public const int DurationMaxMinutes = 480;
    public const int DurationStepMinutes = 5;
}
