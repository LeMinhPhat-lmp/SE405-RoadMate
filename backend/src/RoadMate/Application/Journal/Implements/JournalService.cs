using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Abstractions.Storage;
using RoadMate.Application.Journal.DTOs.Responses;
using RoadMate.Application.Journal.Interfaces;
using RoadMate.Application.Journal.Mappers;
using RoadMate.Application.Trips;
using RoadMate.Application.Trips.Mappers;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Journal.Implements;

public class JournalService(ITripRepository trips, IMemoryRepository memories, IMediaStorage storage) : IJournalService
{
  public async Task<Result<JournalResponse>> GetJournalAsync(Guid userId, Guid tripId, int tz, CancellationToken ct)
  {
    var trip = await trips.GetOwnedWithDetailsAsync(tripId, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    var list = await memories.ListByTripAsync(tripId, ct);
    var days = list.GroupBy(m => DateOnly.FromDateTime(m.TakenAt.UtcDateTime.AddHours(tz)))
      .OrderBy(g => g.Key)
      .Select(g => new JournalDayResponse(g.Key, g.OrderBy(m => m.TakenAt).Select(m => m.ToDto(storage)).ToList())).ToList();
    return new JournalResponse(trip.Id, trip.Title, EnumWire.ToWire(trip.Status), trip.Stats?.ToDto(),
      trip.Waypoints.OrderBy(w => w.Seq).Select(w => w.ToDto()).ToList(), days);
  }
}
