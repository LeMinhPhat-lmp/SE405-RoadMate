namespace RoadMate.Services.Implements;

public class TrackingService(IUnitOfWork uow, IOptions<TrackingOptions> tracking) : ITrackingService
{
  private const int MaxBatch = 500;

  public async Task<Result> AddPointsAsync(Guid userId, Guid tripId, AddPointsRequest req, CancellationToken ct)
  {
    if (req.Points.Count == 0) return Result.Success();
    if (req.Points.Count > MaxBatch) return Error.Validation("BATCH_TOO_LARGE", $"Tối đa {MaxBatch} điểm mỗi lần gửi.");
    if (req.Points.Any(p => !Geo.ValidLatLng(p.Lat, p.Lng))) return Error.Validation("INVALID_LOCATION", "Có toạ độ không hợp lệ.");

    var trip = await LoadActiveWithLive(userId, tripId, ct);
    if (trip.IsFailure) return trip.Error;

    var pts = req.Points.Select(p => new TripPoint
    {
      TripId = tripId,
      Lat = p.Lat,
      Lng = p.Lng,
      Speed = p.Speed,
      Altitude = p.Altitude,
      Battery = p.Battery,
      RecordedAt = p.RecordedAt.ToUtc()
    }).ToList();
    await uow.TripPointRepository.AddRangeAsync(pts, ct);

    var last = pts.OrderBy(p => p.RecordedAt).Last();
    UpsertLive(trip.Value, last.Lat, last.Lng, last.Speed, last.Battery, true, DateTime.UtcNow);
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }

  public async Task<Result> HeartbeatAsync(Guid userId, Guid tripId, HeartbeatRequest req, CancellationToken ct)
  {
    if (!Geo.ValidLatLng(req.Lat, req.Lng)) return Error.Validation("INVALID_LOCATION", "Toạ độ không hợp lệ.");
    var trip = await LoadActiveWithLive(userId, tripId, ct);
    if (trip.IsFailure) return trip.Error;
    UpsertLive(trip.Value, req.Lat, req.Lng, req.Speed, req.Battery, req.NetworkOk, DateTime.UtcNow);
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }

  public async Task<Result<TrackingLinkDto>> CreateLinkAsync(Guid userId, Guid tripId, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetOwnedAsync(tripId, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status == TripStatus.Finished) return Error.Conflict("TRIP_FINISHED", "Chuyến đã kết thúc.");
    trip.TrackingToken = TokenGenerator.NewTrackingToken();   // tạo mới = đổi link, link cũ mất hiệu lực
    trip.TrackingExpiresAt = null; trip.LiveShare = true;
    await uow.SaveChangesAsync(ct);
    return new TrackingLinkDto($"{tracking.Value.BaseUrl.TrimEnd('/')}/{trip.TrackingToken}", trip.TrackingToken);
  }

  public async Task<Result> DisableLinkAsync(Guid userId, Guid tripId, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetOwnedAsync(tripId, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    trip.TrackingToken = null; trip.TrackingExpiresAt = null; trip.LiveShare = false;
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }

  public async Task<Result<PublicTrackDto>> GetPublicAsync(string token, CancellationToken ct)
  {
    var trip = await FindPublicTrip(token, ct);
    if (trip is null) return Error.NotFound("LINK_INVALID", "Link theo dõi không tồn tại hoặc đã hết hạn.");
    var open = (await uow.AlertRepository.ListOpenByTripAsync(trip.Id, ct))
        .Select(a => new PublicAlertDto(a.Type, a.Status, a.Lat, a.Lng, a.CreatedAt)).ToList();
    var live = trip.Live is null ? null : new PublicLiveDto(trip.Live.Lat, trip.Live.Lng, trip.Live.Speed, trip.Live.Battery, trip.Live.NetworkOk, trip.Live.UpdatedAt);
    return new PublicTrackDto(trip.Owner?.Name ?? "", trip.Title, trip.Status, new LocationDto(trip.DestName, trip.DestLat, trip.DestLng), live, open);
  }

  public async Task<Result<PublicRouteDto>> GetPublicRouteAsync(string token, CancellationToken ct)
  {
    var trip = await FindPublicTrip(token, ct);
    if (trip is null) return Error.NotFound("LINK_INVALID", "Link theo dõi không tồn tại hoặc đã hết hạn.");
    var pts = await uow.TripPointRepository.ListRecentAsync(trip.Id, 2000, ct);
    return new PublicRouteDto(trip.PlannedPolyline, pts.Select(p => new LatLng(p.Lat, p.Lng)).ToList());
  }

  public async Task<Result> PublicAckAsync(string token, CancellationToken ct)
  {
    var trip = await FindPublicTrip(token, ct);
    if (trip is null) return Error.NotFound("LINK_INVALID", "Link theo dõi không tồn tại hoặc đã hết hạn.");
    foreach (var a in await uow.AlertRepository.ListOpenByTripAsync(trip.Id, ct))
    { a.Status = AlertStatus.Resolved; a.ResolvedAt = DateTime.UtcNow; a.ResolvedBy = "contact"; a.NextRepeatAt = null; }
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }

  private async Task<Trip?> FindPublicTrip(string token, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(token) || token.Length > 64) return null;
    var t = await uow.TripRepository.GetByTrackingTokenAsync(token, ct);
    if (t is null || (t.TrackingExpiresAt != null && t.TrackingExpiresAt <= DateTime.UtcNow)) return null;
    return t;
  }

  private async Task<Result<Trip>> LoadActiveWithLive(Guid userId, Guid tripId, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetOwnedWithDetailsAsync(tripId, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status != TripStatus.Active) return Errors.TripNotActive;
    return trip;
  }

  private static void UpsertLive(Trip trip, double lat, double lng, double? speed, int? battery, bool net, DateTime now)
  {
    var live = trip.Live ??= new TripLive { TripId = trip.Id };
    live.Lat = lat; live.Lng = lng; live.Speed = speed; live.Battery = battery ?? live.Battery; live.NetworkOk = net; live.UpdatedAt = now;
  }
}
