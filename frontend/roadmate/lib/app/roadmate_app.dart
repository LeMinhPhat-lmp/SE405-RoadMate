part of '../main.dart';

class RoadMateApp extends StatelessWidget {
  const RoadMateApp({required this.api, super.key});
  final RoadMateApi api;

  @override
  Widget build(BuildContext context) => MaterialApp(
    title: 'RoadMate',
    debugShowCheckedModeBanner: false,
    theme: ThemeData(
      colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xff0f766e)),
      useMaterial3: true,
      scaffoldBackgroundColor: const Color(0xfff4f7f6),
    ),
    home: Shell(api: api),
  );
}

class Shell extends StatefulWidget {
  const Shell({required this.api, super.key});
  final RoadMateApi api;

  @override
  State<Shell> createState() => _ShellState();
}

class _ShellState extends State<Shell> {
  int index = 0;
  String? activeTripId;
  late final Future<void> syncUser;

  @override
  void initState() {
    super.initState();
    syncUser = widget.api.syncUser('Demo Rider');
  }

  @override
  Widget build(BuildContext context) => FutureBuilder<void>(
    future: syncUser,
    builder: (context, snapshot) {
      if (snapshot.connectionState != ConnectionState.done) {
        return const Scaffold(body: Center(child: CircularProgressIndicator()));
      }
      if (snapshot.hasError) {
        return Scaffold(
          body: Center(
            child: ErrorBanner(
              'Unable to connect to RoadMate: ${snapshot.error}',
            ),
          ),
        );
      }
      return _buildShell(context);
    },
  );

  Widget _buildShell(BuildContext context) {
    final pages = [
      HomePage(
        api: widget.api,
        onTripCreated: (id) => setState(() => activeTripId = id),
      ),
      TripsPage(api: widget.api),
      JournalPage(api: widget.api, tripId: activeTripId),
      ContactsPage(api: widget.api),
      ProfilePage(api: widget.api),
    ];
    return Scaffold(
      body: SafeArea(
        child: IndexedStack(index: index, children: pages),
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
