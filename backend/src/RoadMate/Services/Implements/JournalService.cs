namespace RoadMate.Services.Implements;

public class JournalService(IUnitOfWork uow) : IJournalService
{
  public async Task<Result<JournalDto>> GetJournalAsync(Guid userId, Guid tripId, int tz, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetOwnedWithDetailsAsync(tripId, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    var memories = await uow.MemoryRepository.ListByTripAsync(tripId, ct);
    var days = memories.GroupBy(m => DateOnly.FromDateTime(m.TakenAt.AddHours(tz)))
      .OrderBy(g => g.Key)
      .Select(g => new JournalDayDto(g.Key, g.OrderBy(m => m.TakenAt).Select(m => m.ToDto()).ToList())).ToList();
    return new JournalDto(trip.Id, trip.Title, trip.Status, trip.Stats?.ToDto(),
      trip.Waypoints.OrderBy(w => w.Seq).Select(w => w.ToDto()).ToList(), days);
  }

  public async Task<Result<List<MemoryDto>>> ListMemoriesAsync(Guid userId, Guid tripId, CancellationToken ct)
  {
    if (await uow.TripRepository.GetOwnedAsync(tripId, userId, ct) is null) return Errors.TripNotFound;
    return (await uow.MemoryRepository.ListByTripAsync(tripId, ct)).Select(m => m.ToDto()).ToList();
  }

  public async Task<Result<MemoryDto>> AddMemoryAsync(Guid userId, Guid tripId, MemoryRequest req, CancellationToken ct)
  {
    if (await uow.TripRepository.GetOwnedAsync(tripId, userId, ct) is null) return Errors.TripNotFound;
    var v = Validate(req); if (v.IsFailure) return v.Error;
    var m = new Memory { Id = Guid.NewGuid(), TripId = tripId };
    Apply(m, req);
    await uow.MemoryRepository.AddAsync(m, ct);
    await uow.SaveChangesAsync(ct);
    return m.ToDto();
  }

  public async Task<Result<MemoryDto>> UpdateMemoryAsync(Guid userId, Guid memoryId, MemoryRequest req, CancellationToken ct)
  {
    var m = await uow.MemoryRepository.GetOwnedAsync(memoryId, userId, ct);
    if (m is null) return Errors.MemoryNotFound;
    var v = Validate(req); if (v.IsFailure) return v.Error;
    Apply(m, req);
    await uow.SaveChangesAsync(ct);
    return m.ToDto();
  }

  public async Task<Result> DeleteMemoryAsync(Guid userId, Guid memoryId, CancellationToken ct)
  {
    var m = await uow.MemoryRepository.GetOwnedAsync(memoryId, userId, ct);
    if (m is null) return Errors.MemoryNotFound;
    uow.MemoryRepository.Delete(m);
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }

  private static Result Validate(MemoryRequest r)
  {
    if (!MemoryType.All.Contains(r.Type)) return Error.Validation("INVALID_TYPE", "type phải là photo|note|video.");
    if (r.Type == MemoryType.Note && string.IsNullOrWhiteSpace(r.Text)) return Error.Validation("TEXT_REQUIRED", "Ghi chú cần có nội dung.");
    if (r.Type != MemoryType.Note && string.IsNullOrWhiteSpace(r.MediaUrl)) return Error.Validation("MEDIA_REQUIRED", "Ảnh/video cần mediaUrl (upload lên Firebase Storage trước).");
    if (r.Rating is < 1 or > 5) return Error.Validation("INVALID_RATING", "rating từ 1 đến 5.");
    if ((r.Lat is null) != (r.Lng is null) || (r.Lat is not null && !Geo.ValidLatLng(r.Lat.Value, r.Lng!.Value)))
      return Error.Validation("INVALID_LOCATION", "Toạ độ không hợp lệ.");
    return Result.Success();
  }

  private static void Apply(Memory m, MemoryRequest r)
  {
    m.Type = r.Type; m.MediaUrl = r.MediaUrl; m.Text = r.Text; m.Lat = r.Lat; m.Lng = r.Lng;
    m.Mood = r.Mood; m.PlaceName = r.PlaceName; m.Rating = r.Rating; m.TakenAt = (r.TakenAt ?? DateTime.UtcNow).ToUtc();
  }
}
