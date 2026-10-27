namespace RoadMate.Entities;

public class User : FullAuditedEntity
{
  public string FirebaseUid { get; set; } = default!;      // ID bên ngoài (Firebase Auth); Id nội bộ là Guid
  public string Name { get; set; } = "";
  public string? Email { get; set; }
  public string? Phone { get; set; }                       // chỉ lấy từ claim phone_number đã xác thực
  public string? AvatarUrl { get; set; }
  public string? BloodType { get; set; }
  public string? MedicalNotes { get; set; }
  public string? VehicleType { get; set; }
  public string? PlateNumber { get; set; }
  public string? FcmToken { get; set; }
  public int IdleMinutes { get; set; } = 15;
  public string CrashSensitivity { get; set; } = "medium"; // low|medium|high
  public string Language { get; set; } = "vi";
  public string Theme { get; set; } = "system";

  public ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();
  public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}

public class EmergencyContact : FullAuditedEntity
{
  public Guid UserId { get; set; }
  public Guid? LinkedUserId { get; set; }
  public string Name { get; set; } = "";
  public string Phone { get; set; } = "";                  // +84...
  public string? Relation { get; set; }
  public int Priority { get; set; }

  public User? User { get; set; }
  public User? LinkedUser { get; set; }
}
