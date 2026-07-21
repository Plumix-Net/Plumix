using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/navigation_toolbar.dart

public sealed class NavigationToolbar : StatelessWidget
{
    public const double KMiddleSpacing = 16.0;

    public NavigationToolbar(
        Widget? leading = null,
        Widget? middle = null,
        Widget? trailing = null,
        bool centerMiddle = true,
        double middleSpacing = KMiddleSpacing,
        Key? key = null) : base(key)
    {
        Leading = leading;
        Middle = middle;
        Trailing = trailing;
        CenterMiddle = centerMiddle;
        MiddleSpacing = middleSpacing;
    }

    public Widget? Leading { get; }
    public Widget? Middle { get; }
    public Widget? Trailing { get; }
    public bool CenterMiddle { get; }
    public double MiddleSpacing { get; }

    public override Widget Build(BuildContext context)
    {
        TextDirection textDirection = Directionality.Of(context);
        var children = new List<Widget>(3);
        if (Leading is not null)
        {
            children.Add(new LayoutId(ToolbarSlot.Leading, Leading));
        }

        if (Middle is not null)
        {
            children.Add(new LayoutId(ToolbarSlot.Middle, Middle));
        }

        if (Trailing is not null)
        {
            children.Add(new LayoutId(ToolbarSlot.Trailing, Trailing));
        }

        return new CustomMultiChildLayout(
            new ToolbarLayout(
                centerMiddle: CenterMiddle,
                middleSpacing: MiddleSpacing,
                textDirection: textDirection),
            children);
    }

    private enum ToolbarSlot
    {
        Leading,
        Middle,
        Trailing
    }

    private sealed class ToolbarLayout(
        bool centerMiddle,
        double middleSpacing,
        TextDirection textDirection) : MultiChildLayoutDelegate
    {
        public bool CenterMiddle { get; } = centerMiddle;
        public double MiddleSpacing { get; } = middleSpacing;
        public TextDirection TextDirection { get; } = textDirection;

        public override void PerformLayout(Size size)
        {
            double leadingWidth = 0.0;
            double trailingWidth = 0.0;

            if (HasChild(ToolbarSlot.Leading))
            {
                var leadingConstraints = new BoxConstraints(
                    MaxWidth: size.Width,
                    MinHeight: size.Height,
                    MaxHeight: size.Height);
                leadingWidth = LayoutChild(ToolbarSlot.Leading, leadingConstraints).Width;
                double leadingX = TextDirection == TextDirection.Rtl
                    ? size.Width - leadingWidth
                    : 0.0;
                PositionChild(ToolbarSlot.Leading, new Point(leadingX, 0.0));
            }

            if (HasChild(ToolbarSlot.Trailing))
            {
                Size trailingSize = LayoutChild(ToolbarSlot.Trailing, BoxConstraints.Loose(size));
                double trailingX = TextDirection == TextDirection.Rtl
                    ? 0.0
                    : size.Width - trailingSize.Width;
                double trailingY = (size.Height - trailingSize.Height) / 2.0;
                trailingWidth = trailingSize.Width;
                PositionChild(ToolbarSlot.Trailing, new Point(trailingX, trailingY));
            }

            if (!HasChild(ToolbarSlot.Middle))
            {
                return;
            }

            double maxWidth = Math.Max(
                size.Width - leadingWidth - trailingWidth - MiddleSpacing * 2.0,
                0.0);
            var middleConstraints = new BoxConstraints(
                MaxWidth: maxWidth,
                MaxHeight: size.Height);
            Size middleSize = LayoutChild(ToolbarSlot.Middle, middleConstraints);
            double middleStartMargin = leadingWidth + MiddleSpacing;
            double middleStart = middleStartMargin;
            double middleY = (size.Height - middleSize.Height) / 2.0;

            if (CenterMiddle)
            {
                middleStart = (size.Width - middleSize.Width) / 2.0;
                if (middleStart + middleSize.Width > size.Width - trailingWidth)
                {
                    middleStart = size.Width - trailingWidth - middleSize.Width - MiddleSpacing;
                }
                else if (middleStart < middleStartMargin)
                {
                    middleStart = middleStartMargin;
                }
            }

            double middleX = TextDirection == TextDirection.Rtl
                ? size.Width - middleSize.Width - middleStart
                : middleStart;
            PositionChild(ToolbarSlot.Middle, new Point(middleX, middleY));
        }

        public override bool ShouldRelayout(MultiChildLayoutDelegate oldDelegate)
        {
            return oldDelegate is not ToolbarLayout oldLayout
                   || oldLayout.CenterMiddle != CenterMiddle
                   || oldLayout.MiddleSpacing != MiddleSpacing
                   || oldLayout.TextDirection != TextDirection;
        }
    }
}
