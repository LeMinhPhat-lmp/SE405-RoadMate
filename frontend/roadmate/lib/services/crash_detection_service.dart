part of '../main.dart';

/// Impact threshold (g) for each backend `crashSensitivity` value.
double crashThresholdG(String? sensitivity) => switch (sensitivity) {
  'high' => 3.0,
  'low' => 4.0,
  _ => 3.5,
};

String crashSensitivityLabel(String? sensitivity) => switch (sensitivity) {
  'high' => 'Nhạy (3g)',
  'low' => 'Ít (4g)',
  _ => 'Trung bình (3.5g)',
};

class CrashEvent {
  const CrashEvent({
    required this.peakG,
    required this.speedBeforeKmh,
    required this.at,
  });
  final double peakG;
  final double speedBeforeKmh;
  final DateTime at;
}

/// Pure crash/fall detection (doc 3.4a). All three conditions must hold:
/// 1. acceleration magnitude above [thresholdG],
/// 2. the vehicle was moving faster than [minSpeedBeforeKmh] just before,
/// 3. during the following [stillnessWindow] the phone is nearly still and
///    the GPS speed dropped below [maxSpeedAfterKmh].
///
/// Timestamps are passed in so the detector can be unit tested and driven by
/// the Debug Simulator without real sensors.
class CrashDetector {
  CrashDetector({
    this.thresholdG = 3.5,
    this.minSpeedBeforeKmh = 15,
    this.maxSpeedAfterKmh = 3,
    this.stillnessWindow = const Duration(seconds: 5),
    this.speedMemory = const Duration(seconds: 10),
    this.maxStillVarianceG2 = 0.05,
    this.onLog,
  });

  double thresholdG;
  final double minSpeedBeforeKmh;
  final double maxSpeedAfterKmh;
  final Duration stillnessWindow;
  final Duration speedMemory;
  final double maxStillVarianceG2;
  final void Function(String message)? onLog;

  /// When false (no active trip, resting) shocks are ignored.
  bool enabled = true;

  final List<(DateTime, double)> _speeds = [];
  DateTime? _windowStart;
  double _peakG = 0;
  double _speedBefore = 0;
  final List<double> _windowMagnitudes = [];

  bool get isEvaluating => _windowStart != null;

  static double magnitudeG(double x, double y, double z) =>
      math.sqrt(x * x + y * y + z * z) / 9.81;

  void addSpeed(double kmh, DateTime at) {
    _speeds.add((at, kmh));
    _speeds.removeWhere((entry) => at.difference(entry.$1) > speedMemory);
  }

  double get latestSpeedKmh => _speeds.isEmpty ? 0 : _speeds.last.$2;

  double _maxRecentSpeed(DateTime at) {
    var maxSpeed = 0.0;
    for (final (time, speed) in _speeds) {
      if (at.difference(time) <= speedMemory && speed > maxSpeed) {
        maxSpeed = speed;
      }
    }
    return maxSpeed;
  }

  /// Feeds one accelerometer sample (m/s², gravity included). Returns a
  /// [CrashEvent] when a crash is confirmed.
  CrashEvent? addAcceleration(double x, double y, double z, DateTime at) {
    final g = magnitudeG(x, y, z);
    final windowStart = _windowStart;
    if (windowStart == null) {
      if (!enabled || g < thresholdG) return null;
      final speedBefore = _maxRecentSpeed(at);
      if (speedBefore <= minSpeedBeforeKmh) {
        onLog?.call(
          'shock ${g.toStringAsFixed(2)}g ignored: speed ${speedBefore.toStringAsFixed(0)} km/h',
        );
        return null;
      }
      _windowStart = at;
      _peakG = g;
      _speedBefore = speedBefore;
      _windowMagnitudes.clear();
      onLog?.call(
        'crash? |a|=${g.toStringAsFixed(2)}g speed ${speedBefore.toStringAsFixed(0)} km/h',
      );
      return null;
    }

    final elapsed = at.difference(windowStart);
    // Ignore the first second: the impact itself is still settling.
    if (elapsed < const Duration(seconds: 1)) {
      if (g > _peakG) _peakG = g;
      return null;
    }
    _windowMagnitudes.add(g);
    if (elapsed < stillnessWindow) return null;

    _windowStart = null;
    final variance = _variance(_windowMagnitudes);
    final speedNow = latestSpeedKmh;
    final still = variance < maxStillVarianceG2;
    final stopped = speedNow < maxSpeedAfterKmh;
    if (still && stopped && enabled) {
      onLog?.call(
        'stillness ${stillnessWindow.inSeconds}s OK (var ${variance.toStringAsFixed(3)}, ${speedNow.toStringAsFixed(0)} km/h) → crash',
      );
      return CrashEvent(peakG: _peakG, speedBeforeKmh: _speedBefore, at: at);
    }
    onLog?.call(
      'crash rejected: var ${variance.toStringAsFixed(3)}, speed ${speedNow.toStringAsFixed(0)} km/h',
    );
    return null;
  }

  void reset() {
    _windowStart = null;
    _windowMagnitudes.clear();
  }

  static double _variance(List<double> values) {
    if (values.length < 2) return double.infinity;
    final mean = values.reduce((a, b) => a + b) / values.length;
    var sum = 0.0;
    for (final value in values) {
      sum += (value - mean) * (value - mean);
    }
    return sum / values.length;
  }
}
