using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Abstractions.Messaging;

public sealed record PushContent(string Title, string Body, IReadOnlyDictionary<string, string> Data);
