namespace RoadMate.Services;

public interface ITripService
{
  Task<Result<List<TripListItemDto>>> ListAsync(string uid, string? status, CancellationToken ct);
  Task<Result<TripDto>> GetAsync(string uid, Guid id, CancellationToken ct);
  Task<Result<TripDto>> CreateAsync(string uid, TripUpsertRequest req, CancellationToken ct);
  Task<Result<TripDto>> UpdateAsync(string uid, Guid id, TripUpsertRequest req, CancellationToken ct);
  Task<Result> DeleteAsync(string uid, Guid id, CancellationToken ct);
  Task<Result<StartTripResponse>> StartAsync(string uid, Guid id, StartTripRequest req, CancellationToken ct);
  Task<Result<TripSummaryDto>> FinishAsync(string uid, Guid id, CancellationToken ct);
  Task<Result<TripSummaryDto>> SummaryAsync(string uid, Guid id, CancellationToken ct);
  Task<Result<TripDto>> SetRestAsync(string uid, Guid id, int minutes, CancellationToken ct);
  Task<Result<TripDto>> ClearRestAsync(string uid, Guid id, CancellationToken ct);
}

public class TripService : ITripService
{
  private static readonly string[] Sens = { "low", "medium", "high" };
  private readonly IUnitOfWork _uow; private readonly string _trackBase;
  public TripService(IUnitOfWork uow, IOptions<TrackingOptions> t) { _uow = uow; _trackBase = t.Value.BaseUrl; }

  public async Task<Result<List<TripListItemDto>>> ListAsync(string uid, string? status, CancellationToken ct) =>
    (await _uow.Trips.ListByOwnerAsync(uid, status, ct)).Select(t => t.ToListItem()).ToList();

  public async Task<Result<TripDto>> GetAsync(string uid, Guid id, CancellationToken ct)
  {
    var t = await _uow.Trips.GetOwnedWithDetailsAsync(id, uid, ct);
    return t is null ? Errors.TripNotFound : t.ToDto(_trackBase);
  }

  public async Task<Result<TripDto>> CreateAsync(string uid, TripUpsertRequest req, CancellationToken ct)
  {
    if (await _uow.Users.GetByUidAsync(uid, ct) is null) return Errors.UserNotSynced;
    var v = Validate(req); if (v.IsFailure) return v.Error;
    var ids = await ResolveContactIds(uid, req.AlertContactIds, ct); if (ids.IsFailure) return ids.Error;

    var trip = new Trip { Id = Guid.NewGuid(), OwnerId = uid, Status = TripStatus.Draft, CreatedAt = DateTime.UtcNow };
    Apply(trip, req);
    SyncRecipients(trip, ids.Value);
    await _uow.Trips.AddAsync(trip, ct);
    await _uow.SaveChangesAsync(ct);
    return trip.ToDto(_trackBase);
  }

  public async Task<Result<TripDto>> UpdateAsync(string uid, Guid id, TripUpsertRequest req, CancellationToken ct)
  {
    var trip = await _uow.Trips.GetOwnedWithDetailsAsync(id, uid, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status != TripStatus.Draft) return Error.Conflict("TRIP_NOT_DRAFT", "Chỉ sửa được chuyến ở trạng thái nháp.");
    var v = Validate(req); if (v.IsFailure) return v.Error;
    var ids = await ResolveContactIds(uid, req.AlertContactIds, ct); if (ids.IsFailure) return ids.Error;
    Apply(trip, req);
    SyncRecipients(trip, ids.Value);
    await _uow.SaveChangesAsync(ct);
    return trip.ToDto(_trackBase);
  }

  public async Task<Result> DeleteAsync(string uid, Guid id, CancellationToken ct)
  {
    var trip = await _uow.Trips.GetOwnedAsync(id, uid, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status == TripStatus.Active) return Error.Conflict("TRIP_ACTIVE", "Hãy kết thúc chuyến trước khi xoá.");
    _uow.Trips.Remove(trip);
    await _uow.SaveChangesAsync(ct);
    return Result.Success();
  }

  public async Task<Result<StartTripResponse>> StartAsync(string uid, Guid id, StartTripRequest req, CancellationToken ct)
  {
    var trip = await _uow.Trips.GetOwnedWithDetailsAsync(id, uid, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status != TripStatus.Draft) return Error.Conflict("TRIP_NOT_DRAFT", "Chuyến đã bắt đầu hoặc đã kết thúc.");
    if (await _uow.Trips.HasActiveAsync(uid, ct)) return Error.Conflict("ANOTHER_TRIP_ACTIVE", "Bạn đang có một chuyến khác đang chạy.");

    var now = DateTime.UtcNow;
    if (req.ExpectedReturnTime is not null)
    {
      var t = req.ExpectedReturnTime.Value.ToUtc();
      if (t <= now) return Error.Validation("INVALID_RETURN_TIME", "Giờ dự kiến về phải ở tương lai.");
      trip.ExpectedReturnTime = t;
    }
    if (req.AlertContactIds is not null)
    {
      var ids = await ResolveContactIds(uid, req.AlertContactIds, ct); if (ids.IsFailure) return ids.Error;
      SyncRecipients(trip, ids.Value);
    }
    if (req.LiveShare is not null) trip.LiveShare = req.LiveShare.Value;

    var warnings = new List<string>();
    if (await _uow.EmergencyContacts.CountByUserAsync(uid, ct) == 0)
      warnings.Add("Bạn chưa có liên hệ khẩn cấp nào, cảnh báo sẽ không gửi được cho ai.");
    if (trip.ExpectedReturnTime is null) warnings.Add("Chưa đặt giờ dự kiến về nên sẽ không có cảnh báo về trễ.");

    trip.Status = TripStatus.Active;
    trip.StartTime = now;
    if (trip.LiveShare) { trip.TrackingToken ??= TokenGenerator.NewTrackingToken(); trip.TrackingExpiresAt = null; }
    await _uow.SaveChangesAsync(ct);

    var dto = trip.ToDto(_trackBase);
    return new StartTripResponse(dto, dto.TrackingUrl, warnings);
  }

  public async Task<Result<TripSummaryDto>> FinishAsync(string uid, Guid id, CancellationToken ct)
  {
    var trip = await _uow.Trips.GetOwnedWithDetailsAsync(id, uid, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status != TripStatus.Active) return Errors.TripNotActive;

    await _uow.ExecuteInTransactionAsync(async token =>
    {
      var points = await _uow.TripPoints.ListByTripAsync(id, token);
      var alertCount = await _uow.Alerts.CountByTripAsync(id, token);
      var s = StatsCalculator.Compute(id, points, alertCount);
      if (trip.Stats is null) trip.Stats = s;
      else
      {
        trip.Stats.DistanceKm = s.DistanceKm; trip.Stats.MovingMin = s.MovingMin; trip.Stats.RestMin = s.RestMin;
        trip.Stats.StopCount = s.StopCount; trip.Stats.MaxSpeed = s.MaxSpeed; trip.Stats.AvgSpeed = s.AvgSpeed;
        trip.Stats.MaxAltitude = s.MaxAltitude; trip.Stats.AlertCount = s.AlertCount;
      }
      var now = DateTime.UtcNow;
      trip.Status = TripStatus.Finished; trip.EndTime = now; trip.RestUntil = null;
      if (trip.TrackingToken is not null) trip.TrackingExpiresAt = now;   // vô hiệu link theo dõi
      await _uow.SaveChangesAsync(token);
    }, ct);

    return new TripSummaryDto(trip.ToDto(_trackBase), trip.Stats!.ToDto());
  }

  public async Task<Result<TripSummaryDto>> SummaryAsync(string uid, Guid id, CancellationToken ct)
  {
    var trip = await _uow.Trips.GetOwnedWithDetailsAsync(id, uid, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status != TripStatus.Finished || trip.Stats is null) return Error.Conflict("TRIP_NOT_FINISHED", "Chuyến chưa kết thúc.");
    return new TripSummaryDto(trip.ToDto(_trackBase), trip.Stats.ToDto());
  }

  public async Task<Result<TripDto>> SetRestAsync(string uid, Guid id, int minutes, CancellationToken ct)
  {
    if (minutes is < 5 or > 480) return Error.Validation("INVALID_REST", "Thời gian nghỉ chân từ 5 đến 480 phút.");
    var trip = await _uow.Trips.GetOwnedWithDetailsAsync(id, uid, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status != TripStatus.Active) return Errors.TripNotActive;
    trip.RestUntil = DateTime.UtcNow.AddMinutes(minutes);
    await _uow.SaveChangesAsync(ct);
    return trip.ToDto(_trackBase);
  }

  public async Task<Result<TripDto>> ClearRestAsync(string uid, Guid id, CancellationToken ct)
  {
    var trip = await _uow.Trips.GetOwnedWithDetailsAsync(id, uid, ct);
    if (trip is null) return Errors.TripNotFound;
    trip.RestUntil = null;
    await _uow.SaveChangesAsync(ct);
    return trip.ToDto(_trackBase);
  }

  // ---------- helpers
  private static Result Validate(TripUpsertRequest r)
  {
    if (string.IsNullOrWhiteSpace(r.Title) || r.Title.Length > 200) return Error.Validation("INVALID_TITLE", "Tên chuyến bắt buộc (≤200 ký tự).");
    if (!Geo.ValidLatLng(r.Origin.Lat, r.Origin.Lng) || !Geo.ValidLatLng(r.Destination.Lat, r.Destination.Lng))
      return Error.Validation("INVALID_LOCATION", "Toạ độ điểm đi/đến không hợp lệ.");
    if ((r.Waypoints?.Count ?? 0) > 20) return Error.Validation("TOO_MANY_WAYPOINTS", "Tối đa 20 điểm dừng.");
    if (r.Waypoints?.Any(w => !Geo.ValidLatLng(w.Lat, w.Lng)) == true) return Error.Validation("INVALID_LOCATION", "Toạ độ điểm dừng không hợp lệ.");
    if (r.CrashSensitivity is not null && !Sens.Contains(r.CrashSensitivity)) return Error.Validation("INVALID_SENSITIVITY", "crashSensitivity phải là low|medium|high.");
    if (r.IdleMinutes is < 1 or > 240) return Error.Validation("INVALID_IDLE", "idleMinutes phải từ 1 đến 240.");
    return Result.Success();
  }

  private async Task<Result<List<Guid>>> ResolveContactIds(string uid, List<Guid>? ids, CancellationToken ct)
  {
    var distinct = (ids ?? new List<Guid>()).Distinct().ToList();
    if (distinct.Count == 0) return distinct;
    var found = await _uow.EmergencyContacts.GetByIdsAsync(distinct, uid, ct);
    return found.Count == distinct.Count ? distinct : Error.Validation("INVALID_CONTACT", "Có liên hệ không thuộc về bạn.");
  }

  private static void Apply(Trip t, TripUpsertRequest r)
  {
    t.Title = r.Title.Trim();
    t.OriginName = r.Origin.Name; t.OriginLat = r.Origin.Lat; t.OriginLng = r.Origin.Lng;
    t.DestName = r.Destination.Name; t.DestLat = r.Destination.Lat; t.DestLng = r.Destination.Lng;
    t.PlannedPolyline = r.PlannedPolyline; t.DistanceKm = r.DistanceKm ?? 0; t.EtaMinutes = r.EtaMinutes ?? 0;
    t.AvoidHighway = r.AvoidHighway; t.AvoidToll = r.AvoidToll;
    t.ExpectedReturnTime = r.ExpectedReturnTime.ToUtc();
    t.IdleMinutes = r.IdleMinutes; t.CrashSensitivity = r.CrashSensitivity; t.LiveShare = r.LiveShare;

    t.Waypoints.Clear();
    int seq = 0;
    foreach (var w in r.Waypoints ?? new List<WaypointInput>())
      t.Waypoints.Add(new TripWaypoint { Id = Guid.NewGuid(), TripId = t.Id, Seq = seq++, Name = w.Name, Lat = w.Lat, Lng = w.Lng, PlannedStayMin = w.PlannedStayMin });
  }

  private static void SyncRecipients(Trip t, List<Guid> ids)
  {
    foreach (var r in t.Recipients.Where(r => !ids.Contains(r.ContactId)).ToList()) t.Recipients.Remove(r);
    foreach (var id in ids.Where(id => t.Recipients.All(r => r.ContactId != id)))
      t.Recipients.Add(new TripAlertRecipient { TripId = t.Id, ContactId = id });
  }
}
