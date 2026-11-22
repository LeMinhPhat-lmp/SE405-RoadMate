using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Users;

namespace RoadMate.Domain.Groups;

public sealed class GroupMessage : AuditableEntity
{
  private GroupMessage() { }
  public Guid GroupId { get; private set; }
  public Guid SenderId { get; private set; }
  public string? Text { get; private set; }
  public string? QuickType { get; private set; }
  public User? Sender { get; private set; }

  internal static GroupMessage CreateQuick(Guid groupId, Guid senderId, string quickType) =>
    new() { Id = Guid.NewGuid(), GroupId = groupId, SenderId = senderId, QuickType = quickType, Text = QuickMessages.Label(quickType) };
}
