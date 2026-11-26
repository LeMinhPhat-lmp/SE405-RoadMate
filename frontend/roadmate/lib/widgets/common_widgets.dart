part of '../main.dart';

class StatCard extends StatelessWidget {
  const StatCard({
    required this.label,
    required this.value,
    required this.icon,
    super.key,
  });
  final String label;
  final String value;
  final IconData icon;

  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: Theme.of(context).colorScheme.primary),
          const SizedBox(height: 8),
          Text(label, style: const TextStyle(color: RoadMateColors.muted)),
          Text(value, style: const TextStyle(fontWeight: FontWeight.bold)),
        ],
      ),
    ),
  );
}

class ChecklistItem extends StatelessWidget {
  const ChecklistItem({required this.text, super.key});
  final String text;

  @override
  Widget build(BuildContext context) => ListTile(
    contentPadding: EdgeInsets.zero,
    leading: const Icon(Icons.check_circle_outline),
    title: Text(text),
  );
}

class EmptyState extends StatelessWidget {
  const EmptyState({required this.text, super.key});
  final String text;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 32),
    child: Center(
      child: Text(
        text,
        textAlign: TextAlign.center,
        style: const TextStyle(color: RoadMateColors.muted),
      ),
    ),
  );
}

class ErrorBanner extends StatelessWidget {
  const ErrorBanner(this.message, {super.key});
  final String message;

  @override
  Widget build(BuildContext context) => Card(
    color: Colors.red.shade50,
    child: Padding(
      padding: const EdgeInsets.all(12),
      child: Text(message, style: TextStyle(color: Colors.red.shade900)),
    ),
  );
}

/// Small uppercase section header ("NGƯỜI NHẬN CẢNH BÁO" in Figma).
class SectionLabel extends StatelessWidget {
  const SectionLabel(this.text, {super.key});
  final String text;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.fromLTRB(4, 18, 4, 8),
    child: Text(
      text.toUpperCase(),
      style: const TextStyle(
        fontSize: 12,
        fontWeight: FontWeight.w700,
        letterSpacing: 0.6,
        color: RoadMateColors.muted,
      ),
    ),
  );
}

class InitialAvatar extends StatelessWidget {
  const InitialAvatar(this.name, {this.radius = 20, this.color, super.key});
  final String? name;
  final double radius;
  final Color? color;

  static String initials(String? name) {
    final parts = (name ?? '').trim().split(RegExp(r'\s+'))
      ..removeWhere((part) => part.isEmpty);
    if (parts.isEmpty) return '?';
    if (parts.length == 1) return parts.first.characters.first.toUpperCase();
    return (parts[parts.length - 2].characters.first +
            parts.last.characters.first)
        .toUpperCase();
  }

  @override
  Widget build(BuildContext context) => CircleAvatar(
    radius: radius,
    backgroundColor: color ?? RoadMateColors.primarySoft,
    child: Text(
      initials(name),
      style: TextStyle(
        color: RoadMateColors.primaryDeep,
        fontWeight: FontWeight.w800,
        fontSize: radius * 0.7,
      ),
    ),
  );
}

/// Value + caption tile used by the stats grids.
class MetricTile extends StatelessWidget {
  const MetricTile({
    required this.value,
    required this.label,
    this.inverted = false,
    super.key,
  });
  final String value;
  final String label;
  final bool inverted;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 10),
    decoration: BoxDecoration(
      color: inverted
          ? Colors.white.withValues(alpha: 0.08)
          : Theme.of(context).colorScheme.surface,
      borderRadius: BorderRadius.circular(14),
    ),
    child: Column(
      children: [
        FittedBox(
          child: Text(
            value,
            style: TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.w800,
              color: inverted ? Colors.white : null,
            ),
          ),
        ),
        const SizedBox(height: 2),
        Text(
          label,
          textAlign: TextAlign.center,
          style: TextStyle(
            fontSize: 12,
            color: inverted ? Colors.white70 : RoadMateColors.muted,
          ),
        ),
      ],
    ),
  );
}

class MapPin extends StatelessWidget {
  const MapPin({required this.label, required this.color, super.key});
  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) => Container(
    alignment: Alignment.center,
    decoration: BoxDecoration(
      color: color,
      shape: BoxShape.circle,
      border: Border.all(color: Colors.white, width: 3),
      boxShadow: const [BoxShadow(blurRadius: 6, color: Colors.black26)],
    ),
    child: Text(
      label,
      style: const TextStyle(
        color: Colors.white,
        fontWeight: FontWeight.w800,
        fontSize: 12,
      ),
    ),
  );
}

/// OpenStreetMap-based map (no API key needed) with the planned route,
/// optional traveled track, stop pins and the rider position.
class RoadMap extends StatelessWidget {
  const RoadMap({
    this.controller,
    this.route = const [],
    this.traveled = const [],
    this.stops = const [],
    this.current,
    this.heading,
    this.extraMarkers = const [],
    this.interactive = true,
    this.initialZoom = 13,
    super.key,
  });

  final MapController? controller;
  final List<LatLng> route;
  final List<LatLng> traveled;

  /// Origin, waypoints, destination in order.
  final List<LatLng> stops;
  final LatLng? current;
  final double? heading;
  final List<Marker> extraMarkers;
  final bool interactive;
  final double initialZoom;

  @override
  Widget build(BuildContext context) {
    final points = [...route, ...stops, ?current];
    final fit = points.length >= 2
        ? CameraFit.coordinates(
            coordinates: points,
            padding: const EdgeInsets.all(48),
            maxZoom: 16,
          )
        : null;
    return FlutterMap(
      mapController: controller,
      options: MapOptions(
        initialCenter: points.isEmpty ? defaultMapCenter : points.first,
        initialZoom: initialZoom,
        initialCameraFit: fit,
        interactionOptions: InteractionOptions(
          flags: interactive ? InteractiveFlag.all : InteractiveFlag.none,
        ),
      ),
      children: [
        TileLayer(
          urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
          userAgentPackageName: 'com.example.roadmate',
        ),
        PolylineLayer(
          polylines: [
            if (route.length >= 2)
              Polyline(
                points: route,
                strokeWidth: 6,
                color: RoadMateColors.primary,
                borderStrokeWidth: 2,
                borderColor: RoadMateColors.primaryDeep,
              ),
            if (traveled.length >= 2)
              Polyline(
                points: traveled,
                strokeWidth: 5,
                color: RoadMateColors.violet,
              ),
          ],
        ),
        MarkerLayer(
          markers: [
            for (var i = 0; i < stops.length; i++)
              Marker(
                point: stops[i],
                width: 30,
                height: 30,
                child: MapPin(
                  label: i == 0
                      ? 'A'
                      : i == stops.length - 1
                      ? 'B'
                      : '$i',
                  color: i == 0
                      ? RoadMateColors.primaryDeep
                      : i == stops.length - 1
                      ? RoadMateColors.coral
                      : RoadMateColors.violet,
                ),
              ),
            ...extraMarkers,
            if (current != null)
              Marker(
                point: current!,
                width: 44,
                height: 44,
                child: Transform.rotate(
                  angle: (heading ?? 0) * math.pi / 180,
                  child: Container(
                    decoration: BoxDecoration(
                      color: RoadMateColors.sky,
                      shape: BoxShape.circle,
                      border: Border.all(color: Colors.white, width: 3),
                      boxShadow: const [
                        BoxShadow(blurRadius: 8, color: Colors.black26),
                      ],
                    ),
                    child: const Icon(
                      Icons.navigation,
                      color: Colors.white,
                      size: 22,
                    ),
                  ),
                ),
              ),
          ],
        ),
        const RichAttributionWidget(
          attributions: [TextSourceAttribution('OpenStreetMap contributors')],
        ),
      ],
    );
  }
}

/// Press and hold for [holdDuration] to trigger (avoids accidental SOS).
class SosHoldButton extends StatefulWidget {
  const SosHoldButton({
    required this.onTriggered,
    this.size = 72,
    this.holdDuration = const Duration(seconds: 3),
    this.onProgress,
    super.key,
  });
  final VoidCallback onTriggered;
  final double size;
  final Duration holdDuration;
  final ValueChanged<double>? onProgress;

  @override
  State<SosHoldButton> createState() => _SosHoldButtonState();
}

class _SosHoldButtonState extends State<SosHoldButton>
    with SingleTickerProviderStateMixin {
  late final AnimationController _hold = AnimationController(
    vsync: this,
    duration: widget.holdDuration,
  );

  @override
  void initState() {
    super.initState();
    _hold.addListener(() => widget.onProgress?.call(_hold.value));
    _hold.addStatusListener((status) {
      if (status == AnimationStatus.completed) {
        HapticFeedback.heavyImpact();
        widget.onTriggered();
        _hold.reset();
      }
    });
  }

  @override
  void dispose() {
    _hold.dispose();
    super.dispose();
  }

  void _cancel() {
    if (_hold.status != AnimationStatus.completed) _hold.reverse();
  }

  @override
  Widget build(BuildContext context) => Semantics(
    button: true,
    label: 'SOS, giữ ${widget.holdDuration.inSeconds} giây để gửi',
    child: GestureDetector(
      onTapDown: (_) {
        HapticFeedback.mediumImpact();
        _hold.forward();
      },
      onTapUp: (_) => _cancel(),
      onTapCancel: _cancel,
      child: AnimatedBuilder(
        animation: _hold,
        builder: (context, _) => SizedBox(
          width: widget.size,
          height: widget.size,
          child: Stack(
            alignment: Alignment.center,
            children: [
              Container(
                decoration: const BoxDecoration(
                  color: RoadMateColors.danger,
                  shape: BoxShape.circle,
                  boxShadow: [
                    BoxShadow(blurRadius: 12, color: Color(0x55e5484d)),
                  ],
                ),
              ),
              SizedBox.expand(
                child: CircularProgressIndicator(
                  value: _hold.value,
                  strokeWidth: 5,
                  color: Colors.white,
                  backgroundColor: Colors.white24,
                ),
              ),
              Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    'SOS',
                    style: TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.w900,
                      fontSize: widget.size * 0.24,
                    ),
                  ),
                  Text(
                    'giữ ${widget.holdDuration.inSeconds}s',
                    style: TextStyle(
                      color: Colors.white70,
                      fontSize: widget.size * 0.13,
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    ),
  );
}
