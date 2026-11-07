using Microsoft.AspNetCore.RateLimiting;

namespace RoadMate.Controllers;

[EnableRateLimiting("maps")]
public class TripRouteController(ITripRouteService svc) : ApiControllerBase
{
  /// <remarks>
  /// ⚠ CẦN CẤU HÌNH Goong:ApiKey. Tính (hoặc tính lại) lộ trình của chuyến từ điểm đi/điểm dừng/điểm đến + tuỳ chọn tránh cao tốc/phí đã lưu,
  /// rồi LƯU plannedPolyline (dùng phát hiện lệch tuyến). Không có body = lập lộ trình đầy đủ (cập nhật cả distanceKm/etaMinutes, chỉ chuyến nháp/đang chạy).
  /// Có body.from = tính lại từ vị trí hiện tại cho chuyến đang chạy (chỉ cập nhật plannedPolyline).
  /// </remarks>
  [HttpPost("trips/{id:guid}/route")]
  public async Task<IActionResult> Refresh(Guid id, RefreshRouteRequest? req, CancellationToken ct) =>
    Handle(await svc.RefreshAsync(UserId, id, req ?? new RefreshRouteRequest(null, null, null), ct));
}
