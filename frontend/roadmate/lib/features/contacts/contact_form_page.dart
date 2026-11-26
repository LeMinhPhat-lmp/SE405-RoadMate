part of '../../main.dart';

/// RM/34 – add or edit one emergency contact. Pops with true when saved or
/// deleted.
class ContactFormPage extends StatefulWidget {
  const ContactFormPage({
    required this.api,
    this.contact,
    this.suggestedPriority = 1,
    super.key,
  });
  final RoadMateApi api;
  final Map<String, dynamic>? contact;
  final int suggestedPriority;

  @override
  State<ContactFormPage> createState() => _ContactFormPageState();
}

class _ContactFormPageState extends State<ContactFormPage> {
  static const relations = ['Gia đình', 'Bạn bè', 'Người yêu', 'Đồng nghiệp'];
  late final name = TextEditingController(
    text: widget.contact?['name'] as String?,
  );
  late final phone = TextEditingController(
    text: widget.contact?['phone'] as String?,
  );
  late String relation = widget.contact?['relation'] as String? ?? 'Gia đình';
  late int priority =
      (widget.contact?['priority'] as num?)?.toInt() ??
      widget.suggestedPriority;
  bool saving = false;
  String? error;

  bool get editing => widget.contact != null;

  static final _phonePattern = RegExp(r'^(\+?84|0)\d{9,10}$');

  Future<void> _save() async {
    final nameValue = name.text.trim();
    final phoneValue = phone.text.replaceAll(RegExp(r'[\s.-]'), '');
    if (nameValue.isEmpty) {
      setState(() => error = 'Nhập tên liên hệ.');
      return;
    }
    if (!_phonePattern.hasMatch(phoneValue)) {
      setState(() => error = 'Số điện thoại Việt Nam không hợp lệ.');
      return;
    }
    setState(() {
      saving = true;
      error = null;
    });
    try {
      if (editing) {
        await widget.api.updateContact(
          widget.contact!['id'] as String,
          name: nameValue,
          phone: phoneValue,
          relation: relation,
          priority: priority,
        );
      } else {
        await widget.api.addContact(
          nameValue,
          phoneValue,
          relation,
          priority: priority,
        );
      }
      if (mounted) Navigator.pop(context, true);
    } on Object catch (exception) {
      if (mounted) setState(() => error = '$exception');
    } finally {
      if (mounted) setState(() => saving = false);
    }
  }

  Future<void> _delete() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('Xoá ${widget.contact!['name']}?'),
        content: const Text('Người này sẽ không nhận cảnh báo nữa.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Huỷ'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Xoá'),
          ),
        ],
      ),
    );
    if (confirm != true) return;
    setState(() => saving = true);
    try {
      await widget.api.deleteContact(widget.contact!['id'] as String);
      if (mounted) Navigator.pop(context, true);
    } on Object catch (exception) {
      if (mounted) setState(() => error = '$exception');
    } finally {
      if (mounted) setState(() => saving = false);
    }
  }

  @override
  void dispose() {
    name.dispose();
    phone.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: Text(editing ? 'Sửa liên hệ' : 'Thêm liên hệ khẩn cấp'),
    ),
    body: ListView(
      padding: const EdgeInsets.all(20),
      children: [
        TextField(
          controller: name,
          textCapitalization: TextCapitalization.words,
          decoration: const InputDecoration(labelText: 'Tên'),
        ),
        const SizedBox(height: 12),
        TextField(
          controller: phone,
          keyboardType: TextInputType.phone,
          decoration: const InputDecoration(
            labelText: 'Số điện thoại',
            hintText: '09xx xxx xxx',
          ),
        ),
        const SectionLabel('Mối quan hệ'),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            for (final value in {...relations, relation})
              ChoiceChip(
                label: Text(value),
                selected: relation == value,
                onSelected: (_) => setState(() => relation = value),
              ),
          ],
        ),
        const SectionLabel('Thứ tự ưu tiên'),
        SegmentedButton<int>(
          segments: [
            for (var i = 1; i <= ContactsPage.maxContacts; i++)
              ButtonSegment(value: i, label: Text('$i')),
          ],
          selected: {priority},
          onSelectionChanged: (value) => setState(() => priority = value.first),
        ),
        const SizedBox(height: 16),
        const Card(
          child: Column(
            children: [
              ListTile(
                leading: Icon(Icons.sms_outlined),
                title: Text('SMS (hoạt động khi không có mạng)'),
              ),
              ListTile(
                leading: Icon(Icons.notifications_active_outlined),
                title: Text('Push notification (nếu người thân có app)'),
              ),
            ],
          ),
        ),
        if (error != null) ErrorBanner(error!),
      ],
    ),
    bottomNavigationBar: SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 12),
        child: Row(
          children: [
            if (editing) ...[
              OutlinedButton(
                style: OutlinedButton.styleFrom(
                  foregroundColor: RoadMateColors.danger,
                ),
                onPressed: saving ? null : _delete,
                child: const Text('Xoá'),
              ),
              const SizedBox(width: 10),
            ],
            Expanded(
              child: FilledButton(
                onPressed: saving ? null : _save,
                child: Text(saving ? 'Đang lưu…' : 'Lưu liên hệ'),
              ),
            ),
          ],
        ),
      ),
    ),
  );
}
