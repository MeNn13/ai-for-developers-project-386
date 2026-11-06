using System.Text.Json;
using BookingCalendar;

var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
var start = DateTimeOffset.Parse("2026-11-09T10:00:00+03:00", System.Globalization.CultureInfo.InvariantCulture);
var meetingType = new MeetingType { Id = "intro", Title = "Intro", Description = "", DurationMinutes = 30 };
var meeting = new Meeting { Id = "14888a80-4f05-4218-97f4-88f08aee86df", MeetingTypeId = "intro", MeetingTypeTitle = "Intro", DurationMinutes = 30, Start = start, End = start.AddMinutes(30), GuestName = "Guest" };
var samples = new Dictionary<string, object>
{
    ["HealthResponse"] = new HealthResponse(),
    ["MeetingType"] = meetingType,
    ["MeetingTypes"] = new[] { meetingType },
    ["Meeting"] = meeting,
    ["Meetings"] = new[] { meeting },
    ["MeetingCreateRequest"] = new MeetingCreateRequest { MeetingTypeId = "intro", Start = start, GuestName = "Guest" },
    ["MeetingCreatedResponse"] = new MeetingCreatedResponse { Body = meeting },
    ["MeetingTypeCreatedResponse"] = new MeetingTypeCreatedResponse { Body = meetingType },
    ["SlotsResponse"] = new SlotsResponse { Timezone = "Europe/Moscow", Window = new SlotWindow { From = start.Date, To = start.Date.AddDays(13) }, Days = Enumerable.Range(0, 14).Select(i => new SlotDay { Date = start.Date.AddDays(i), Slots = i == 0 ? [new Slot { Start = start, End = start.AddMinutes(30), Available = true }] : [] }).ToArray() },
};
foreach (var (slug, status) in new[] { ("validation-failed", 400), ("meeting-type-not-found", 404), ("slot-taken", 409), ("slot-not-bookable", 400), ("meeting-type-exists", 409) })
{
    var problem = new ProblemDetails { Type = slug, Title = "Error", Status = status, Detail = "Details", Instance = "/api/meetings" };
    if (slug == "validation-failed") problem.Errors = new Dictionary<string, string[]> { ["start"] = ["Invalid"] };
    samples[slug] = problem;
}
Directory.CreateDirectory("docs/experiments/server-models/artifacts");
File.WriteAllText("docs/experiments/server-models/artifacts/wire.json", JsonSerializer.Serialize(samples, options));
Console.WriteLine("Serialized " + samples.Count + " samples to artifacts/wire.json");
