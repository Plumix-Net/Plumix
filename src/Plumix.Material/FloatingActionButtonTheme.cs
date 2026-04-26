using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/floating_action_button_theme.dart (approximate)

public sealed record FloatingActionButtonThemeData(
    Color? ForegroundColor = null,
    Color? BackgroundColor = null,
    Color? FocusColor = null,
    Color? HoverColor = null,
    Color? SplashColor = null,
    double? Elevation = null,
    double? FocusElevation = null,
    double? HoverElevation = null,
    double? DisabledElevation = null,
    double? HighlightElevation = null,
    BorderRadius? Shape = null,
    double? IconSize = null,
    BoxConstraints? SizeConstraints = null,
    BoxConstraints? SmallSizeConstraints = null,
    BoxConstraints? LargeSizeConstraints = null,
    BoxConstraints? ExtendedSizeConstraints = null,
    double? ExtendedIconLabelSpacing = null,
    Thickness? ExtendedPadding = null,
    TextStyle? ExtendedTextStyle = null,
    MouseCursor? MouseCursor = null,
    bool? EnableFeedback = null,
    MaterialTapTargetSize? MaterialTapTargetSize = null);

public sealed class FloatingActionButtonTheme : InheritedWidget
{
    public FloatingActionButtonTheme(
        FloatingActionButtonThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public FloatingActionButtonThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((FloatingActionButtonTheme)oldWidget).Data, Data);
    }

    public static FloatingActionButtonThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<FloatingActionButtonTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).FloatingActionButtonTheme;
    }
}
