using RoadMate.Application.Accounts.DTOs.Responses;
using RoadMate.Application.Accounts.DTOs.Requests;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Accounts.Interfaces;

public interface IAuthService
{
  Task<Result<UserResponse>> SyncAsync(TokenIdentity identity, SyncRequest req, CancellationToken ct);
  Task<Result<UserResponse>> GetMeAsync(Guid userId, CancellationToken ct);
  Task<Result<UserResponse>> UpdateMeAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct);
}
