using Npgsql;

namespace RoadMate.Data;

public static class DbExceptionExtensions
{
  private const string UniqueViolation = "23505";
  private const string ForeignKeyViolation = "23503";

  public static bool IsUniqueViolation(this DbUpdateException ex) => ex.InnerException is PostgresException { SqlState: UniqueViolation };
  public static bool IsForeignKeyViolation(this DbUpdateException ex) => ex.InnerException is PostgresException { SqlState: ForeignKeyViolation };
}
