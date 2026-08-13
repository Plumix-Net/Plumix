using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/list_tile.dart

internal enum ListTileSlot
{
    Leading,
    Title,
    Subtitle,
    Trailing,
}

internal sealed class ListTileRenderWidget : SlottedMultiChildRenderObjectWidget<ListTileSlot>
{
    private static readonly IReadOnlyList<ListTileSlot> AllSlots = Enum.GetValues<ListTileSlot>();

    public ListTileRenderWidget(
        Widget? leading,
        Widget title,
        Widget? subtitle,
        Widget? trailing,
        bool isThreeLine,
        bool isDense,
        VisualDensity visualDensity,
        TextDirection textDirection,
        TextBaseline titleBaselineType,
        TextBaseline? subtitleBaselineType,
        double horizontalTitleGap,
        double minVerticalPadding,
        double minLeadingWidth,
        double? minTileHeight,
        ListTileTitleAlignment titleAlignment)
    {
        Leading = leading;
        Title = title;
        Subtitle = subtitle;
        Trailing = trailing;
        IsThreeLine = isThreeLine;
        IsDense = isDense;
        VisualDensity = visualDensity;
        TextDirection = textDirection;
        TitleBaselineType = titleBaselineType;
        SubtitleBaselineType = subtitleBaselineType;
        HorizontalTitleGap = horizontalTitleGap;
        MinVerticalPadding = minVerticalPadding;
        MinLeadingWidth = minLeadingWidth;
        MinTileHeight = minTileHeight;
        TitleAlignment = titleAlignment;
    }

    public Widget? Leading { get; }
    public Widget Title { get; }
    public Widget? Subtitle { get; }
    public Widget? Trailing { get; }
    public bool IsThreeLine { get; }
    public bool IsDense { get; }
    public VisualDensity VisualDensity { get; }
    public TextDirection TextDirection { get; }
    public TextBaseline TitleBaselineType { get; }
    public TextBaseline? SubtitleBaselineType { get; }
    public double HorizontalTitleGap { get; }
    public double MinVerticalPadding { get; }
    public double MinLeadingWidth { get; }
    public double? MinTileHeight { get; }
    public ListTileTitleAlignment TitleAlignment { get; }

    public override IReadOnlyList<ListTileSlot> Slots => AllSlots;

    public override Widget? ChildForSlot(ListTileSlot slot)
    {
        return slot switch
        {
            ListTileSlot.Leading => Leading,
            ListTileSlot.Title => Title,
            ListTileSlot.Subtitle => Subtitle,
            ListTileSlot.Trailing => Trailing,
            _ => null,
        };
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderListTile(
            isDense: IsDense,
            visualDensity: VisualDensity,
            isThreeLine: IsThreeLine,
            textDirection: TextDirection,
            titleBaselineType: TitleBaselineType,
            subtitleBaselineType: SubtitleBaselineType,
            horizontalTitleGap: HorizontalTitleGap,
            minVerticalPadding: MinVerticalPadding,
            minLeadingWidth: MinLeadingWidth,
            minTileHeight: MinTileHeight,
            titleAlignment: TitleAlignment);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var tile = (RenderListTile)renderObject;
        tile.IsDense = IsDense;
        tile.VisualDensity = VisualDensity;
        tile.IsThreeLine = IsThreeLine;
        tile.TextDirection = TextDirection;
        tile.TitleBaselineType = TitleBaselineType;
        tile.SubtitleBaselineType = SubtitleBaselineType;
        tile.HorizontalTitleGap = HorizontalTitleGap;
        tile.MinVerticalPadding = MinVerticalPadding;
        tile.MinLeadingWidth = MinLeadingWidth;
        tile.MinTileHeight = MinTileHeight;
        tile.TitleAlignment = TitleAlignment;
    }
}

internal sealed class RenderListTile : RenderBox, ISlottedRenderObjectContainer
{
    private RenderBox? _leading;
    private RenderBox? _title;
    private RenderBox? _subtitle;
    private RenderBox? _trailing;
    private bool _isDense;
    private VisualDensity _visualDensity;
    private bool _isThreeLine;
    private TextDirection _textDirection;
    private TextBaseline _titleBaselineType;
    private TextBaseline? _subtitleBaselineType;
    private double _horizontalTitleGap;
    private double _minVerticalPadding;
    private double _minLeadingWidth;
    private double? _minTileHeight;
    private ListTileTitleAlignment _titleAlignment;

    public RenderListTile(
        bool isDense,
        VisualDensity visualDensity,
        bool isThreeLine,
        TextDirection textDirection,
        TextBaseline titleBaselineType,
        TextBaseline? subtitleBaselineType,
        double horizontalTitleGap,
        double minVerticalPadding,
        double minLeadingWidth,
        double? minTileHeight,
        ListTileTitleAlignment titleAlignment)
    {
        _isDense = isDense;
        _visualDensity = visualDensity;
        _isThreeLine = isThreeLine;
        _textDirection = textDirection;
        _titleBaselineType = titleBaselineType;
        _subtitleBaselineType = subtitleBaselineType;
        _horizontalTitleGap = horizontalTitleGap;
        _minVerticalPadding = minVerticalPadding;
        _minLeadingWidth = minLeadingWidth;
        _minTileHeight = minTileHeight;
        _titleAlignment = titleAlignment;
    }

    public RenderBox? Leading => _leading;
    public RenderBox Title => _title ?? throw new InvalidOperationException("ListTile title is not mounted.");
    public RenderBox? Subtitle => _subtitle;
    public RenderBox? Trailing => _trailing;

    public bool IsDense
    {
        get => _isDense;
        set => SetLayoutValue(ref _isDense, value);
    }

    public VisualDensity VisualDensity
    {
        get => _visualDensity;
        set => SetLayoutValue(ref _visualDensity, value);
    }

    public bool IsThreeLine
    {
        get => _isThreeLine;
        set => SetLayoutValue(ref _isThreeLine, value);
    }

    public TextDirection TextDirection
    {
        get => _textDirection;
        set => SetLayoutValue(ref _textDirection, value);
    }

    public TextBaseline TitleBaselineType
    {
        get => _titleBaselineType;
        set => SetLayoutValue(ref _titleBaselineType, value);
    }

    public TextBaseline? SubtitleBaselineType
    {
        get => _subtitleBaselineType;
        set => SetLayoutValue(ref _subtitleBaselineType, value);
    }

    public double HorizontalTitleGap
    {
        get => _horizontalTitleGap;
        set => SetLayoutValue(ref _horizontalTitleGap, value);
    }

    public double MinVerticalPadding
    {
        get => _minVerticalPadding;
        set => SetLayoutValue(ref _minVerticalPadding, value);
    }

    public double MinLeadingWidth
    {
        get => _minLeadingWidth;
        set => SetLayoutValue(ref _minLeadingWidth, value);
    }

    public double? MinTileHeight
    {
        get => _minTileHeight;
        set => SetLayoutValue(ref _minTileHeight, value);
    }

    public ListTileTitleAlignment TitleAlignment
    {
        get => _titleAlignment;
        set => SetLayoutValue(ref _titleAlignment, value);
    }

    private double EffectiveHorizontalTitleGap => _horizontalTitleGap + (_visualDensity.Horizontal * 2.0);

    private double DefaultTileHeight => _visualDensity.BaseSizeAdjustment.Y + (_isThreeLine
        ? (_isDense ? 76.0 : 88.0)
        : _subtitle is not null
            ? (_isDense ? 64.0 : 72.0)
            : (_isDense ? 48.0 : 56.0));

    private double TargetTileHeight => _minTileHeight ?? DefaultTileHeight;

    public void SetChild(RenderObject? child, object slot)
    {
        RenderBox? box = child switch
        {
            null => null,
            RenderBox renderBox => renderBox,
            _ => throw new InvalidOperationException("ListTile slots require RenderBox children."),
        };

        switch ((ListTileSlot)slot)
        {
            case ListTileSlot.Leading:
                SetSlotChild(ref _leading, box);
                break;
            case ListTileSlot.Title:
                SetSlotChild(ref _title, box);
                break;
            case ListTileSlot.Subtitle:
                SetSlotChild(ref _subtitle, box);
                break;
            case ListTileSlot.Trailing:
                SetSlotChild(ref _trailing, box);
                break;
        }
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not BoxParentData)
        {
            child.parentData = new BoxParentData();
        }
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        Visit(_leading, visitor);
        Visit(_title, visitor);
        Visit(_subtitle, visitor);
        Visit(_trailing, visitor);
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        double leadingWidth = _leading is null
            ? 0.0
            : Math.Max(_leading.GetMinIntrinsicWidth(height), _minLeadingWidth)
              + EffectiveHorizontalTitleGap;
        return leadingWidth
               + Math.Max(Title.GetMinIntrinsicWidth(height), _subtitle?.GetMinIntrinsicWidth(height) ?? 0.0)
               + (_trailing?.GetMaxIntrinsicWidth(height) ?? 0.0);
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        double leadingWidth = _leading is null
            ? 0.0
            : Math.Max(_leading.GetMaxIntrinsicWidth(height), _minLeadingWidth)
              + EffectiveHorizontalTitleGap;
        return leadingWidth
               + Math.Max(Title.GetMaxIntrinsicWidth(height), _subtitle?.GetMaxIntrinsicWidth(height) ?? 0.0)
               + (_trailing?.GetMaxIntrinsicWidth(height) ?? 0.0);
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        double contentHeight = Title.GetMinIntrinsicHeight(width)
                               + (_subtitle?.GetMinIntrinsicHeight(width) ?? 0.0)
                               + (2.0 * _minVerticalPadding);
        return Math.Max(TargetTileHeight, contentHeight);
    }

    protected override double ComputeMaxIntrinsicHeight(double width) => ComputeMinIntrinsicHeight(width);

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        double? childBaseline = Title.GetDistanceToBaseline(baseline, onlyReal: true);
        return childBaseline.HasValue ? childBaseline.Value + ParentDataOf(Title).offset.Y : null;
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        ListTileSizes sizes = ComputeSizes(
            (child, childConstraints) => child.GetDryLayout(childConstraints),
            (child, childConstraints, childBaseline) => child.GetDryBaseline(childConstraints, childBaseline),
            constraints);
        double? titleBaseline = Title.GetDryBaseline(sizes.TextConstraints, baseline);
        return titleBaseline.HasValue ? titleBaseline.Value + sizes.TitleY : null;
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return constraints.Constrain(ComputeSizes(
            (child, childConstraints) => child.GetDryLayout(childConstraints),
            (child, childConstraints, baseline) => child.GetDryBaseline(childConstraints, baseline),
            constraints).TileSize);
    }

    protected override void PerformLayout()
    {
        ListTileSizes sizes = ComputeSizes(
            LayoutChild,
            GetChildBaseline,
            Constraints,
            PositionChild);
        Size = Constraints.Constrain(sizes.TileSize);
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        PaintChild(context, _leading, offset);
        PaintChild(context, _title, offset);
        PaintChild(context, _subtitle, offset);
        PaintChild(context, _trailing, offset);
    }

    protected override bool HitTestSelf(Point position) => true;

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        return HitTestChild(result, _leading, position)
               || HitTestChild(result, _title, position)
               || HitTestChild(result, _subtitle, position)
               || HitTestChild(result, _trailing, position);
    }

    private ListTileSizes ComputeSizes(
        Func<RenderBox, BoxConstraints, Size> getSize,
        Func<RenderBox, BoxConstraints, TextBaseline, double?> getBaseline,
        BoxConstraints constraints,
        Action<RenderBox, Point>? positionChild = null)
    {
        BoxConstraints looseConstraints = constraints.Loosen();
        double tileWidth = looseConstraints.MaxWidth;
        double maxIconHeight = (_isDense ? 48.0 : 56.0) + _visualDensity.BaseSizeAdjustment.Y;
        BoxConstraints iconConstraints = looseConstraints.Enforce(new BoxConstraints(MaxHeight: maxIconHeight));
        Size? leadingSize = _leading is null ? null : getSize(_leading, iconConstraints);
        Size? trailingSize = _trailing is null ? null : getSize(_trailing, iconConstraints);

        if (!double.IsFinite(tileWidth))
        {
            throw new InvalidOperationException("ListTile requires a bounded width.");
        }
        if (tileWidth == 0.0)
        {
            BoxConstraints zeroConstraints = BoxConstraints.Tight(new Size());
            getSize(Title, zeroConstraints);
            positionChild?.Invoke(Title, default);
            if (_leading is not null)
            {
                getSize(_leading, zeroConstraints);
                positionChild?.Invoke(_leading, default);
            }
            if (_subtitle is not null)
            {
                getSize(_subtitle, zeroConstraints);
                positionChild?.Invoke(_subtitle, default);
            }
            if (_trailing is not null)
            {
                getSize(_trailing, zeroConstraints);
                positionChild?.Invoke(_trailing, default);
            }

            return new ListTileSizes(0.0, zeroConstraints, new Size());
        }
        if (tileWidth != 0.0 && (tileWidth == leadingSize?.Width || tileWidth == trailingSize?.Width))
        {
            string slot = tileWidth == leadingSize?.Width ? "Leading" : "Trailing";
            throw new InvalidOperationException(
                $"{slot} widget consumes the entire tile width (including ListTile.contentPadding).");
        }

        double titleStart = leadingSize is null
            ? 0.0
            : Math.Max(_minLeadingWidth, leadingSize.Value.Width) + EffectiveHorizontalTitleGap;
        double adjustedTrailingWidth = trailingSize is null
            ? 0.0
            : Math.Max(trailingSize.Value.Width + EffectiveHorizontalTitleGap, 32.0);
        double textWidth = tileWidth - titleStart - adjustedTrailingWidth;
        if (textWidth < 0.0)
        {
            throw new InvalidOperationException("ListTile leading and trailing widgets leave no width for the title.");
        }

        BoxConstraints textConstraints = looseConstraints.Tighten(width: textWidth);
        double titleHeight = getSize(Title, textConstraints).Height;
        bool isLtr = _textDirection == TextDirection.Ltr;
        double titleY;
        double tileHeight;
        if (_subtitle is null)
        {
            tileHeight = Math.Max(TargetTileHeight, titleHeight + (2.0 * _minVerticalPadding));
            titleY = (tileHeight - titleHeight) / 2.0;
        }
        else
        {
            double subtitleHeight = getSize(_subtitle, textConstraints).Height;
            double titleBaseline = getBaseline(Title, textConstraints, _titleBaselineType) ?? titleHeight;
            double subtitleBaseline = getBaseline(
                _subtitle,
                textConstraints,
                _subtitleBaselineType ?? _titleBaselineType) ?? subtitleHeight;
            double targetTitleY = (_isThreeLine
                ? (_isDense ? 22.0 : 28.0)
                : (_isDense ? 28.0 : 32.0)) - titleBaseline;
            double targetSubtitleY = (_isThreeLine
                ? (_isDense ? 42.0 : 48.0)
                : (_isDense ? 48.0 : 52.0))
                + (_visualDensity.Vertical * 2.0)
                - subtitleBaseline;
            double halfOverlap = Math.Max(targetTitleY + titleHeight - targetSubtitleY, 0.0) / 2.0;
            double idealTitleY = targetTitleY - halfOverlap;
            double idealSubtitleY = targetSubtitleY + halfOverlap;
            bool compact = idealTitleY < _minVerticalPadding
                           || idealSubtitleY + subtitleHeight + _minVerticalPadding > TargetTileHeight;
            positionChild?.Invoke(
                _subtitle,
                new Point(
                    isLtr ? titleStart : adjustedTrailingWidth,
                    compact ? _minVerticalPadding + titleHeight : idealSubtitleY));
            tileHeight = compact
                ? (2.0 * _minVerticalPadding) + titleHeight + subtitleHeight
                : TargetTileHeight;
            titleY = compact ? _minVerticalPadding : idealTitleY;
        }

        if (positionChild is not null)
        {
            positionChild(Title, new Point(isLtr ? titleStart : adjustedTrailingWidth, titleY));
            if (_leading is not null && leadingSize.HasValue)
            {
                positionChild(
                    _leading,
                    new Point(
                        isLtr ? 0.0 : tileWidth - leadingSize.Value.Width,
                        ResolveSlotY(leadingSize.Value.Height, tileHeight, isLeading: true)));
            }
            if (_trailing is not null && trailingSize.HasValue)
            {
                positionChild(
                    _trailing,
                    new Point(
                        isLtr ? tileWidth - trailingSize.Value.Width : 0.0,
                        ResolveSlotY(trailingSize.Value.Height, tileHeight, isLeading: false)));
            }
        }

        return new ListTileSizes(titleY, textConstraints, new Size(tileWidth, tileHeight));
    }

    private double ResolveSlotY(double childHeight, double tileHeight, bool isLeading)
    {
        return _titleAlignment switch
        {
            ListTileTitleAlignment.ThreeLine => _isThreeLine
                ? _minVerticalPadding
                : (tileHeight - childHeight) / 2.0,
            ListTileTitleAlignment.TitleHeight when tileHeight > 72.0 => 16.0,
            ListTileTitleAlignment.TitleHeight => isLeading
                ? Math.Min((tileHeight - childHeight) / 2.0, 16.0)
                : (tileHeight - childHeight) / 2.0,
            ListTileTitleAlignment.Top => _minVerticalPadding,
            ListTileTitleAlignment.Center => (tileHeight - childHeight) / 2.0,
            ListTileTitleAlignment.Bottom => tileHeight - childHeight - _minVerticalPadding,
            _ => 0.0,
        };
    }

    private static Size LayoutChild(RenderBox child, BoxConstraints constraints)
    {
        child.Layout(constraints, parentUsesSize: true);
        return child.Size;
    }

    private static double? GetChildBaseline(
        RenderBox child,
        BoxConstraints constraints,
        TextBaseline baseline)
    {
        return child.GetDistanceToBaseline(baseline, onlyReal: true);
    }

    private static void PositionChild(RenderBox child, Point offset)
    {
        ParentDataOf(child).offset = offset;
    }

    private static void PaintChild(PaintingContext context, RenderBox? child, Point offset)
    {
        if (child is not null)
        {
            context.PaintChild(child, ParentDataOf(child).offset + offset);
        }
    }

    private static bool HitTestChild(BoxHitTestResult result, RenderBox? child, Point position)
    {
        if (child is null)
        {
            return false;
        }

        return child.HitTest(result, position - ParentDataOf(child).offset);
    }

    private void SetSlotChild(ref RenderBox? field, RenderBox? value)
    {
        if (ReferenceEquals(field, value))
        {
            return;
        }

        if (field is not null)
        {
            DropChild(field);
        }

        field = value;
        if (field is not null)
        {
            AdoptChild(field);
        }
    }

    private void SetLayoutValue<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        MarkNeedsLayout();
    }

    private static BoxParentData ParentDataOf(RenderBox child) => (BoxParentData)child.parentData!;

    private static void Visit(RenderBox? child, Action<RenderObject> visitor)
    {
        if (child is not null)
        {
            visitor(child);
        }
    }

    private readonly record struct ListTileSizes(
        double TitleY,
        BoxConstraints TextConstraints,
        Size TileSize);
}
