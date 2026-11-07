using RoadMate.Infrastructure.Storage;

namespace RoadMate.Services.Journal;

/// <summary>Loại file và dung lượng được phép cho ảnh/video nhật ký (hàm thuần).</summary>
public static class MediaPolicy
{
  private static readonly Dictionary<string, string> ImageTypes = new() { ["image/jpeg"] = ".jpg", ["image/png"] = ".png", ["image/webp"] = ".webp" };
  private static readonly Dictionary<string, string> VideoTypes = new() { ["video/mp4"] = ".mp4", ["video/quicktime"] = ".mov" };

  public static bool IsMediaKind(string kind) => kind is MemoryType.Photo or MemoryType.Video;

  /// <summary>Đuôi file tương ứng content type của loại <paramref name="kind"/>; false nếu không được phép.</summary>
  public static bool TryGetExtension(string kind, string contentType, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? extension)
  {
    var map = kind == MemoryType.Photo ? ImageTypes : kind == MemoryType.Video ? VideoTypes : null;
    if (map is not null && map.TryGetValue(contentType, out var ext))
    {
      extension = ext;
      return true;
    }
    extension = null;
    return false;
  }

  public static long MaxBytes(string kind, StorageOptions o) => kind == MemoryType.Video ? o.MaxVideoBytes : o.MaxImageBytes;
}
