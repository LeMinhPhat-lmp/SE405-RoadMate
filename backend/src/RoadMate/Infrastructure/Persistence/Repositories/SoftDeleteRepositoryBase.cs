using RoadMate.Application.Abstractions.Identity;
using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Common;

namespace RoadMate.Infrastructure.Persistence.Repositories;

/// <summary>Soft delete goes through the entity (private setters), stamped with the acting user and the clock.</summary>
public abstract class SoftDeleteRepositoryBase<T>(AppDbContext db, ICurrentUser user, TimeProvider clock) : RepositoryBase<T>(db) where T : Entity, ISoftDeletableEntity
{
  public void Delete(T entity) => entity.SoftDelete(user.UserId ?? AuditConstants.SystemUserId, clock.GetUtcNow());
}
