namespace RoadMate.Services.Implements;

public class TripService(IUnitOfWork uow, IOptions<TrackingOptions> tracking) : ITripService
{
  private static readonly string[] Sens = { "low", "medium", "high" };

  public async Task<Result<List<TripListItemDto>>> ListAsync(Guid userId, string? status, CancellationToken ct) =>
    (await uow.TripRepository.ListByOwnerAsync(userId, status, ct)).Select(t => t.ToListItem()).ToList();

  public async Task<Result<TripDto>> GetAsync(Guid userId, Guid id, CancellationToken ct)
  {
    var t = await uow.TripRepository.GetOwnedWithDetailsAsync(id, userId, ct);
    return t is null ? Errors.TripNotFound : t.ToDto(tracking.Value.BaseUrl);
  }

  public async Task<Result<TripDto>> CreateAsync(Guid userId, TripUpsertRequest req, CancellationToken ct)
  {
    if (await uow.UserRepository.GetByIdAsync(userId, false, ct) is null) return Errors.UserNotSynced;
    var v = Validate(req); if (v.IsFailure) return v.Error;
    var ids = await ResolveContactIds(userId, req.AlertContactIds, ct); if (ids.IsFailure) return ids.Error;

    var trip = new Trip { Id = Guid.NewGuid(), OwnerId = userId, Status = TripStatus.Draft };
    Apply(trip, req);
    SyncRecipients(trip, ids.Value);
    await uow.TripRepository.AddAsync(trip, ct);
    await uow.SaveChangesAsync(ct);
    return trip.ToDto(tracking.Value.BaseUrl);
  }

  public async Task<Result<TripDto>> UpdateAsync(Guid userId, Guid id, TripUpsertRequest req, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetOwnedWithDetailsAsync(id, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status != TripStatus.Draft) return Error.Conflict("TRIP_NOT_DRAFT", "Chỉ sửa được chuyến ở trạng thái nháp.");
    var v = Validate(req); if (v.IsFailure) return v.Error;
    var ids = await ResolveContactIds(userId, req.AlertContactIds, ct); if (ids.IsFailure) return ids.Error;
    Apply(trip, req);
    SyncRecipients(trip, ids.Value);
    await uow.SaveChangesAsync(ct);
    return trip.ToDto(tracking.Value.BaseUrl);
  }

  public async Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetOwnedAsync(id, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status == TripStatus.Active) return Error.Conflict("TRIP_ACTIVE", "Hãy kết thúc chuyến trước khi xoá.");
    uow.TripRepository.Delete(trip);
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }

  public async Task<Result<StartTripResponse>> StartAsync(Guid userId, Guid id, StartTripRequest req, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetOwnedWithDetailsAsync(id, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status != TripStatus.Draft) return Error.Conflict("TRIP_NOT_DRAFT", "Chuyến đã bắt đầu hoặc đã kết thúc.");
    if (await uow.TripRepository.HasActiveAsync(userId, ct)) return Error.Conflict("ANOTHER_TRIP_ACTIVE", "Bạn đang có một chuyến khác đang chạy.");

    var now = DateTime.UtcNow;
    if (req.ExpectedReturnTime is not null)
    {
      var t = req.ExpectedReturnTime.Value.ToUtc();
      if (t <= now) return Error.Validation("INVALID_RETURN_TIME", "Giờ dự kiến về phải ở tương lai.");
      trip.ExpectedReturnTime = t;
    }
    if (req.AlertContactIds is not null)
    {
      var ids = await ResolveContactIds(userId, req.AlertContactIds, ct); if (ids.IsFailure) return ids.Error;
      SyncRecipients(trip, ids.Value);
    }
    if (req.LiveShare is not null) trip.LiveShare = req.LiveShare.Value;

    var warnings = new List<string>();
    if (await uow.EmergencyContactRepository.CountByUserAsync(userId, ct) == 0)
      warnings.Add("Bạn chưa có liên hệ khẩn cấp nào, cảnh báo sẽ không gửi được cho ai.");
    if (trip.ExpectedReturnTime is null) warnings.Add("Chưa đặt giờ dự kiến về nên sẽ không có cảnh báo về trễ.");

    trip.Status = TripStatus.Active;
    trip.StartTime = now;
    if (trip.LiveShare) { trip.TrackingToken ??= TokenGenerator.NewTrackingToken(); trip.TrackingExpiresAt = null; }
    await uow.SaveChangesAsync(ct);

    var dto = trip.ToDto(tracking.Value.BaseUrl);
    return new StartTripResponse(dto, dto.TrackingUrl, warnings);
  }

  public async Task<Result<TripSummaryDto>> FinishAsync(Guid userId, Guid id, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetOwnedWithDetailsAsync(id, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status != TripStatus.Active) return Errors.TripNotActive;

    await uow.ExecuteInTransactionAsync(async token =>
    {
      var points = await uow.TripPointRepository.ListByTripAsync(id, token);
      var alertCount = await uow.AlertRepository.CountByTripAsync(id, token);
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
      await uow.SaveChangesAsync(token);
    }, ct);

    return new TripSummaryDto(trip.ToDto(tracking.Value.BaseUrl), trip.Stats!.ToDto());
  }

  public async Task<Result<TripSummaryDto>> SummaryAsync(Guid userId, Guid id, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetOwnedWithDetailsAsync(id, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status != TripStatus.Finished || trip.Stats is null) return Error.Conflict("TRIP_NOT_FINISHED", "Chuyến chưa kết thúc.");
    return new TripSummaryDto(trip.ToDto(tracking.Value.BaseUrl), trip.Stats.ToDto());
  }

  public async Task<Result<TripDto>> SetRestAsync(Guid userId, Guid id, int minutes, CancellationToken ct)
  {
    if (minutes is < 5 or > 480) return Error.Validation("INVALID_REST", "Thời gian nghỉ chân từ 5 đến 480 phút.");
    var trip = await uow.TripRepository.GetOwnedWithDetailsAsync(id, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status != TripStatus.Active) return Errors.TripNotActive;
    trip.RestUntil = DateTime.UtcNow.AddMinutes(minutes);
    await uow.SaveChangesAsync(ct);
    return trip.ToDto(tracking.Value.BaseUrl);
  }

  public async Task<Result<TripDto>> ClearRestAsync(Guid userId, Guid id, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetOwnedWithDetailsAsync(id, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    trip.RestUntil = null;
    await uow.SaveChangesAsync(ct);
    return trip.ToDto(tracking.Value.BaseUrl);
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

  private async Task<Result<List<Guid>>> ResolveContactIds(Guid userId, List<Guid>? ids, CancellationToken ct)
  {
    var distinct = (ids ?? new List<Guid>()).Distinct().ToList();
    if (distinct.Count == 0) return distinct;
    var found = await uow.EmergencyContactRepository.GetByIdsAsync(distinct, userId, ct);
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
