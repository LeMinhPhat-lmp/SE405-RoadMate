namespace RoadMate.Domain.Users;

/// <summary>Request-shape-free profile update: null = keep the stored value.</summary>
public sealed record ProfileUpdate(string? Name, string? AvatarUrl, string? BloodType, string? MedicalNotes, string? VehicleType,
  string? PlateNumber, int? IdleMinutes, CrashSensitivity? CrashSensitivity, string? Language, string? Theme, string? FcmToken);
