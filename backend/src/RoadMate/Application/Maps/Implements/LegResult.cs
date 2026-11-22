using System.Text.Json;
using System.Text.RegularExpressions;
using RoadMate.Domain.Common;
using RoadMate.Application.Common;
using RoadMate.Application.Shared;
using System.Text;
using Microsoft.Extensions.Options;
using RoadMate.Application.Maps.DTOs.Responses;

namespace RoadMate.Application.Maps.Implements;

internal sealed record LegResult(RouteCandidate Route, List<RouteWarningResponse> Warnings);
