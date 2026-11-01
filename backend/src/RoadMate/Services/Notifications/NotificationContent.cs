namespace RoadMate.Services.Notifications;

/// <param name="Text">Nội dung đầy đủ (SMS và nội dung push).</param>
/// <param name="Title">Tiêu đề push.</param>
/// <param name="Data">Dữ liệu kèm push để app mở đúng màn hình.</param>
public record NotificationContent(string Text, string Title, IDictionary<string, string> Data);
