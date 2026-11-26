part of '../../main.dart';

/// RM/32 – personal, medical and vehicle info sent with rescue alerts.
class MedicalInfoPage extends StatefulWidget {
  const MedicalInfoPage({required this.api, required this.me, super.key});
  final RoadMateApi api;
  final Map<String, dynamic> me;

  @override
  State<MedicalInfoPage> createState() => _MedicalInfoPageState();
}

class _MedicalInfoPageState extends State<MedicalInfoPage> {
  static const bloodTypes = ['O+', 'O-', 'A+', 'A-', 'B+', 'B-', 'AB+', 'AB-'];
  static const vehicles = ['Xe máy', 'Ô tô'];
  late final name = TextEditingController(text: widget.me['name'] as String?);
  late final medical = TextEditingController(
    text: widget.me['medicalNotes'] as String?,
  );
  late final plate = TextEditingController(
    text: widget.me['plateNumber'] as String?,
  );
  late final model = TextEditingController(text: _modelFrom(widget.me));
  late String? bloodType = widget.me['bloodType'] as String?;
  late String vehicle = _vehicleFrom(widget.me);
  bool saving = false;
  String? error;

  /// vehicleType is stored as "Xe máy · Exciter 155" (kind · model).
  static String _vehicleFrom(Map<String, dynamic> me) {
    final value = me['vehicleType'] as String? ?? '';
    return value.startsWith('Ô tô') ? 'Ô tô' : 'Xe máy';
  }

  static String _modelFrom(Map<String, dynamic> me) {
    final value = me['vehicleType'] as String? ?? '';
    final parts = value.split(' · ');
    if (parts.length > 1) return parts.sublist(1).join(' · ');
    return vehicles.contains(value) ? '' : value;
  }

  Future<void> _save() async {
    setState(() {
      saving = true;
      error = null;
    });
    final modelText = model.text.trim();
    try {
      await widget.api.updateMe(
        name: name.text.trim().isEmpty ? null : name.text.trim(),
        bloodType: bloodType,
        medicalNotes: medical.text.trim(),
        vehicleType: modelText.isEmpty ? vehicle : '$vehicle · $modelText',
        plateNumber: plate.text.trim().toUpperCase(),
      );
      if (FirebaseBridge.available && name.text.trim().isNotEmpty) {
        await FirebaseAuth.instance.currentUser?.updateDisplayName(
          name.text.trim(),
        );
      }
      if (mounted) Navigator.pop(context);
    } on Object catch (exception) {
      if (mounted) setState(() => error = '$exception');
    } finally {
      if (mounted) setState(() => saving = false);
    }
  }

  @override
  void dispose() {
    name.dispose();
    medical.dispose();
    plate.dispose();
    model.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Thông tin y tế & xe'),
          Text(
            'Gửi kèm khi có cảnh báo cứu hộ',
            style: TextStyle(fontSize: 13, color: RoadMateColors.muted),
          ),
        ],
      ),
    ),
    body: ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Center(child: InitialAvatar(name.text, radius: 42)),
        const SizedBox(height: 16),
        TextField(
          controller: name,
          textCapitalization: TextCapitalization.words,
          decoration: const InputDecoration(labelText: 'Họ và tên'),
          onChanged: (_) => setState(() {}),
        ),
        const SizedBox(height: 12),
        DropdownButtonFormField<String>(
          initialValue: bloodTypes.contains(bloodType) ? bloodType : null,
          decoration: const InputDecoration(labelText: 'Nhóm máu'),
          items: [
            for (final type in bloodTypes)
              DropdownMenuItem(value: type, child: Text(type)),
          ],
          onChanged: (value) => setState(() => bloodType = value),
        ),
        const SizedBox(height: 12),
        TextField(
          controller: medical,
          maxLines: 2,
          decoration: const InputDecoration(
            labelText: 'Dị ứng / bệnh nền',
            hintText: 'Ví dụ: dị ứng penicillin',
          ),
        ),
        const SectionLabel('Phương tiện'),
        SegmentedButton<String>(
          segments: [
            for (final value in vehicles)
              ButtonSegment(
                value: value,
                label: Text(value),
                icon: Icon(
                  value == 'Ô tô' ? Icons.directions_car : Icons.two_wheeler,
                ),
              ),
          ],
          selected: {vehicle},
          onSelectionChanged: (value) => setState(() => vehicle = value.first),
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            Expanded(
              child: TextField(
                controller: model,
                decoration: const InputDecoration(labelText: 'Dòng xe'),
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: TextField(
                controller: plate,
                textCapitalization: TextCapitalization.characters,
                decoration: const InputDecoration(labelText: 'Biển số'),
              ),
            ),
          ],
        ),
        if (error != null) ErrorBanner(error!),
      ],
    ),
    bottomNavigationBar: SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 12),
        child: FilledButton(
          onPressed: saving ? null : _save,
          child: Text(saving ? 'Đang lưu…' : 'Lưu thông tin'),
        ),
      ),
    ),
  );
}
