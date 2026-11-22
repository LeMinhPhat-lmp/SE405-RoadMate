namespace RoadMate.Api.Logging;

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
