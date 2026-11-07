using Microsoft.AspNetCore.RateLimiting;

namespace RoadMate.Controllers;

/// <remarks>Nguồn Open-Meteo (không cần key). Dự báo được cache theo ô lưới ~11 km trong Weather:CacheMinutes phút.</remarks>
[EnableRateLimiting("maps")]
public class WeatherController(IWeatherService svc) : ApiControllerBase
{
  /// <summary>Thời tiết hiện tại, dự báo vài giờ tới và cảnh báo (mưa to/gió mạnh/giông) tại một điểm.</summary>
  [HttpGet("weather")]
  public async Task<IActionResult> Get([FromQuery] double lat, [FromQuery] double lng, CancellationToken ct) =>
    Handle(await svc.GetAsync(lat, lng, ct));

  /// <summary>Thời tiết theo từng điểm trên tuyến (điểm đi, điểm dừng, điểm đến).</summary>
  [HttpGet("trips/{id:guid}/weather")]
  public async Task<IActionResult> ForTrip(Guid id, CancellationToken ct) => Handle(await svc.GetForTripAsync(UserId, id, ct));
}
