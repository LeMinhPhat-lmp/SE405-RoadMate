using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Identity;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Users;

namespace RoadMate.Infrastructure.Persistence.Repositories;

public class EmergencyContactRepository(AppDbContext db, ICurrentUser user, TimeProvider clock)
  : SoftDeleteRepositoryBase<EmergencyContact>(db, user, clock), IEmergencyContactRepository
{
  public async Task<IReadOnlyList<EmergencyContact>> ListForUserAsync(Guid userId, CancellationToken ct = default) =>
    await Set.AsNoTracking().Where(c => c.UserId == userId).OrderBy(c => c.Priority).ThenBy(c => c.CreatedAt).ThenBy(c => c.Id).ToListAsync(ct);

  public Task<EmergencyContact?> GetOwnedAsync(Guid userId, Guid id, CancellationToken ct = default) =>
    Set.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);

  public async Task<IReadOnlyList<Guid>> FilterOwnedIdsAsync(Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
    await Set.AsNoTracking().Where(c => c.UserId == userId && ids.Contains(c.Id)).Select(c => c.Id).ToListAsync(ct);

  /// <summary>The trip's chosen contacts; falls back to all of the owner's contacts when none were chosen (or all chosen ones were deleted).</summary>
  public async Task<IReadOnlyList<AlertRecipient>> GetAlertRecipientsAsync(Guid tripId, Guid ownerId, CancellationToken ct = default)
  {
    var chosenIds = await Db.TripAlertRecipients.AsNoTracking().Where(r => r.TripId == tripId).Select(r => r.ContactId).ToListAsync(ct);
    var q = Set.AsNoTracking().Where(c => c.UserId == ownerId);
    var chosen = chosenIds.Count == 0 ? null : q.Where(c => chosenIds.Contains(c.Id));
    var contacts = chosen is null ? [] : await chosen.OrderBy(c => c.Priority).ThenBy(c => c.Id).ToListAsync(ct);
    if (contacts.Count == 0) contacts = await q.OrderBy(c => c.Priority).ThenBy(c => c.Id).ToListAsync(ct);
    var linked = contacts.Where(c => c.LinkedUserId != null).Select(c => c.LinkedUserId!.Value).ToList();
    var tokens = linked.Count == 0 ? [] : await Db.Users.AsNoTracking().Where(u => linked.Contains(u.Id) && u.FcmToken != null && u.FcmToken != "").ToDictionaryAsync(u => u.Id, u => u.FcmToken!, ct);
    return contacts.Select(c => new AlertRecipient(c.Id, c.Name, c.Phone, c.LinkedUserId, c.LinkedUserId is { } l && tokens.TryGetValue(l, out var t) ? t : null)).ToList();
  }

  public async Task<int> LinkByPhoneAsync(string phone, Guid userId, CancellationToken ct = default)
  {
    var matches = await Set.Where(c => c.Phone == phone && c.LinkedUserId == null && c.UserId != userId).ToListAsync(ct);
    foreach (var c in matches) c.LinkTo(userId);
    return matches.Count;
  }
}
