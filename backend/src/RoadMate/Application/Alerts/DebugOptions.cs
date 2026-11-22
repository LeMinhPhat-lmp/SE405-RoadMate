namespace RoadMate.Application.Alerts;

public class DebugOptions
{
  public bool EnableSimulator { get; set; }
  /// <summary>Số điện thoại được phép nhận SMS THẬT khi giả lập với dryRun=false (vd số các thành viên nhóm khi demo).</summary>
  public List<string> AllowedPhones { get; set; } = [];
}
