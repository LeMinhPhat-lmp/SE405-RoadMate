namespace RoadMate.Services.Trips;

public class TripService(IUnitOfWork uow, IOptions<TrackingOptions> tracking) : ITripService
{
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
    var valid = TripUpsertRequestValidator.Validate(req);
    if (valid.IsFailure) return valid.Error;
    var contactIds = await uow.EmergencyContactRepository.ResolveOwnedIdsAsync(userId, req.AlertContactIds, ct);
    if (contactIds.IsFailure) return contactIds.Error;

    var trip = new Trip { Id = Guid.NewGuid(), OwnerId = userId, Status = TripStatus.Draft };
    trip.ApplyFrom(req);
    trip.SetRecipients(contactIds.Value);
    await uow.TripRepository.AddAsync(trip, ct);
    await uow.SaveChangesAsync(ct);
    return trip.ToDto(tracking.Value.BaseUrl);
  }

  public async Task<Result<TripDto>> UpdateAsync(Guid userId, Guid id, TripUpsertRequest req, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetOwnedWithDetailsAsync(id, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status != TripStatus.Draft) return Errors.TripNotDraft;
    var valid = TripUpsertRequestValidator.Validate(req);
    if (valid.IsFailure) return valid.Error;
    var contactIds = await uow.EmergencyContactRepository.ResolveOwnedIdsAsync(userId, req.AlertContactIds, ct);
    if (contactIds.IsFailure) return contactIds.Error;

    trip.ApplyFrom(req);
    trip.SetRecipients(contactIds.Value);
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
}
