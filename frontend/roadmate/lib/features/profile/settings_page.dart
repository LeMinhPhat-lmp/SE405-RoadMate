part of '../../main.dart';

/// RM/35 – alert thresholds (synced to the profile) and app preferences.
class SettingsPage extends StatefulWidget {
  const SettingsPage({
    required this.api,
    required this.me,
    this.runtime,
    super.key,
  });
  final RoadMateApi api;
  final Map<String, dynamic> me;
  final TripRuntime? runtime;

  @override
  State<SettingsPage> createState() => _SettingsPageState();
}

class _SettingsPageState extends State<SettingsPage> {
  late String sensitivity =
      widget.me['crashSensitivity'] as String? ?? 'medium';
  late int idleMinutes = (widget.me['idleMinutes'] as num?)?.toInt() ?? 15;
  String? error;

  Future<void> _sync({String? crashSensitivity, int? idle}) async {
    try {
      await widget.api.updateMe(
        crashSensitivity: crashSensitivity,
        idleMinutes: idle,
      );
      setState(() => error = null);
    } on Object catch (exception) {
      setState(() => error = '$exception');
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Cài đặt')),
    body: ListView(
      padding: const EdgeInsets.all(20),
      children: [
        const SectionLabel('Ngưỡng cảnh báo'),
        Card(
          child: Column(
            children: [
              Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        const Expanded(
                          child: Text(
                            'Độ nhạy phát hiện va chạm',
                            style: TextStyle(fontWeight: FontWeight.w700),
                          ),
                        ),
                        Text('${crashThresholdG(sensitivity)}g'),
                      ],
                    ),
                    const SizedBox(height: 10),
                    SegmentedButton<String>(
                      segments: const [
                        ButtonSegment(value: 'high', label: Text('Nhạy (3g)')),
                        ButtonSegment(
                          value: 'medium',
                          label: Text('Trung bình'),
                        ),
                        ButtonSegment(value: 'low', label: Text('Ít (4g)')),
                      ],
                      selected: {sensitivity},
                      onSelectionChanged: (value) {
                        setState(() => sensitivity = value.first);
                        unawaited(_sync(crashSensitivity: value.first));
                      },
                    ),
                  ],
                ),
              ),
              ListTile(
                title: const Text('Cảnh báo dừng lâu'),
                subtitle: const Text('Sau khi đứng yên'),
                trailing: DropdownButton<int>(
                  value: idleMinutes,
                  underline: const SizedBox.shrink(),
                  items: {1, 5, 10, 15, 20, 30, 45, 60, idleMinutes}
                      .map(
                        (value) => DropdownMenuItem(
                          value: value,
                          child: Text('$value phút'),
                        ),
                      )
                      .toList(),
                  onChanged: (value) {
                    if (value == null) return;
                    setState(() => idleMinutes = value);
                    unawaited(_sync(idle: value));
                  },
                ),
              ),
              const ListTile(
                title: Text('Đếm ngược trước khi gửi'),
                subtitle: Text('Màn hình "Bạn có ổn không?"'),
                trailing: Text('30 giây'),
              ),
            ],
          ),
        ),
        if (error != null) ErrorBanner(error!),
        const SectionLabel('Chung'),
        Card(
          child: Column(
            children: [
              ValueListenableBuilder<ThemeMode>(
                valueListenable: RoadMateApp.themeMode,
                builder: (context, mode, _) => ListTile(
                  title: const Text('Giao diện'),
                  trailing: DropdownButton<ThemeMode>(
                    value: mode,
                    underline: const SizedBox.shrink(),
                    items: const [
                      DropdownMenuItem(
                        value: ThemeMode.system,
                        child: Text('Theo hệ thống'),
                      ),
                      DropdownMenuItem(
                        value: ThemeMode.light,
                        child: Text('Sáng'),
                      ),
                      DropdownMenuItem(
                        value: ThemeMode.dark,
                        child: Text('Tối'),
                      ),
                    ],
                    onChanged: (value) {
                      if (value == null) return;
                      unawaited(RoadMateApp.setThemeMode(value));
                      unawaited(
                        widget.api
                            .updateMe(theme: value.name)
                            .catchError((Object _) => <String, dynamic>{}),
                      );
                    },
                  ),
                ),
              ),
              const ListTile(
                title: Text('Ngôn ngữ'),
                trailing: Text('Tiếng Việt'),
              ),
              if (widget.runtime != null)
                AnimatedBuilder(
                  animation: widget.runtime!,
                  builder: (context, _) => SwitchListTile(
                    title: const Text('Hướng dẫn bằng giọng nói'),
                    subtitle: const Text('Đọc hướng rẽ (TTS)'),
                    value: widget.runtime!.voiceEnabled,
                    onChanged: widget.runtime!.setVoiceEnabled,
                  ),
                ),
              if (FirebaseBridge.available)
                ListTile(
                  leading: const Icon(
                    Icons.logout,
                    color: RoadMateColors.danger,
                  ),
                  title: const Text(
                    'Đăng xuất',
                    style: TextStyle(color: RoadMateColors.danger),
                  ),
                  onTap: () async {
                    final navigator = Navigator.of(context);
                    await widget.runtime?.stopTracking();
                    await FirebaseAuth.instance.signOut();
                    navigator.popUntil((route) => route.isFirst);
                  },
                ),
            ],
          ),
        ),
      ],
    ),
  );
}
