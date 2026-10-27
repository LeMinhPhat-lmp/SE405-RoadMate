namespace RoadMate.Services;

public interface IJournalService
{
  Task<Result<JournalDto>> GetJournalAsync(string uid, Guid tripId, int tzOffsetHours, CancellationToken ct);
  Task<Result<List<MemoryDto>>> ListMemoriesAsync(string uid, Guid tripId, CancellationToken ct);
  Task<Result<MemoryDto>> AddMemoryAsync(string uid, Guid tripId, MemoryRequest req, CancellationToken ct);
  Task<Result<MemoryDto>> UpdateMemoryAsync(string uid, Guid memoryId, MemoryRequest req, CancellationToken ct);
  Task<Result> DeleteMemoryAsync(string uid, Guid memoryId, CancellationToken ct);
}

public class JournalService : IJournalService
{
  private readonly IUnitOfWork _uow;
  public JournalService(IUnitOfWork uow) => _uow = uow;

  public async Task<Result<JournalDto>> GetJournalAsync(string uid, Guid tripId, int tz, CancellationToken ct)
  {
    var trip = await _uow.Trips.GetOwnedWithDetailsAsync(tripId, uid, ct);
    if (trip is null) return Errors.TripNotFound;
    var memories = await _uow.Memories.ListByTripAsync(tripId, ct);
    var days = memories.GroupBy(m => DateOnly.FromDateTime(m.TakenAt.AddHours(tz)))
      .OrderBy(g => g.Key)
      .Select(g => new JournalDayDto(g.Key, g.OrderBy(m => m.TakenAt).Select(m => m.ToDto()).ToList())).ToList();
    return new JournalDto(trip.Id, trip.Title, trip.Status, trip.Stats?.ToDto(),
      trip.Waypoints.OrderBy(w => w.Seq).Select(w => w.ToDto()).ToList(), days);
  }

  public async Task<Result<List<MemoryDto>>> ListMemoriesAsync(string uid, Guid tripId, CancellationToken ct)
  {
    if (await _uow.Trips.GetOwnedAsync(tripId, uid, ct) is null) return Errors.TripNotFound;
    return (await _uow.Memories.ListByTripAsync(tripId, ct)).Select(m => m.ToDto()).ToList();
  }

  public async Task<Result<MemoryDto>> AddMemoryAsync(string uid, Guid tripId, MemoryRequest req, CancellationToken ct)
  {
    if (await _uow.Trips.GetOwnedAsync(tripId, uid, ct) is null) return Errors.TripNotFound;
    var v = Validate(req); if (v.IsFailure) return v.Error;
    var m = new Memory { Id = Guid.NewGuid(), TripId = tripId };
    Apply(m, req);
    await _uow.Memories.AddAsync(m, ct);
    await _uow.SaveChangesAsync(ct);
    return m.ToDto();
  }

  public async Task<Result<MemoryDto>> UpdateMemoryAsync(string uid, Guid memoryId, MemoryRequest req, CancellationToken ct)
  {
    var m = await _uow.Memories.GetOwnedAsync(memoryId, uid, ct);
    if (m is null) return Errors.MemoryNotFound;
    var v = Validate(req); if (v.IsFailure) return v.Error;
    Apply(m, req);
    await _uow.SaveChangesAsync(ct);
    return m.ToDto();
  }

  public async Task<Result> DeleteMemoryAsync(string uid, Guid memoryId, CancellationToken ct)
  {
    var m = await _uow.Memories.GetOwnedAsync(memoryId, uid, ct);
    if (m is null) return Errors.MemoryNotFound;
    _uow.Memories.Remove(m);
    await _uow.SaveChangesAsync(ct);
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
