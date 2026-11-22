using System.Globalization;
using System.Text;
using RoadMate.Application.Common;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Alerts;

public sealed record OwnerInfo(string Name, string? BloodType, string? PlateNumber);
