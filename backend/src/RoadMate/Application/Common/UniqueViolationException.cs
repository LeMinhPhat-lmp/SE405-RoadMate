namespace RoadMate.Application.Common;

/// <summary>Persistence-neutral exceptions raised by the UnitOfWork (Infrastructure translates provider exceptions into these).</summary>
public class UniqueViolationException(string? constraint = null, Exception? inner = null) : Exception("Vi phạm ràng buộc duy nhất.", inner)
{
  public string? Constraint { get; } = constraint;
}
