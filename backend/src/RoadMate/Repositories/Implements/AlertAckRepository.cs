using RoadMate.Data;

namespace RoadMate.Repositories.Implements;

public class AlertAckRepository(AppDbContext context) : Repository<AlertAck>(context), IAlertAckRepository
{
  public Task<List<AlertAck>> ListByAlertAsync(Guid alertId, CancellationToken ct = default) =>
    Set.Where(a => a.AlertId == alertId).ToListAsync(ct);

  public Task<AlertAck?> GetByTokenAsync(string token, CancellationToken ct = default) =>
    string.IsNullOrWhiteSpace(token) || token.Length > 64
      ? Task.FromResult<AlertAck?>(null)
      : Set.Include(a => a.Alert).ThenInclude(a => a!.User).FirstOrDefaultAsync(a => a.Token == token, ct);
}
