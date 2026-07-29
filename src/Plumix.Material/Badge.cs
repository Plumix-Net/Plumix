using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/badge.dart

public sealed class Badge : StatelessWidget
{
    public Badge(
        Color? backgroundColor = null,
        Color? textColor = null,
        double? smallSize = null,
        double? largeSize = null,
        TextStyle? textStyle = null,
        Thickness? padding = null,
        AlignmentGeometry? alignment = null,
        Vector? offset = null,
        Widget? label = null,
        bool isLabelVisible = true,
        Widget? child = null,
        Key? key = null) : base(key)
    {
        BackgroundColor = backgroundColor;
        TextColor = textColor;
        SmallSize = smallSize;
        LargeSize = largeSize;
        TextStyle = textStyle;
        Padding = padding;
        Alignment = alignment;
        Offset = offset;
        Label = label;
        IsLabelVisible = isLabelVisible;
        Child = child;
    }

    public static Badge Count(
        int count,
        int maxCount = 999,
        Color? backgroundColor = null,
        Color? textColor = null,
        double? smallSize = null,
        double? largeSize = null,
        TextStyle? textStyle = null,
        Thickness? padding = null,
        AlignmentGeometry? alignment = null,
        Vector? offset = null,
        bool isLabelVisible = true,
        Widget? child = null,
        Key? key = null)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Badge count must be non-negative.");
        }

        if (maxCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCount), "Badge maxCount must be positive.");
        }

        return new Badge(
            backgroundColor: backgroundColor,
            textColor: textColor,
            smallSize: smallSize,
            largeSize: largeSize,
            textStyle: textStyle,
            padding: padding,
            alignment: alignment,
            offset: offset,
            label: new Text(count > maxCount ? $"{maxCount}+" : count.ToString()),
            isLabelVisible: isLabelVisible,
            child: child,
            key: key);
    }

    public Color? BackgroundColor { get; }
    public Color? TextColor { get; }
    public double? SmallSize { get; }
    public double? LargeSize { get; }
    public TextStyle? TextStyle { get; }
    public Thickness? Padding { get; }
    public AlignmentGeometry? Alignment { get; }
    public Vector? Offset { get; }
    public Widget? Label { get; }
    public bool IsLabelVisible { get; }
    public Widget? Child { get; }

    public override Widget Build(BuildContext context)
    {
        if (!IsLabelVisible)
        {
            return Child ?? new SizedBox();
        }

        var theme = Theme.Of(context);
        var badgeTheme = BadgeTheme.Of(context);
        var backgroundColor = BackgroundColor ?? badgeTheme.BackgroundColor ?? theme.ErrorColor;
        var textColor = TextColor ?? badgeTheme.TextColor ?? theme.OnErrorColor;
        bool hasLabel = Label is not null;
        double widthOffset = hasLabel
            ? LargeSize ?? badgeTheme.LargeSize ?? 16.0
            : SmallSize ?? badgeTheme.SmallSize ?? 6.0;

        Widget badge;
        if (hasLabel)
        {
            var style = (TextStyle ?? badgeTheme.TextStyle ?? theme.TextTheme.LabelSmall) with
            {
                Color = textColor,
            };
            badge = new DefaultTextStyle(
                style: style,
                child: new ClipRRect(
                    borderRadius: BorderRadius.Circular(10_000),
                    child: new BadgeHorizontalStadium(
                        minSize: widthOffset,
                        child: new Container(
                            alignment: Plumix.Rendering.Alignment.Center,
                            padding: Padding ?? badgeTheme.Padding ?? new Thickness(4, 0),
                            decoration: new BoxDecoration(
                                Color: backgroundColor,
                                BorderRadius: BorderRadius.Circular(10_000)),
                            child: Label))));
        }
        else
        {
            badge = new ClipRRect(
                borderRadius: BorderRadius.Circular(10_000),
                child: new Container(
                    width: widthOffset,
                    height: widthOffset,
                    decoration: new BoxDecoration(
                        Color: backgroundColor,
                        BorderRadius: BorderRadius.Circular(10_000))));
        }

        if (Child is null)
        {
            return badge;
        }

        var textDirection = Directionality.Of(context);
        AlignmentGeometry resolvedAlignment = Alignment
                                              ?? badgeTheme.Alignment
                                              ?? AlignmentDirectional.TopEnd;
        var defaultOffset = textDirection == TextDirection.Ltr
            ? new Vector(4, -4)
            : new Vector(-4, -4);
        var effectiveOffset = (Offset ?? badgeTheme.Offset ?? defaultOffset) + new Vector(0, 8);

        return new Stack(
            clipBehavior: Clip.None,
            children:
            [
                Child,
                new Positioned(
                    left: 0,
                    top: 0,
                    right: 0,
                    bottom: 0,
                    child: new BadgePositioner(
                        alignment: resolvedAlignment,
                        offset: hasLabel ? effectiveOffset : default,
                        widthOffset: widthOffset,
                        hasLabel: hasLabel,
                        textDirection: textDirection,
                        child: badge)),
            ]);
    }
}

internal sealed class BadgePositioner : SingleChildRenderObjectWidget
{
    public BadgePositioner(
        AlignmentGeometry alignment,
        Vector offset,
        double widthOffset,
        bool hasLabel,
        TextDirection textDirection,
        Widget child) : base(child)
    {
        Alignment = alignment;
        Offset = offset;
        WidthOffset = widthOffset;
        HasLabel = hasLabel;
        TextDirection = textDirection;
    }

    public AlignmentGeometry Alignment { get; }
    public Vector Offset { get; }
    public double WidthOffset { get; }
    public bool HasLabel { get; }
    public TextDirection TextDirection { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderBadgePositioner(
            Alignment,
            Offset,
            WidthOffset,
            HasLabel,
            TextDirection);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var badge = (RenderBadgePositioner)renderObject;
        badge.Alignment = Alignment;
        badge.Offset = Offset;
        badge.WidthOffset = WidthOffset;
        badge.HasLabel = HasLabel;
        badge.TextDirection = TextDirection;
    }
}

internal sealed class RenderBadgePositioner : RenderProxyBox
{
    private AlignmentGeometry _alignment;
    private Vector _offset;
    private double _widthOffset;
    private bool _hasLabel;
    private TextDirection _textDirection;

    public RenderBadgePositioner(
        AlignmentGeometry alignment,
        Vector offset,
        double widthOffset,
        bool hasLabel,
        TextDirection textDirection)
    {
        _alignment = alignment;
        _offset = offset;
        _widthOffset = widthOffset;
        _hasLabel = hasLabel;
        _textDirection = textDirection;
    }

    public AlignmentGeometry Alignment
    {
        get => _alignment;
        set { if (_alignment != value) { _alignment = value; MarkNeedsLayout(); } }
    }

    public Vector Offset
    {
        get => _offset;
        set { if (_offset != value) { _offset = value; MarkNeedsLayout(); } }
    }

    public double WidthOffset
    {
        get => _widthOffset;
        set { if (_widthOffset != value) { _widthOffset = value; MarkNeedsLayout(); } }
    }

    public bool HasLabel
    {
        get => _hasLabel;
        set { if (_hasLabel != value) { _hasLabel = value; MarkNeedsLayout(); } }
    }

    public TextDirection TextDirection
    {
        get => _textDirection;
        set { if (_textDirection != value) { _textDirection = value; MarkNeedsLayout(); } }
    }

    protected override void PerformLayout()
    {
        if (!Constraints.HasBoundedWidth || !Constraints.HasBoundedHeight)
        {
            throw new InvalidOperationException("Badge overlay requires bounded constraints.");
        }

        Size = Constraints.Biggest;
        if (Child is null)
        {
            return;
        }

        Child.Layout(new BoxConstraints(), parentUsesSize: true);
        var alignmentSpace = new Size(Math.Max(0, Size.Width - WidthOffset), Size.Height);
        Alignment resolvedAlignment = Alignment.Resolve(TextDirection);
        var location = resolvedAlignment.AlongOffset(alignmentSpace, new Size()) + Offset;
        if (HasLabel)
        {
            location -= new Vector(0, Child.Size.Height / 2.0);
        }

        ((BoxParentData)Child.parentData!).offset = location;
    }
}

internal sealed class BadgeHorizontalStadium : SingleChildRenderObjectWidget
{
    public BadgeHorizontalStadium(double minSize, Widget child) : base(child)
    {
        MinSize = minSize;
    }

    public double MinSize { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderBadgeHorizontalStadium(MinSize);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderBadgeHorizontalStadium)renderObject).MinSize = MinSize;
    }
}

internal sealed class RenderBadgeHorizontalStadium : RenderProxyBox
{
    private double _minSize;

    public RenderBadgeHorizontalStadium(double minSize)
    {
        _minSize = minSize;
    }

    public double MinSize
    {
        get => _minSize;
        set { if (_minSize != value) { _minSize = value; MarkNeedsLayout(); } }
    }

    protected override void PerformLayout()
    {
        if (Child is null)
        {
            Size = Constraints.Constrain(new Size(MinSize, MinSize));
            return;
        }

        var firstConstraints = new BoxConstraints(
            MinWidth: Math.Min(MinSize, Constraints.MaxWidth),
            MaxWidth: Constraints.MaxWidth,
            MinHeight: Math.Min(MinSize, Constraints.MaxHeight),
            MaxHeight: Constraints.MaxHeight);
        Child.Layout(firstConstraints, parentUsesSize: true);
        var target = Constraints.Constrain(new Size(
            Math.Max(Child.Size.Width, Child.Size.Height),
            Math.Max(MinSize, Child.Size.Height)));

        if (Math.Abs(Child.Size.Width - target.Width) > 0.001 || Math.Abs(Child.Size.Height - target.Height) > 0.001)
        {
            Child.Layout(BoxConstraints.Tight(target), parentUsesSize: true);
        }

        Size = target;
        ((BoxParentData)Child.parentData!).offset = new Point();
    }
}
