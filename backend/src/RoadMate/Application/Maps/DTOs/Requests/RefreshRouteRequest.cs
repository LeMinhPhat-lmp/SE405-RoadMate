using RoadMate.Application.Shared;

namespace RoadMate.Application.Maps.DTOs.Requests;

// ---------- Trip route
/// <param name="From">Có = tính lại từ vị trí hiện tại (chỉ chuyến đang chạy), vd khi lệch tuyến.</param>
/// <param name="PassedWaypoints">Số điểm dừng đã đi qua (bỏ qua khi tính lại). Mặc định 0.</param>
public record RefreshRouteRequest(LatLng? From, int? PassedWaypoints, string? Vehicle);
