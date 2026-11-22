namespace RoadMate.Api.Logging;

/// <summary>Cấu hình nơi ghi log (section "Observability"). Mức log/override nằm ở section "Serilog:MinimumLevel".</summary>
public class ObservabilityOptions
{
  public const string Section = "Observability";

  public bool ConsoleEnabled { get; set; } = true;
  public FileLogOptions File { get; set; } = new();
  public ElasticLogOptions Elasticsearch { get; set; } = new();
}
