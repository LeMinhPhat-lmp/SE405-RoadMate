namespace RoadMate.Application.Groups.DTOs.Responses;

public record GroupMessageResponse(Guid Id, Guid SenderId, string SenderName, string QuickType, string Label, DateTimeOffset SentAt);
