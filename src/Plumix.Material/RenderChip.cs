using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/chip.dart

internal enum ChipSlot
{
    Label,
    Avatar,
    DeleteIcon,
}

internal sealed class EnsureMinSemanticsSize : SingleChildRenderObjectWidget
{
    public EnsureMinSemanticsSize(
        Size minSemanticSize,
        string label,
        bool enabled,
        Action? onTap,
        Widget child) : base(child)
    {
        MinSemanticSize = minSemanticSize;
        Label = label;
        Enabled = enabled;
        OnTap = onTap;
    }

    public Size MinSemanticSize { get; }
    public string Label { get; }
    public bool Enabled { get; }
    public Action? OnTap { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderEnsureMinSemanticsSize(MinSemanticSize, Label, Enabled, OnTap);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var semantics = (RenderEnsureMinSemanticsSize)renderObject;
        semantics.MinSemanticSize = MinSemanticSize;
        semantics.Label = Label;
        semantics.Enabled = Enabled;
        semantics.OnTap = OnTap;
    }
}

internal sealed class RenderEnsureMinSemanticsSize : RenderProxyBox
{
    private Size _minSemanticSize;
    private string _label;
    private bool _enabled;
    private Action? _onTap;

    public RenderEnsureMinSemanticsSize(
        Size minSemanticSize,
        string label,
        bool enabled,
        Action? onTap)
    {
        _minSemanticSize = minSemanticSize;
        _label = label;
        _enabled = enabled;
        _onTap = onTap;
    }

    public Size MinSemanticSize
    {
        get => _minSemanticSize;
        set => SetSemanticsValue(ref _minSemanticSize, value);
    }

    public string Label
    {
        get => _label;
        set => SetSemanticsValue(ref _label, value);
    }

    public bool Enabled
    {
        get => _enabled;
        set => SetSemanticsValue(ref _enabled, value);
    }

    public Action? OnTap
    {
        get => _onTap;
        set => SetSemanticsValue(ref _onTap, value);
    }

    protected override Rect SemanticBounds
    {
        get
        {
            double width = Math.Max(Size.Width, _minSemanticSize.Width);
            double height = Math.Max(Size.Height, _minSemanticSize.Height);
            return new Rect(
                (Size.Width - width) / 2.0,
                (Size.Height - height) / 2.0,
                width,
                height);
        }
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        configuration.IsSemanticBoundary = true;
        configuration.IsMergingSemanticsOfDescendants = true;
        configuration.Label = _label;
        configuration.Flags = SemanticsFlags.IsButton
                              | (_enabled ? SemanticsFlags.IsEnabled : SemanticsFlags.None);
        if (_onTap is not null)
        {
            configuration.AddActionHandler(SemanticsActions.Tap, _onTap);
        }
    }

    private void SetSemanticsValue<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        MarkNeedsSemanticsUpdate();
    }
}

internal sealed class ChipRenderWidget : SlottedMultiChildRenderObjectWidget<ChipSlot>
{
    private static readonly IReadOnlyList<ChipSlot> AllSlots = Enum.GetValues<ChipSlot>();

    public ChipRenderWidget(
        Widget label,
        Widget? avatar,
        Widget? deleteIcon,
        Thickness padding,
        Thickness labelPadding,
        VisualDensity visualDensity,
        TextDirection textDirection,
        bool isEnabled,
        bool canTap,
        bool showCheckmark,
        Color checkmarkColor,
        ShapeBorder avatarBorder,
        BoxConstraints? avatarBoxConstraints,
        BoxConstraints? deleteIconBoxConstraints,
        double checkmarkProgress,
        double avatarDrawerProgress,
        double deleteDrawerProgress,
        double enableProgress)
    {
        Label = label;
        Avatar = avatar;
        DeleteIcon = deleteIcon;
        Padding = padding;
        LabelPadding = labelPadding;
        VisualDensity = visualDensity;
        TextDirection = textDirection;
        IsEnabled = isEnabled;
        CanTap = canTap;
        ShowCheckmark = showCheckmark;
        CheckmarkColor = checkmarkColor;
        AvatarBorder = avatarBorder;
        AvatarBoxConstraints = avatarBoxConstraints;
        DeleteIconBoxConstraints = deleteIconBoxConstraints;
        CheckmarkProgress = checkmarkProgress;
        AvatarDrawerProgress = avatarDrawerProgress;
        DeleteDrawerProgress = deleteDrawerProgress;
        EnableProgress = enableProgress;
    }

    public Widget Label { get; }
    public Widget? Avatar { get; }
    public Widget? DeleteIcon { get; }
    public Thickness Padding { get; }
    public Thickness LabelPadding { get; }
    public VisualDensity VisualDensity { get; }
    public TextDirection TextDirection { get; }
    public bool IsEnabled { get; }
    public bool CanTap { get; }
    public bool ShowCheckmark { get; }
    public Color CheckmarkColor { get; }
    public ShapeBorder AvatarBorder { get; }
    public BoxConstraints? AvatarBoxConstraints { get; }
    public BoxConstraints? DeleteIconBoxConstraints { get; }
    public double CheckmarkProgress { get; }
    public double AvatarDrawerProgress { get; }
    public double DeleteDrawerProgress { get; }
    public double EnableProgress { get; }

    public override IReadOnlyList<ChipSlot> Slots => AllSlots;

    public override Widget? ChildForSlot(ChipSlot slot)
    {
        return slot switch
        {
            ChipSlot.Label => Label,
            ChipSlot.Avatar => Avatar,
            ChipSlot.DeleteIcon => DeleteIcon,
            _ => null,
        };
    }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderChip(
            padding: Padding,
            labelPadding: LabelPadding,
            visualDensity: VisualDensity,
            textDirection: TextDirection,
            isEnabled: IsEnabled,
            canTap: CanTap,
            showCheckmark: ShowCheckmark,
            checkmarkColor: CheckmarkColor,
            avatarBorder: AvatarBorder,
            avatarBoxConstraints: AvatarBoxConstraints,
            deleteIconBoxConstraints: DeleteIconBoxConstraints,
            checkmarkProgress: CheckmarkProgress,
            avatarDrawerProgress: AvatarDrawerProgress,
            deleteDrawerProgress: DeleteDrawerProgress,
            enableProgress: EnableProgress);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var chip = (RenderChip)renderObject;
        chip.Padding = Padding;
        chip.LabelPadding = LabelPadding;
        chip.VisualDensity = VisualDensity;
        chip.TextDirection = TextDirection;
        chip.IsEnabled = IsEnabled;
        chip.CanTap = CanTap;
        chip.ShowCheckmark = ShowCheckmark;
        chip.CheckmarkColor = CheckmarkColor;
        chip.AvatarBorder = AvatarBorder;
        chip.AvatarBoxConstraints = AvatarBoxConstraints;
        chip.DeleteIconBoxConstraints = DeleteIconBoxConstraints;
        chip.CheckmarkProgress = CheckmarkProgress;
        chip.AvatarDrawerProgress = AvatarDrawerProgress;
        chip.DeleteDrawerProgress = DeleteDrawerProgress;
        chip.EnableProgress = EnableProgress;
    }
}

internal sealed class RenderChip : RenderBox, ISlottedRenderObjectContainer
{
    private const double MinChipHeight = 32.0;
    private RenderBox? _avatar;
    private RenderBox? _label;
    private RenderBox? _deleteIcon;
    private Thickness _padding;
    private Thickness _labelPadding;
    private VisualDensity _visualDensity;
    private TextDirection _textDirection;
    private bool _isEnabled;
    private bool _canTap;
    private bool _showCheckmark;
    private Color _checkmarkColor;
    private ShapeBorder _avatarBorder;
    private BoxConstraints? _avatarBoxConstraints;
    private BoxConstraints? _deleteIconBoxConstraints;
    private double _checkmarkProgress;
    private double _avatarDrawerProgress;
    private double _deleteDrawerProgress;
    private double _enableProgress;
    private Rect _pressRect;
    private Rect _deleteButtonRect;

    public RenderChip(
        Thickness padding,
        Thickness labelPadding,
        VisualDensity visualDensity,
        TextDirection textDirection,
        bool isEnabled,
        bool canTap,
        bool showCheckmark,
        Color checkmarkColor,
        ShapeBorder avatarBorder,
        BoxConstraints? avatarBoxConstraints,
        BoxConstraints? deleteIconBoxConstraints,
        double checkmarkProgress,
        double avatarDrawerProgress,
        double deleteDrawerProgress,
        double enableProgress)
    {
        _padding = padding;
        _labelPadding = labelPadding;
        _visualDensity = visualDensity;
        _textDirection = textDirection;
        _isEnabled = isEnabled;
        _canTap = canTap;
        _showCheckmark = showCheckmark;
        _checkmarkColor = checkmarkColor;
        _avatarBorder = avatarBorder;
        _avatarBoxConstraints = avatarBoxConstraints;
        _deleteIconBoxConstraints = deleteIconBoxConstraints;
        _checkmarkProgress = checkmarkProgress;
        _avatarDrawerProgress = avatarDrawerProgress;
        _deleteDrawerProgress = deleteDrawerProgress;
        _enableProgress = enableProgress;
    }

    public RenderBox? Avatar => _avatar;
    public RenderBox Label => _label ?? throw new InvalidOperationException("Chip label is not mounted.");
    public RenderBox? DeleteIcon => _deleteIcon;
    public Rect PressRect => _pressRect;
    public Rect DeleteButtonRect => _deleteButtonRect;

    public Thickness Padding
    {
        get => _padding;
        set => SetLayoutValue(ref _padding, value);
    }

    public Thickness LabelPadding
    {
        get => _labelPadding;
        set => SetLayoutValue(ref _labelPadding, value);
    }

    public VisualDensity VisualDensity
    {
        get => _visualDensity;
        set => SetLayoutValue(ref _visualDensity, value);
    }

    public TextDirection TextDirection
    {
        get => _textDirection;
        set => SetLayoutValue(ref _textDirection, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetPaintValue(ref _isEnabled, value);
    }

    public bool CanTap
    {
        get => _canTap;
        set => SetLayoutValue(ref _canTap, value);
    }

    public bool ShowCheckmark
    {
        get => _showCheckmark;
        set => SetLayoutValue(ref _showCheckmark, value);
    }

    public Color CheckmarkColor
    {
        get => _checkmarkColor;
        set => SetPaintValue(ref _checkmarkColor, value);
    }

    public ShapeBorder AvatarBorder
    {
        get => _avatarBorder;
        set => SetPaintValue(ref _avatarBorder, value);
    }

    public BoxConstraints? AvatarBoxConstraints
    {
        get => _avatarBoxConstraints;
        set => SetLayoutValue(ref _avatarBoxConstraints, value);
    }

    public BoxConstraints? DeleteIconBoxConstraints
    {
        get => _deleteIconBoxConstraints;
        set => SetLayoutValue(ref _deleteIconBoxConstraints, value);
    }

    public double CheckmarkProgress
    {
        get => _checkmarkProgress;
        set => SetProgress(ref _checkmarkProgress, value, layout: false);
    }

    public double AvatarDrawerProgress
    {
        get => _avatarDrawerProgress;
        set => SetProgress(ref _avatarDrawerProgress, value, layout: true);
    }

    public double DeleteDrawerProgress
    {
        get => _deleteDrawerProgress;
        set => SetProgress(ref _deleteDrawerProgress, value, layout: true);
    }

    public double EnableProgress
    {
        get => _enableProgress;
        set => SetProgress(ref _enableProgress, value, layout: false);
    }

    public void SetChild(RenderObject? child, object slot)
    {
        RenderBox? box = child switch
        {
            null => null,
            RenderBox renderBox => renderBox,
            _ => throw new InvalidOperationException("Chip slots require RenderBox children."),
        };

        switch ((ChipSlot)slot)
        {
            case ChipSlot.Label:
                SetSlotChild(ref _label, box);
                break;
            case ChipSlot.Avatar:
                SetSlotChild(ref _avatar, box);
                break;
            case ChipSlot.DeleteIcon:
                SetSlotChild(ref _deleteIcon, box);
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
        Visit(_avatar, visitor);
        Visit(_label, visitor);
        Visit(_deleteIcon, visitor);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        VisitForSemantics(_avatar, visitor);
        VisitForSemantics(_label, visitor);
        VisitForSemantics(_deleteIcon, visitor);
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        return Horizontal(_padding)
               + Horizontal(_labelPadding)
               + (_avatar?.GetMinIntrinsicWidth(height) ?? 0.0)
               + Label.GetMinIntrinsicWidth(height)
               + (_deleteIcon?.GetMinIntrinsicWidth(height) ?? 0.0);
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        return Horizontal(_padding)
               + Horizontal(_labelPadding)
               + (_avatar?.GetMaxIntrinsicWidth(height) ?? 0.0)
               + Label.GetMaxIntrinsicWidth(height)
               + (_deleteIcon?.GetMaxIntrinsicWidth(height) ?? 0.0);
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        return Math.Max(
            MinChipHeight,
            Vertical(_padding) + Vertical(_labelPadding) + Label.GetMinIntrinsicHeight(width));
    }

    protected override double ComputeMaxIntrinsicHeight(double width) => ComputeMinIntrinsicHeight(width);

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        double? childBaseline = Label.GetDistanceToBaseline(baseline, onlyReal: true);
        return childBaseline.HasValue ? childBaseline.Value + ParentDataOf(Label).offset.Y : null;
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return constraints.Constrain(ComputeSizes(
            constraints,
            static (child, childConstraints) => child.GetDryLayout(childConstraints)).PaddedSize);
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        ChipSizes sizes = ComputeSizes(
            constraints,
            static (child, childConstraints) => child.GetDryLayout(childConstraints));
        double? childBaseline = Label.GetDryBaseline(sizes.LabelConstraints, baseline);
        if (!childBaseline.HasValue)
        {
            return null;
        }

        double labelContentHeight = sizes.LabelSize.Height - Vertical(_labelPadding);
        return childBaseline.Value
               + ((sizes.ContentSize - labelContentHeight + sizes.DensityAdjustmentY) / 2.0)
               + _padding.Top
               + _labelPadding.Top;
    }

    protected override void PerformLayout()
    {
        ChipSizes sizes = ComputeSizes(Constraints, LayoutChild);
        Size = Constraints.Constrain(sizes.PaddedSize);
        PositionChildren(sizes);
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        double opacity = _isEnabled ? 1.0 : 97.0 / 255.0;
        if (!_isEnabled || _enableProgress < 1.0)
        {
            opacity = (97.0 / 255.0) + ((1.0 - (97.0 / 255.0)) * _enableProgress);
        }

        PaintWithOpacity(context, _avatar, offset, opacity);
        PaintAvatarSelection(context, offset);
        PaintWithOpacity(context, _deleteIcon, offset, opacity);
        PaintWithOpacity(context, _label, offset, opacity);
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        if (position.X < 0.0 || position.Y < 0.0 || position.X > Size.Width || position.Y > Size.Height)
        {
            return false;
        }

        bool onDelete = _deleteButtonRect.Contains(position) || HitIsOnDeleteIcon(position);
        RenderBox target = onDelete && _deleteIcon is not null ? _deleteIcon : Label;
        bool childHit = target.HitTest(
            result,
            new Point(target.Size.Width / 2.0, target.Size.Height / 2.0));
        if (childHit || HitTestSelf(position))
        {
            result.Add(new BoxHitTestEntry(this, position));
            return true;
        }

        return false;
    }

    protected override bool HitTestSelf(Point position)
    {
        return _pressRect.Contains(position) || _deleteButtonRect.Contains(position);
    }

    private ChipSizes ComputeSizes(
        BoxConstraints constraints,
        Func<RenderBox, BoxConstraints, Size> layoutChild)
    {
        BoxConstraints contentConstraints = constraints.Loosen();
        Size rawLabelSize = layoutChild(Label, contentConstraints);
        double contentSize = Math.Max(
            MinChipHeight - Vertical(_padding) + Vertical(_labelPadding),
            rawLabelSize.Height + Vertical(_labelPadding));

        Size actualAvatarSize = new(contentSize, contentSize);
        if (_avatar is not null)
        {
            BoxConstraints avatarConstraints = _avatarBoxConstraints
                                               ?? BoxConstraints.TightFor(contentSize, contentSize);
            actualAvatarSize = layoutChild(_avatar, avatarConstraints);
        }

        bool showAvatar = _avatar is not null;
        bool showCheckmarkArea = _showCheckmark && _checkmarkProgress > 0.0;
        double avatarWidth = showAvatar || showCheckmarkArea
            ? (showAvatar ? actualAvatarSize.Width : contentSize) * _avatarDrawerProgress
            : 0.0;
        var logicalAvatarSize = new Size(avatarWidth, actualAvatarSize.Height);

        Size actualDeleteSize = new(contentSize, contentSize);
        if (_deleteIcon is not null)
        {
            BoxConstraints deleteConstraints = _deleteIconBoxConstraints
                                               ?? BoxConstraints.TightFor(contentSize, contentSize);
            actualDeleteSize = layoutChild(_deleteIcon, deleteConstraints);
        }

        double deleteWidth = _deleteIcon is null ? 0.0 : _deleteDrawerProgress * actualDeleteSize.Width;
        var logicalDeleteSize = new Size(deleteWidth, actualDeleteSize.Height);
        double freeSpace = contentConstraints.MaxWidth
                           - logicalAvatarSize.Width
                           - logicalDeleteSize.Width
                           - Horizontal(_labelPadding)
                           - Horizontal(_padding);
        double maxLabelWidth = double.IsFinite(freeSpace)
            ? Math.Max(0.0, freeSpace)
            : rawLabelSize.Width;
        var labelConstraints = new BoxConstraints(
            MinHeight: rawLabelSize.Height,
            MaxWidth: maxLabelWidth,
            MaxHeight: contentSize);
        Size actualLabelSize = layoutChild(Label, labelConstraints);
        var labelSize = new Size(
            actualLabelSize.Width + Horizontal(_labelPadding),
            actualLabelSize.Height + Vertical(_labelPadding));
        double densityAdjustmentY = _visualDensity.BaseSizeAdjustment.Y / 2.0;
        double overallWidth = logicalAvatarSize.Width + labelSize.Width + logicalDeleteSize.Width;
        double overallHeight = Math.Max(0.0, contentSize + densityAdjustmentY);
        var paddedSize = new Size(
            overallWidth + Horizontal(_padding),
            overallHeight + Vertical(_padding));
        return new ChipSizes(
            actualAvatarSize,
            logicalAvatarSize,
            actualLabelSize,
            labelSize,
            labelConstraints,
            actualDeleteSize,
            logicalDeleteSize,
            contentSize,
            densityAdjustmentY,
            overallWidth,
            overallHeight,
            paddedSize);
    }

    private void PositionChildren(ChipSizes sizes)
    {
        bool ltr = _textDirection == TextDirection.Ltr;
        double start = ltr ? 0.0 : sizes.OverallWidth;
        Point avatarOffset = default;
        Point labelOffset;
        Point deleteOffset = default;

        if (sizes.LogicalAvatarSize.Width > 0.0)
        {
            double avatarX = ltr
                ? start - sizes.ActualAvatarSize.Width + sizes.LogicalAvatarSize.Width
                : start;
            avatarOffset = CenterLayout(sizes.LogicalAvatarSize, avatarX, sizes, ltr);
            start += ltr ? sizes.LogicalAvatarSize.Width : -sizes.LogicalAvatarSize.Width;
        }

        labelOffset = CenterLayout(sizes.LabelSize, start, sizes, ltr);
        start += ltr ? sizes.LabelSize.Width : -sizes.LabelSize.Width;

        if (ltr)
        {
            _pressRect = _canTap
                ? new Rect(
                    0.0,
                    0.0,
                    sizes.LogicalDeleteSize.Width > 0.0
                        ? start + _padding.Left
                        : sizes.OverallWidth + Horizontal(_padding),
                    sizes.OverallHeight + Vertical(_padding))
                : default;
            start -= sizes.ActualDeleteSize.Width - sizes.LogicalDeleteSize.Width;
            if (sizes.LogicalDeleteSize.Width > 0.0)
            {
                deleteOffset = CenterLayout(sizes.LogicalDeleteSize, start, sizes, ltr: true);
                _deleteButtonRect = new Rect(
                    start + _padding.Left,
                    0.0,
                    sizes.LogicalDeleteSize.Width + _padding.Right,
                    sizes.OverallHeight + Vertical(_padding));
            }
            else
            {
                _deleteButtonRect = default;
            }
        }
        else
        {
            if (sizes.LogicalDeleteSize.Width > 0.0)
            {
                _deleteButtonRect = new Rect(
                    0.0,
                    0.0,
                    sizes.LogicalDeleteSize.Width + _padding.Right,
                    sizes.OverallHeight + Vertical(_padding));
                deleteOffset = CenterLayout(sizes.LogicalDeleteSize, start, sizes, ltr: false);
            }
            else
            {
                _deleteButtonRect = default;
            }

            start -= sizes.LogicalDeleteSize.Width;
            _pressRect = _canTap
                ? new Rect(
                    _deleteButtonRect.Width,
                    0.0,
                    sizes.OverallWidth - _deleteButtonRect.Width + Horizontal(_padding),
                    sizes.OverallHeight + Vertical(_padding))
                : default;
        }

        double labelCentering = ((sizes.LabelSize.Height - Vertical(_labelPadding))
                                 - sizes.ActualLabelSize.Height) / 2.0;
        Point paddingOffset = new(_padding.Left, _padding.Top);
        if (_avatar is not null)
        {
            ParentDataOf(_avatar).offset = paddingOffset + avatarOffset;
        }

        ParentDataOf(Label).offset = paddingOffset
                                     + labelOffset
                                     + new Vector(_labelPadding.Left, _labelPadding.Top + labelCentering);
        if (_deleteIcon is not null)
        {
            ParentDataOf(_deleteIcon).offset = paddingOffset + deleteOffset;
        }
    }

    private static Point CenterLayout(Size boxSize, double x, ChipSizes sizes, bool ltr)
    {
        double resolvedX = ltr ? x : x - boxSize.Width;
        double y = (sizes.ContentSize - boxSize.Height + sizes.DensityAdjustmentY) / 2.0;
        return new Point(resolvedX, y);
    }

    private bool HitIsOnDeleteIcon(Point position)
    {
        if (_deleteIcon is null || _deleteButtonRect.Width <= 0.0)
        {
            return false;
        }

        double deflatedWidth = Math.Max(0.0, Size.Width - Horizontal(_padding));
        double deleteWidth = _deleteButtonRect.Width;
        double accessibleWidth = Math.Min(
            deflatedWidth * 0.499,
            Math.Min(_labelPadding.Right + deleteWidth, 24.0 + (deleteWidth / 2.0)));
        double localX = position.X - _padding.Left;
        return _textDirection == TextDirection.Ltr
            ? localX >= deflatedWidth - accessibleWidth
            : localX <= accessibleWidth;
    }

    private void PaintAvatarSelection(PaintingContext context, Point offset)
    {
        if (!_showCheckmark || _checkmarkProgress <= 0.0 || _avatarDrawerProgress <= 0.0)
        {
            return;
        }

        Rect avatarRect;
        if (_avatar is not null)
        {
            avatarRect = new Rect(ParentDataOf(_avatar).offset + offset, _avatar.Size);
            var scrim = new SolidColorBrush(Color.FromArgb(0x60, 0x19, 0x19, 0x19));
            if (_avatarBorder.Shape == BoxShape.Circle)
            {
                context.DrawCircle(
                    scrim,
                    null,
                    avatarRect.Center,
                    Math.Min(avatarRect.Width, avatarRect.Height) / 2.0);
            }
            else
            {
                context.DrawRectangle(
                    scrim,
                    null,
                    avatarRect,
                    _avatarBorder.BorderRadius);
            }
        }
        else
        {
            double contentHeight = Math.Max(0.0, Size.Height - Vertical(_padding));
            double x = _textDirection == TextDirection.Ltr
                ? offset.X + _padding.Left
                : offset.X + Size.Width - _padding.Right - contentHeight;
            avatarRect = new Rect(x, offset.Y + _padding.Top, contentHeight, contentHeight);
        }

        PaintCheckmark(context, avatarRect, _checkmarkProgress);
    }

    private void PaintCheckmark(PaintingContext context, Rect avatarRect, double progress)
    {
        double checkSize = avatarRect.Height * 0.75;
        Point origin = avatarRect.Position + new Vector(avatarRect.Height * 0.125, avatarRect.Height * 0.125);
        Point start = origin + new Vector(checkSize * 0.15, checkSize * 0.45);
        Point middle = origin + new Vector(checkSize * 0.4, checkSize * 0.7);
        Point end = origin + new Vector(checkSize * 0.85, checkSize * 0.25);
        var color = Color.FromArgb(
            (byte)Math.Round(_checkmarkColor.A * Math.Clamp(progress, 0.0, 1.0)),
            _checkmarkColor.R,
            _checkmarkColor.G,
            _checkmarkColor.B);
        var pen = new Pen(new SolidColorBrush(color), 2.0 * avatarRect.Height / 24.0);
        if (progress < 0.5)
        {
            context.DrawLine(pen, start, Lerp(start, middle, progress * 2.0));
            return;
        }

        context.DrawLine(pen, start, middle);
        context.DrawLine(pen, middle, Lerp(middle, end, (progress - 0.5) * 2.0));
    }

    private static Point Lerp(Point begin, Point end, double t)
    {
        return new Point(
            begin.X + ((end.X - begin.X) * t),
            begin.Y + ((end.Y - begin.Y) * t));
    }

    private static void PaintWithOpacity(
        PaintingContext context,
        RenderBox? child,
        Point offset,
        double opacity)
    {
        if (child is null)
        {
            return;
        }

        Point childOffset = ParentDataOf(child).offset + offset;
        if (opacity >= 0.999)
        {
            context.PaintChild(child, childOffset);
            return;
        }

        context.PushOpacity(opacity, childContext => childContext.PaintChild(child, childOffset));
    }

    private static Size LayoutChild(RenderBox child, BoxConstraints constraints)
    {
        child.Layout(constraints, parentUsesSize: true);
        return child.Size;
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

    private void SetProgress(ref double field, double value, bool layout)
    {
        double next = Math.Clamp(value, 0.0, 1.0);
        if (Math.Abs(field - next) <= 0.000001)
        {
            return;
        }

        field = next;
        if (layout)
        {
            MarkNeedsLayout();
        }
        else
        {
            MarkNeedsPaint();
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

    private void SetPaintValue<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        MarkNeedsPaint();
    }

    private static double Horizontal(Thickness value) => value.Left + value.Right;

    private static double Vertical(Thickness value) => value.Top + value.Bottom;

    private static BoxParentData ParentDataOf(RenderBox child) => (BoxParentData)child.parentData!;

    private static void Visit(RenderBox? child, Action<RenderObject> visitor)
    {
        if (child is not null)
        {
            visitor(child);
        }
    }

    private static void VisitForSemantics(
        RenderBox? child,
        Action<RenderObject, Point, Matrix> visitor)
    {
        if (child is not null)
        {
            visitor(child, ParentDataOf(child).offset, Matrix.Identity);
        }
    }

    private readonly record struct ChipSizes(
        Size ActualAvatarSize,
        Size LogicalAvatarSize,
        Size ActualLabelSize,
        Size LabelSize,
        BoxConstraints LabelConstraints,
        Size ActualDeleteSize,
        Size LogicalDeleteSize,
        double ContentSize,
        double DensityAdjustmentY,
        double OverallWidth,
        double OverallHeight,
        Size PaddedSize);
}
