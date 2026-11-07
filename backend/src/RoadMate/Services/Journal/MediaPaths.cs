using System.Text.RegularExpressions;

namespace RoadMate.Services.Journal;

/// <summary>
/// Quy ước đường dẫn object: <c>users/{firebaseUid}/trips/{tripId}/{mediaId}{ext}</c>. Server là bên DUY NHẤT sinh đường dẫn,
/// và khi nhận lại mediaPath thì kiểm tra khớp đúng user + chuyến -> không ai gắn được file của người khác vào nhật ký của mình.
/// </summary>
public static class MediaPaths
{
  public static string Build(string firebaseUid, Guid tripId, string extension) =>
    $"users/{firebaseUid}/trips/{tripId}/{Guid.NewGuid():N}{extension}";

  public static bool IsOwnedBy(string path, string firebaseUid, Guid tripId) =>
    Regex.IsMatch(path, $"^users/{Regex.Escape(firebaseUid)}/trips/{tripId}/[0-9a-f]{{32}}\\.[a-z0-9]{{2,5}}$", RegexOptions.CultureInvariant);
}
