namespace RoadMate.Services.Groups;

/// <summary>Cấu hình nhóm phượt (section "Groups").</summary>
public class GroupOptions
{
  public int MaxMembers { get; set; } = 10;
  /// <summary>Tin nhắn nhóm được giữ bao nhiêu ngày (job nền xoá tin cũ).</summary>
  public int MessageRetentionDays { get; set; } = 7;
  public int MaxMessagesPerPoll { get; set; } = 50;

  /// <summary>Tụt đoàn: cách trưởng đoàn quá số mét này...</summary>
  public double StragglerMeters { get; set; } = 2000;
  /// <summary>...liên tục quá số giây này (lọc nhiễu GPS) thì mới báo.</summary>
  public int StragglerSeconds { get; set; } = 120;
  /// <summary>Vị trí cũ hơn số giây này coi như không còn tín hiệu, không dùng để tính tụt đoàn.</summary>
  public int LocationFreshSeconds { get; set; } = 120;
  public int NotifyCooldownMinutes { get; set; } = 10;
  public int MonitorIntervalSeconds { get; set; } = 30;
}
