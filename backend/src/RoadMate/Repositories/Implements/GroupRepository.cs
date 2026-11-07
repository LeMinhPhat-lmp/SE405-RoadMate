using RoadMate.Data;

namespace RoadMate.Repositories.Implements;

public class GroupRepository(AppDbContext context, ICurrentUserContext currentUser)
  : SoftDeleteRepository<Group>(context, currentUser), IGroupRepository
{
  public Task<bool> CodeExistsAsync(string code, CancellationToken ct = default) => Set.AnyAsync(g => g.Code == code, ct);

  public Task<Group?> GetByCodeAsync(string code, CancellationToken ct = default) =>
    Set.Include(g => g.Members).ThenInclude(m => m.User).AsSplitQuery().FirstOrDefaultAsync(g => g.Code == code, ct);

  public Task<Group?> GetWithMembersAsync(Guid id, CancellationToken ct = default) =>
    Set.Include(g => g.Members).ThenInclude(m => m.User).AsSplitQuery().FirstOrDefaultAsync(g => g.Id == id, ct);

  public Task<List<Group>> ListByMemberAsync(Guid userId, CancellationToken ct = default) =>
    Set.AsNoTracking().Include(g => g.Members).Where(g => g.Members.Any(m => m.UserId == userId))
      .OrderByDescending(g => g.CreatedAt).ToListAsync(ct);

  public Task<List<Group>> ListRidingAsync(CancellationToken ct = default) =>
    Set.AsNoTracking().Include(g => g.Members).ThenInclude(m => m.User).AsSplitQuery()
      .Where(g => g.Status == GroupStatus.Riding).ToListAsync(ct);
}
