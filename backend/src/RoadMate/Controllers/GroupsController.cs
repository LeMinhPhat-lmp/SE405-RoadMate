using Microsoft.AspNetCore.RateLimiting;

namespace RoadMate.Controllers;

/// <remarks>
/// Nhóm phượt (tối đa 10 xe). Thời gian thực bằng polling: vị trí thành viên + tin mới trả kèm
/// <c>POST trips/{id}/heartbeat</c> (cần gắn chuyến vào nhóm bằng <c>PUT trips/{id}/group</c>).
/// </remarks>
public class GroupsController(IGroupService svc) : ApiControllerBase
{
  [HttpPost("groups")]
  public async Task<IActionResult> Create(CreateGroupRequest req, CancellationToken ct) => HandleCreated(await svc.CreateAsync(UserId, req, ct));

  /// <summary>Vào nhóm bằng mã 6 ký tự. Giới hạn tần suất để chống dò mã.</summary>
  [HttpPost("groups/join")]
  [EnableRateLimiting("public")]
  public async Task<IActionResult> Join(JoinGroupRequest req, CancellationToken ct) => Handle(await svc.JoinAsync(UserId, req, ct));

  [HttpGet("groups")]
  public async Task<IActionResult> ListMine(CancellationToken ct) => Handle(await svc.ListMineAsync(UserId, ct));

  [HttpGet("groups/{id:guid}")]
  public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Handle(await svc.GetAsync(UserId, id, ct));

  [HttpPost("groups/{id:guid}/leave")]
  public async Task<IActionResult> Leave(Guid id, CancellationToken ct) => Handle(await svc.LeaveAsync(UserId, id, ct));

  /// <summary>Trưởng đoàn xoá thành viên.</summary>
  [HttpDelete("groups/{id:guid}/members/{memberUserId:guid}")]
  public async Task<IActionResult> RemoveMember(Guid id, Guid memberUserId, CancellationToken ct) =>
    Handle(await svc.RemoveMemberAsync(UserId, id, memberUserId, ct));

  [HttpPut("groups/{id:guid}/meeting-point")]
  public async Task<IActionResult> SetMeetingPoint(Guid id, MeetingPointRequest req, CancellationToken ct) =>
    Handle(await svc.SetMeetingPointAsync(UserId, id, req, ct));

  /// <summary>Trưởng đoàn bấm xuất phát: bật cảnh báo tụt đoàn.</summary>
  [HttpPost("groups/{id:guid}/start")]
  public async Task<IActionResult> Start(Guid id, CancellationToken ct) => Handle(await svc.StartAsync(UserId, id, ct));

  [HttpPost("groups/{id:guid}/close")]
  public async Task<IActionResult> Close(Guid id, CancellationToken ct) => Handle(await svc.CloseAsync(UserId, id, ct));

  /// <summary>Sinh mã mời mới (mã cũ hết hiệu lực).</summary>
  [HttpPost("groups/{id:guid}/code")]
  public async Task<IActionResult> RegenerateCode(Guid id, CancellationToken ct) => Handle(await svc.RegenerateCodeAsync(UserId, id, ct));

  /// <summary>Tin nhắn nhóm; <c>since</c> (UTC) chỉ lấy tin mới hơn mốc này.</summary>
  [HttpGet("groups/{id:guid}/messages")]
  public async Task<IActionResult> Messages(Guid id, [FromQuery] DateTime? since, CancellationToken ct) =>
    Handle(await svc.ListMessagesAsync(UserId, id, since, ct));

  /// <summary>Gửi tin định sẵn: rest | out_of_fuel | need_help | police. "need_help" còn push cho cả nhóm.</summary>
  [HttpPost("groups/{id:guid}/messages")]
  public async Task<IActionResult> SendMessage(Guid id, SendGroupMessageRequest req, CancellationToken ct) =>
    HandleCreated(await svc.SendMessageAsync(UserId, id, req, ct));

  /// <summary>Gắn chuyến của mình vào nhóm (groupId = null để gỡ).</summary>
  [HttpPut("trips/{tripId:guid}/group")]
  public async Task<IActionResult> AttachTrip(Guid tripId, AttachTripRequest req, CancellationToken ct) =>
    Handle(await svc.AttachTripAsync(UserId, tripId, req.GroupId, ct));
}
