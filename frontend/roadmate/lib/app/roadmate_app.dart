part of '../main.dart';

class RoadMateApp extends StatefulWidget {
  const RoadMateApp({required this.api, super.key});
  final RoadMateApi api;

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
  Widget build(BuildContext context) => MaterialApp(
    title: 'RoadMate',
    debugShowCheckedModeBanner: false,
    theme: ThemeData(
      colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xff0f766e)),
      useMaterial3: true,
      scaffoldBackgroundColor: const Color(0xfff4f7f6),
    ),
    home: ValueListenableBuilder<bool>(
      valueListenable: FirebaseBridge.availability,
      builder: (context, firebaseAvailable, _) {
        if (!firebaseAvailable) return Shell(api: widget.api);
        return FirebaseAuthGate(api: widget.api);
      },
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
          debugPrint('RoadMate could not register push notifications: $pushError');
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

  @override
  void initState() {
    super.initState();
    unawaited(runtime.restore());
  }

  @override
  void dispose() {
    runtime.dispose();
    super.dispose();
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
        onTripCreated: (id) => setState(() => activeTripId = id),
      ),
      TripsPage(api: widget.api, runtime: runtime),
      JournalPage(
        api: widget.api,
        tripId: runtime.activeTripId ?? activeTripId,
      ),
      ContactsPage(api: widget.api),
      ProfilePage(api: widget.api),
    ];
    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            if (widget.startupError != null)
              MaterialBanner(
                content: Text(
                  'Account sync unavailable: ${widget.startupError}',
                ),
                actions: [
                  TextButton(
                    onPressed: widget.onRetryStartup,
                    child: const Text('Retry'),
                  ),
                ],
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
            label: 'Home',
          ),
          NavigationDestination(
            icon: Icon(Icons.route_outlined),
            selectedIcon: Icon(Icons.route),
            label: 'Trips',
          ),
          NavigationDestination(
            icon: Icon(Icons.book_outlined),
            selectedIcon: Icon(Icons.book),
            label: 'Journal',
          ),
          NavigationDestination(
            icon: Icon(Icons.people_outline),
            selectedIcon: Icon(Icons.people),
            label: 'Contacts',
          ),
          NavigationDestination(
            icon: Icon(Icons.person_outline),
            selectedIcon: Icon(Icons.person),
            label: 'Profile',
          ),
        ],
      ),
    );
  }
}
