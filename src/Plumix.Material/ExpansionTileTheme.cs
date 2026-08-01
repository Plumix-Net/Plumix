using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/expansion_tile_theme.dart
public sealed record ExpansionAnimationStyle(
    TimeSpan? Duration = null,
    Curve? Curve = null,
    Curve? ReverseCurve = null)
{
    public static ExpansionAnimationStyle NoAnimation { get; } = new(TimeSpan.Zero, Curves.Linear, Curves.Linear);
}

public sealed partial record ExpansionTileThemeData(
    Color? BackgroundColor = null,
    Color? CollapsedBackgroundColor = null,
    Thickness? TilePadding = null,
    Alignment? ExpandedAlignment = null,
    CrossAxisAlignment? ExpandedCrossAxisAlignment = null,
    Thickness? ChildrenPadding = null,
    Color? IconColor = null,
    Color? CollapsedIconColor = null,
    Color? TextColor = null,
    Color? CollapsedTextColor = null,
    BorderRadius? Shape = null,
    BorderRadius? CollapsedShape = null,
    Clip? ClipBehavior = null,
    ListTileControlAffinity? ControlAffinity = null,
    bool? Dense = null,
    double? MinTileHeight = null,
    bool? EnableFeedback = null,
    ExpansionAnimationStyle? ExpansionAnimationStyle = null);

public sealed class ExpansionTileTheme : InheritedWidget
{
    public ExpansionTileTheme(
        ExpansionTileThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ExpansionTileThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((ExpansionTileTheme)oldWidget).Data, Data);
    }

    public static ExpansionTileThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<ExpansionTileTheme>()?.Data
               ?? Theme.Of(context).ExpansionTileTheme;
    }
}
