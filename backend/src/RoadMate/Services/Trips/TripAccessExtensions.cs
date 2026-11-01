namespace RoadMate.Services.Trips;

public static class TripAccessExtensions
{
  /// <summary>
  /// Chuyến của <paramref name="userId"/> đang ở trạng thái Active (dùng cho GPS, heartbeat, cảnh báo, nghỉ chân, kết thúc chuyến).
  /// Lỗi: TRIP_NOT_FOUND nếu không tồn tại/không thuộc user, TRIP_NOT_ACTIVE nếu chưa bắt đầu hoặc đã kết thúc.
  /// </summary>
  public static async Task<Result<Trip>> GetOwnedActiveAsync(this ITripRepository trips, Guid tripId, Guid userId, bool withDetails, CancellationToken ct)
  {
    var trip = withDetails
      ? await trips.GetOwnedWithDetailsAsync(tripId, userId, ct)
      : await trips.GetOwnedAsync(tripId, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    return trip.Status == TripStatus.Active ? trip : Errors.TripNotActive;
  }
}
