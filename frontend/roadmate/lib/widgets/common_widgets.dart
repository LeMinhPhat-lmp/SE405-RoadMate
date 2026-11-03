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
          Text(label, style: const TextStyle(color: Colors.black54)),
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
        style: const TextStyle(color: Colors.black54),
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
