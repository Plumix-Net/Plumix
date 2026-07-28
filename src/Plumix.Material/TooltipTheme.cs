using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/tooltip_theme.dart

public sealed record TooltipThemeData(
    double? Height = null,
    BoxConstraints? Constraints = null,
    Thickness? Padding = null,
    Thickness? Margin = null,
    double? VerticalOffset = null,
    bool? PreferBelow = null,
    bool? ExcludeFromSemantics = null,
    BoxDecoration? Decoration = null,
    TextStyle? TextStyle = null,
    TextAlign? TextAlign = null,
    TimeSpan? WaitDuration = null,
    TimeSpan? ShowDuration = null,
    TimeSpan? ExitDuration = null,
    TooltipTriggerMode? TriggerMode = null,
    bool? EnableFeedback = null)
{
    public TooltipThemeData Validate()
    {
        if (Height.HasValue && Constraints.HasValue)
        {
            throw new ArgumentException("Only one of height and constraints may be specified.");
        }

        return this;
    }
}

public sealed class TooltipTheme : InheritedWidget
{
    public TooltipTheme(TooltipThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = (data ?? throw new ArgumentNullException(nameof(data))).Validate();
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public TooltipThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((TooltipTheme)oldWidget).Data, Data);
    }

    public static TooltipThemeData Of(BuildContext context)
    {
        return (context.DependOnInherited<TooltipTheme>()?.Data ?? Theme.Of(context).TooltipTheme).Validate();
    }
}
