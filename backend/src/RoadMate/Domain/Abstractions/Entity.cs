using System.ComponentModel.DataAnnotations.Schema;

namespace RoadMate.Domain.Abstractions;

public abstract class Entity : IHasDomainEvents
{
  private readonly List<IDomainEvent> _events = [];

  public Guid Id { get; protected set; }

  [NotMapped] public IReadOnlyCollection<IDomainEvent> DomainEvents => _events;
  protected void AddEvent(IDomainEvent e) => _events.Add(e);
  public void ClearDomainEvents() => _events.Clear();
}
