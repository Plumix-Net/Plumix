using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/expansion_tile_theme.dart
public sealed partial record ExpansionTileThemeData(
    Color? BackgroundColor = null,
    Color? CollapsedBackgroundColor = null,
    EdgeInsetsGeometry? TilePadding = null,
    AlignmentGeometry? ExpandedAlignment = null,
    EdgeInsetsGeometry? ChildrenPadding = null,
    Color? IconColor = null,
    Color? CollapsedIconColor = null,
    Color? TextColor = null,
    Color? CollapsedTextColor = null,
    ShapeBorder? Shape = null,
    ShapeBorder? CollapsedShape = null,
    Clip? ClipBehavior = null,
    AnimationStyle? ExpansionAnimationStyle = null)
{
    public ExpansionTileThemeData CopyWith(
        Color? backgroundColor = null,
        Color? collapsedBackgroundColor = null,
        EdgeInsetsGeometry? tilePadding = null,
        AlignmentGeometry? expandedAlignment = null,
        EdgeInsetsGeometry? childrenPadding = null,
        Color? iconColor = null,
        Color? collapsedIconColor = null,
        Color? textColor = null,
        Color? collapsedTextColor = null,
        ShapeBorder? shape = null,
        ShapeBorder? collapsedShape = null,
        Clip? clipBehavior = null,
        AnimationStyle? expansionAnimationStyle = null)
    {
        return new ExpansionTileThemeData(
            BackgroundColor: backgroundColor ?? BackgroundColor,
            CollapsedBackgroundColor: collapsedBackgroundColor ?? CollapsedBackgroundColor,
            TilePadding: tilePadding ?? TilePadding,
            ExpandedAlignment: expandedAlignment ?? ExpandedAlignment,
            ChildrenPadding: childrenPadding ?? ChildrenPadding,
            IconColor: iconColor ?? IconColor,
            CollapsedIconColor: collapsedIconColor ?? CollapsedIconColor,
            TextColor: textColor ?? TextColor,
            CollapsedTextColor: collapsedTextColor ?? CollapsedTextColor,
            Shape: shape ?? Shape,
            CollapsedShape: collapsedShape ?? CollapsedShape,
            ClipBehavior: clipBehavior ?? ClipBehavior,
            ExpansionAnimationStyle: expansionAnimationStyle ?? ExpansionAnimationStyle);
    }
}

public sealed class ExpansionTileTheme : InheritedTheme
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

    public static ExpansionTileThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<ExpansionTileTheme>()?.Data
               ?? Theme.Of(context).ExpansionTileTheme;
    }

    public override Widget Build(BuildContext context) => Child;

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new ExpansionTileTheme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((ExpansionTileTheme)oldWidget).Data, Data);
    }
}
