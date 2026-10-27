namespace RoadMate.Infrastructure.Interfaces;

/// <summary>Người dùng hiện tại của request (null khi chạy ngoài HTTP, ví dụ background worker).</summary>
public interface ICurrentUserContext
{
  /// <summary>Id nội bộ (Guid) của bảng User; null nếu chưa đăng nhập hoặc chưa /auth/sync.</summary>
  Guid? UserId { get; }
}
