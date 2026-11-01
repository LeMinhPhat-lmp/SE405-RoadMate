namespace RoadMate.Startup;

/// <summary>
/// Nạp file <c>.env</c> (tìm từ thư mục hiện tại ngược lên tối đa vài cấp) vào biến môi trường của tiến trình
/// TRƯỚC khi tạo host, để cấu hình .NET đọc được. Không ghi đè biến đã có sẵn trong môi trường.
/// Chỉ cần: <c>GOONG_API_KEY=...</c> (hoặc dạng chuẩn .NET <c>Goong__ApiKey=...</c>).
/// </summary>
public static class DotEnv
{
  /// <summary>Tên "thân thiện" trong .env → khoá cấu hình .NET (dùng "__" thay cho ":").</summary>
  private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
  {
    ["GOONG_API_KEY"] = "Goong__ApiKey",
    ["GOONG_BASE_URL"] = "Goong__BaseUrl",
    ["ESMS_API_KEY"] = "Sms__ESms__ApiKey",
    ["ESMS_SECRET_KEY"] = "Sms__ESms__SecretKey"
  };

  /// <returns>Đường dẫn file .env đã nạp; null nếu không tìm thấy.</returns>
  public static string? Load(string? startDir = null, int maxLevels = 6)
  {
    var dir = new DirectoryInfo(startDir ?? Directory.GetCurrentDirectory());
    for (var i = 0; i <= maxLevels && dir is not null; i++, dir = dir.Parent)
    {
      var file = Path.Combine(dir.FullName, ".env");
      if (!File.Exists(file)) continue;
      foreach (var (key, value) in Parse(File.ReadAllLines(file)))
      {
        var name = Aliases.TryGetValue(key, out var mapped) ? mapped : key;
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name))) Environment.SetEnvironmentVariable(name, value);
      }
      return file;
    }
    return null;
  }

  public static IEnumerable<(string Key, string Value)> Parse(IEnumerable<string> lines)
  {
    foreach (var raw in lines)
    {
      var line = raw.Trim();
      if (line.Length == 0 || line[0] == '#') continue;
      if (line.StartsWith("export ", StringComparison.Ordinal)) line = line[7..].TrimStart();
      var eq = line.IndexOf('=');
      if (eq <= 0) continue;
      var key = line[..eq].Trim();
      var value = line[(eq + 1)..].Trim();
      if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[^1] == value[0]) value = value[1..^1];
      else
      {
        var hash = value.IndexOf(" #", StringComparison.Ordinal);   // chú thích cuối dòng (chỉ với giá trị không đặt trong ngoặc kép)
        if (hash >= 0) value = value[..hash].TrimEnd();
      }
      if (key.Length > 0 && value.Length > 0) yield return (key, value);
    }
  }
}
