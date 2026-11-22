using RoadMate.Application.Abstractions.Storage;

namespace RoadMate.Application.Journal;

/// <summary>Loại file và dung lượng được phép cho ảnh/video nhật ký (hàm thuần).</summary>
public static class MediaPolicy
{
  public const string PhotoKind = "photo", VideoKind = "video";
  private static readonly Dictionary<string, string> ImageTypes = new() { ["image/jpeg"] = ".jpg", ["image/png"] = ".png", ["image/webp"] = ".webp" };
  private static readonly Dictionary<string, string> VideoTypes = new() { ["video/mp4"] = ".mp4", ["video/quicktime"] = ".mov" };

  public static bool IsMediaKind(string kind) => kind is PhotoKind or VideoKind;

  /// <summary>Đuôi file tương ứng content type của loại <paramref name="kind"/>; false nếu không được phép.</summary>
  public static bool TryGetExtension(string kind, string contentType, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? extension)
  {
    var map = kind == PhotoKind ? ImageTypes : kind == VideoKind ? VideoTypes : null;
    if (map is not null && map.TryGetValue(contentType, out var ext))
    {
      extension = ext;
      return true;
    }
    extension = null;
    return false;
  }

  public static long MaxBytes(string kind, StorageOptions o) => kind == VideoKind ? o.MaxVideoBytes : o.MaxImageBytes;
}
