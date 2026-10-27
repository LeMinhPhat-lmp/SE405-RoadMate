using System.Text.RegularExpressions;

namespace RoadMate.Services;

public interface IEmergencyContactService
{
  Task<Result<List<ContactDto>>> ListAsync(string uid, CancellationToken ct);
  Task<Result<ContactDto>> CreateAsync(string uid, ContactRequest req, CancellationToken ct);
  Task<Result<ContactDto>> UpdateAsync(string uid, Guid id, ContactRequest req, CancellationToken ct);
  Task<Result> DeleteAsync(string uid, Guid id, CancellationToken ct);
  Task<Result<List<DeliveryDto>>> TestAsync(string uid, Guid id, CancellationToken ct);
}

public class EmergencyContactService : IEmergencyContactService
{
  public const int MaxContacts = 5;
  private static readonly Regex VnPhone = new(@"^\+84\d{9,10}$", RegexOptions.Compiled);
  private readonly IUnitOfWork _uow; private readonly IAlertDispatcher _dispatcher;
  public EmergencyContactService(IUnitOfWork uow, IAlertDispatcher d) { _uow = uow; _dispatcher = d; }

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

  public async Task<Result<List<ContactDto>>> ListAsync(string uid, CancellationToken ct) =>
      (await _uow.EmergencyContacts.ListByUserAsync(uid, ct)).Select(c => c.ToDto()).ToList();

  public async Task<Result<ContactDto>> CreateAsync(string uid, ContactRequest req, CancellationToken ct)
  {
    if (await _uow.Users.GetByUidAsync(uid, ct) is null) return Errors.UserNotSynced;
    if (string.IsNullOrWhiteSpace(req.Name)) return Error.Validation("INVALID_NAME", "Tên không được để trống.");
    var phone = NormalizePhone(req.Phone);
    if (phone is null) return Error.Validation("INVALID_PHONE", "Số điện thoại Việt Nam không hợp lệ.");
    var count = await _uow.EmergencyContacts.CountByUserAsync(uid, ct);
    if (count >= MaxContacts) return Error.Conflict("CONTACT_LIMIT", $"Tối đa {MaxContacts} liên hệ khẩn cấp.");

    var linked = await _uow.Users.GetByPhoneAsync(phone, ct);
    var c = new EmergencyContact
    {
      Id = Guid.NewGuid(),
      UserId = uid,
      Name = req.Name.Trim(),
      Phone = phone,
      Relation = req.Relation,
      Priority = req.Priority ?? count + 1,
      LinkedUserId = linked != null && linked.Uid != uid ? linked.Uid : null
    };
    await _uow.EmergencyContacts.AddAsync(c, ct);
    await _uow.SaveChangesAsync(ct);
    return c.ToDto();
  }

  public async Task<Result<ContactDto>> UpdateAsync(string uid, Guid id, ContactRequest req, CancellationToken ct)
  {
    var c = await _uow.EmergencyContacts.GetOwnedAsync(id, uid, ct);
    if (c is null) return Errors.ContactNotFound;
    if (string.IsNullOrWhiteSpace(req.Name)) return Error.Validation("INVALID_NAME", "Tên không được để trống.");
    var phone = NormalizePhone(req.Phone);
    if (phone is null) return Error.Validation("INVALID_PHONE", "Số điện thoại Việt Nam không hợp lệ.");
    if (phone != c.Phone)
    {
      var linked = await _uow.Users.GetByPhoneAsync(phone, ct);
      c.LinkedUserId = linked != null && linked.Uid != uid ? linked.Uid : null;
    }
    c.Name = req.Name.Trim(); c.Phone = phone; c.Relation = req.Relation; c.Priority = req.Priority ?? c.Priority;
    await _uow.SaveChangesAsync(ct);
    return c.ToDto();
  }

  public async Task<Result> DeleteAsync(string uid, Guid id, CancellationToken ct)
  {
    var c = await _uow.EmergencyContacts.GetOwnedAsync(id, uid, ct);
    if (c is null) return Errors.ContactNotFound;
    _uow.EmergencyContacts.Remove(c);
    await _uow.SaveChangesAsync(ct);
    return Result.Success();
  }

  public async Task<Result<List<DeliveryDto>>> TestAsync(string uid, Guid id, CancellationToken ct)
  {
    var c = await _uow.EmergencyContacts.GetOwnedAsync(id, uid, ct);
    if (c is null) return Errors.ContactNotFound;
    var user = await _uow.Users.GetByUidAsync(uid, ct);
    if (user is null) return Errors.UserNotSynced;
    var res = await _dispatcher.SendTestAsync(user, c, ct);
    return res.Select(d => new DeliveryDto(d.ContactId, d.Channel, d.Status, d.Error)).ToList();
  }
}
