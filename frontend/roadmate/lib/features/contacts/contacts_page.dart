part of '../../main.dart';

/// RM/33 – up to 5 emergency contacts ordered by priority, with a test
/// message that does not raise a real alert.
class ContactsPage extends StatefulWidget {
  const ContactsPage({required this.api, this.showBack = false, super.key});
  final RoadMateApi api;
  final bool showBack;

  static const maxContacts = 5;

  @override
  State<ContactsPage> createState() => _ContactsPageState();
}

class _ContactsPageState extends State<ContactsPage> {
  late Future<List<Map<String, dynamic>>> contacts;
  bool testing = false;

  @override
  void initState() {
    super.initState();
    contacts = _load();
  }

  Future<List<Map<String, dynamic>>> _load() async {
    final list = await widget.api.getContacts();
    list.sort(
      (a, b) => ((a['priority'] as num?) ?? 9).compareTo(
        (b['priority'] as num?) ?? 9,
      ),
    );
    return list;
  }

  void reload() => setState(() => contacts = _load());

  Future<void> _edit(
    List<Map<String, dynamic>> all, [
    Map<String, dynamic>? contact,
  ]) async {
    final changed = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => ContactFormPage(
          api: widget.api,
          contact: contact,
          suggestedPriority: math.min(all.length + 1, ContactsPage.maxContacts),
        ),
      ),
    );
    if (changed == true) reload();
  }

  Future<void> _reorder(
    List<Map<String, dynamic>> list,
    int from,
    int to,
  ) async {
    final item = list.removeAt(from);
    list.insert(to, item);
    setState(() {});
    try {
      for (var i = 0; i < list.length; i++) {
        final contact = list[i];
        if (contact['priority'] == i + 1) continue;
        await widget.api.updateContact(
          contact['id'] as String,
          name: contact['name'] as String,
          phone: contact['phone'] as String,
          relation: contact['relation'] as String? ?? '',
          priority: i + 1,
        );
      }
    } on Object catch (exception) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$exception')));
      }
    }
    reload();
  }

  Future<void> _testAll(List<Map<String, dynamic>> list) async {
    setState(() => testing = true);
    var sent = 0;
    String? failure;
    for (final contact in list) {
      try {
        await widget.api.testContact(contact['id'] as String);
        sent++;
      } on Object catch (exception) {
        failure = '$exception';
      }
    }
    if (!mounted) return;
    setState(() => testing = false);
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          failure == null
              ? 'Đã gửi thử tới $sent/${list.length} liên hệ'
              : 'Gửi thử $sent/${list.length} · lỗi: $failure',
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final body = RefreshIndicator(
      onRefresh: () async => reload(),
      child: FutureBuilder<List<Map<String, dynamic>>>(
        future: contacts,
        builder: (context, snapshot) {
          final values = snapshot.data ?? [];
          return ListView(
            padding: const EdgeInsets.all(20),
            children: [
              if (!widget.showBack)
                const Text(
                  'Liên hệ khẩn cấp',
                  style: TextStyle(fontSize: 28, fontWeight: FontWeight.w800),
                ),
              const Text(
                'Tối đa 5 người · kéo để đổi thứ tự ưu tiên',
                style: TextStyle(color: RoadMateColors.muted),
              ),
              const SizedBox(height: 12),
              Card(
                color: RoadMateColors.primarySoft,
                child: const ListTile(
                  leading: Icon(
                    Icons.shield_outlined,
                    color: RoadMateColors.primaryDeep,
                  ),
                  title: Text(
                    'Họ sẽ nhận SMS + push kèm vị trí khi bạn gặp sự cố hoặc bấm SOS.',
                    style: TextStyle(color: RoadMateColors.ink),
                  ),
                ),
              ),
              if (snapshot.connectionState == ConnectionState.waiting)
                const Padding(
                  padding: EdgeInsets.all(24),
                  child: Center(child: CircularProgressIndicator()),
                )
              else if (snapshot.hasError)
                ErrorBanner(snapshot.error.toString())
              else ...[
                if (values.isEmpty)
                  const EmptyState(
                    text: 'Thêm người cần biết khi bạn cần giúp đỡ.',
                  ),
                ReorderableListView(
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  onReorderItem: (from, to) => _reorder(values, from, to),
                  children: [
                    for (final contact in values)
                      Card(
                        key: ValueKey(contact['id']),
                        child: ListTile(
                          onTap: () => _edit(values, contact),
                          leading: InitialAvatar(contact['name'] as String?),
                          title: Row(
                            children: [
                              Flexible(
                                child: Text(
                                  contact['name'] as String,
                                  overflow: TextOverflow.ellipsis,
                                  style: const TextStyle(
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                              ),
                              const SizedBox(width: 6),
                              Text(
                                'Ưu tiên ${contact['priority']}',
                                style: const TextStyle(
                                  fontSize: 12,
                                  color: RoadMateColors.primaryDeep,
                                ),
                              ),
                            ],
                          ),
                          subtitle: Text(
                            '${contact['relation'] ?? 'Liên hệ'} · ${contact['phone']}'
                            '${contact['hasApp'] == true ? ' · có app' : ''}',
                          ),
                          trailing: IconButton(
                            tooltip: 'Gọi',
                            icon: const Icon(Icons.phone_outlined),
                            onPressed: () =>
                                openDialer(contact['phone'] as String),
                          ),
                        ),
                      ),
                  ],
                ),
                if (values.length < ContactsPage.maxContacts)
                  OutlinedButton.icon(
                    onPressed: () => _edit(values),
                    icon: const Icon(Icons.person_add_alt),
                    label: Text(
                      'Thêm liên hệ (còn ${ContactsPage.maxContacts - values.length})',
                    ),
                  ),
                const SizedBox(height: 16),
                if (values.isNotEmpty)
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Gửi thử cảnh báo',
                            style: TextStyle(fontWeight: FontWeight.w800),
                          ),
                          const Text(
                            'Gửi tin nhắn mẫu để chắc chắn người thân nhận được. '
                            'Không kích hoạt cảnh báo thật.',
                            style: TextStyle(color: RoadMateColors.muted),
                          ),
                          const SizedBox(height: 10),
                          FilledButton.icon(
                            onPressed: testing ? null : () => _testAll(values),
                            icon: const Icon(Icons.send),
                            label: Text(testing ? 'Đang gửi…' : 'Gửi thử ngay'),
                          ),
                        ],
                      ),
                    ),
                  ),
              ],
            ],
          );
        },
      ),
    );
    if (!widget.showBack) return body;
    return Scaffold(
      appBar: AppBar(title: const Text('Liên hệ khẩn cấp')),
      body: body,
    );
  }
}
