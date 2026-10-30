using System.Net;
using System.Text.Json;

using BookingCalendar.Api.Domain;

using Microsoft.AspNetCore.Http;

namespace BookingCalendar.Api.Tests;

// Р”РѕРєР°Р·Р°С‚РµР»СЊСЃС‚РІРѕ Р°С‚РѕРјР°СЂРЅРѕСЃС‚Рё РёР· ADR 0001 Рё Testing Decisions #6: 10 РѕРґРЅРѕРІСЂРµРјРµРЅРЅС‹С…
// POST РЅР° РѕРґРёРЅ СЃС‚Р°СЂС‚, СЂРѕРІРЅРѕ РѕРґРёРЅ 201, РґРµРІСЏС‚СЊ 409 slot-taken, РЅРё РѕРґРЅРѕРіРѕ 500;
// С„РёРЅР°Р»СЊРЅС‹Р№ GET РїРѕРєР°Р·С‹РІР°РµС‚ СЂРѕРІРЅРѕ РѕРґРЅСѓ РІСЃС‚СЂРµС‡Сѓ СЌС‚РѕРіРѕ СЂР°СѓРЅРґР°. 20 СЂР°СѓРЅРґРѕРІ, РІ РєР°Р¶РґРѕРј
// СЃРІРµР¶РёР№ С‚РёРї РІСЃС‚СЂРµС‡Рё Рё СЃРІРѕР№ СЃС‚Р°СЂС‚. Р‘РµР· Thread.Sleep Рё Task.Delay.
public class BookingAtomicityTests(TestApp app) : IClassFixture<TestApp>
{
    private const int ParallelRequests = 10;
    private const int Rounds = 20;

    [Fact]
    public async Task ConcurrentBookingsOnOneSlotCreateExactlyOneMeeting()
    {
        app.SetNow(MeetingHttp.Moscow(2, 8, 0));
        using var client = app.CreateClient();

        for (var round = 0; round < Rounds; round++)
        {
            var typeId = $"race-{round}";
            using (var typeResponse = await MeetingHttp.CreateMeetingTypeAsync(client, typeId, "Р“РѕРЅРєР°", 30))
            {
                typeResponse.EnsureSuccessStatusCode();
            }

            var start = RoundStart(round);
            var responses = await Task.WhenAll(Enumerable.Range(0, ParallelRequests)
                .Select(_ => MeetingHttp.CreateMeetingAsync(client, typeId, start, "Р“РѕРЅРєР°")));

            try
            {
                var statuses = responses.Select(response => (int)response.StatusCode).ToArray();
                Assert.Equal(1, statuses.Count(status => status == StatusCodes.Status201Created));
                Assert.Equal(
                    ParallelRequests - 1,
                    statuses.Count(status => status == StatusCodes.Status409Conflict));
                Assert.DoesNotContain(StatusCodes.Status500InternalServerError, statuses);

                foreach (var conflict in responses.Where(response =>
                    response.StatusCode == HttpStatusCode.Conflict))
                {
                    await ProblemAssertions.AssertProblem(conflict, "slot-taken");
                }

                var meetings = await ListMeetingsAsync(client);
                Assert.Single(meetings, meeting =>
                    meeting.GetProperty("meetingTypeId").GetString() == typeId);
                Assert.Equal(round + 1, meetings.Count);
            }
            finally
            {
                foreach (var response in responses)
                {
                    response.Dispose();
                }
            }
        }
    }

    // РЎС‚Р°СЂС‚С‹ РёРґСѓС‚ РїРѕ СЃРµС‚РєРµ: 16 СЃС‚Р°СЂС‚РѕРІ РІ РґРµРЅСЊ, РґР°Р»СЊС€Рµ СЃР»РµРґСѓСЋС‰РёР№ СЂР°Р±РѕС‡РёР№ РґРµРЅСЊ.
    private static DateTimeOffset RoundStart(int round)
    {
        var day = 2 + (round / SlotRules.GridStartsPerDay);
        var minutesFromStart = (round % SlotRules.GridStartsPerDay) * SlotRules.GridStepMinutes;
        return MeetingHttp.Moscow(
            day,
            SlotRules.GridStartHour + (minutesFromStart / 60),
            minutesFromStart % 60);
    }

    private static async Task<IReadOnlyList<JsonElement>> ListMeetingsAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/meetings");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return [.. document.RootElement.EnumerateArray().Select(meeting => meeting.Clone())];
    }
}
