using System.Globalization;
using System.Text;
using RoadMate.Application.Common;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Alerts;

public sealed record NotificationContent(string Sms, string PushTitle, IReadOnlyDictionary<string, string> Data);
