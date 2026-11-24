using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Alerts;

namespace RoadMate.Infrastructure.Persistence.Repositories;

public class AlertAckRepository(AppDbContext db) : RepositoryBase<AlertAck>(db), IAlertAckRepository
{
  public async Task<IReadOnlyList<AlertAck>> ListByAlertAsync(Guid alertId, CancellationToken ct = default) =>
    await Set.AsNoTracking().Where(a => a.AlertId == alertId).ToListAsync(ct);

  public Task<AlertAck?> GetByTokenAsync(string token, CancellationToken ct = default) =>
    Set.Include(a => a.Alert).ThenInclude(a => a!.User).FirstOrDefaultAsync(a => a.Token == token, ct);
}
