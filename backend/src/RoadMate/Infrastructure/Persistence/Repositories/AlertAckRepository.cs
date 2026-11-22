using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Alerts;

namespace RoadMate.Infrastructure.Persistence.Repositories;

public class AlertAckRepository(AppDbContext db) : IAlertAckRepository
{
  public Task AddRangeAsync(IEnumerable<AlertAck> acks, CancellationToken ct = default) => db.AlertAcks.AddRangeAsync(acks, ct);
  public async Task<IReadOnlyList<AlertAck>> ListByAlertAsync(Guid alertId, CancellationToken ct = default) =>
    await db.AlertAcks.Where(a => a.AlertId == alertId).ToListAsync(ct);
  public Task<AlertAck?> GetByTokenAsync(string token, CancellationToken ct = default) =>
    db.AlertAcks.Include(a => a.Alert).ThenInclude(a => a!.User).FirstOrDefaultAsync(a => a.Token == token, ct);
}
