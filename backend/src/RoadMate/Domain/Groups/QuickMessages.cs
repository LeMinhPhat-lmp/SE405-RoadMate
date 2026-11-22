namespace RoadMate.Domain.Groups;

/// <summary>Preset group messages (free chat is not supported).</summary>
public static class QuickMessages
{
  public const string Rest = "rest", OutOfFuel = "out_of_fuel", NeedHelp = "need_help", Police = "police";

  private static readonly Dictionary<string, string> Labels = new()
  {
    [Rest] = "Dừng nghỉ",
    [OutOfFuel] = "Hết xăng",
    [NeedHelp] = "Cần hỗ trợ",
    [Police] = "Có chốt giao thông"
  };

  public static IReadOnlyCollection<string> All => Labels.Keys;
  public static bool IsValid(string type) => Labels.ContainsKey(type);
  public static string? Label(string type) => Labels.GetValueOrDefault(type);
  public static bool PushesToGroup(string type) => type == NeedHelp;
}
