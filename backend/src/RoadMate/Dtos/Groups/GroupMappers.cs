namespace RoadMate.Dtos.Groups;

public static class GroupMappers
{
  /// <summary>Trưởng đoàn trước, sau đó theo thứ tự tham gia.</summary>
  public static List<GroupMemberDto> ToMemberDtos(this Group g)
  {
    var leader = g.Members.FirstOrDefault(m => m.UserId == g.LeaderId);
    return g.Members.OrderBy(m => m.UserId == g.LeaderId ? 0 : 1).ThenBy(m => m.JoinedAt).Select(m => m.ToDto(leader)).ToList();
  }

  public static GroupMemberDto ToDto(this GroupMember m, GroupMember? leader)
  {
    int? distance = leader is { LastLat: not null, LastLng: not null } && m.UserId != leader.UserId && m.LastLat is not null && m.LastLng is not null
      ? (int)Math.Round(Geo.HaversineMeters(leader.LastLat.Value, leader.LastLng.Value, m.LastLat.Value, m.LastLng.Value))
      : null;
    return new(m.UserId, m.User?.Name ?? "", m.Role, m.LastLat, m.LastLng, m.LastBattery, m.LocationUpdatedAt, distance);
  }

  public static GroupDto ToDto(this Group g, int maxMembers) => new(
    g.Id, g.Name, g.Code, g.Status, g.LeaderId,
    g.MeetingLat is not null && g.MeetingLng is not null ? new MeetingPointDto(g.MeetingName, g.MeetingLat.Value, g.MeetingLng.Value) : null,
    g.DepartAt, maxMembers, g.ToMemberDtos());

  public static GroupMessageDto ToDto(this GroupMessage m) =>
    new(m.Id, m.SenderId, m.Sender?.Name ?? "", m.QuickType ?? "", m.QuickType is null ? m.Text ?? "" : QuickMessages.Label(m.QuickType), m.CreatedAt);
}
