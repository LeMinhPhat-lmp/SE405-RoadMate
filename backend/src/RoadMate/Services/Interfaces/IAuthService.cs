namespace RoadMate.Services.Interfaces;

public interface IAuthService
{
  Task<Result<UserDto>> SyncAsync(string firebaseUid, string? email, string? name, string? phone, SyncRequest req, CancellationToken ct);
  Task<Result<UserDto>> GetMeAsync(Guid userId, CancellationToken ct);
  Task<Result<UserDto>> UpdateMeAsync(Guid userId, UpdateProfileRequest req, CancellationToken ct);
}
