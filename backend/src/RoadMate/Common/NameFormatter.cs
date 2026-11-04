namespace RoadMate.Common;

public static class NameFormatter
{
  /// <summary>"Nguyễn Văn An" -> "Nguyễn A." (hiển thị trên trang công khai để hạn chế lộ danh tính).</summary>
  public static string Abbreviate(string? fullName)
  {
    var parts = (fullName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    return parts.Length switch
    {
      0 => "",
      1 => parts[0],
      _ => $"{parts[0]} {char.ToUpperInvariant(parts[^1][0])}."
    };
  }
}
