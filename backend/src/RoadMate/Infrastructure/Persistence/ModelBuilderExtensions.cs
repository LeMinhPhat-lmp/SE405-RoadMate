using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RoadMate.Domain.Abstractions;

namespace RoadMate.Infrastructure.Persistence;

public static class ModelBuilderExtensions
{
  /// <summary>Mọi Entity đều tự sinh Id bằng Guid.NewGuid() trong Domain; cấu hình ValueGeneratedNever để EF Core nhận diện entity con thêm vào aggregate đang tracked là Added thay vì Modified.</summary>
  public static ModelBuilder ApplyClientGeneratedIds(this ModelBuilder b)
  {
    foreach (var et in b.Model.GetEntityTypes().Where(t => typeof(Entity).IsAssignableFrom(t.ClrType)))
    {
      b.Entity(et.ClrType).Property(nameof(Entity.Id)).ValueGeneratedNever();
    }
    return b;
  }

  /// <summary>Mọi entity ISoftDeletableEntity tự động ẩn bản ghi đã xoá mềm (dùng IgnoreQueryFilters() để xem lại).</summary>
  public static ModelBuilder ApplySoftDeleteQueryFilters(this ModelBuilder b)
  {
    foreach (var et in b.Model.GetEntityTypes().Where(t => typeof(ISoftDeletableEntity).IsAssignableFrom(t.ClrType)))
    {
      var p = Expression.Parameter(et.ClrType, "e");
      var deletedAt = Expression.Property(p, nameof(ISoftDeletableEntity.DeletedAt));
      var body = Expression.Equal(deletedAt, Expression.Constant(null, typeof(DateTimeOffset?)));
      et.SetQueryFilter(Expression.Lambda(body, p));
    }
    return b;
  }
}
