using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Domain.Users;

public sealed class EmergencyContact : FullAuditedEntity
{
  public const int MaxPerUser = Errors.MaxContactsPerUser;

  private EmergencyContact() { }

  public Guid UserId { get; private set; }
  public Guid? LinkedUserId { get; private set; }
  public string Name { get; private set; } = "";
  public string Phone { get; private set; } = "";
  public string? Relation { get; private set; }
  public int Priority { get; private set; }
  public User? LinkedUser { get; private set; }

  internal static EmergencyContact Create(Guid userId, string name, PhoneNumber phone, string? relation, int priority, Guid? linkedUserId) =>
    new() { Id = Guid.NewGuid(), UserId = userId, Name = name.Trim(), Phone = phone.Value, Relation = relation, Priority = priority, LinkedUserId = linkedUserId };

  public Result Update(string name, PhoneNumber phone, string? relation, int? priority, Guid? linkedUserId, bool relink)
  {
    if (string.IsNullOrWhiteSpace(name)) return Errors.InvalidName;
    Name = name.Trim(); Phone = phone.Value; Relation = relation; Priority = priority ?? Priority;
    if (relink) LinkedUserId = linkedUserId;
    return Result.Success();
  }

  public void LinkTo(Guid userId) => LinkedUserId = userId;
}
