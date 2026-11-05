part of '../../main.dart';

class AuthPage extends StatefulWidget {
  const AuthPage({required this.api, super.key});
  final RoadMateApi api;

  @override
  State<AuthPage> createState() => _AuthPageState();
}

class _AuthPageState extends State<AuthPage> {
  final email = TextEditingController();
  final password = TextEditingController();
  final name = TextEditingController();
  bool registering = false;
  bool loading = false;
  String? error;

  @override
  void dispose() {
    email.dispose();
    password.dispose();
    name.dispose();
    super.dispose();
  }

  Future<void> submit() async {
    final emailValue = email.text.trim();
    final passwordValue = password.text;
    if (emailValue.isEmpty || passwordValue.length < 6) {
      setState(
        () => error = 'Enter an email and a password of at least 6 characters.',
      );
      return;
    }
    if (registering && name.text.trim().isEmpty) {
      setState(() => error = 'Enter your name.');
      return;
    }

    setState(() {
      loading = true;
      error = null;
    });
    try {
      final credential = registering
          ? await FirebaseAuth.instance.createUserWithEmailAndPassword(
              email: emailValue,
              password: passwordValue,
            )
          : await FirebaseAuth.instance.signInWithEmailAndPassword(
              email: emailValue,
              password: passwordValue,
            );
      if (registering) {
        await credential.user?.updateDisplayName(name.text.trim());
      }
      final user = FirebaseAuth.instance.currentUser;
      await widget.api.syncUser(
        user?.displayName?.trim().isNotEmpty == true
            ? user!.displayName!
            : emailValue,
      );
    } on FirebaseAuthException catch (exception) {
      if (mounted) setState(() => error = exception.message ?? exception.code);
    } on Object catch (exception) {
      if (mounted) setState(() => error = exception.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    body: SafeArea(
      child: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const Icon(Icons.shield_outlined, size: 64),
                const SizedBox(height: 16),
                Text(
                  registering
                      ? 'Create your RoadMate account'
                      : 'Welcome to RoadMate',
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    fontSize: 26,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  registering
                      ? 'Your account keeps trips and safety settings synced.'
                      : 'Sign in to sync your trips with the RoadMate API.',
                  textAlign: TextAlign.center,
                  style: const TextStyle(color: Colors.black54),
                ),
                const SizedBox(height: 28),
                if (registering)
                  TextField(
                    controller: name,
                    textInputAction: TextInputAction.next,
                    decoration: const InputDecoration(
                      labelText: 'Name',
                      border: OutlineInputBorder(),
                    ),
                  ),
                if (registering) const SizedBox(height: 12),
                TextField(
                  controller: email,
                  keyboardType: TextInputType.emailAddress,
                  textInputAction: TextInputAction.next,
                  decoration: const InputDecoration(
                    labelText: 'Email',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: password,
                  obscureText: true,
                  onSubmitted: (_) => submit(),
                  decoration: const InputDecoration(
                    labelText: 'Password',
                    helperText: 'At least 6 characters',
                    border: OutlineInputBorder(),
                  ),
                ),
                if (error != null) ...[
                  const SizedBox(height: 12),
                  ErrorBanner(error!),
                ],
                const SizedBox(height: 20),
                FilledButton(
                  onPressed: loading ? null : submit,
                  child: Text(
                    loading
                        ? 'Please wait...'
                        : registering
                        ? 'Create account'
                        : 'Sign in',
                  ),
                ),
                TextButton(
                  onPressed: loading
                      ? null
                      : () => setState(() {
                          registering = !registering;
                          error = null;
                        }),
                  child: Text(
                    registering
                        ? 'Already have an account? Sign in'
                        : 'New to RoadMate? Create an account',
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    ),
  );
}
