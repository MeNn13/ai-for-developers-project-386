namespace BookingCalendar.Api.Contracts;

// Описание ошибки в формате RFC 7807. Type — короткий slug из контракта,
// Errors — карта «поле -> сообщения», только у validation-failed.
public sealed record ApiProblem(
    string Type,
    string Title,
    int Status,
    string Detail,
    string Instance,
    IReadOnlyDictionary<string, string[]>? Errors = null);
