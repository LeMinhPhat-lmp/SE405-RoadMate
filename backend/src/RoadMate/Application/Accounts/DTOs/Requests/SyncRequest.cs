namespace RoadMate.Application.Accounts.DTOs.Requests;

public record SyncRequest(string? FcmToken, string? Name);