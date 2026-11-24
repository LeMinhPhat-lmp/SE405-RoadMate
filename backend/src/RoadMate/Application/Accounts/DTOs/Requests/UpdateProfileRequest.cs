namespace RoadMate.Application.Accounts.DTOs.Requests;

public record UpdateProfileRequest(string? Name, string? AvatarUrl, string? BloodType, string? MedicalNotes, string? VehicleType,
  string? PlateNumber, int? IdleMinutes, string? CrashSensitivity, string? Language, string? Theme, string? FcmToken);