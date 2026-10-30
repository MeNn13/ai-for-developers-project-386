using Microsoft.AspNetCore.Mvc.Testing;

namespace BookingCalendar.Api.Tests;

// Единое приложение для HTTP-тестов: на нём живут поведенческие тесты и conformance.
// Подмена времени через FakeTimeProvider появится вместе с задачами о времени (#25).
public sealed class TestApp : WebApplicationFactory<Program>
{
}
