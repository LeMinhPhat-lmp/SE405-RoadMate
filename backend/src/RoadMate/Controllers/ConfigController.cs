namespace RoadMate.Controllers;

public class ConfigController : ApiControllerBase
{
  [AllowAnonymous]
  [HttpGet("config/emergency-numbers")]
  public IActionResult EmergencyNumbers() => Ok(new[]
  {
    new { number = "113", service = "Công an" }, new { number = "114", service = "Cứu hỏa, cứu nạn cứu hộ" },
    new { number = "115", service = "Cấp cứu y tế" }, new { number = "112", service = "Số khẩn cấp thống nhất" }
  });
}
