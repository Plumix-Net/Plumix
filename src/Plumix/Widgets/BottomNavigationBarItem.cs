using Avalonia.Media;
using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/bottom_navigation_bar_item.dart

/// <summary>An item used by Material bottom navigation bars and Cupertino tab bars.</summary>
public sealed class BottomNavigationBarItem
{
    public BottomNavigationBarItem(
        Widget icon,
        string? label = null,
        Widget? activeIcon = null,
        Color? backgroundColor = null,
        string? tooltip = null,
        Key? key = null,
        string? semanticsLabel = null)
    {
        Icon = icon ?? throw new ArgumentNullException(nameof(icon));
        Label = label;
        ActiveIcon = activeIcon ?? icon;
        BackgroundColor = backgroundColor;
        Tooltip = tooltip;
        Key = key;
        SemanticsLabel = semanticsLabel;
    }

    public Key? Key { get; }

    public Widget Icon { get; }

    public Widget ActiveIcon { get; }

    public string? Label { get; }

    public Color? BackgroundColor { get; }

    public string? Tooltip { get; }

    public string? SemanticsLabel { get; }
}
