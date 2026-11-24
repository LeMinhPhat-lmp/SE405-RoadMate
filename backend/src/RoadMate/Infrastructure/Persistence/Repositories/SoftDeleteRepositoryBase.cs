using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Identity;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Common;

namespace RoadMate.Infrastructure.Persistence.Repositories;

/// <summary>Soft delete goes through the entity (private setters), stamped with the acting user and the clock; never physically deletes rows.</summary>
public abstract class SoftDeleteRepositoryBase<T>(AppDbContext db, ICurrentUser user, TimeProvider clock)
  : RepositoryBase<T>(db), ISoftDeleteRepository<T> where T : Entity, ISoftDeletableEntity
{
  public void Delete(T entity)
  {
    MarkDeleted(entity, user.UserId ?? AuditConstants.SystemUserId, clock.GetUtcNow());
  }

  public void DeleteRange(IEnumerable<T> entities)
  {
    var actor = user.UserId ?? AuditConstants.SystemUserId;
    var now = clock.GetUtcNow();
    foreach (var entity in entities) MarkDeleted(entity, actor, now);
  }

  private void MarkDeleted(T entity, Guid actor, DateTimeOffset now)
  {
    if (Db.Entry(entity).State == EntityState.Detached) Set.Attach(entity);
    entity.SoftDelete(actor, now);
  }
}
