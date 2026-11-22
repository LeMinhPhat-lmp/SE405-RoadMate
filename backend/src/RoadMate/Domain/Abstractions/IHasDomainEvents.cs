namespace RoadMate.Domain.Abstractions;

public interface IHasDomainEvents
{
  IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
  void ClearDomainEvents();
}
