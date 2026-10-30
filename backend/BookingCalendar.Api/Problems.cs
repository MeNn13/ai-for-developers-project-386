using System.Text.Json;
using System.Text.Json.Serialization;

using BookingCalendar.Api.Contracts;

namespace BookingCalendar.Api;

// Все ошибки API отдаются как application/problem+json по схеме ProblemDetails
// из контракта: type — короткий slug, title и detail — русский текст для человека.
internal static class Problems
{
    private static readonly JsonSerializerOptions s_problemJsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static IResult ValidationFailed(
        string instance,
        string detail,
        IReadOnlyDictionary<string, string[]>? errors = null) =>
        Create("validation-failed", "Запрос некорректен", StatusCodes.Status400BadRequest, instance, detail, errors);

    public static IResult NotFound(string instance) =>
        Create("not-found", "Не найдено", StatusCodes.Status404NotFound, instance, "Маршрут API не существует.");

    public static IResult MeetingTypeNotFound(string instance) =>
        Create("meeting-type-not-found", "Не найдено", StatusCodes.Status404NotFound, instance, "Тип встречи с таким id не найден.");

    public static IResult MeetingTypeExists(string instance) =>
        Create("meeting-type-exists", "Конфликт", StatusCodes.Status409Conflict, instance, "Тип встречи с таким id уже существует.");

    private static IResult Create(
        string slug,
        string title,
        int statusCode,
        string instance,
        string detail,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        var problem = new ApiProblem(slug, title, statusCode, detail, instance, errors);
        return Results.Json(problem, s_problemJsonOptions, "application/problem+json", statusCode);
    }
}
