using Avalonia;
using Flutter.Foundation;

namespace Flutter.Widgets;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/safe_area.dart (approximate)

public sealed class SafeArea : StatelessWidget
{
    public SafeArea(
        Widget child,
        bool left = true,
        bool top = true,
        bool right = true,
        bool bottom = true,
        Thickness? minimum = null,
        bool maintainBottomViewPadding = false,
        Key? key = null) : base(key)
    {
        Child = child;
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
        Minimum = minimum ?? default;
        MaintainBottomViewPadding = maintainBottomViewPadding;
    }

    public Widget Child { get; }

    public bool Left { get; }

    public bool Top { get; }

    public bool Right { get; }

    public bool Bottom { get; }

    public Thickness Minimum { get; }

    public bool MaintainBottomViewPadding { get; }

    public override Widget Build(BuildContext context)
    {
        var padding = MediaQuery.PaddingOf(context);

        // Flutter parity: when keyboard consumes bottom padding, keep viewPadding.bottom if requested.
        if (MaintainBottomViewPadding)
        {
            var viewPadding = MediaQuery.ViewPaddingOf(context);
            padding = new Thickness(padding.Left, padding.Top, padding.Right, viewPadding.Bottom);
        }

        var resolvedPadding = new Thickness(
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
}
