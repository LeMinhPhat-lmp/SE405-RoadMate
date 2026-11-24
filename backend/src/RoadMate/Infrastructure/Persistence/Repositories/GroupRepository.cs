using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Identity;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Groups;

namespace RoadMate.Infrastructure.Persistence.Repositories;

public class GroupRepository(AppDbContext db, ICurrentUser user, TimeProvider clock) : SoftDeleteRepositoryBase<Group>(db, user, clock), IGroupRepository
{
  public Task<Group?> GetWithMembersAsync(Guid id, CancellationToken ct = default) =>
    Set.Include(g => g.Members).ThenInclude(m => m.User).AsSplitQuery().FirstOrDefaultAsync(g => g.Id == id, ct);

  public Task<Group?> GetByCodeAsync(string code, CancellationToken ct = default) =>
    Set.Include(g => g.Members).ThenInclude(m => m.User).AsSplitQuery().FirstOrDefaultAsync(g => g.Code == code, ct);

  public Task<bool> CodeExistsAsync(string code, CancellationToken ct = default) => Set.AnyAsync(g => g.Code == code, ct);

  public async Task<IReadOnlyList<Group>> ListRidingAsync(CancellationToken ct = default) =>
    await Set.Include(g => g.Members).ThenInclude(m => m.User).AsSplitQuery().Where(g => g.Status == GroupStatus.Riding).ToListAsync(ct);
}
