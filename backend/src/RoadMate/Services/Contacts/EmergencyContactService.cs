using RoadMate.Services.Notifications;

namespace RoadMate.Services.Contacts;

public class EmergencyContactService(IUnitOfWork uow, IContactNotifier notifier) : IEmergencyContactService
{
  public async Task<Result<List<ContactDto>>> ListAsync(Guid userId, CancellationToken ct) =>
      (await uow.EmergencyContactRepository.ListByUserAsync(userId, ct)).Select(c => c.ToDto()).ToList();

  public async Task<Result<ContactDto>> CreateAsync(Guid userId, ContactRequest req, CancellationToken ct)
  {
    if (await uow.UserRepository.GetByIdAsync(userId, false, ct) is null) return Errors.UserNotSynced;
    var phone = ContactRequestValidator.Validate(req);
    if (phone.IsFailure) return phone.Error;
    var count = await uow.EmergencyContactRepository.CountByUserAsync(userId, ct);
    if (count >= EmergencyContact.MaxPerUser) return Error.Conflict("CONTACT_LIMIT", $"Tối đa {EmergencyContact.MaxPerUser} liên hệ khẩn cấp.");

    var c = new EmergencyContact
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Name = req.Name.Trim(),
      Phone = phone.Value,
      Relation = req.Relation,
      Priority = req.Priority ?? count + 1,
      LinkedUserId = await uow.UserRepository.ResolveLinkedUserIdAsync(phone.Value, userId, ct)
    };
    await uow.EmergencyContactRepository.AddAsync(c, ct);
    await uow.SaveChangesAsync(ct);
    return c.ToDto();
  }

  public async Task<Result<ContactDto>> UpdateAsync(Guid userId, Guid id, ContactRequest req, CancellationToken ct)
  {
    var c = await uow.EmergencyContactRepository.GetOwnedAsync(id, userId, ct);
    if (c is null) return Errors.ContactNotFound;
    var phone = ContactRequestValidator.Validate(req);
    if (phone.IsFailure) return phone.Error;
    if (phone.Value != c.Phone) c.LinkedUserId = await uow.UserRepository.ResolveLinkedUserIdAsync(phone.Value, userId, ct);
    c.Name = req.Name.Trim(); c.Phone = phone.Value; c.Relation = req.Relation; c.Priority = req.Priority ?? c.Priority;
    await uow.SaveChangesAsync(ct);
    return c.ToDto();
  }

  public async Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken ct)
  {
    var c = await uow.EmergencyContactRepository.GetOwnedAsync(id, userId, ct);
    if (c is null) return Errors.ContactNotFound;
    uow.EmergencyContactRepository.Delete(c);
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }

  public async Task<Result<List<DeliveryDto>>> TestAsync(Guid userId, Guid id, CancellationToken ct)
  {
    var c = await uow.EmergencyContactRepository.GetOwnedAsync(id, userId, ct);
    if (c is null) return Errors.ContactNotFound;
    var user = await uow.UserRepository.GetByIdAsync(userId, false, ct);
    if (user is null) return Errors.UserNotSynced;

    var content = new NotificationContent($"[RoadMate] Tin nhắn thử: {user.Name} đã thêm bạn làm người liên hệ khẩn cấp.",
      "RoadMate: tin nhắn thử", new Dictionary<string, string> { ["type"] = "test" });
    var deliveries = await notifier.NotifyAsync(Guid.Empty, new[] { c }, content, NotificationMode.Live, ct);   // tin thử: không lưu log
    return deliveries.Select(d => d.ToDto()).ToList();
  }
}
