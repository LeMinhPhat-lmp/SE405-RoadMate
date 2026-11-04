namespace RoadMate.Services.Groups;

/// <summary>Tin nhắn định sẵn của nhóm (chat tự do chưa hỗ trợ).</summary>
public static class QuickMessages
{
  public const string Rest = "rest", OutOfFuel = "out_of_fuel", NeedHelp = "need_help", Police = "police";

  private static readonly Dictionary<string, string> Labels = new()
  {
    [Rest] = "Dừng nghỉ", [OutOfFuel] = "Hết xăng", [NeedHelp] = "Cần hỗ trợ", [Police] = "Có cảnh sát giao thông"
  };

  public static bool IsValid(string? type) => type is not null && Labels.ContainsKey(type);
  public static string Label(string type) => Labels.TryGetValue(type, out var l) ? l : type;
  public static IReadOnlyCollection<string> All => Labels.Keys;
  /// <summary>Chỉ "Cần hỗ trợ" gửi push cho cả nhóm (tránh spam thông báo).</summary>
  public static bool PushesToGroup(string type) => type == NeedHelp;
}
