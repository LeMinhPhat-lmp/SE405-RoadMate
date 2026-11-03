part of '../../main.dart';

class ContactsPage extends StatefulWidget {
  const ContactsPage({required this.api, super.key});
  final RoadMateApi api;

  @override
  State<ContactsPage> createState() => _ContactsPageState();
}

class _ContactsPageState extends State<ContactsPage> {
  late Future<List<Map<String, dynamic>>> contacts;

  @override
  void initState() {
    super.initState();
    contacts = widget.api.getContacts();
  }

  Future<void> add() async {
    final name = TextEditingController();
    final phone = TextEditingController();
    final relation = TextEditingController(text: 'Friend');
    final accepted = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Emergency contact'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: name,
              decoration: const InputDecoration(labelText: 'Name'),
            ),
            TextField(
              controller: phone,
              decoration: const InputDecoration(labelText: 'Phone'),
              keyboardType: TextInputType.phone,
            ),
            TextField(
              controller: relation,
              decoration: const InputDecoration(labelText: 'Relation'),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Save'),
          ),
        ],
      ),
    );
    if (accepted != true || name.text.isEmpty || phone.text.isEmpty) {
      name.dispose();
      phone.dispose();
      relation.dispose();
      return;
    }
    try {
      await widget.api.addContact(name.text, phone.text, relation.text);
      if (mounted) setState(() => contacts = widget.api.getContacts());
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    }
    name.dispose();
    phone.dispose();
    relation.dispose();
  }

  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(20),
    children: [
      Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          const Text(
            'Emergency contacts',
            style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
          ),
          IconButton(onPressed: add, icon: const Icon(Icons.add_circle)),
        ],
      ),
      FutureBuilder<List<Map<String, dynamic>>>(
        future: contacts,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const CircularProgressIndicator();
          }
          if (snapshot.hasError) return ErrorBanner(snapshot.error.toString());
          final values = snapshot.data ?? [];
          if (values.isEmpty) {
            return const EmptyState(
              text: 'Add someone who should know when you need help.',
            );
          }
          return Column(
            children: values
                .map(
                  (contact) => Card(
                    child: ListTile(
                      leading: const CircleAvatar(child: Icon(Icons.person)),
                      title: Text(contact['name'] as String),
                      subtitle: Text(
                        '${contact['phone']} · ${contact['relation'] ?? 'Contact'}',
                      ),
                    ),
                  ),
                )
                .toList(),
          );
        },
      ),
    ],
  );
}
