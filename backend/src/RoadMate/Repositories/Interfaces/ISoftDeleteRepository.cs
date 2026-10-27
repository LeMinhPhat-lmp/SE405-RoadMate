namespace RoadMate.Repositories.Interfaces;

public interface ISoftDeleteRepository<T> where T : class
{
  void Delete(T entity);
  void DeleteRange(IEnumerable<T> entities);
  void Restore(T entity);
  void RestoreRange(IEnumerable<T> entities);
}