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
  bool acceptedTerms = false;
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
    final minLength = registering ? 8 : 6;
    if (emailValue.isEmpty || passwordValue.length < minLength) {
      setState(
        () => error = 'Nhập email và mật khẩu ít nhất $minLength ký tự.',
      );
      return;
    }
    if (registering && name.text.trim().isEmpty) {
      setState(() => error = 'Nhập họ và tên của bạn.');
      return;
    }
    if (registering && !acceptedTerms) {
      setState(() => error = 'Bạn cần đồng ý với điều khoản sử dụng.');
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

  Future<void> resetPassword() async {
    final emailValue = email.text.trim();
    if (emailValue.isEmpty) {
      setState(() => error = 'Nhập email để nhận link đặt lại mật khẩu.');
      return;
    }
    try {
      await FirebaseAuth.instance.sendPasswordResetEmail(email: emailValue);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Đã gửi link đặt lại mật khẩu tới $emailValue'),
          ),
        );
      }
    } on FirebaseAuthException catch (exception) {
      if (mounted) setState(() => error = exception.message ?? exception.code);
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
                Align(
                  alignment: Alignment.centerLeft,
                  child: Container(
                    width: 56,
                    height: 56,
                    decoration: BoxDecoration(
                      color: RoadMateColors.primary,
                      borderRadius: BorderRadius.circular(18),
                    ),
                    child: const Icon(Icons.navigation, color: Colors.white),
                  ),
                ),
                const SizedBox(height: 20),
                Text(
                  registering ? 'Tạo tài khoản' : 'Chào mừng trở lại 👋',
                  style: const TextStyle(
                    fontSize: 28,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 6),
                Text(
                  registering
                      ? 'Chỉ mất 1 phút để sẵn sàng lên đường'
                      : 'Đăng nhập để tiếp tục hành trình của bạn',
                  style: const TextStyle(color: RoadMateColors.muted),
                ),
                const SizedBox(height: 28),
                if (registering)
                  TextField(
                    controller: name,
                    textInputAction: TextInputAction.next,
                    textCapitalization: TextCapitalization.words,
                    decoration: const InputDecoration(labelText: 'Họ và tên'),
                  ),
                if (registering) const SizedBox(height: 12),
                TextField(
                  controller: email,
                  keyboardType: TextInputType.emailAddress,
                  textInputAction: TextInputAction.next,
                  decoration: const InputDecoration(labelText: 'Email'),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: password,
                  obscureText: true,
                  onSubmitted: (_) => submit(),
                  decoration: InputDecoration(
                    labelText: 'Mật khẩu',
                    helperText: registering ? 'Ít nhất 8 ký tự' : null,
                  ),
                ),
                if (!registering)
                  Align(
                    alignment: Alignment.centerRight,
                    child: TextButton(
                      onPressed: loading ? null : resetPassword,
                      child: const Text('Quên mật khẩu?'),
                    ),
                  ),
                if (registering)
                  CheckboxListTile(
                    value: acceptedTerms,
                    contentPadding: EdgeInsets.zero,
                    controlAffinity: ListTileControlAffinity.leading,
                    onChanged: (value) =>
                        setState(() => acceptedTerms = value ?? false),
                    title: const Text(
                      'Tôi đồng ý với Điều khoản sử dụng và hiểu rằng RoadMate '
                      'không thay thế dịch vụ cứu hộ chuyên nghiệp.',
                      style: TextStyle(fontSize: 13),
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
                        ? 'Vui lòng chờ...'
                        : registering
                        ? 'Tạo tài khoản'
                        : 'Đăng nhập',
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
                        ? 'Đã có tài khoản? Đăng nhập'
                        : 'Chưa có tài khoản? Đăng ký',
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
