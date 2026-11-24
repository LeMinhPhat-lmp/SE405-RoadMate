using RoadMate.Application.Accounts.DTOs.Responses;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Accounts.DTOs.Requests;
using RoadMate.Application.Accounts.Interfaces;
using RoadMate.Application.Accounts.Mappers;
using RoadMate.Application.Common;
using RoadMate.Domain.Common;
using RoadMate.Domain.Users;

namespace RoadMate.Application.Accounts.Implements;

public class AuthService(IUserRepository users, IEmergencyContactRepository contacts, IUnitOfWork uow) : IAuthService
{
  public async Task<Result<UserResponse>> SyncAsync(TokenIdentity id, SyncRequest req, CancellationToken ct)
  {
    var user = await users.GetByFirebaseUidAsync(id.FirebaseUid, ct);
    if (user is null)
    {
      user = User.Create(id.FirebaseUid, id.Email, req.Name ?? id.Name ?? id.Email?.Split('@')[0] ?? "Người dùng");
      await users.AddAsync(user, ct);
    }
    user.ApplyAuthSync(null, req.FcmToken, id.VerifiedPhone);
    try
    {
      if (!string.IsNullOrWhiteSpace(id.VerifiedPhone)) await contacts.LinkByPhoneAsync(id.VerifiedPhone, user.Id, ct);
      await uow.SaveChangesAsync(ct);
    }
    catch (UniqueViolationException)
    {
      // Two first-time syncs raced: the other request created the user; return that one.
      uow.DiscardChanges();
      var existing = await users.GetByFirebaseUidAsync(id.FirebaseUid, ct);
      return existing is null ? Errors.UserNotSynced : existing.ToResponse();
    }
    return user.ToResponse();
  }

  public async Task<Result<UserResponse>> GetMeAsync(Guid userId, CancellationToken ct)
  {
    var u = await users.GetByIdAsync(userId, cancellationToken: ct);
    return u is null ? Errors.UserNotSynced : u.ToResponse();
  }

  public async Task<Result<UserResponse>> UpdateMeAsync(Guid userId, UpdateProfileRequest r, CancellationToken ct)
  {
    var u = await users.GetByIdAsync(userId, trackChanges: true, ct);
    if (u is null) return Errors.UserNotSynced;
    var updated = u.UpdateProfile(r.ToUpdate());
    if (updated.IsFailure) return updated.Error;
    await uow.SaveChangesAsync(ct);
    return u.ToResponse();
  }
}
