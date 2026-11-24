using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Common;
using RoadMate.Application.Tracking;
using RoadMate.Application.Trips.DTOs.Requests;
using RoadMate.Application.Trips.DTOs.Responses;
using RoadMate.Application.Trips.Interfaces;
using RoadMate.Application.Trips.Mappers;
using RoadMate.Domain.Common;
using RoadMate.Domain.Trips;

namespace RoadMate.Application.Trips.Implements;

/// <summary>Orchestration only: state rules live in <c>Trip</c>; closing the trip's alerts happens in an event handler inside the same transaction.</summary>
public class TripLifecycleService(ITripRepository trips, IEmergencyContactRepository contacts, IUnitOfWork uow, ITripPointReader points, IAlertRepository alerts,
  IOptions<TrackingOptions> tracking, TimeProvider clock, ILogger<TripLifecycleService> log) : ITripLifecycleService
{
  private string BaseUrl => tracking.Value.BaseUrl;

  public async Task<Result<StartTripResponse>> StartAsync(Guid userId, Guid id, StartTripRequest req, CancellationToken ct)
  {
    var trip = await trips.GetOwnedWithDetailsAsync(id, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status != TripStatus.Draft) return Errors.TripNotDraft;
    if (await trips.HasActiveAsync(userId, ct)) return Error.Conflict("ANOTHER_TRIP_ACTIVE", "Bạn đang có một chuyến khác đang chạy.");

    if (req.AlertContactIds is not null)
    {
      var ids = await contacts.ResolveOwnedContactIdsAsync(userId, req.AlertContactIds, ct);
      if (ids.IsFailure) return ids.Error;
      trip.SetRecipients(ids.Value);
    }
    var started = trip.Start(clock.GetUtcNow(), req.ExpectedReturnTime.ToUtcOffset(), req.LiveShare);
    if (started.IsFailure) return started.Error;

    var warnings = new List<string>();
    if ((await contacts.ListForUserAsync(userId, ct)).Count == 0) warnings.Add("Bạn chưa có liên hệ khẩn cấp nào, cảnh báo sẽ không gửi được cho ai.");
    if (trip.ExpectedReturnTime is null) warnings.Add("Chưa đặt giờ dự kiến về nên sẽ không có cảnh báo về trễ.");
    await uow.SaveChangesAsync(ct);
    log.LogInformation("Chuyến {TripId} bắt đầu (user {UserId}, liveShare {LiveShare}, {Warnings} cảnh báo cấu hình)", id, userId, trip.LiveShare, warnings.Count);
    var dto = trip.ToDto(BaseUrl);
    return new StartTripResponse(dto, dto.TrackingUrl, warnings);
  }

  public async Task<Result<TripSummaryResponse>> FinishAsync(Guid userId, Guid id, CancellationToken ct)
  {
    var found = await trips.GetOwnedActiveAsync(id, userId, true, ct);
    if (found.IsFailure) return found.Error;
    var trip = found.Value;
    var samples = await points.ListAsync(id, ct);
    var stats = TripStatistics.Compute(samples, await alerts.CountForTripAsync(id, ct));
    var finished = trip.Finish(clock.GetUtcNow(), TimeSpan.FromMinutes(tracking.Value.ExpireAfterFinishMinutes), stats);
    if (finished.IsFailure) return finished.Error;
    await uow.SaveChangesAsync(ct);   // TripFinished handler closes open alerts in this same transaction
    log.LogInformation("Chuyến {TripId} kết thúc: {Km} km, {Alerts} cảnh báo", id, stats.DistanceKm, stats.AlertCount);
    return new TripSummaryResponse(trip.ToDto(BaseUrl), trip.Stats!.ToDto());
  }

  public async Task<Result<TripSummaryResponse>> SummaryAsync(Guid userId, Guid id, CancellationToken ct)
  {
    var trip = await trips.GetOwnedWithDetailsAsync(id, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status != TripStatus.Finished || trip.Stats is null) return Error.Conflict("TRIP_NOT_FINISHED", "Chuyến chưa kết thúc.");
    return new TripSummaryResponse(trip.ToDto(BaseUrl), trip.Stats.ToDto());
  }

  public Task<Result<TripResponse>> SetRestAsync(Guid userId, Guid id, int minutes, CancellationToken ct) =>
    MutateActiveAsync(userId, id, t => t.SetRest(minutes, clock.GetUtcNow()), ct);

  public Task<Result<TripResponse>> ClearRestAsync(Guid userId, Guid id, CancellationToken ct) =>
    MutateActiveAsync(userId, id, t => t.ClearRest(), ct);

  /// <summary>Closing open <c>lateReturn</c> alerts is done by the ExpectedReturnExtended handler (same transaction).</summary>
  public Task<Result<TripResponse>> ExtendReturnAsync(Guid userId, Guid id, DateTime expectedReturnTime, CancellationToken ct) =>
    MutateActiveAsync(userId, id, t => t.ExtendExpectedReturn(expectedReturnTime.ToUtcOffset(), clock.GetUtcNow()), ct);

  private async Task<Result<TripResponse>> MutateActiveAsync(Guid userId, Guid id, Func<Trip, Result> change, CancellationToken ct)
  {
    var found = await trips.GetOwnedActiveAsync(id, userId, true, ct);
    if (found.IsFailure) return found.Error;
    var r = change(found.Value);
    if (r.IsFailure) return r.Error;
    await uow.SaveChangesAsync(ct);
    return found.Value.ToDto(BaseUrl);
  }
}
