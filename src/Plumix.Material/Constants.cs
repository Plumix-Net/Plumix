using Plumix.Rendering;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/constants.dart

/// <summary>The Material library's top-level constants.</summary>
public static class MaterialConstants
{
    // Mirrors Flutter's `kThemeChangeDuration` from `material/constants.dart`.
    /// <summary>The amount of time theme change animations should last.</summary>
    public static readonly TimeSpan ThemeAnimationDuration = TimeSpan.FromMilliseconds(200);

    // Mirrors Flutter's `kToolbarHeight` from `material/constants.dart`.
    /// <summary>The height of a Material Design toolbar (an <see cref="AppBar"/> without its bottom).</summary>
    public const double ToolbarHeight = 56.0;

    // Mirrors Flutter's `kBottomNavigationBarHeight` from `material/constants.dart`.
    /// <summary>The height of a <see cref="BottomNavigationBar"/>.</summary>
    public const double BottomNavigationBarHeight = 56.0;

    // Mirrors Flutter's `kTabScrollDuration` from `material/constants.dart`.
    /// <summary>The duration of a <see cref="TabController"/>'s index-change animation.</summary>
    public static readonly TimeSpan TabScrollDuration = TimeSpan.FromMilliseconds(300);

    // Mirrors Flutter's `kTabLabelPadding` from `material/constants.dart`.
    /// <summary>The horizontal padding included by default in each <see cref="Tab"/> label.</summary>
    public static readonly EdgeInsetsGeometry TabLabelPadding = EdgeInsetsGeometry.Symmetric(horizontal: 16.0);

    // Mirrors Flutter's `kMaterialListPadding` from `material/constants.dart`.
    /// <summary>The padding added around Material list contents.</summary>
    public static readonly EdgeInsetsGeometry MaterialListPadding = EdgeInsetsGeometry.Symmetric(vertical: 8.0);
}
