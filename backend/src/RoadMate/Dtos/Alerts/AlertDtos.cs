namespace RoadMate.Dtos.Alerts;

public record CreateAlertRequest(string ClientId, string Type, double Lat, double Lng, int? Battery);
public record AlertLocationRequest(double Lat, double Lng, int? Battery);
public record DeliveryDto(Guid? ContactId, string Channel, string Status, string? Error);
public record AlertAckDto(Guid ContactId, DateTime? AckedAt);
public record AlertDto(Guid Id, Guid TripId, string Type, string Status, double Lat, double Lng, int? Battery, int RepeatCount,
    DateTimeOffset CreatedAt, DateTime? ResolvedAt, string? ResolvedBy, List<DeliveryDto>? Deliveries,
    List<AlertAckDto> Acks, string? SimulationMode);
/// <param name="DryRun">Mặc định true: không gửi SMS/push thật. false: chỉ gửi thật tới số trong Debug:AllowedPhones.</param>
public record SimulateAlertRequest(Guid TripId, string? Type, double? Lat, double? Lng, bool? DryRun);
