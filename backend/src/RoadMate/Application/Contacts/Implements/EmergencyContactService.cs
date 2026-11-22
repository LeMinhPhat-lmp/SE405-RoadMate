using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Common;
using RoadMate.Application.Contacts.DTOs.Requests;
using RoadMate.Application.Contacts.DTOs.Responses;
using RoadMate.Application.Contacts.Interfaces;
using RoadMate.Application.Contacts.Mappers;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Contacts.Implements;

public class EmergencyContactService(IUserRepository users, IEmergencyContactRepository contacts, IUnitOfWork uow, TimeProvider clock) : IEmergencyContactService
{
  public async Task<Result<List<ContactResponse>>> ListAsync(Guid userId, CancellationToken ct) =>
    (await contacts.ListForUserAsync(userId, ct)).Select(c => c.ToDto()).ToList();

  public Task<Result<ContactResponse>> CreateAsync(Guid userId, ContactRequest req, CancellationToken ct) =>
    // Two concurrent creates could both see 4 contacts: the User row's xmin token makes one of them retry and see 5.
    OptimisticRetry.RunAsync<ContactResponse>(uow, async token =>
    {
      var user = await users.GetWithContactsAsync(userId, token);
      if (user is null) return Errors.UserNotSynced;
      var phone = PhoneNumber.ParseVietnam(req.Phone);
      if (phone.IsFailure) return phone.Error;
      var linked = await LinkedUserAsync(phone.Value, userId, token);
      var added = user.AddContact(req.Name, phone.Value, req.Relation, req.Priority, linked);
      if (added.IsFailure) return added.Error;
      await uow.SaveChangesAsync(token);
      return added.Value.ToDto();
    }, ct: ct);

  public async Task<Result<ContactResponse>> UpdateAsync(Guid userId, Guid id, ContactRequest req, CancellationToken ct)
  {
    var c = await contacts.GetOwnedAsync(userId, id, ct);
    if (c is null) return Errors.ContactNotFound;
    var phone = PhoneNumber.ParseVietnam(req.Phone);
    if (phone.IsFailure) return phone.Error;
    var changed = phone.Value.Value != c.Phone;
    var linked = changed ? await LinkedUserAsync(phone.Value, userId, ct) : c.LinkedUserId;
    var updated = c.Update(req.Name, phone.Value, req.Relation, req.Priority, linked, relink: changed);
    if (updated.IsFailure) return updated.Error;
    await uow.SaveChangesAsync(ct);
    return c.ToDto();
  }

  public async Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken ct)
  {
    var user = await users.GetWithContactsAsync(userId, ct);
    if (user is null) return Errors.ContactNotFound;
    var removed = user.RemoveContact(id, userId, clock.GetUtcNow());
    if (removed.IsFailure) return removed.Error;
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }

  /// <summary>RoadMate account that owns this verified phone (so the relative can receive push); null when none or it is the owner.</summary>
  private async Task<Guid?> LinkedUserAsync(PhoneNumber phone, Guid ownerId, CancellationToken ct)
  {
    var id = await users.FindIdByVerifiedPhoneAsync(phone.Value, ct);
    return id is { } v && v != ownerId ? v : null;
  }
}
