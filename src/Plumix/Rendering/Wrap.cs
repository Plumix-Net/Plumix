using Avalonia;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/wrap.dart

namespace Plumix.Rendering;

public enum WrapAlignment
{
    Start,
    End,
    Center,
    SpaceBetween,
    SpaceAround,
    SpaceEvenly,
}

public enum WrapCrossAlignment
{
    Start,
    End,
    Center,
}

public sealed class WrapParentData : ContainerBoxParentData<RenderBox>
{
}

public sealed class RenderWrap : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, WrapParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, WrapParentData> _container;
    private Axis _direction;
    private WrapAlignment _alignment;
    private double _spacing;
    private WrapAlignment _runAlignment;
    private double _runSpacing;
    private WrapCrossAlignment _crossAxisAlignment;
    private TextDirection? _textDirection;
    private VerticalDirection _verticalDirection;
    private Clip _clipBehavior;
    private bool _hasVisualOverflow;

    public RenderWrap(
        List<RenderBox>? children = null,
        Axis direction = Axis.Horizontal,
        WrapAlignment alignment = WrapAlignment.Start,
        double spacing = 0,
        WrapAlignment runAlignment = WrapAlignment.Start,
        double runSpacing = 0,
        WrapCrossAlignment crossAxisAlignment = WrapCrossAlignment.Start,
        TextDirection? textDirection = null,
        VerticalDirection verticalDirection = VerticalDirection.Down,
        Clip clipBehavior = Clip.None)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, WrapParentData>(this);
        _direction = direction;
        _alignment = alignment;
        _spacing = spacing;
        _runAlignment = runAlignment;
        _runSpacing = runSpacing;
        _crossAxisAlignment = crossAxisAlignment;
        _textDirection = textDirection;
        _verticalDirection = verticalDirection;
        _clipBehavior = clipBehavior;

        if (children is not null)
        {
            AddAll(children);
        }
    }

    public Axis Direction
    {
        get => _direction;
        set => SetLayoutProperty(ref _direction, value);
    }

    public WrapAlignment Alignment
    {
        get => _alignment;
        set => SetLayoutProperty(ref _alignment, value);
    }

    public double Spacing
    {
        get => _spacing;
        set => SetLayoutProperty(ref _spacing, value);
    }

    public WrapAlignment RunAlignment
    {
        get => _runAlignment;
        set => SetLayoutProperty(ref _runAlignment, value);
    }

    public double RunSpacing
    {
        get => _runSpacing;
        set => SetLayoutProperty(ref _runSpacing, value);
    }

    public WrapCrossAlignment CrossAxisAlignment
    {
        get => _crossAxisAlignment;
        set => SetLayoutProperty(ref _crossAxisAlignment, value);
    }

    public TextDirection? TextDirection
    {
        get => _textDirection;
        set => SetLayoutProperty(ref _textDirection, value);
    }

    public VerticalDirection VerticalDirection
    {
        get => _verticalDirection;
        set => SetLayoutProperty(ref _verticalDirection, value);
    }

    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (_clipBehavior == value)
            {
                return;
            }

            _clipBehavior = value;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    public RenderBox? FirstChild => _container.FirstChild;
    public RenderBox? LastChild => _container.LastChild;
    public int ChildCount => _container.ChildCount;

    public void AddAll(List<RenderBox>? children) => _container.AddAll(children);

    public void RemoveAll() => _container.RemoveAll();
    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);
    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);
    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);
    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);
    public void Remove(RenderBox child) => _container.Remove(child);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not WrapParentData)
        {
            child.parentData = new WrapParentData();
        }
    }

    protected override void PerformLayout()
    {
        if (FirstChild is null)
        {
            Size = Constraints.Smallest;
            _hasVisualOverflow = false;
            return;
        }

        List<WrapRun> runs = ComputeRuns();
        double childrenMainExtent = runs.Max(run => run.MainExtent);
        double childrenCrossExtent = runs.Sum(run => run.CrossExtent)
                                     + (RunSpacing * Math.Max(0, runs.Count - 1));
        Size = Constraints.Constrain(ToSize(childrenMainExtent, childrenCrossExtent));

        double containerMainExtent = GetMainExtent(Size);
        double containerCrossExtent = GetCrossExtent(Size);
        _hasVisualOverflow = childrenMainExtent > containerMainExtent
                             || childrenCrossExtent > containerCrossExtent;

        PositionChildren(runs, childrenMainExtent, childrenCrossExtent);
    }


    private readonly LayerHandle<ClipRectLayer> _clipRectLayer = new();

    /// <inheritdoc />
    public override void Dispose()
    {
        _clipRectLayer.Layer = null;
        base.Dispose();
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (_hasVisualOverflow && ClipBehavior != Clip.None)
        {
            _clipRectLayer.Layer = context.PushClipRect(
                NeedsCompositing,
                offset,
                new Rect(new Point(0, 0), Size),
                DefaultPaint,
                ClipBehavior,
                _clipRectLayer.Layer);
            return;
        }

        _clipRectLayer.Layer = null;
        DefaultPaint(context, offset);
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            visitor(child);
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            WrapParentData parentData = (WrapParentData)child.parentData!;
            visitor(child);
        }
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        return DefaultHitTestChildren(result, position);
    }

    public void DefaultPaint(PaintingContext context, Point offset) => _container.DefaultPaint(context, offset);

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position)
    {
        return _container.DefaultHitTestChildren(result, position);
    }

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after)
    {
        Insert((RenderBox)child, after as RenderBox);
    }

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after)
    {
        Move((RenderBox)child, after as RenderBox);
    }

    void IRenderObjectContainer.Remove(RenderObject child)
    {
        Remove((RenderBox)child);
    }

    private List<WrapRun> ComputeRuns()
    {
        BoxConstraints childConstraints = Direction == Axis.Horizontal
            ? new BoxConstraints(MaxWidth: Constraints.MaxWidth)
            : new BoxConstraints(MaxHeight: Constraints.MaxHeight);
        double mainAxisLimit = Direction == Axis.Horizontal ? Constraints.MaxWidth : Constraints.MaxHeight;
        bool flipMainAxis = AreAxesFlipped().FlipMainAxis;
        var runs = new List<WrapRun>();
        WrapRun? currentRun = null;

        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            child.Layout(childConstraints, parentUsesSize: true);
            double childMainExtent = GetMainExtent(child.Size);
            double childCrossExtent = GetCrossExtent(child.Size);

            if (currentRun is not null
                && currentRun.ChildCount > 0
                && currentRun.MainExtent + Spacing + childMainExtent > mainAxisLimit)
            {
                runs.Add(currentRun);
                currentRun = null;
            }

            if (currentRun is null)
            {
                currentRun = new WrapRun(child, childMainExtent, childCrossExtent);
            }
            else
            {
                currentRun.Add(child, childMainExtent, childCrossExtent, Spacing, flipMainAxis);
            }
        }

        if (currentRun is not null)
        {
            runs.Add(currentRun);
        }

        return runs;
    }

    private void PositionChildren(
        IReadOnlyList<WrapRun> runs,
        double childrenMainExtent,
        double childrenCrossExtent)
    {
        (bool flipMainAxis, bool flipCrossAxis) = AreAxesFlipped();
        double containerMainExtent = GetMainExtent(Size);
        double containerCrossExtent = GetCrossExtent(Size);
        double crossAxisFreeSpace = Math.Max(0, containerCrossExtent - childrenCrossExtent);
        (double runLeadingSpace, double runBetweenSpace) = DistributeSpace(
            RunAlignment,
            crossAxisFreeSpace,
            RunSpacing,
            runs.Count,
            flipCrossAxis);
        double runCrossOffset = runLeadingSpace;
        IEnumerable<WrapRun> visualRuns = flipCrossAxis ? runs.Reverse() : runs;
        double crossAlignment = ResolveCrossAlignment(flipCrossAxis);

        foreach (WrapRun run in visualRuns)
        {
            double mainAxisFreeSpace = Math.Max(0, containerMainExtent - run.MainExtent);
            (double childLeadingSpace, double childBetweenSpace) = DistributeSpace(
                Alignment,
                mainAxisFreeSpace,
                Spacing,
                run.ChildCount,
                flipMainAxis);
            double childMainOffset = childLeadingSpace;
            RenderBox? child = run.LeadingChild;

            for (int index = 0; index < run.ChildCount && child is not null; index++)
            {
                double childCrossOffset = crossAlignment * (run.CrossExtent - GetCrossExtent(child.Size));
                ((WrapParentData)child.parentData!).offset = ToOffset(
                    childMainOffset,
                    runCrossOffset + childCrossOffset);
                childMainOffset += GetMainExtent(child.Size) + childBetweenSpace;
                child = flipMainAxis ? ChildBefore(child) : ChildAfter(child);
            }

            runCrossOffset += run.CrossExtent + runBetweenSpace;
        }
    }

    private (bool FlipMainAxis, bool FlipCrossAxis) AreAxesFlipped()
    {
        bool flipHorizontal = (TextDirection ?? UI.TextDirection.Ltr) == UI.TextDirection.Rtl;
        bool flipVertical = VerticalDirection == Painting.VerticalDirection.Up;
        return Direction == Axis.Horizontal
            ? (flipHorizontal, flipVertical)
            : (flipVertical, flipHorizontal);
    }

    private double ResolveCrossAlignment(bool flipCrossAxis)
    {
        double alignment = CrossAxisAlignment switch
        {
            WrapCrossAlignment.Start => 0,
            WrapCrossAlignment.Center => 0.5,
            WrapCrossAlignment.End => 1,
            _ => throw new ArgumentOutOfRangeException(),
        };
        return flipCrossAxis ? 1 - alignment : alignment;
    }

    private static (double LeadingSpace, double BetweenSpace) DistributeSpace(
        WrapAlignment alignment,
        double freeSpace,
        double itemSpacing,
        int itemCount,
        bool flipped)
    {
        return alignment switch
        {
            WrapAlignment.Start => (flipped ? freeSpace : 0, itemSpacing),
            WrapAlignment.End => (flipped ? 0 : freeSpace, itemSpacing),
            WrapAlignment.Center => (freeSpace / 2, itemSpacing),
            WrapAlignment.SpaceBetween when itemCount < 2 =>
                (flipped ? freeSpace : 0, itemSpacing),
            WrapAlignment.SpaceBetween => (0, (freeSpace / (itemCount - 1)) + itemSpacing),
            WrapAlignment.SpaceAround =>
                (freeSpace / itemCount / 2, (freeSpace / itemCount) + itemSpacing),
            WrapAlignment.SpaceEvenly =>
                (freeSpace / (itemCount + 1), (freeSpace / (itemCount + 1)) + itemSpacing),
            _ => throw new ArgumentOutOfRangeException(nameof(alignment)),
        };
    }

    private double GetMainExtent(Size size) => Direction == Axis.Horizontal ? size.Width : size.Height;
    private double GetCrossExtent(Size size) => Direction == Axis.Horizontal ? size.Height : size.Width;
    private Size ToSize(double mainExtent, double crossExtent) => Direction == Axis.Horizontal
        ? new Size(mainExtent, crossExtent)
        : new Size(crossExtent, mainExtent);
    private Point ToOffset(double mainOffset, double crossOffset) => Direction == Axis.Horizontal
        ? new Point(mainOffset, crossOffset)
        : new Point(crossOffset, mainOffset);

    private void SetLayoutProperty<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        MarkNeedsLayout();
    }

    private sealed class WrapRun
    {
        public WrapRun(RenderBox child, double mainExtent, double crossExtent)
        {
            LeadingChild = child;
            MainExtent = mainExtent;
            CrossExtent = crossExtent;
            ChildCount = 1;
        }

        public RenderBox LeadingChild { get; private set; }
        public double MainExtent { get; private set; }
        public double CrossExtent { get; private set; }
        public int ChildCount { get; private set; }

        public void Add(
            RenderBox child,
            double mainExtent,
            double crossExtent,
            double spacing,
            bool flipMainAxis)
        {
            MainExtent += spacing + mainExtent;
            CrossExtent = Math.Max(CrossExtent, crossExtent);
            ChildCount++;
            if (flipMainAxis)
            {
                LeadingChild = child;
            }
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new EnumProperty<Axis>("direction", Direction));
        properties.Add(new EnumProperty<WrapAlignment>("alignment", Alignment));
        properties.Add(new DoubleProperty("spacing", Spacing));
        properties.Add(new EnumProperty<WrapAlignment>("runAlignment", RunAlignment));
        properties.Add(new DoubleProperty("runSpacing", RunSpacing));
        properties.Add(new DoubleProperty("crossAxisAlignment", RunSpacing));
        properties.Add(new EnumProperty<TextDirection>(
            "textDirection",
            TextDirection,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new EnumProperty<VerticalDirection>(
            "verticalDirection",
            VerticalDirection,
            defaultValue: VerticalDirection.Down));
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => _container.DebugDescribeChildren();
}
