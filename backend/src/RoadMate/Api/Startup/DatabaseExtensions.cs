using RoadMate.Infrastructure.Persistence;

namespace RoadMate.Api.Startup;

public static class DatabaseExtensions
{
  /// <summary>Database:EnsureCreated=true -> tạo schema khi khởi động (dev nhanh; sau này chuyển sang EF migrations).</summary>
  public static void EnsureDatabaseCreatedIfConfigured(this WebApplication app)
  {
    if (!app.Configuration.GetValue<bool>("Database:EnsureCreated")) return;
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
  }
}
