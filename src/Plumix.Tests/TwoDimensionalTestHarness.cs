using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Ported from flutter/packages/flutter/test/widgets/two_dimensional_utils.dart — the shared harness
// two_dimensional_viewport_test.dart and two_dimensional_scroll_view_test.dart build their cases on.

namespace Plumix.Tests;

/// <summary>The 6x6 builder delegate the Flutter harness shares between its cases.</summary>
internal static class TwoDimensionalHarness
{
    public const double CellExtent = 200.0;

    public static readonly Color Amber100 = Color.FromUInt32(0xFFFFF8E1);
    public static readonly Color BlueAccent100 = Color.FromUInt32(0xFF82B1FF);

    public static TwoDimensionalChildBuilderDelegate BuilderDelegate(
        int? maxXIndex = 5,
        int? maxYIndex = 5,
        bool addRepaintBoundaries = true,
        bool addAutomaticKeepAlives = true,
        TwoDimensionalIndexedWidgetBuilder? builder = null)
    {
        return new TwoDimensionalChildBuilderDelegate(
            builder ?? DefaultBuilder,
            maxXIndex: maxXIndex,
            maxYIndex: maxYIndex,
            addRepaintBoundaries: addRepaintBoundaries,
            addAutomaticKeepAlives: addAutomaticKeepAlives);
    }

    public static Widget DefaultBuilder(BuildContext context, ChildVicinity vicinity)
    {
        return new Container(
            key: new ValueKey<ChildVicinity>(vicinity),
            color: CellColor(vicinity.XIndex, vicinity.YIndex),
            height: CellExtent,
            width: CellExtent,
            child: new SizedBox());
    }

    public static Color? CellColor(int xIndex, int yIndex)
    {
        if (xIndex % 2 == 0 && yIndex % 2 == 0)
        {
            return Amber100;
        }

        return xIndex % 2 != 0 && yIndex % 2 != 0 ? BlueAccent100 : null;
    }

    /// <summary>The 100x100 unkeyed grid the list-delegate cases use.</summary>
    public static IReadOnlyList<IReadOnlyList<Widget>> Children(int rows = 100, int columns = 100)
    {
        var result = new List<IReadOnlyList<Widget>>(rows);
        for (int yIndex = 0; yIndex < rows; yIndex++)
        {
            var row = new List<Widget>(columns);
            for (int xIndex = 0; xIndex < columns; xIndex++)
            {
                row.Add(new Container(
                    color: CellColor(xIndex, yIndex),
                    height: CellExtent,
                    width: CellExtent,
                    child: new SizedBox()));
            }

            result.Add(row);
        }

        return result;
    }
}

/// <remarks>Flutter's <c>TestExtendedParentData</c>.</remarks>
internal sealed class TestExtendedParentData : TwoDimensionalViewportParentData
{
    public int? TestValue { get; set; }
}

/// <remarks>Flutter's <c>TestParentDataWidget</c>.</remarks>
internal sealed class TestParentDataWidget : ParentDataWidget<TestExtendedParentData>
{
    public TestParentDataWidget(Widget child, int? testValue, Key? key = null) : base(child, key)
    {
        TestValue = testValue;
    }

    public int? TestValue { get; }

    public override Type DebugTypicalAncestorWidgetType => typeof(SimpleBuilderTableViewport);

    protected override void ApplyParentData(RenderObject renderObject)
    {
        var parentData = (TestExtendedParentData)renderObject.parentData!;
        parentData.TestValue = TestValue;
    }
}

/// <remarks>Flutter's <c>SimpleBuilderTableView</c>.</remarks>
internal sealed class SimpleBuilderTableView : TwoDimensionalScrollView
{
    public SimpleBuilderTableView(
        TwoDimensionalChildBuilderDelegate @delegate,
        bool? primary = null,
        Axis mainAxis = Axis.Vertical,
        ScrollableDetails? verticalDetails = null,
        ScrollableDetails? horizontalDetails = null,
        ScrollCacheExtent? scrollCacheExtent = null,
        DiagonalDragBehavior diagonalDragBehavior = DiagonalDragBehavior.None,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        ScrollViewKeyboardDismissBehavior? keyboardDismissBehavior = null,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        bool useCacheExtent = false,
        bool applyDimensions = true,
        bool forgetToLayoutChild = false,
        bool setLayoutOffset = true,
        Key? key = null) : base(
            @delegate,
            primary: primary,
            mainAxis: mainAxis,
            verticalDetails: verticalDetails,
            horizontalDetails: horizontalDetails,
            scrollCacheExtent: scrollCacheExtent,
            diagonalDragBehavior: diagonalDragBehavior,
            dragStartBehavior: dragStartBehavior,
            keyboardDismissBehavior: keyboardDismissBehavior,
            clipBehavior: clipBehavior,
            hitTestBehavior: hitTestBehavior,
            key: key)
    {
        UseCacheExtent = useCacheExtent;
        ApplyDimensions = applyDimensions;
        ForgetToLayoutChild = forgetToLayoutChild;
        SetLayoutOffset = setLayoutOffset;
    }

    public bool UseCacheExtent { get; }

    public bool ApplyDimensions { get; }

    public bool ForgetToLayoutChild { get; }

    public bool SetLayoutOffset { get; }

    public override Widget BuildViewport(
        BuildContext context,
        ViewportOffset verticalOffset,
        ViewportOffset horizontalOffset)
    {
        return new SimpleBuilderTableViewport(
            verticalOffset: verticalOffset,
            verticalAxisDirection: VerticalDetails.Direction,
            horizontalOffset: horizontalOffset,
            horizontalAxisDirection: HorizontalDetails.Direction,
            @delegate: (TwoDimensionalChildBuilderDelegate)Delegate,
            mainAxis: MainAxis,
            scrollCacheExtent: ScrollCacheExtent,
            clipBehavior: ClipBehavior,
            useCacheExtent: UseCacheExtent,
            applyDimensions: ApplyDimensions,
            forgetToLayoutChild: ForgetToLayoutChild,
            setLayoutOffset: SetLayoutOffset);
    }
}

/// <remarks>Flutter's <c>SimpleBuilderTableViewport</c>.</remarks>
internal sealed class SimpleBuilderTableViewport : TwoDimensionalViewport
{
    public SimpleBuilderTableViewport(
        ViewportOffset verticalOffset,
        AxisDirection verticalAxisDirection,
        ViewportOffset horizontalOffset,
        AxisDirection horizontalAxisDirection,
        TwoDimensionalChildBuilderDelegate @delegate,
        Axis mainAxis,
        ScrollCacheExtent? scrollCacheExtent = null,
        Clip clipBehavior = Clip.HardEdge,
        bool useCacheExtent = false,
        bool applyDimensions = true,
        bool forgetToLayoutChild = false,
        bool setLayoutOffset = true,
        Key? key = null) : base(
            verticalOffset,
            verticalAxisDirection,
            horizontalOffset,
            horizontalAxisDirection,
            @delegate,
            mainAxis,
            scrollCacheExtent: scrollCacheExtent,
            clipBehavior: clipBehavior,
            key: key)
    {
        UseCacheExtent = useCacheExtent;
        ApplyDimensions = applyDimensions;
        ForgetToLayoutChild = forgetToLayoutChild;
        SetLayoutOffset = setLayoutOffset;
    }

    public bool UseCacheExtent { get; }

    public bool ApplyDimensions { get; }

    public bool ForgetToLayoutChild { get; }

    public bool SetLayoutOffset { get; }

    public override RenderTwoDimensionalViewport CreateRenderObject(BuildContext context)
    {
        return new RenderSimpleBuilderTableViewport(
            horizontalOffset: HorizontalOffset,
            horizontalAxisDirection: HorizontalAxisDirection,
            verticalOffset: VerticalOffset,
            verticalAxisDirection: VerticalAxisDirection,
            @delegate: (TwoDimensionalChildBuilderDelegate)Delegate,
            mainAxis: MainAxis,
            childManager: ChildManagerOf(context),
            scrollCacheExtent: ScrollCacheExtent,
            clipBehavior: ClipBehavior,
            applyDimensions: ApplyDimensions,
            setLayoutOffset: SetLayoutOffset,
            useCacheExtent: UseCacheExtent,
            forgetToLayoutChild: ForgetToLayoutChild);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var viewport = (RenderSimpleBuilderTableViewport)renderObject;
        viewport.HorizontalOffset = HorizontalOffset;
        viewport.HorizontalAxisDirection = HorizontalAxisDirection;
        viewport.VerticalOffset = VerticalOffset;
        viewport.VerticalAxisDirection = VerticalAxisDirection;
        viewport.MainAxis = MainAxis;
        viewport.Delegate = Delegate;
        viewport.ScrollCacheExtent = ScrollCacheExtent!;
        viewport.ClipBehavior = ClipBehavior;
    }
}

/// <remarks>Flutter's <c>RenderSimpleBuilderTableViewport</c>.</remarks>
internal sealed class RenderSimpleBuilderTableViewport : RenderTwoDimensionalViewport
{
    public RenderSimpleBuilderTableViewport(
        ViewportOffset horizontalOffset,
        AxisDirection horizontalAxisDirection,
        ViewportOffset verticalOffset,
        AxisDirection verticalAxisDirection,
        TwoDimensionalChildBuilderDelegate @delegate,
        Axis mainAxis,
        ITwoDimensionalChildManager childManager,
        ScrollCacheExtent? scrollCacheExtent = null,
        Clip clipBehavior = Clip.HardEdge,
        bool applyDimensions = true,
        bool setLayoutOffset = true,
        bool useCacheExtent = false,
        bool forgetToLayoutChild = false) : base(
            horizontalOffset,
            horizontalAxisDirection,
            verticalOffset,
            verticalAxisDirection,
            @delegate,
            mainAxis,
            childManager,
            scrollCacheExtent: scrollCacheExtent,
            clipBehavior: clipBehavior)
    {
        ApplyDimensions = applyDimensions;
        SetLayoutOffset = setLayoutOffset;
        UseCacheExtent = useCacheExtent;
        ForgetToLayoutChild = forgetToLayoutChild;
    }

    public bool ApplyDimensions { get; }

    public bool SetLayoutOffset { get; }

    public bool UseCacheExtent { get; }

    public bool ForgetToLayoutChild { get; }

    /// <summary>A public window onto the protected <c>GetChildFor</c>.</summary>
    public RenderBox? TestGetChildFor(ChildVicinity vicinity) => GetChildFor(vicinity);

    public override TestExtendedParentData ParentDataOf(RenderBox child) =>
        (TestExtendedParentData)base.ParentDataOf(child);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not TestExtendedParentData)
        {
            child.parentData = new TestExtendedParentData();
        }
    }

    protected override void LayoutChildSequence()
    {
        double horizontalPixels = HorizontalOffset.Pixels;
        double verticalPixels = VerticalOffset.Pixels;
        double viewportWidth = ViewportDimension.Width;
        double viewportHeight = ViewportDimension.Height;
        var builderDelegate = (TwoDimensionalChildBuilderDelegate)Delegate;

        double cacheExtentValue = UseCacheExtent ? CacheExtent : 0.0;
        double horizontalCacheExtent = CacheExtentStyle == CacheExtentStyle.Viewport
            ? viewportWidth * cacheExtentValue
            : cacheExtentValue;
        double verticalCacheExtent = CacheExtentStyle == CacheExtentStyle.Viewport
            ? viewportHeight * cacheExtentValue
            : cacheExtentValue;

        int maxRowIndex = builderDelegate.MaxYIndex ?? 5;
        int maxColumnIndex = builderDelegate.MaxXIndex ?? 5;

        int leadingColumn = Math.Max(
            (int)Math.Floor((horizontalPixels - horizontalCacheExtent) / TwoDimensionalHarness.CellExtent),
            0);
        int leadingRow = Math.Max(
            (int)Math.Floor((verticalPixels - verticalCacheExtent) / TwoDimensionalHarness.CellExtent),
            0);
        int trailingColumn = Math.Min(
            (int)Math.Ceiling(
                (horizontalPixels + viewportWidth + horizontalCacheExtent) / TwoDimensionalHarness.CellExtent),
            maxColumnIndex);
        int trailingRow = Math.Min(
            (int)Math.Ceiling(
                (verticalPixels + viewportHeight + verticalCacheExtent) / TwoDimensionalHarness.CellExtent),
            maxRowIndex);

        double xLayoutOffset = (leadingColumn * TwoDimensionalHarness.CellExtent) - HorizontalOffset.Pixels;
        for (int column = leadingColumn; column <= trailingColumn; column++)
        {
            double yLayoutOffset = (leadingRow * TwoDimensionalHarness.CellExtent) - VerticalOffset.Pixels;
            for (int row = leadingRow; row <= trailingRow; row++)
            {
                var vicinity = new ChildVicinity(xIndex: column, yIndex: row);
                RenderBox? child = BuildOrObtainChildFor(vicinity);
                if (!ForgetToLayoutChild)
                {
                    child?.Layout(
                        Constraints.Tighten(
                            width: TwoDimensionalHarness.CellExtent,
                            height: TwoDimensionalHarness.CellExtent),
                        parentUsesSize: true);
                }

                if (SetLayoutOffset && child != null)
                {
                    ParentDataOf(child).LayoutOffset = new Point(xLayoutOffset, yLayoutOffset);
                }

                yLayoutOffset += TwoDimensionalHarness.CellExtent;
            }

            xLayoutOffset += TwoDimensionalHarness.CellExtent;
        }

        if (ApplyDimensions)
        {
            VerticalOffset.ApplyContentDimensions(
                0.0,
                Math.Clamp(
                    (TwoDimensionalHarness.CellExtent * (maxRowIndex + 1)) - ViewportDimension.Height,
                    0.0,
                    double.PositiveInfinity));
            HorizontalOffset.ApplyContentDimensions(
                0.0,
                Math.Clamp(
                    (TwoDimensionalHarness.CellExtent * (maxColumnIndex + 1)) - ViewportDimension.Width,
                    0.0,
                    double.PositiveInfinity));
        }
    }
}

/// <remarks>Flutter's <c>SimpleListTableView</c>.</remarks>
internal sealed class SimpleListTableView : TwoDimensionalScrollView
{
    public SimpleListTableView(
        TwoDimensionalChildListDelegate @delegate,
        Axis mainAxis = Axis.Vertical,
        ScrollableDetails? verticalDetails = null,
        ScrollableDetails? horizontalDetails = null,
        DiagonalDragBehavior diagonalDragBehavior = DiagonalDragBehavior.None,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null) : base(
            @delegate,
            mainAxis: mainAxis,
            verticalDetails: verticalDetails,
            horizontalDetails: horizontalDetails,
            diagonalDragBehavior: diagonalDragBehavior,
            clipBehavior: clipBehavior,
            key: key)
    {
    }

    public override Widget BuildViewport(
        BuildContext context,
        ViewportOffset verticalOffset,
        ViewportOffset horizontalOffset)
    {
        return new SimpleListTableViewport(
            verticalOffset: verticalOffset,
            verticalAxisDirection: VerticalDetails.Direction,
            horizontalOffset: horizontalOffset,
            horizontalAxisDirection: HorizontalDetails.Direction,
            @delegate: (TwoDimensionalChildListDelegate)Delegate,
            mainAxis: MainAxis,
            clipBehavior: ClipBehavior);
    }
}

/// <remarks>Flutter's <c>SimpleListTableViewport</c>.</remarks>
internal sealed class SimpleListTableViewport : TwoDimensionalViewport
{
    public SimpleListTableViewport(
        ViewportOffset verticalOffset,
        AxisDirection verticalAxisDirection,
        ViewportOffset horizontalOffset,
        AxisDirection horizontalAxisDirection,
        TwoDimensionalChildListDelegate @delegate,
        Axis mainAxis,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null) : base(
            verticalOffset,
            verticalAxisDirection,
            horizontalOffset,
            horizontalAxisDirection,
            @delegate,
            mainAxis,
            clipBehavior: clipBehavior,
            key: key)
    {
    }

    public override RenderTwoDimensionalViewport CreateRenderObject(BuildContext context)
    {
        return new RenderSimpleListTableViewport(
            horizontalOffset: HorizontalOffset,
            horizontalAxisDirection: HorizontalAxisDirection,
            verticalOffset: VerticalOffset,
            verticalAxisDirection: VerticalAxisDirection,
            @delegate: (TwoDimensionalChildListDelegate)Delegate,
            mainAxis: MainAxis,
            childManager: ChildManagerOf(context),
            clipBehavior: ClipBehavior);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var viewport = (RenderSimpleListTableViewport)renderObject;
        viewport.HorizontalOffset = HorizontalOffset;
        viewport.HorizontalAxisDirection = HorizontalAxisDirection;
        viewport.VerticalOffset = VerticalOffset;
        viewport.VerticalAxisDirection = VerticalAxisDirection;
        viewport.MainAxis = MainAxis;
        viewport.Delegate = Delegate;
        viewport.ClipBehavior = ClipBehavior;
    }
}

/// <remarks>Flutter's <c>RenderSimpleListTableViewport</c>.</remarks>
internal sealed class RenderSimpleListTableViewport : RenderTwoDimensionalViewport
{
    public RenderSimpleListTableViewport(
        ViewportOffset horizontalOffset,
        AxisDirection horizontalAxisDirection,
        ViewportOffset verticalOffset,
        AxisDirection verticalAxisDirection,
        TwoDimensionalChildListDelegate @delegate,
        Axis mainAxis,
        ITwoDimensionalChildManager childManager,
        Clip clipBehavior = Clip.HardEdge) : base(
            horizontalOffset,
            horizontalAxisDirection,
            verticalOffset,
            verticalAxisDirection,
            @delegate,
            mainAxis,
            childManager,
            clipBehavior: clipBehavior)
    {
    }

    protected override void LayoutChildSequence()
    {
        double horizontalPixels = HorizontalOffset.Pixels;
        double verticalPixels = VerticalOffset.Pixels;
        double viewportWidth = ViewportDimension.Width;
        double viewportHeight = ViewportDimension.Height;
        var listDelegate = (TwoDimensionalChildListDelegate)Delegate;
        int rowCount = listDelegate.Children.Count;
        int columnCount = listDelegate.Children[0].Count;

        int leadingColumn = Math.Max((int)Math.Floor(horizontalPixels / TwoDimensionalHarness.CellExtent), 0);
        int leadingRow = Math.Max((int)Math.Floor(verticalPixels / TwoDimensionalHarness.CellExtent), 0);
        int trailingColumn = Math.Min(
            (int)Math.Ceiling((horizontalPixels + viewportWidth) / TwoDimensionalHarness.CellExtent),
            columnCount - 1);
        int trailingRow = Math.Min(
            (int)Math.Ceiling((verticalPixels + viewportHeight) / TwoDimensionalHarness.CellExtent),
            rowCount - 1);

        double xLayoutOffset = (leadingColumn * TwoDimensionalHarness.CellExtent) - HorizontalOffset.Pixels;
        for (int column = leadingColumn; column <= trailingColumn; column++)
        {
            double yLayoutOffset = (leadingRow * TwoDimensionalHarness.CellExtent) - VerticalOffset.Pixels;
            for (int row = leadingRow; row <= trailingRow; row++)
            {
                var vicinity = new ChildVicinity(xIndex: column, yIndex: row);
                RenderBox child = BuildOrObtainChildFor(vicinity)!;
                child.Layout(
                    Constraints.Tighten(
                        width: TwoDimensionalHarness.CellExtent,
                        height: TwoDimensionalHarness.CellExtent),
                    parentUsesSize: true);
                ParentDataOf(child).LayoutOffset = new Point(xLayoutOffset, yLayoutOffset);
                yLayoutOffset += TwoDimensionalHarness.CellExtent;
            }

            xLayoutOffset += TwoDimensionalHarness.CellExtent;
        }

        VerticalOffset.ApplyContentDimensions(
            0.0,
            Math.Max((TwoDimensionalHarness.CellExtent * rowCount) - ViewportDimension.Height, 0.0));
        HorizontalOffset.ApplyContentDimensions(
            0.0,
            Math.Max((TwoDimensionalHarness.CellExtent * columnCount) - ViewportDimension.Width, 0.0));
    }
}

/// <summary>
/// Mounts one widget under a <see cref="RenderView"/> and drives the frame pipeline by hand, the way
/// the other scroll tests do.
/// </summary>
internal sealed class TwoDimensionalRenderHarness : IDisposable
{
    private readonly BuildOwner _owner = new();
    private readonly HarnessRootElement _rootElement;
    private readonly PipelineOwner _pipeline;

    public TwoDimensionalRenderHarness(Widget rootWidget)
    {
        RenderView = new RenderView();
        _pipeline = new PipelineOwner(RenderView);
        _pipeline.Attach(RenderView);
        _rootElement = new HarnessRootElement(RenderView, rootWidget);
        _rootElement.Attach(_owner);
        _rootElement.Mount(parent: null, newSlot: null);
        _owner.FlushBuild();
    }

    public RenderView RenderView { get; }

    /// <summary>A context inside the mounted tree, for delegates that never read it.</summary>
    public BuildContext RootContext => _rootElement.OwnContext;

    public void Pump(Size size)
    {
        _owner.FlushBuild();
        _pipeline.RequestLayout();
        _pipeline.FlushLayout(size);
        _pipeline.FlushCompositingBits();
        _pipeline.FlushPaint();
    }

    public void Replace(Widget child)
    {
        _rootElement.Update(child);
        _owner.FlushBuild();
    }

    public void Dispose() => _rootElement.Unmount();

    private sealed class HarnessRootElement : Element, IRenderObjectHost
    {
        private readonly RenderView _renderView;
        private Element? _child;

        public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
        {
            _renderView = renderView;
        }

        public BuildContext OwnContext => new(this);

        public override RenderObject? RenderObject => _child?.RenderObject;

        public override Element? RenderObjectAttachingChild => _child;

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        public override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
            _renderView.Child = (RenderBox)child;
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (child is RenderBox renderBox && ReferenceEquals(_renderView.Child, renderBox))
            {
                _renderView.Child = null;
            }
        }

        public override void Unmount()
        {
            if (_child != null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
        }
    }
}
