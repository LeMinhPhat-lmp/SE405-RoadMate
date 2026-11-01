namespace RoadMate.Services.Trips;

public class TripLifecycleService(IUnitOfWork uow, IOptions<TrackingOptions> tracking) : ITripLifecycleService
{
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
      trip.Finish(DateTime.UtcNow);
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
    var found = await uow.TripRepository.GetOwnedActiveAsync(id, userId, true, ct);
    if (found.IsFailure) return found.Error;
    found.Value.RestUntil = DateTime.UtcNow.AddMinutes(minutes);
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
}
