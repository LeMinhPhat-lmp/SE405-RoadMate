using RoadMate.Data;

namespace RoadMate.Repositories.Implements;

public class EmergencyContactRepository(AppDbContext context, ICurrentUserContext currentUser)
  : SoftDeleteRepository<EmergencyContact>(context, currentUser), IEmergencyContactRepository
{
  public Task<List<EmergencyContact>> ListByUserAsync(Guid userId, CancellationToken ct = default) =>
    Set.Where(c => c.UserId == userId).OrderBy(c => c.Priority).ToListAsync(ct);
  public Task<int> CountByUserAsync(Guid userId, CancellationToken ct = default) => Set.CountAsync(c => c.UserId == userId, ct);
  public Task<EmergencyContact?> GetOwnedAsync(Guid id, Guid userId, CancellationToken ct = default) =>
    Set.Include(c => c.LinkedUser).FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);
  public Task<List<EmergencyContact>> GetByIdsAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken ct = default)
  {
    var list = ids.Distinct().ToList();
    return Set.Where(c => c.UserId == userId && list.Contains(c.Id)).ToListAsync(ct);
  }
  public Task<List<EmergencyContact>> ListByPhoneAsync(string phone, CancellationToken ct = default) =>
    Set.Where(c => c.Phone == phone).ToListAsync(ct);

  public async Task<List<EmergencyContact>> GetAlertRecipientsAsync(Guid tripId, Guid userId, CancellationToken ct = default)
  {
    var selected = await context.TripAlertRecipients.Where(r => r.TripId == tripId).Select(r => r.ContactId).ToListAsync(ct);
    var all = await Set.Include(c => c.LinkedUser).Where(c => c.UserId == userId).OrderBy(c => c.Priority).ToListAsync(ct);
    var chosen = all.Where(c => selected.Contains(c.Id)).ToList();
    return chosen.Count > 0 ? chosen : all;              // không chọn (hoặc người đã chọn bị xoá) -> gửi cho tất cả
  }
}
