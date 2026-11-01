namespace RoadMate.Dtos.Accounts;

public record SyncRequest(string? FcmToken, string? Name);
public record UpdateProfileRequest(string? Name, string? AvatarUrl, string? BloodType, string? MedicalNotes, string? VehicleType,
  string? PlateNumber, int? IdleMinutes, string? CrashSensitivity, string? Language, string? Theme, string? FcmToken);
public record UserDto(Guid Id, string Name, string? Email, string? Phone, string? AvatarUrl, string? BloodType, string? MedicalNotes,
  string? VehicleType, string? PlateNumber, int IdleMinutes, string CrashSensitivity, string Language, string Theme);
