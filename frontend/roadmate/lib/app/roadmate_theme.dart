part of '../main.dart';

/// Colors from the Figma "palette" frame.
class RoadMateColors {
  static const primary = Color(0xff0dc160);
  static const primaryDeep = Color(0xff079a4a);
  static const primarySoft = Color(0xffe7f8ee);
  static const ink = Color(0xff0f1f17);
  static const muted = Color(0xff66766d);
  static const surface = Color(0xfff4f7f5);
  static const violet = Color(0xff7c6cff);
  static const amber = Color(0xffffb547);
  static const coral = Color(0xffff6b6b);
  static const sky = Color(0xff38bdf8);
  static const danger = Color(0xffe5484d);
}

class RoadMateTheme {
  static ThemeData light() => _build(Brightness.light);
  static ThemeData dark() => _build(Brightness.dark);

  static ThemeData _build(Brightness brightness) {
    final dark = brightness == Brightness.dark;
    final scheme = ColorScheme.fromSeed(
      seedColor: RoadMateColors.primary,
      brightness: brightness,
      primary: dark ? RoadMateColors.primary : RoadMateColors.primaryDeep,
      error: RoadMateColors.danger,
      surface: dark ? const Color(0xff101814) : Colors.white,
    );
    return ThemeData(
      colorScheme: scheme,
      useMaterial3: true,
      scaffoldBackgroundColor: dark
          ? const Color(0xff0a110d)
          : RoadMateColors.surface,
      cardTheme: CardThemeData(
        elevation: 0,
        color: scheme.surface,
        margin: const EdgeInsets.symmetric(vertical: 6),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18)),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          minimumSize: const Size(0, 52),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
          textStyle: const TextStyle(fontWeight: FontWeight.w700, fontSize: 15),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          minimumSize: const Size(0, 52),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: dark ? const Color(0xff17221c) : Colors.white,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: BorderSide.none,
        ),
      ),
      chipTheme: ChipThemeData(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
      ),
    );
  }
}
