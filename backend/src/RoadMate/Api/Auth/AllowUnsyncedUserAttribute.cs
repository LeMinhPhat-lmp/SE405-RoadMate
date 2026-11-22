namespace RoadMate.Api.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowUnsyncedUserAttribute : Attribute { }
