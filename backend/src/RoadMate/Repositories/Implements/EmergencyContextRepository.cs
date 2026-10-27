using RoadMate.Data;
using RoadMate.Repositories.Interfaces;

namespace RoadMate.Repositories.Implements;

public class EmergencyContactRepository(AppDbContext context) : Repository<EmergencyContact>(context), IEmergencyContactRepository
{
  public Task<List<EmergencyContact>> ListByUserAsync(string uid, CancellationToken ct = default) =>
    Set.Where(c => c.UserId == uid).OrderBy(c => c.Priority).ToListAsync(ct);
  public Task<int> CountByUserAsync(string uid, CancellationToken ct = default) => Set.CountAsync(c => c.UserId == uid, ct);
  public Task<EmergencyContact?> GetOwnedAsync(Guid id, string uid, CancellationToken ct = default) =>
    Set.Include(c => c.LinkedUser).FirstOrDefaultAsync(c => c.Id == id && c.UserId == uid, ct);
  public Task<List<EmergencyContact>> GetByIdsAsync(IEnumerable<Guid> ids, string uid, CancellationToken ct = default)
  {
    var list = ids.Distinct().ToList();
    return Set.Where(c => c.UserId == uid && list.Contains(c.Id)).ToListAsync(ct);
  }
  public Task<List<EmergencyContact>> ListByPhoneAsync(string phone, CancellationToken ct = default) =>
      Set.Where(c => c.Phone == phone).ToListAsync(ct);
  public async Task<List<EmergencyContact>> GetAlertRecipientsAsync(Guid tripId, string uid, CancellationToken ct = default)
  {
    var selected = await context.TripAlertRecipients.Where(r => r.TripId == tripId).Select(r => r.ContactId).ToListAsync(ct);
    var q = Set.Include(c => c.LinkedUser).Where(c => c.UserId == uid);
    if (selected.Count > 0) q = q.Where(c => selected.Contains(c.Id));
    return await q.OrderBy(c => c.Priority).ToListAsync(ct);
  }
}
