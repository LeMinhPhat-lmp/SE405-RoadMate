part of '../../main.dart';

/// RM/31 – profile header with the rescue info, safety shortcuts and app
/// settings.
class ProfilePage extends StatefulWidget {
  const ProfilePage({required this.api, this.runtime, super.key});
  final RoadMateApi api;
  final TripRuntime? runtime;

  @override
  State<ProfilePage> createState() => _ProfilePageState();
}

class _ProfilePageState extends State<ProfilePage> {
  late Future<(Map<String, dynamic>, List<Map<String, dynamic>>)> data =
      _load();

  Future<(Map<String, dynamic>, List<Map<String, dynamic>>)> _load() async {
    final results = await Future.wait([
      widget.api.getMe(),
      widget.api.getContacts(),
    ]);
    return (
      results[0] as Map<String, dynamic>,
      results[1] as List<Map<String, dynamic>>,
    );
  }

  void reload() => setState(() => data = _load());

  Future<void> _open(Widget page) async {
    await Navigator.push(context, MaterialPageRoute(builder: (_) => page));
    reload();
  }

  @override
  Widget build(BuildContext context) => RefreshIndicator(
    onRefresh: () async => reload(),
    child: FutureBuilder<(Map<String, dynamic>, List<Map<String, dynamic>>)>(
      future: data,
      builder: (context, snapshot) {
        final me = snapshot.data?.$1 ?? const <String, dynamic>{};
        final contacts = snapshot.data?.$2 ?? const <Map<String, dynamic>>[];
        return ListView(
          padding: const EdgeInsets.all(20),
          children: [
            if (snapshot.connectionState == ConnectionState.waiting)
              const LinearProgressIndicator(),
            if (snapshot.hasError) ErrorBanner(snapshot.error.toString()),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(18),
                child: Column(
                  children: [
                    Row(
                      children: [
                        InitialAvatar(me['name'] as String?, radius: 32),
                        const SizedBox(width: 14),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                me['name'] as String? ?? 'RoadMate rider',
                                style: const TextStyle(
                                  fontSize: 20,
                                  fontWeight: FontWeight.w800,
                                ),
                              ),
                              Text(
                                me['email'] as String? ??
                                    'Tài khoản phát triển',
                                style: const TextStyle(
                                  color: RoadMateColors.muted,
                                ),
                              ),
                            ],
                          ),
                        ),
                        IconButton(
                          tooltip: 'Sửa hồ sơ',
                          icon: const Icon(Icons.edit_outlined),
                          onPressed: snapshot.hasData
                              ? () => _open(
                                  MedicalInfoPage(api: widget.api, me: me),
                                )
                              : null,
                        ),
                      ],
                    ),
                    const SizedBox(height: 14),
                    Row(
                      children: [
                        Expanded(
                          child: MetricTile(
                            value: me['bloodType'] as String? ?? '--',
                            label: 'Nhóm máu',
                          ),
                        ),
                        Expanded(
                          child: MetricTile(
                            value: me['plateNumber'] as String? ?? '--',
                            label: 'Biển số',
                          ),
                        ),
                        Expanded(
                          child: MetricTile(
                            value: me['vehicleType'] as String? ?? '--',
                            label: 'Xe',
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
            const SectionLabel('An toàn'),
            Card(
              child: Column(
                children: [
                  ListTile(
                    leading: const Icon(Icons.contact_phone_outlined),
                    title: const Text('Liên hệ khẩn cấp'),
                    subtitle: Text(
                      '${contacts.length}/${ContactsPage.maxContacts} người'
                      '${contacts.isEmpty ? '' : ' · ${contacts.map((c) => c['name']).join(', ')}'}',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () =>
                        _open(ContactsPage(api: widget.api, showBack: true)),
                  ),
                  ListTile(
                    leading: const Icon(Icons.medical_information_outlined),
                    title: const Text('Thông tin y tế & xe'),
                    subtitle: const Text('Nhóm máu, dị ứng, biển số'),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: snapshot.hasData
                        ? () => _open(MedicalInfoPage(api: widget.api, me: me))
                        : null,
                  ),
                  ListTile(
                    leading: const Icon(Icons.tune),
                    title: const Text('Ngưỡng cảnh báo & cài đặt'),
                    subtitle: Text(
                      'Dừng ${me['idleMinutes'] ?? 15}p · độ nhạy '
                      '${crashSensitivityLabel(me['crashSensitivity'] as String?)}',
                    ),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: snapshot.hasData
                        ? () => _open(
                            SettingsPage(
                              api: widget.api,
                              runtime: widget.runtime,
                              me: me,
                            ),
                          )
                        : null,
                  ),
                  if (widget.runtime != null)
                    ListTile(
                      leading: const Icon(Icons.bug_report_outlined),
                      title: const Text('Debug Simulator'),
                      subtitle: const Text(
                        'Giả lập va chạm, dừng lâu… để demo',
                      ),
                      trailing: const Icon(Icons.chevron_right),
                      onTap: () => _open(
                        DebugSimulatorPage(
                          api: widget.api,
                          runtime: widget.runtime!,
                        ),
                      ),
                    ),
                  if (widget.runtime != null)
                    ListTile(
                      leading: const Icon(
                        Icons.emergency_outlined,
                        color: RoadMateColors.danger,
                      ),
                      title: const Text('SOS khẩn cấp'),
                      subtitle: const Text('Giữ 3 giây · gọi 115 / 113 / 112'),
                      trailing: const Icon(Icons.chevron_right),
                      onTap: () =>
                          Navigator.of(context)
                              .push(SosPage.route(widget.runtime!)),
                    ),
                ],
              ),
            ),
            const SizedBox(height: 12),
            const Text(
              'RoadMate là sản phẩm học tập, không thay thế dịch vụ cứu hộ '
              'chuyên nghiệp. Hãy chủ động gọi cấp cứu khi có sự cố nghiêm trọng.',
              style: TextStyle(color: RoadMateColors.muted, fontSize: 12),
            ),
          ],
        );
      },
    ),
  );
}
