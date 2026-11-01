namespace RoadMate.Services.Journal;

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
}
