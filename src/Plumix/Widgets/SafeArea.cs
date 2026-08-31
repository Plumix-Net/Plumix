using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/safe_area.dart

public sealed class SafeArea : StatelessWidget
{
    public SafeArea(
        Widget child,
        bool left = true,
        bool top = true,
        bool right = true,
        bool bottom = true,
        EdgeInsets? minimum = null,
        bool maintainBottomViewPadding = false,
        Key? key = null) : base(key)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
        Minimum = minimum ?? EdgeInsets.Zero;
        MaintainBottomViewPadding = maintainBottomViewPadding;
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public bool Left { get; }

    public bool Top { get; }

    public bool Right { get; }

    public bool Bottom { get; }

    public EdgeInsets Minimum { get; }

    public bool MaintainBottomViewPadding { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        Thickness padding = MediaQuery.PaddingOf(context);

        // Bottom padding has been consumed - i.e. by the keyboard.
        if (MaintainBottomViewPadding)
        {
            Thickness viewPadding = MediaQuery.ViewPaddingOf(context);
            padding = new Thickness(padding.Left, padding.Top, padding.Right, viewPadding.Bottom);
        }

        var resolvedPadding = EdgeInsets.Only(
            Math.Max(Left ? padding.Left : 0.0, Minimum.Left),
            Math.Max(Top ? padding.Top : 0.0, Minimum.Top),
            Math.Max(Right ? padding.Right : 0.0, Minimum.Right),
            Math.Max(Bottom ? padding.Bottom : 0.0, Minimum.Bottom));

        return new Padding(
            insets: resolvedPadding,
            child: MediaQuery.RemovePadding(
                context: context,
                removeLeft: Left,
                removeTop: Top,
                removeRight: Right,
                removeBottom: Bottom,
                child: Child));
    }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new FlagProperty("left", value: Left, ifTrue: "avoid left padding"));
        properties.Add(new FlagProperty("top", value: Top, ifTrue: "avoid top padding"));
        properties.Add(new FlagProperty("right", value: Right, ifTrue: "avoid right padding"));
        properties.Add(new FlagProperty("bottom", value: Bottom, ifTrue: "avoid bottom padding"));
    }
}

/// <summary>A sliver that insets another sliver to avoid operating system intrusions.</summary>
public sealed class SliverSafeArea : StatelessWidget
{
    public SliverSafeArea(
        Widget sliver,
        bool left = true,
        bool top = true,
        bool right = true,
        bool bottom = true,
        EdgeInsets? minimum = null,
        Key? key = null) : base(key)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
        Minimum = minimum ?? EdgeInsets.Zero;
        Sliver = sliver ?? throw new ArgumentNullException(nameof(sliver));
    }

    public bool Left { get; }

    public bool Top { get; }

    public bool Right { get; }

    public bool Bottom { get; }

    public EdgeInsets Minimum { get; }

    public Widget Sliver { get; }

    public override Widget Build(BuildContext context)
    {
        Thickness padding = MediaQuery.PaddingOf(context);
        var resolvedPadding = EdgeInsets.Only(
            Math.Max(Left ? padding.Left : 0.0, Minimum.Left),
            Math.Max(Top ? padding.Top : 0.0, Minimum.Top),
            Math.Max(Right ? padding.Right : 0.0, Minimum.Right),
            Math.Max(Bottom ? padding.Bottom : 0.0, Minimum.Bottom));

        return new SliverPadding(
            padding: new Thickness(
                resolvedPadding.Left,
                resolvedPadding.Top,
                resolvedPadding.Right,
                resolvedPadding.Bottom),
            sliver: MediaQuery.RemovePadding(
                context: context,
                removeLeft: Left,
                removeTop: Top,
                removeRight: Right,
                removeBottom: Bottom,
                child: Sliver));
    }

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        base.DebugFillProperties(properties);
        properties.Add(new FlagProperty("left", value: Left, ifTrue: "avoid left padding"));
        properties.Add(new FlagProperty("top", value: Top, ifTrue: "avoid top padding"));
        properties.Add(new FlagProperty("right", value: Right, ifTrue: "avoid right padding"));
        properties.Add(new FlagProperty("bottom", value: Bottom, ifTrue: "avoid bottom padding"));
    }
}
