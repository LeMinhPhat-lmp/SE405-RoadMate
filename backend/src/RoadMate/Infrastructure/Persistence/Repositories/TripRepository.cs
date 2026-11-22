using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Identity;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Trips;

namespace RoadMate.Infrastructure.Persistence.Repositories;

public class TripRepository(AppDbContext db, ICurrentUser user, TimeProvider clock) : SoftDeleteRepositoryBase<Trip>(db, user, clock), ITripRepository
{
  public Task<Trip?> GetOwnedAsync(Guid tripId, Guid ownerId, CancellationToken ct = default) =>
    Set.FirstOrDefaultAsync(t => t.Id == tripId && t.OwnerId == ownerId, ct);

  public Task<Trip?> GetOwnedWithDetailsAsync(Guid tripId, Guid ownerId, CancellationToken ct = default) =>
    Set.Include(t => t.Waypoints).Include(t => t.Recipients).Include(t => t.Live).Include(t => t.Stats).AsSplitQuery()
      .FirstOrDefaultAsync(t => t.Id == tripId && t.OwnerId == ownerId, ct);

  public async Task<IReadOnlyList<Trip>> ListByOwnerAsync(Guid ownerId, TripStatus? status, CancellationToken ct = default) =>
    await Set.AsNoTracking().Include(t => t.Waypoints).Include(t => t.Recipients).AsSplitQuery()
      .Where(t => t.OwnerId == ownerId && (status == null || t.Status == status)).OrderByDescending(t => t.CreatedAt).ToListAsync(ct);

  public Task<bool> HasActiveAsync(Guid ownerId, CancellationToken ct = default) => Set.AnyAsync(t => t.OwnerId == ownerId && t.Status == TripStatus.Active, ct);

  public async Task<IReadOnlyList<Trip>> ListActiveForMonitoringAsync(CancellationToken ct = default) =>
    await Set.Include(t => t.Live).Include(t => t.Waypoints).Include(t => t.Owner).AsSplitQuery().Where(t => t.Status == TripStatus.Active).ToListAsync(ct);

  public async Task<IReadOnlyList<Trip>> ListInGroupAsync(Guid userId, Guid groupId, CancellationToken ct = default) =>
    await Set.Where(t => t.OwnerId == userId && t.GroupId == groupId).ToListAsync(ct);
}
