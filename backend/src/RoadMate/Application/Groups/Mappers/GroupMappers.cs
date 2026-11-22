using RoadMate.Application.Common;
using RoadMate.Application.Groups.DTOs.Responses;
using RoadMate.Domain.Common;
using RoadMate.Domain.Groups;

namespace RoadMate.Application.Groups.Mappers;

/// <summary>Pure mappers (also used by the read-model adapter in Infrastructure).</summary>
public static class GroupMappers
{
  /// <summary>Leader first, then by join order.</summary>
  public static List<GroupMemberResponse> ToMemberDtos(this Group g)
  {
    var leader = g.Members.FirstOrDefault(m => m.UserId == g.LeaderId);
    return g.Members.OrderBy(m => m.UserId == g.LeaderId ? 0 : 1).ThenBy(m => m.JoinedAt).Select(m => m.ToDto(leader)).ToList();
  }

  public static GroupMemberResponse ToDto(this GroupMember m, GroupMember? leader)
  {
    int? distance = leader is { LastLat: not null, LastLng: not null } && m.UserId != leader.UserId && m.LastLat is not null && m.LastLng is not null
      ? (int)Math.Round(Geo.HaversineMeters(leader.LastLat.Value, leader.LastLng.Value, m.LastLat.Value, m.LastLng.Value))
      : null;
    return new(m.UserId, m.User?.Name ?? "", EnumWire.ToWire(m.Role), m.LastLat, m.LastLng, m.LastBattery, m.LocationUpdatedAt.ToWire(), distance);
  }

  public static GroupResponse ToDto(this Group g, int maxMembers) => new(
    g.Id, g.Name, g.Code, EnumWire.ToWire(g.Status), g.LeaderId,
    g.MeetingLat is not null && g.MeetingLng is not null ? new MeetingPointResponse(g.MeetingName, g.MeetingLat.Value, g.MeetingLng.Value) : null,
    g.DepartAt.ToWire(), maxMembers, g.ToMemberDtos());

  public static GroupResponse ToResponse(this Group g, int maxMembers) => g.ToDto(maxMembers);

  public static GroupMessageResponse ToDto(this GroupMessage m) =>
    new(m.Id, m.SenderId, m.Sender?.Name ?? "", m.QuickType ?? "", m.QuickType is null ? m.Text ?? "" : QuickMessages.Label(m.QuickType), m.CreatedAt);

  public static GroupMessageResponse ToResponse(this GroupMessage m) => m.ToDto();
}
