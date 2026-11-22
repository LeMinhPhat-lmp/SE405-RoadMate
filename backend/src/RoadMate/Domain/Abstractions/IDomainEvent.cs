namespace RoadMate.Domain.Abstractions;

/// <summary>Marker for in-process domain events (raised by entities, dispatched before commit in the same transaction).</summary>
public interface IDomainEvent { DateTimeOffset OccurredAt { get; } }
