namespace RoadMate.Infrastructure.Auth;

/// <summary>Cho phép endpoint chạy khi user đã đăng nhập Firebase nhưng chưa có bản ghi User (vd: POST /auth/sync).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowUnsyncedUserAttribute : Attribute { }
