part of '../main.dart';

/// Default map center (Ho Chi Minh City) when no GPS fix is available yet.
const defaultMapCenter = LatLng(10.7769, 106.7009);

/// Decodes a Google/Goong encoded polyline (precision 5).
List<LatLng> decodePolyline(String? encoded) {
  if (encoded == null || encoded.isEmpty) return const [];
  final points = <LatLng>[];
  var index = 0;
  var lat = 0;
  var lng = 0;
  int readValue() {
    var result = 0;
    var shift = 0;
    int byte;
    do {
      byte = encoded.codeUnitAt(index++) - 63;
      result |= (byte & 0x1f) << shift;
      shift += 5;
    } while (byte >= 0x20 && index < encoded.length);
    return (result & 1) != 0 ? ~(result >> 1) : result >> 1;
  }

  while (index < encoded.length) {
    lat += readValue();
    if (index >= encoded.length) break;
    lng += readValue();
    points.add(LatLng(lat / 1e5, lng / 1e5));
  }
  return points;
}

/// Great-circle distance in meters.
double metersBetween(LatLng a, LatLng b) {
  const earthRadius = 6371000.0;
  double rad(double degrees) => degrees * math.pi / 180;
  final dLat = rad(b.latitude - a.latitude);
  final dLng = rad(b.longitude - a.longitude);
  final h =
      math.pow(math.sin(dLat / 2), 2) +
      math.cos(rad(a.latitude)) *
          math.cos(rad(b.latitude)) *
          math.pow(math.sin(dLng / 2), 2);
  return 2 * earthRadius * math.asin(math.min(1, math.sqrt(h)));
}

String formatDistance(num? meters) {
  if (meters == null) return '--';
  if (meters < 1000) return '${meters.round()} m';
  final km = meters / 1000;
  return '${km.toStringAsFixed(km >= 100 ? 0 : 1)} km';
}

String formatKm(num? km) => km == null ? '--' : formatDistance(km * 1000);

/// "7g 10p" style duration used across the Figma screens.
String formatMinutes(num? minutes) {
  if (minutes == null) return '--';
  final total = minutes.round();
  final hours = total ~/ 60;
  final rest = total % 60;
  if (hours == 0) return '${rest}p';
  return rest == 0
      ? '${hours}g'
      : '${hours}g ${rest.toString().padLeft(2, '0')}p';
}

String formatClock(DateTime time) {
  final local = time.toLocal();
  return '${local.hour.toString().padLeft(2, '0')}:${local.minute.toString().padLeft(2, '0')}';
}

String formatDate(DateTime time) {
  final local = time.toLocal();
  return '${local.day.toString().padLeft(2, '0')}/${local.month.toString().padLeft(2, '0')}/${local.year}';
}

String formatCountdown(Duration duration) {
  final seconds = duration.inSeconds.clamp(0, 359999);
  final minutes = seconds ~/ 60;
  return '${minutes.toString().padLeft(2, '0')}:${(seconds % 60).toString().padLeft(2, '0')}';
}

String googleMapsLink(double lat, double lng) =>
    'https://maps.google.com/?q=${lat.toStringAsFixed(5)},${lng.toStringAsFixed(5)}';

DateTime? parseTime(Object? value) =>
    value == null ? null : DateTime.tryParse(value.toString());

double? asDouble(Object? value) => (value as num?)?.toDouble();
