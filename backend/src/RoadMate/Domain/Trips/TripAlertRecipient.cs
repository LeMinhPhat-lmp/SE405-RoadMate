using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Users;

namespace RoadMate.Domain.Trips;

public sealed class TripAlertRecipient : Entity
{
  private TripAlertRecipient() { }
  public Guid TripId { get; private set; }
  public Guid ContactId { get; private set; }
  public EmergencyContact? Contact { get; private set; }
  internal static TripAlertRecipient Create(Guid tripId, Guid contactId) => new() { Id = Guid.NewGuid(), TripId = tripId, ContactId = contactId };
}
