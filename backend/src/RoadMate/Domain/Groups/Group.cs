using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Common;
using RoadMate.Domain.Events;
using RoadMate.Domain.Users;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Domain.Groups;

public sealed class Group : FullAuditedEntity, IAggregateRoot
{
  private readonly List<GroupMember> _members = [];

  private Group() { }

  public string Name { get; private set; } = "";
  public string Code { get; private set; } = "";
  public GroupStatus Status { get; private set; }
  public Guid LeaderId { get; private set; }
  public string? MeetingName { get; private set; }
  public double? MeetingLat { get; private set; }
  public double? MeetingLng { get; private set; }
  public DateTimeOffset? DepartAt { get; private set; }
  public User? Leader { get; private set; }
  public IReadOnlyCollection<GroupMember> Members => _members.AsReadOnly();

  public bool IsMember(Guid userId) => _members.Any(m => m.UserId == userId);
  public GroupMember? MemberOf(Guid userId) => _members.FirstOrDefault(m => m.UserId == userId);
  public GroupMember? LeaderMember => MemberOf(LeaderId);

  public static Result<Group> Create(Guid leaderId, string name, string? meetingName, double? lat, double? lng, DateTimeOffset? departAt, string code, DateTimeOffset now)
  {
    var g = new Group
    {
      Id = Guid.NewGuid(),
      Code = code,
      LeaderId = leaderId,
      Name = name.Trim(),
      Status = GroupStatus.Open
    };
    var meetingRes = g.ApplyMeeting(meetingName, lat, lng, departAt, now);
    if (meetingRes.IsFailure) return meetingRes.Error;

    g._members.Add(GroupMember.Create(g.Id, leaderId, GroupRole.Leader, now));
    return g;
  }

  public Result Join(Guid userId, DateTimeOffset now, int maxMembers)
  {
    if (IsMember(userId)) return Result.Success();
    if (Status == GroupStatus.Closed) return GroupErrors.Closed;
    if (_members.Count >= maxMembers) return GroupErrors.Full(maxMembers);

    _members.Add(GroupMember.Create(Id, userId, GroupRole.Member, now));
    Touch(now);
    return Result.Success();
  }

  public Result Leave(Guid userId, DateTimeOffset now)
  {
    var member = MemberOf(userId);
    if (member is null) return GroupErrors.MemberNotFound;
    return RemoveCore(member, now);
  }

  public Result RemoveMember(Guid actorId, Guid targetUserId, DateTimeOffset now)
  {
    if (LeaderId != actorId) return GroupErrors.NotLeader;
    if (targetUserId == actorId) return GroupErrors.CannotRemoveSelf;
    var member = MemberOf(targetUserId);
    if (member is null) return GroupErrors.MemberNotFound;
    return RemoveCore(member, now);
  }

  public Result RemoveCore(GroupMember member, DateTimeOffset now)
  {
    _members.Remove(member);
    var remaining = _members.OrderBy(m => m.JoinedAt).ToList();
    if (remaining.Count == 0)
    {
      Status = GroupStatus.Closed;
    }
    else if (member.UserId == LeaderId)
    {
      LeaderId = remaining[0].UserId;
      remaining[0].Role = GroupRole.Leader;
    }
    Touch(now);
    AddEvent(new MemberRemovedFromGroup(Id, member.UserId, now));
    return Result.Success();
  }

  public Result Start(Guid actorId, DateTimeOffset now)
  {
    if (LeaderId != actorId) return GroupErrors.NotLeader;
    if (Status == GroupStatus.Closed) return GroupErrors.Closed;
    if (Status != GroupStatus.Riding)
    {
      Status = GroupStatus.Riding;
      Touch(now);
    }
    return Result.Success();
  }

  public Result Close(Guid actorId, DateTimeOffset now)
  {
    if (LeaderId != actorId) return GroupErrors.NotLeader;
    if (Status != GroupStatus.Closed)
    {
      Status = GroupStatus.Closed;
      Touch(now);
    }
    return Result.Success();
  }

  public Result RegenerateCode(Guid actorId, string newCode, DateTimeOffset now)
  {
    if (LeaderId != actorId) return GroupErrors.NotLeader;
    if (Status == GroupStatus.Closed) return GroupErrors.Closed;
    Code = newCode;
    Touch(now);
    return Result.Success();
  }

  public Result SetMeetingPoint(Guid actorId, string name, double lat, double lng, DateTimeOffset? departAt, DateTimeOffset now)
  {
    if (LeaderId != actorId) return GroupErrors.NotLeader;
    if (Status == GroupStatus.Closed) return GroupErrors.Closed;
    var res = ApplyMeeting(name, lat, lng, departAt, now);
    if (res.IsSuccess) Touch(now);
    return res;
  }

  public Result ApplyMeeting(string? name, double? lat, double? lng, DateTimeOffset? departAt, DateTimeOffset now)
  {
    if (lat.HasValue != lng.HasValue) return Errors.InvalidLocation;
    if (lat.HasValue && GeoPoint.Create(lat.Value, lng!.Value).IsFailure) return Errors.InvalidLocation;
    if (departAt.HasValue && departAt.Value <= now) return GroupErrors.InvalidDepartTime;

    MeetingName = name != null ? name.Trim() : null;
    MeetingLat = lat;
    MeetingLng = lng;
    DepartAt = departAt;
    return Result.Success();
  }

  public Result ReportMemberLocation(Guid userId, GeoPoint at, int? battery, DateTimeOffset now)
  {
    var member = MemberOf(userId);
    if (member is null) return GroupErrors.NotFound;
    member.Report(at, battery, now);
    return Result.Success();
  }

  public DateTimeOffset? UpdateStraggling(Guid memberUserId, bool beyondRange, DateTimeOffset now)
  {
    var member = MemberOf(memberUserId);
    if (member is null) return null;
    if (!beyondRange)
    {
      member.StragglingSince = null;
    }
    else if (!member.StragglingSince.HasValue)
    {
      member.StragglingSince = now;
    }
    return member.StragglingSince;
  }

  public Result<GroupMessage> PostQuickMessage(Guid senderId, string quickType)
  {
    if (!QuickMessages.IsValid(quickType))
    {
      return Error.Validation("INVALID_QUICK_TYPE", "quickType phải là một trong: " + string.Join(", ", QuickMessages.All) + ".");
    }
    if (!IsMember(senderId)) return GroupErrors.NotFound;
    if (Status == GroupStatus.Closed) return GroupErrors.Closed;
    return GroupMessage.CreateQuick(Id, senderId, quickType);
  }
}
