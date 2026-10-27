namespace RoadMate.Repositories.Interfaces;

/// <summary>Xoá mềm: gán DeletedAt/DeletedBy. Bản ghi đã xoá bị ẩn bởi global query filter trong AppDbContext.</summary>
public interface ISoftDeleteRepository<T> where T : class
{
  void Delete(T entity);
  void DeleteRange(IEnumerable<T> entities);
  void Restore(T entity);
  void RestoreRange(IEnumerable<T> entities);
  /// <summary>Lấy bản ghi ĐÃ xoá mềm (bỏ qua query filter) để khôi phục.</summary>
  Task<T?> GetDeletedByIdAsync(Guid id, bool trackChanges = true, CancellationToken ct = default);
  Task<IEnumerable<T>> ListDeletedAsync(int skip, int take, CancellationToken ct = default);
}
