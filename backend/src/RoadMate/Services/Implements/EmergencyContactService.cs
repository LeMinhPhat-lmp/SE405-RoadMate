using System.Text.RegularExpressions;

namespace RoadMate.Services.Implements;

public class EmergencyContactService(IUnitOfWork uow, IAlertDispatcher dispatcher) : IEmergencyContactService
{
  public const int MaxContacts = 5;
  private static readonly Regex VnPhone = new(@"^\+84\d{9,10}$", RegexOptions.Compiled);

  /// <summary>Chuẩn hoá về +84xxxxxxxxx; trả null nếu không hợp lệ.</summary>
  public static string? NormalizePhone(string raw)
  {
    var s = new string(raw.Where(ch => char.IsDigit(ch) || ch == '+').ToArray());
    if (s.StartsWith("+84")) { }
    else if (s.StartsWith("84")) s = "+" + s;
    else if (s.StartsWith("0")) s = "+84" + s[1..];
    else return null;
    return VnPhone.IsMatch(s) ? s : null;
  }

  public async Task<Result<List<ContactDto>>> ListAsync(Guid userId, CancellationToken ct) =>
      (await uow.EmergencyContactRepository.ListByUserAsync(userId, ct)).Select(c => c.ToDto()).ToList();

  public async Task<Result<ContactDto>> CreateAsync(Guid userId, ContactRequest req, CancellationToken ct)
  {
    if (await uow.UserRepository.GetByIdAsync(userId, false, ct) is null) return Errors.UserNotSynced;
    if (string.IsNullOrWhiteSpace(req.Name)) return Error.Validation("INVALID_NAME", "Tên không được để trống.");
    var phone = NormalizePhone(req.Phone);
    if (phone is null) return Error.Validation("INVALID_PHONE", "Số điện thoại Việt Nam không hợp lệ.");
    var count = await uow.EmergencyContactRepository.CountByUserAsync(userId, ct);
    if (count >= MaxContacts) return Error.Conflict("CONTACT_LIMIT", $"Tối đa {MaxContacts} liên hệ khẩn cấp.");

    var linked = await uow.UserRepository.GetByPhoneAsync(phone, ct);
    var c = new EmergencyContact
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Name = req.Name.Trim(),
      Phone = phone,
      Relation = req.Relation,
      Priority = req.Priority ?? count + 1,
      LinkedUserId = linked != null && linked.Id != userId ? linked.Id : null
    };
    await uow.EmergencyContactRepository.AddAsync(c, ct);
    await uow.SaveChangesAsync(ct);
    return c.ToDto();
  }

  public async Task<Result<ContactDto>> UpdateAsync(Guid userId, Guid id, ContactRequest req, CancellationToken ct)
  {
    var c = await uow.EmergencyContactRepository.GetOwnedAsync(id, userId, ct);
    if (c is null) return Errors.ContactNotFound;
    if (string.IsNullOrWhiteSpace(req.Name)) return Error.Validation("INVALID_NAME", "Tên không được để trống.");
    var phone = NormalizePhone(req.Phone);
    if (phone is null) return Error.Validation("INVALID_PHONE", "Số điện thoại Việt Nam không hợp lệ.");
    if (phone != c.Phone)
    {
      var linked = await uow.UserRepository.GetByPhoneAsync(phone, ct);
      c.LinkedUserId = linked != null && linked.Id != userId ? linked.Id : null;
    }
    c.Name = req.Name.Trim(); c.Phone = phone; c.Relation = req.Relation; c.Priority = req.Priority ?? c.Priority;
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
    var res = await dispatcher.SendTestAsync(user, c, ct);
    return res.Select(d => new DeliveryDto(d.ContactId, d.Channel, d.Status, d.Error)).ToList();
  }
}
