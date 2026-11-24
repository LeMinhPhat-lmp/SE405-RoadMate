using System.Text.Json;
using System.Text.RegularExpressions;
using RoadMate.Domain.Common;
using RoadMate.Application.Common;
using RoadMate.Application.Shared;
using System.Text;
using Microsoft.Extensions.Options;
using RoadMate.Application.Maps.DTOs.Responses;

namespace RoadMate.Application.Maps.Implements;

internal sealed record RouteCandidate(List<(double Lat, double Lng)> Points, double DistanceM, int DurationS,
  List<StepResponse> Steps, string? StartAddress, string? EndAddress, bool HasHighway, bool HasToll, List<string> ProviderWarnings);
