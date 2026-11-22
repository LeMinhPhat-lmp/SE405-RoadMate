using RoadMate.Application.Groups.DTOs.Requests;
using RoadMate.Application.Groups.DTOs.Responses;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Groups.Interfaces;

public interface IGroupMessageService
{
  Task<Result<GroupMessageResponse>> SendMessageAsync(Guid userId, Guid groupId, SendGroupMessageRequest req, CancellationToken ct);
  Task<Result<List<GroupMessageResponse>>> ListMessagesAsync(Guid userId, Guid groupId, DateTime? since, CancellationToken ct);
}
