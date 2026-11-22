using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Domain.Users;

public sealed class User : FullAuditedEntity, IAggregateRoot
{
  private readonly List<EmergencyContact> _contacts = [];

  private User()
  {
    IdleMinutes = 15;
    CrashSensitivity = CrashSensitivity.Medium;
    Language = "vi";
    Theme = "system";
  }

  public string FirebaseUid { get; private set; } = default!;
  public string Name { get; private set; } = "";
  public string? Email { get; private set; }
  /// <summary>Only from the verified phone_number claim.</summary>
  public string? Phone { get; private set; }
  public string? AvatarUrl { get; private set; }
  public string? BloodType { get; private set; }
  public string? MedicalNotes { get; private set; }
  public string? VehicleType { get; private set; }
  public string? PlateNumber { get; private set; }
  public int IdleMinutes { get; private set; } = 15;
  public CrashSensitivity CrashSensitivity { get; private set; } = CrashSensitivity.Medium;
  public string Language { get; private set; } = "vi";
  public string Theme { get; private set; } = "system";
  public string? FcmToken { get; private set; }

  public IReadOnlyCollection<EmergencyContact> Contacts => _contacts.AsReadOnly();
  public int ActiveContactCount => _contacts.Count(c => !c.IsDeleted);

  public static User Create(string firebaseUid, string? email, string name)
  {
    return new User
    {
      Id = Guid.NewGuid(),
      FirebaseUid = firebaseUid,
      Email = string.IsNullOrWhiteSpace(email) ? null : email,
      Name = name
    };
  }

  public void ApplyAuthSync(string? name, string? fcmToken, string? verifiedPhone)
  {
    if (string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(name))
    {
      Name = name.Trim();
    }
    if (!string.IsNullOrWhiteSpace(fcmToken))
    {
      FcmToken = fcmToken;
    }
    if (!string.IsNullOrWhiteSpace(verifiedPhone))
    {
      Phone = verifiedPhone;
    }
  }

  public Result UpdateProfile(ProfileUpdate u)
  {
    if (u.IdleMinutes is { } im && (im < 1 || im > 240))
    {
      return Errors.InvalidIdle;
    }
    if (!string.IsNullOrWhiteSpace(u.Name))
    {
      Name = u.Name.Trim();
    }
    if (u.AvatarUrl != null) AvatarUrl = u.AvatarUrl;
    if (u.BloodType != null) BloodType = u.BloodType;
    if (u.MedicalNotes != null) MedicalNotes = u.MedicalNotes;
    if (u.VehicleType != null) VehicleType = u.VehicleType;
    if (u.PlateNumber != null) PlateNumber = u.PlateNumber;
    if (u.IdleMinutes is { } imVal) IdleMinutes = imVal;
    if (u.CrashSensitivity is { } csVal) CrashSensitivity = csVal;
    if (u.Language != null) Language = u.Language;
    if (u.Theme != null) Theme = u.Theme;
    if (!string.IsNullOrWhiteSpace(u.FcmToken)) FcmToken = u.FcmToken;
    return Result.Success();
  }

  public Result<EmergencyContact> AddContact(string name, PhoneNumber phone, string? relation, int? priority, Guid? linkedUserId)
  {
    if (string.IsNullOrWhiteSpace(name))
    {
      return Errors.InvalidName;
    }
    if (ActiveContactCount >= 5)
    {
      return Errors.ContactLimit;
    }
    int prio = priority.HasValue ? priority.Value : ActiveContactCount + 1;
    var c = EmergencyContact.Create(Id, name, phone, relation, prio, linkedUserId);
    _contacts.Add(c);
    return c;
  }

  public Result<EmergencyContact> RemoveContact(Guid contactId, Guid actor, DateTimeOffset now)
  {
    var contact = _contacts.FirstOrDefault(c => c.Id == contactId);
    if (contact is null)
    {
      return Errors.ContactNotFound;
    }
    contact.SoftDelete(actor, now);
    return contact;
  }
}
