namespace RoadMate.Application.Accounts.DTOs.Responses;

public record UserResponse(Guid Id, string Name, string? Email, string? Phone, string? AvatarUrl, string? BloodType, string? MedicalNotes,
  string? VehicleType, string? PlateNumber, int IdleMinutes, string CrashSensitivity, string Language, string Theme);
