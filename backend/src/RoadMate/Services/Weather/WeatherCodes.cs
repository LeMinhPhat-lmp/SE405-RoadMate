namespace RoadMate.Services.Weather;

/// <summary>Mã thời tiết WMO (Open-Meteo) -> mô tả tiếng Việt (hàm thuần).</summary>
public static class WeatherCodes
{
  public static string Describe(int code) => code switch
  {
    0 => "Trời quang",
    1 => "Ít mây",
    2 => "Mây rải rác",
    3 => "Nhiều mây",
    45 or 48 => "Sương mù",
    >= 51 and <= 57 => "Mưa phùn",
    >= 61 and <= 65 => "Mưa",
    66 or 67 => "Mưa lạnh",
    >= 71 and <= 77 => "Tuyết",
    >= 80 and <= 82 => "Mưa rào",
    85 or 86 => "Tuyết rơi",
    95 => "Giông",
    96 or 99 => "Giông kèm mưa đá",
    _ => "Không rõ"
  };

  public static bool IsThunderstorm(int code) => code is 95 or 96 or 99;
}
