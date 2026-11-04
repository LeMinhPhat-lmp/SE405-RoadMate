namespace RoadMate.Infrastructure.Logging;

/// <summary>Cấu hình nơi ghi log (section "Observability"). Mức log/override nằm ở section "Serilog:MinimumLevel".</summary>
public class ObservabilityOptions
{
  public const string Section = "Observability";

  public bool ConsoleEnabled { get; set; } = true;
  public FileLogOptions File { get; set; } = new();
  public ElasticLogOptions Elasticsearch { get; set; } = new();
}

public class FileLogOptions
{
  public bool Enabled { get; set; } = true;
  public string Path { get; set; } = "logs/roadmate-.log";
  public int RetainedFiles { get; set; } = 14;
}

public class ElasticLogOptions
{
  public bool Enabled { get; set; }
  public string Uri { get; set; } = "http://localhost:9200";
  /// <summary>Mỗi ngày một index: roadmate-logs-2026.11.04 (Kibana data view: roadmate-logs-*).</summary>
  public string IndexFormat { get; set; } = "roadmate-logs-{0:yyyy.MM.dd}";
  public string? Username { get; set; }
  /// <summary>Nạp qua biến môi trường Observability__Elasticsearch__Password, không ghi vào appsettings.</summary>
  public string? Password { get; set; }
  /// <summary>Thư mục đệm trên đĩa: nếu Elasticsearch tạm chết, log được giữ lại và gửi bù khi nó sống lại.</summary>
  public string BufferPath { get; set; } = "logs/es-buffer";
}
