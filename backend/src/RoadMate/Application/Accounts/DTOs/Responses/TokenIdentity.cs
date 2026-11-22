namespace RoadMate.Application.Accounts.DTOs.Responses;

public sealed record TokenIdentity(string FirebaseUid, string? Email, string? Name, string? VerifiedPhone);
