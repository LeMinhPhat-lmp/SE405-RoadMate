import 'package:flutter_test/flutter_test.dart';
import 'package:latlong2/latlong.dart';
import 'package:roadmate/main.dart';

final t0 = DateTime(2026, 11, 26, 10, 42);
DateTime at(int ms) => t0.add(Duration(milliseconds: ms));

/// Feeds a still phone (≈1g) every 100 ms from [fromMs] to [toMs].
CrashEvent? feedStill(CrashDetector detector, int fromMs, int toMs) {
  CrashEvent? event;
  for (var ms = fromMs; ms <= toMs && event == null; ms += 100) {
    event = detector.addAcceleration(0.05, 0.1, 9.8, at(ms));
  }
  return event;
}

void main() {
  group('CrashDetector', () {
    test('detects impact at speed followed by stillness', () {
      final detector = CrashDetector(thresholdG: 3.5);
      detector.addSpeed(46, at(-2000));
      expect(detector.addAcceleration(25, 30, 20, at(0)), isNull);
      expect(detector.isEvaluating, isTrue);
      detector.addSpeed(1, at(1500));
      final event = feedStill(detector, 100, 5500);
      expect(event, isNotNull);
      expect(event!.peakG, greaterThan(4));
      expect(event.speedBeforeKmh, 46);
    });

    test('ignores a dropped phone while standing still', () {
      final detector = CrashDetector();
      detector.addSpeed(0, at(-2000));
      expect(detector.addAcceleration(25, 30, 20, at(0)), isNull);
      expect(detector.isEvaluating, isFalse);
    });

    test('ignores shocks below the threshold (bumpy road)', () {
      final detector = CrashDetector(thresholdG: 3.5);
      detector.addSpeed(50, at(-1000));
      // ≈ 2.5g pothole.
      expect(detector.addAcceleration(15, 15, 12, at(0)), isNull);
      expect(detector.isEvaluating, isFalse);
    });

    test('rejects when the rider keeps moving after the shock', () {
      final detector = CrashDetector();
      detector.addSpeed(40, at(-1000));
      detector.addAcceleration(25, 30, 20, at(0));
      detector.addSpeed(38, at(2000));
      expect(feedStill(detector, 100, 5500), isNull);
      expect(detector.isEvaluating, isFalse);
    });

    test('rejects when the phone keeps shaking after the shock', () {
      final detector = CrashDetector();
      detector.addSpeed(40, at(-1000));
      detector.addAcceleration(25, 30, 20, at(0));
      detector.addSpeed(0, at(2000));
      CrashEvent? event;
      for (var ms = 100; ms <= 5500; ms += 100) {
        final z = ms % 200 == 0 ? 25.0 : 2.0;
        event ??= detector.addAcceleration(0, 0, z, at(ms));
      }
      expect(event, isNull);
    });

    test('does nothing while disabled (rest mode)', () {
      final detector = CrashDetector()..enabled = false;
      detector.addSpeed(46, at(-1000));
      expect(detector.addAcceleration(25, 30, 20, at(0)), isNull);
      expect(detector.isEvaluating, isFalse);
    });

    test('sensitivity maps to thresholds', () {
      expect(crashThresholdG('high'), 3.0);
      expect(crashThresholdG('medium'), 3.5);
      expect(crashThresholdG('low'), 4.0);
      expect(crashThresholdG(null), 3.5);
    });
  });

  group('IdleDetector', () {
    test('asks once after the idle threshold', () {
      final detector = IdleDetector(idleMinutes: 15);
      detector.update(0.5, t0);
      expect(detector.check(t0.add(const Duration(minutes: 14))), isFalse);
      expect(detector.check(t0.add(const Duration(minutes: 15))), isTrue);
      expect(detector.check(t0.add(const Duration(minutes: 16))), isFalse);
    });

    test('moving resets the timer', () {
      final detector = IdleDetector(idleMinutes: 15);
      detector.update(0, t0);
      detector.update(30, t0.add(const Duration(minutes: 10)));
      detector.update(0, t0.add(const Duration(minutes: 11)));
      expect(detector.check(t0.add(const Duration(minutes: 20))), isFalse);
      expect(detector.check(t0.add(const Duration(minutes: 26))), isTrue);
    });

    test('acknowledge restarts the countdown', () {
      final detector = IdleDetector(idleMinutes: 15);
      detector.update(0, t0);
      final prompt = t0.add(const Duration(minutes: 15));
      expect(detector.check(prompt), isTrue);
      detector.acknowledge(prompt);
      expect(detector.check(prompt.add(const Duration(minutes: 14))), isFalse);
      expect(detector.check(prompt.add(const Duration(minutes: 15))), isTrue);
    });

    test('rest mode suppresses the prompt until it ends', () {
      final detector = IdleDetector(idleMinutes: 15);
      detector.update(0, t0);
      detector.startRest(t0, const Duration(minutes: 30));
      expect(detector.check(t0.add(const Duration(minutes: 20))), isFalse);
      expect(detector.isResting(t0.add(const Duration(minutes: 29))), isTrue);
      final end = t0.add(const Duration(minutes: 30));
      detector.endRest(end);
      expect(detector.check(end.add(const Duration(minutes: 10))), isFalse);
      expect(detector.check(end.add(const Duration(minutes: 15))), isTrue);
    });

    test('planned stops allow a longer stay', () {
      const cafe = LatLng(11.5481, 107.8039);
      final detector = IdleDetector(
        idleMinutes: 15,
        plannedStops: const [PlannedStop(point: cafe, stayMinutes: 30)],
      );
      detector.update(0, t0, point: const LatLng(11.5482, 107.8040));
      expect(detector.check(t0.add(const Duration(minutes: 20))), isFalse);
      expect(detector.check(t0.add(const Duration(minutes: 45))), isTrue);
    });

    test('simulateElapsed triggers immediately (Debug Simulator)', () {
      final detector = IdleDetector(idleMinutes: 15);
      detector.update(0, t0);
      detector.simulateElapsed(t0);
      expect(detector.check(t0), isTrue);
    });
  });

  group('geo utils', () {
    test('decodes a Google encoded polyline', () {
      final points = decodePolyline('_p~iF~ps|U_ulLnnqC_mqNvxq`@');
      expect(points, hasLength(3));
      expect(points.first.latitude, closeTo(38.5, 1e-5));
      expect(points.first.longitude, closeTo(-120.2, 1e-5));
      expect(points.last.latitude, closeTo(43.252, 1e-5));
      expect(points.last.longitude, closeTo(-126.453, 1e-5));
    });

    test('empty polyline decodes to no points', () {
      expect(decodePolyline(null), isEmpty);
      expect(decodePolyline(''), isEmpty);
    });

    test('distance between two points', () {
      final d = metersBetween(
        const LatLng(10.7769, 106.7009),
        const LatLng(10.7869, 106.7009),
      );
      expect(d, closeTo(1112, 5));
    });

    test('formats durations like the Figma screens', () {
      expect(formatMinutes(430), '7g 10p');
      expect(formatMinutes(45), '45p');
      expect(formatMinutes(120), '2g');
      expect(formatDistance(300), '300 m');
      expect(formatDistance(86400), '86.4 km');
      expect(formatKm(308), '308 km');
    });
  });
}
