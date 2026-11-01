using System.Linq.Expressions;

namespace RoadMate.Data;

public static class ModelBuilderExtensions
{
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
