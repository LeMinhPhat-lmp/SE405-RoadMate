namespace RoadMate.Services.Alerts;

public interface IAlertRepeatProcessor
{
  /// <summary>Gửi lại các cảnh báo đã đến hạn lặp; đóng cảnh báo của chuyến đã kết thúc; dừng lặp khi vượt MaxRepeats.</summary>
  Task ProcessDueAsync(CancellationToken ct);
}
