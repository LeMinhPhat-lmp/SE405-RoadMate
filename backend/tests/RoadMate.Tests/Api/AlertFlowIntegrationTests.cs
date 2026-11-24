using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RoadMate.Application.Alerts;

namespace RoadMate.Tests.Api;

/// <summary>End-to-end over HTTP against real PostgreSQL. Scenario ids follow tests/TEST_SCENARIOS_INTEGRATION.md.</summary>
[Collection("api"), Trait("Category", "Integration")]
public class AlertFlowIntegrationTests(ApiFixture fx)
{
  private static string Uid() => "it-" + Guid.NewGuid().ToString("N")[..8];

  private static async Task<JsonElement> J(HttpResponseMessage r) => JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;

  private async Task<(HttpClient client, Guid tripId)> ActiveTripWithContactAsync()
  {
    var c = fx.ClientFor(Uid());
    Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("/api/v1/auth/sync", new { fcmToken = (string?)null })).StatusCode);
    // Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync("/api/v1/emergency-contacts", new { name = "Mẹ", phone = "0912345678" })).StatusCode);
    var contactResponse = await c.PostAsJsonAsync(
      "/api/v1/emergency-contacts",
      new { name = "Mẹ", phone = "0912345678" }
    );

    var contactBody = await contactResponse.Content.ReadAsStringAsync();

    Assert.True(
      contactResponse.StatusCode == HttpStatusCode.Created,
      $"Create emergency contact failed.\n" +
      $"Status: {(int)contactResponse.StatusCode} {contactResponse.StatusCode}\n" +
      $"Body: {contactBody}"
    );
    var trip = await J(await c.PostAsJsonAsync("/api/v1/trips", new
    {
      title = "T",
      origin = new { name = "A", lat = 10.0, lng = 106.0 },
      destination = new { name = "B", lat = 11.0, lng = 107.0 },
      avoidHighway = false,
      avoidToll = false,
      liveShare = false
    }));
    var id = trip.GetProperty("id").GetGuid();
    Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/v1/trips/{id}/start", new { })).StatusCode);
    return (c, id);
  }

  [Fact, Trait("Scenario", "EXTRA-T-HEALTH-01")]
  public async Task Health_is_anonymous() => Assert.Equal(HttpStatusCode.OK, (await fx.Factory.CreateClient().GetAsync("/health")).StatusCode);

  [Fact, Trait("Scenario", "T-AUTH-01")]
  public async Task Unsynced_user_gets_409()
  {
    var r = await fx.ClientFor(Uid()).GetAsync("/api/v1/trips");
    Assert.Equal(HttpStatusCode.Conflict, r.StatusCode); Assert.Equal("USER_NOT_SYNCED", (await J(r)).GetProperty("title").GetString());
  }

  [Fact, Trait("Scenario", "T-ALERT-01")]
  public async Task Crash_alert_is_queued_then_sent_by_the_outbox_and_repeats_are_idempotent()
  {
    var (c, tripId) = await ActiveTripWithContactAsync();
    var body = new { clientId = "it-" + Guid.NewGuid().ToString("N"), type = "crash", lat = 10.5, lng = 106.5, battery = 70 };
    var first = await c.PostAsJsonAsync($"/api/v1/trips/{tripId}/alerts", body);
    Assert.Equal(HttpStatusCode.Created, first.StatusCode);
    var dto = await J(first);
    Assert.Equal("pending", dto.GetProperty("status").GetString());
    Assert.Equal("queued", dto.GetProperty("deliveries")[0].GetProperty("status").GetString());

    var again = await J(await c.PostAsJsonAsync($"/api/v1/trips/{tripId}/alerts", body));
    Assert.Equal(dto.GetProperty("id").GetGuid(), again.GetProperty("id").GetGuid());

    using (var scope = fx.Scope()) await scope.ServiceProvider.GetRequiredService<IOutboxProcessor>().ProcessBatchAsync("it", default);
    var list = await J(await c.GetAsync($"/api/v1/trips/{tripId}/alerts"));
    Assert.Equal("sent", list[0].GetProperty("status").GetString());
    Assert.Equal("sent", list[0].GetProperty("deliveries")[0].GetProperty("status").GetString());
  }

  [Fact, Trait("Scenario", "T-ALERT-02")]
  public async Task Parallel_requests_with_the_same_clientId_create_exactly_one_alert()
  {
    var (c, tripId) = await ActiveTripWithContactAsync();
    var body = new { clientId = "race-" + Guid.NewGuid().ToString("N"), type = "sos", lat = 10.5, lng = 106.5, battery = 70 };
    var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => c.PostAsJsonAsync($"/api/v1/trips/{tripId}/alerts", body)));
    Assert.All(results, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
    var ids = (await Task.WhenAll(results.Select(J))).Select(j => j.GetProperty("id").GetGuid()).Distinct().ToList();
    Assert.Single(ids);
    var alerts = await J(await c.GetAsync($"/api/v1/trips/{tripId}/alerts"));
    Assert.Equal(JsonValueKind.Array, alerts.ValueKind);
    Assert.Single(alerts.EnumerateArray());
  }

  [Fact, Trait("Scenario", "EXTRA-T-ACK-01")]
  public async Task Public_ack_stops_the_contact_but_the_alert_stays_open()
  {
    var (c, tripId) = await ActiveTripWithContactAsync();
    var dto = await J(await c.PostAsJsonAsync($"/api/v1/trips/{tripId}/alerts", new { clientId = "ack-" + Guid.NewGuid().ToString("N"), type = "crash", lat = 10.5, lng = 106.5 }));
    string token;
    using (var scope = fx.Scope())
    {
      var db = scope.ServiceProvider.GetRequiredService<RoadMate.Infrastructure.Persistence.AppDbContext>();
      token = db.AlertAcks.Where(a => a.AlertId == dto.GetProperty("id").GetGuid()).Select(a => a.Token).Single();
    }
    var anon = fx.Factory.CreateClient();
    Assert.Equal(HttpStatusCode.OK, (await anon.PostAsync($"/api/v1/public/ack/{token}", null)).StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, (await anon.PostAsync("/api/v1/public/ack/nope", null)).StatusCode);
    Assert.Equal("pending", (await J(await c.GetAsync($"/api/v1/trips/{tripId}/alerts")))[0].GetProperty("status").GetString());
  }

  [Fact, Trait("Scenario", "EXTRA-T-TELE-01")]
  public async Task Newest_position_wins_even_when_batches_arrive_out_of_order()
  {
    var (c, tripId) = await ActiveTripWithContactAsync(); var t0 = DateTime.UtcNow;
    object P(double lat, int sec) => new { lat, lng = 106.0, speed = 30.0, battery = 80, recordedAt = t0.AddSeconds(sec) };
    Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsJsonAsync($"/api/v1/trips/{tripId}/points", new { points = new[] { P(11.0, 20) } })).StatusCode);
    Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsJsonAsync($"/api/v1/trips/{tripId}/points", new { points = new[] { P(10.0, 0), P(10.5, 10) } })).StatusCode);
    using var scope = fx.Scope();
    var db = scope.ServiceProvider.GetRequiredService<RoadMate.Infrastructure.Persistence.AppDbContext>();
    Assert.Equal(11.0, db.TripLives.Single(l => l.TripId == tripId).Lat);
    Assert.Equal(3, db.TripPoints.Count(p => p.TripId == tripId));
  }

  [Fact, Trait("Scenario", "EXTRA-T-OUTBOX-01")]
  public async Task Two_workers_never_claim_the_same_delivery()
  {
    var (c, tripId) = await ActiveTripWithContactAsync();
    for (var i = 0; i < 5; i++) await c.PostAsJsonAsync($"/api/v1/trips/{tripId}/alerts", new { clientId = $"par-{Guid.NewGuid():N}", type = "crash", lat = 10.5, lng = 106.5 });
    async Task<List<Guid>> Claim(string worker)
    {
      using var s = fx.Scope();
      var repo = s.ServiceProvider.GetRequiredService<RoadMate.Application.Abstractions.Persistence.INotificationDeliveryRepository>();
      return (await repo.ClaimDueAsync(worker, DateTimeOffset.UtcNow.AddMinutes(1), TimeSpan.FromMinutes(1), 100, default)).Select(d => d.Id).ToList();
    }
    var all = await Task.WhenAll(Claim("w1"), Claim("w2"), Claim("w3"));
    var flat = all.SelectMany(x => x).ToList();
    Assert.Equal(flat.Count, flat.Distinct().Count());       // no row claimed twice
  }
}
