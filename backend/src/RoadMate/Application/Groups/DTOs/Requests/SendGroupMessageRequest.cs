namespace RoadMate.Application.Groups.DTOs.Requests;

/// <param name="QuickType">rest | out_of_fuel | need_help | police.</param>
public record SendGroupMessageRequest(string QuickType);
