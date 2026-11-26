part of '../main.dart';

/// Lets safety prompts (crash countdown, idle check) open from any screen.
final roadMateNavigatorKey = GlobalKey<NavigatorState>();

class RoadMateApp extends StatefulWidget {
  const RoadMateApp({required this.api, super.key});
  final RoadMateApi api;

  static final ValueNotifier<ThemeMode> themeMode = ValueNotifier(
    ThemeMode.system,
  );

  static Future<void> loadThemeMode() async {
    final prefs = await SharedPreferences.getInstance();
    themeMode.value = switch (prefs.getString('theme_mode')) {
      'dark' => ThemeMode.dark,
      'light' => ThemeMode.light,
      _ => ThemeMode.system,
    };
  }

  static Future<void> setThemeMode(ThemeMode mode) async {
    themeMode.value = mode;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('theme_mode', mode.name);
  }

  @override
  State<RoadMateApp> createState() => _RoadMateAppState();
}

class _RoadMateAppState extends State<RoadMateApp> {
  @override
  void dispose() {
    widget.api.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => ValueListenableBuilder<ThemeMode>(
    valueListenable: RoadMateApp.themeMode,
    builder: (context, mode, _) => MaterialApp(
      title: 'RoadMate',
      debugShowCheckedModeBanner: false,
      navigatorKey: roadMateNavigatorKey,
      theme: RoadMateTheme.light(),
      darkTheme: RoadMateTheme.dark(),
      themeMode: mode,
      home: ValueListenableBuilder<bool>(
        valueListenable: FirebaseBridge.availability,
        builder: (context, firebaseAvailable, _) {
          if (!firebaseAvailable) return Shell(api: widget.api);
          return FirebaseAuthGate(api: widget.api);
        },
      ),
    ),
  );
}

class FirebaseAuthGate extends StatelessWidget {
  const FirebaseAuthGate({required this.api, super.key});
  final RoadMateApi api;

  @override
  Widget build(BuildContext context) => StreamBuilder<User?>(
    stream: FirebaseAuth.instance.authStateChanges(),
    builder: (context, snapshot) {
      if (snapshot.connectionState == ConnectionState.waiting) {
        return const Scaffold(body: Center(child: CircularProgressIndicator()));
      }
      final user = snapshot.data;
      return user == null
          ? AuthPage(api: api)
          : AuthenticatedShell(api: api, user: user);
    },
  );
}

class AuthenticatedShell extends StatefulWidget {
  const AuthenticatedShell({required this.api, required this.user, super.key});
  final RoadMateApi api;
  final User user;

  @override
  State<AuthenticatedShell> createState() => _AuthenticatedShellState();
}

class _AuthenticatedShellState extends State<AuthenticatedShell> {
  Object? error;
  StreamSubscription<String>? _tokenSubscription;

  @override
  void initState() {
    super.initState();
    unawaited(_syncUser());
    _tokenSubscription = FirebaseBridge.tokenRefresh.listen((token) {
      unawaited(_syncUser(fcmToken: token));
    });
  }

  @override
  void dispose() {
    unawaited(_tokenSubscription?.cancel());
    super.dispose();
  }

  Future<void> _syncUser({String? fcmToken}) async {
    try {
      String? token = fcmToken;
      if (token == null) {
        try {
          token = await FirebaseBridge.requestPushToken();
        } on Object catch (pushError) {
          debugPrint(
            'RoadMate could not register push notifications: $pushError',
          );
        }
      }
      await widget.api.syncUser(
        widget.user.displayName?.trim().isNotEmpty == true
            ? widget.user.displayName!
            : widget.user.email ?? 'RoadMate rider',
        fcmToken: token,
      );
    } catch (exception) {
      if (mounted) setState(() => error = exception);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Shell(
      api: widget.api,
      startupError: error,
      onRetryStartup: () {
        setState(() => error = null);
        unawaited(_syncUser());
      },
    );
  }
}

class Shell extends StatefulWidget {
  const Shell({
    required this.api,
    this.startupError,
    this.onRetryStartup,
    super.key,
  });
  final RoadMateApi api;
  final Object? startupError;
  final VoidCallback? onRetryStartup;

  @override
  State<Shell> createState() => _ShellState();
}

class _ShellState extends State<Shell> {
  int index = 0;
  String? activeTripId;
  late final TripRuntime runtime = TripRuntime(widget.api);
  StreamSubscription<SafetyEvent>? _safetyEvents;

  @override
  void initState() {
    super.initState();
    _safetyEvents = runtime.events.listen(_onSafetyEvent);
    unawaited(_restore());
  }

  Future<void> _restore() async {
    await runtime.restore();
    // An emergency alert survived an app restart: show it again.
    if (runtime.alerts.active) {
      unawaited(
        roadMateNavigatorKey.currentState?.push(AlertActivePage.route(runtime)),
      );
    }
  }

  void _onSafetyEvent(SafetyEvent event) {
    final navigator = roadMateNavigatorKey.currentState;
    if (navigator == null) return;
    switch (event.type) {
      case SafetyEventType.crash:
        unawaited(navigator.push(CrashAlertPage.route(runtime, event.crash!)));
      case SafetyEventType.idle:
        unawaited(
          navigator.push(
            IdleCheckPage.route(
              runtime,
              event.stoppedFor ??
                  Duration(minutes: runtime.idleDetector.idleMinutes),
            ),
          ),
        );
      case SafetyEventType.restEnded:
        final context = navigator.context;
        ScaffoldMessenger.maybeOf(context)?.showSnackBar(
          const SnackBar(
            content: Text('Hết giờ nghỉ chân – tiếp tục hành trình nhé!'),
          ),
        );
    }
  }

  @override
  void dispose() {
    unawaited(_safetyEvents?.cancel());
    runtime.dispose();
    super.dispose();
  }

  void _openNavigation() {
    unawaited(
      Navigator.of(context).push(NavigationPage.route(widget.api, runtime)),
    );
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: runtime,
    builder: (context, _) => _buildShell(context),
  );

  Widget _buildShell(BuildContext context) {
    final pages = [
      HomePage(
        api: widget.api,
        runtime: runtime,
        onTripCreated: (id) => setState(() => activeTripId = id),
        onOpenTab: (value) => setState(() => index = value),
      ),
      TripsPage(api: widget.api, runtime: runtime),
      JournalPage(
        api: widget.api,
        runtime: runtime,
        tripId: runtime.activeTripId ?? activeTripId,
      ),
      ContactsPage(api: widget.api),
      ProfilePage(api: widget.api, runtime: runtime),
    ];
    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            if (widget.startupError != null)
              MaterialBanner(
                content: Text(
                  'Chưa đồng bộ được tài khoản: ${widget.startupError}',
                ),
                actions: [
                  TextButton(
                    onPressed: widget.onRetryStartup,
                    child: const Text('Thử lại'),
                  ),
                ],
              ),
            if (runtime.alerts.active)
              _ActiveBanner(
                color: RoadMateColors.danger,
                icon: Icons.emergency,
                text: 'Cảnh báo khẩn cấp đang hoạt động',
                onTap: () => unawaited(
                  Navigator.of(context).push(AlertActivePage.route(runtime)),
                ),
              )
            else if (runtime.isTracking)
              _ActiveBanner(
                color: RoadMateColors.primaryDeep,
                icon: Icons.navigation,
                text: runtime.resting
                    ? 'Đang nghỉ chân · chạm để mở dẫn đường'
                    : 'Chuyến đi đang chạy · chạm để mở dẫn đường',
                onTap: _openNavigation,
              ),
            Expanded(
              child: IndexedStack(index: index, children: pages),
            ),
          ],
        ),
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: index,
        onDestinationSelected: (value) => setState(() => index = value),
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.home_outlined),
            selectedIcon: Icon(Icons.home),
            label: 'Trang chủ',
          ),
          NavigationDestination(
            icon: Icon(Icons.route_outlined),
            selectedIcon: Icon(Icons.route),
            label: 'Chuyến đi',
          ),
          NavigationDestination(
            icon: Icon(Icons.book_outlined),
            selectedIcon: Icon(Icons.book),
            label: 'Nhật ký',
          ),
          NavigationDestination(
            icon: Icon(Icons.people_outline),
            selectedIcon: Icon(Icons.people),
            label: 'Liên hệ',
          ),
          NavigationDestination(
            icon: Icon(Icons.person_outline),
            selectedIcon: Icon(Icons.person),
            label: 'Tôi',
          ),
        ],
      ),
    );
  }
}

class _ActiveBanner extends StatelessWidget {
  const _ActiveBanner({
    required this.color,
    required this.icon,
    required this.text,
    required this.onTap,
  });
  final Color color;
  final IconData icon;
  final String text;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Material(
    color: color,
    child: InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
        child: Row(
          children: [
            Icon(icon, color: Colors.white, size: 20),
            const SizedBox(width: 10),
            Expanded(
              child: Text(
                text,
                style: const TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            const Icon(Icons.chevron_right, color: Colors.white),
          ],
        ),
      ),
    ),
  );
}
