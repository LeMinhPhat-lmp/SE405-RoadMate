namespace RoadMate.Infrastructure.Firebase;

public class FirebaseOptions
{
  public string ProjectId { get; set; } = "";
  /// <summary>Đường dẫn file service account JSON (KHÔNG commit). Dùng chung cho FCM và ký URL Storage.</summary>
  public string CredentialPath { get; set; } = "";
}
