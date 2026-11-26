part of '../main.dart';

/// A planned stop where the rider may stay longer without being asked.
class PlannedStop {
  const PlannedStop({required this.point, required this.stayMinutes});
  final LatLng point;
  final int stayMinutes;
}

/// Pure "stopped too long" detection (doc 3.4b).
///
/// Speed below [idleSpeedKmh] for longer than the idle threshold – while not
/// in rest mode – makes [check] return true once. Answering the prompt with
/// [acknowledge] restarts the countdown. Near a planned stop the threshold is
/// extended by that stop's planned stay.
class IdleDetector {
  IdleDetector({
    this.idleMinutes = 15,
    this.idleSpeedKmh = 2,
    this.plannedStopRadiusMeters = 250,
    this.plannedStops = const [],
  });

  int idleMinutes;
  final double idleSpeedKmh;
  final double plannedStopRadiusMeters;
  List<PlannedStop> plannedStops;

  DateTime? _stillSince;
  DateTime? restUntil;
  bool _prompted = false;
  LatLng? _lastPoint;

  DateTime? get stillSince => _stillSince;
  bool get prompted => _prompted;

  bool isResting(DateTime now) => restUntil != null && now.isBefore(restUntil!);

  Duration stoppedFor(DateTime now) =>
      _stillSince == null ? Duration.zero : now.difference(_stillSince!);

  void update(double speedKmh, DateTime at, {LatLng? point}) {
    if (point != null) _lastPoint = point;
    if (speedKmh < idleSpeedKmh) {
      _stillSince ??= at;
    } else {
      _stillSince = null;
      _prompted = false;
    }
  }

  /// Threshold that applies at the current position.
  Duration threshold() {
    var minutes = idleMinutes;
    final point = _lastPoint;
    if (point != null) {
      for (final stop in plannedStops) {
        if (metersBetween(point, stop.point) <= plannedStopRadiusMeters) {
          minutes += stop.stayMinutes;
          break;
        }
      }
    }
    return Duration(minutes: minutes);
  }

  /// True exactly once per idle period when the rider should be asked.
  bool check(DateTime now) {
    if (_prompted || _stillSince == null || isResting(now)) return false;
    if (stoppedFor(now) < threshold()) return false;
    _prompted = true;
    return true;
  }

  /// "Tôi ổn" – start counting again from now.
  void acknowledge(DateTime now) {
    _prompted = false;
    if (_stillSince != null) _stillSince = now;
  }

  void startRest(DateTime now, Duration duration) {
    restUntil = now.add(duration);
    _prompted = false;
  }

  /// Ends rest mode; the stop timer restarts so the rider is not asked
  /// immediately after a long break.
  void endRest(DateTime now) {
    restUntil = null;
    _prompted = false;
    if (_stillSince != null) _stillSince = now;
  }

  /// Debug Simulator: pretend the rider has been still for the threshold.
  void simulateElapsed(DateTime now) {
    _stillSince = now.subtract(threshold());
    _prompted = false;
    restUntil = null;
  }
}
