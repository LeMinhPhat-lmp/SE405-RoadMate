namespace RoadMate.Services.Trips;

public class TripLifecycleService(IUnitOfWork uow, IAlertService alerts, IOptions<TrackingOptions> tracking, TimeProvider clock,
  ILogger<TripLifecycleService> log) : ITripLifecycleService
{
  public async Task<Result<StartTripResponse>> StartAsync(Guid userId, Guid id, StartTripRequest req, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetOwnedWithDetailsAsync(id, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status != TripStatus.Draft) return Errors.TripNotDraft;
    if (await uow.TripRepository.HasActiveAsync(userId, ct)) return Error.Conflict("ANOTHER_TRIP_ACTIVE", "Bạn đang có một chuyến khác đang chạy.");

    var now = clock.UtcNow();
    if (req.ExpectedReturnTime is not null)
    {
      var t = req.ExpectedReturnTime.Value.ToUtc();
      if (t <= now) return Error.Validation("INVALID_RETURN_TIME", "Giờ dự kiến về phải ở tương lai.");
      trip.ExpectedReturnTime = t;
    }
    if (req.AlertContactIds is not null)
    {
      var contactIds = await uow.EmergencyContactRepository.ResolveOwnedIdsAsync(userId, req.AlertContactIds, ct);
      if (contactIds.IsFailure) return contactIds.Error;
      trip.SetRecipients(contactIds.Value);
    }
    if (req.LiveShare is not null) trip.LiveShare = req.LiveShare.Value;

    var warnings = new List<string>();
    if (await uow.EmergencyContactRepository.CountByUserAsync(userId, ct) == 0)
      warnings.Add("Bạn chưa có liên hệ khẩn cấp nào, cảnh báo sẽ không gửi được cho ai.");
    if (trip.ExpectedReturnTime is null) warnings.Add("Chưa đặt giờ dự kiến về nên sẽ không có cảnh báo về trễ.");

    trip.Start(now);
    await uow.SaveChangesAsync(ct);
    log.LogInformation("Chuyến {TripId} bắt đầu (user {UserId}, liveShare {LiveShare}, {Warnings} cảnh báo cấu hình)", id, userId, trip.LiveShare, warnings.Count);

    var dto = trip.ToDto(tracking.Value.BaseUrl);
    return new StartTripResponse(dto, dto.TrackingUrl, warnings);
  }

  public async Task<Result<TripSummaryDto>> FinishAsync(Guid userId, Guid id, CancellationToken ct)
  {
    var found = await uow.TripRepository.GetOwnedActiveAsync(id, userId, true, ct);
    if (found.IsFailure) return found.Error;
    var trip = found.Value;

    await uow.ExecuteInTransactionAsync(async token =>
    {
      var points = await uow.TripPointRepository.ListByTripAsync(id, token);
      var alertCount = await uow.AlertRepository.CountByTripAsync(id, token);
      trip.Stats ??= new TripStats { TripId = id };
      StatsCalculator.Fill(trip.Stats, points, alertCount);
      trip.Finish(clock.UtcNow(), TimeSpan.FromMinutes(tracking.Value.ExpireAfterFinishMinutes));
      await uow.SaveChangesAsync(token);
    }, ct);

    // Chuyến đã kết thúc = chủ chuyến an toàn: đóng mọi cảnh báo còn mở (và báo người thân nếu họ đã nhận tin).
    await alerts.CloseOpenAsync(id, null, AlertResolver.User, ct);
    log.LogInformation("Chuyến {TripId} kết thúc: {Km} km, {Alerts} cảnh báo", id, trip.Stats!.DistanceKm, trip.Stats.AlertCount);

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
    var found = await uow.TripRepository.GetOwnedActiveAsync(id, userId, true, ct);
    if (found.IsFailure) return found.Error;
    found.Value.RestUntil = clock.UtcNow().AddMinutes(minutes);
    await uow.SaveChangesAsync(ct);
    return found.Value.ToDto(tracking.Value.BaseUrl);
  }

  public async Task<Result<TripDto>> ClearRestAsync(Guid userId, Guid id, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetOwnedWithDetailsAsync(id, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    trip.RestUntil = null;
    await uow.SaveChangesAsync(ct);
    return trip.ToDto(tracking.Value.BaseUrl);
  }

  public async Task<Result<TripDto>> ExtendReturnAsync(Guid userId, Guid id, DateTime expectedReturnTime, CancellationToken ct)
  {
    var found = await uow.TripRepository.GetOwnedActiveAsync(id, userId, true, ct);
    if (found.IsFailure) return found.Error;
    var newTime = expectedReturnTime.ToUtc();
    if (newTime <= clock.UtcNow()) return Error.Validation("INVALID_RETURN_TIME", "Giờ dự kiến về phải ở tương lai.");

    found.Value.ExtendExpectedReturn(newTime);
    await uow.SaveChangesAsync(ct);
    await alerts.CloseOpenAsync(id, AlertType.LateReturn, AlertResolver.User, ct);   // chủ chuyến đã phản hồi -> đóng cảnh báo về trễ
    log.LogInformation("Chuyến {TripId} đổi giờ dự kiến về sang {ExpectedReturnTime:o}", id, newTime);
    return found.Value.ToDto(tracking.Value.BaseUrl);
  }
}
