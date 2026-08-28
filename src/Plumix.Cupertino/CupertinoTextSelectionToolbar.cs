using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/text_selection_toolbar.dart

/// <summary>Builds the toolbar surface (background, shape and shadow) around its children.</summary>
public delegate Widget CupertinoToolbarBuilder(
    BuildContext context,
    Point anchorAbove,
    Point anchorBelow,
    Widget child);

/// <summary>An iOS-style text-selection toolbar with horizontal overflow pages.</summary>
public sealed class CupertinoTextSelectionToolbar : StatelessWidget
{
    /// <summary>The size of the arrow pointing at the anchor.</summary>
    internal static readonly Size ToolbarArrowSize = new(14.0, 7.0);

    /// <summary>The radius of the toolbar's rounded corners.</summary>
    internal static readonly Radius ToolbarBorderRadius = Radius.Circular(8.0);

    /// <summary>The size of the chevron painted on the paging buttons.</summary>
    internal const double ToolbarChevronSize = 10.0;

    internal const double ToolbarChevronThickness = 2.0;

    /// <summary>The gap between the toolbar and the anchor it points at.</summary>
    internal const double ToolbarContentDistance = 8.0;

    /// <summary>The minimum horizontal gap between the arrow and the edge of the screen.</summary>
    internal const double ArrowScreenPadding = 26.0;

    /// <summary>The minimum padding from all edges of the toolbar to all edges of the screen.</summary>
    public const double ToolbarScreenPadding = 8.0;

    internal static readonly CupertinoDynamicColor ToolbarBackgroundColor =
        CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0xFFF6F6F6),
            Color.FromUInt32(0xFF222222));

    internal static readonly CupertinoDynamicColor ToolbarDividerColor =
        CupertinoDynamicColor.WithBrightness(
            Color.FromUInt32(0xFFD6D6D6),
            Color.FromUInt32(0xFF424242));

    internal static readonly CupertinoDynamicColor ToolbarTextColor =
        CupertinoDynamicColor.WithBrightness(CupertinoColors.Black, CupertinoColors.White);

    internal static readonly TimeSpan ToolbarTransitionDuration = TimeSpan.FromMilliseconds(125);

    public CupertinoTextSelectionToolbar(
        Point anchorAbove,
        Point anchorBelow,
        IReadOnlyList<Widget> children,
        CupertinoToolbarBuilder? toolbarBuilder = null,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(children);
        if (children.Count == 0)
        {
            throw new ArgumentException("Toolbar children must not be empty.", nameof(children));
        }

        AnchorAbove = anchorAbove;
        AnchorBelow = anchorBelow;
        Children = children;
        ToolbarBuilder = toolbarBuilder ?? DefaultToolbarBuilder;
    }

    /// <summary>The focal point above the selection that the toolbar attaches to.</summary>
    public Point AnchorAbove { get; }

    /// <summary>The focal point below the selection that the toolbar attaches to.</summary>
    public Point AnchorBelow { get; }

    /// <summary>The children of the toolbar, typically buttons.</summary>
    public IReadOnlyList<Widget> Children { get; }

    /// <summary>Builds the toolbar surface around <see cref="Children"/>.</summary>
    public CupertinoToolbarBuilder ToolbarBuilder { get; }

    // Builds a toolbar just like the default iOS toolbar, with the right color background and a
    // rounded cutout with an arrow.
    private static Widget DefaultToolbarBuilder(
        BuildContext context,
        Point anchorAbove,
        Point anchorBelow,
        Widget child)
    {
        return new CupertinoTextSelectionToolbarShape(
            anchorAbove: anchorAbove,
            anchorBelow: anchorBelow,
            shadowColor: CupertinoTheme.BrightnessOf(context) == PlatformBrightness.Light
                ? Color.FromArgb(0x33, 0x00, 0x00, 0x00)
                : null,
            child: new ColoredBox(ToolbarBackgroundColor.ResolveFrom(context), child: child));
    }

    public override Widget Build(BuildContext context)
    {
        Thickness mediaQueryPadding = MediaQuery.PaddingOf(context);
        double paddingAbove = mediaQueryPadding.Top + ToolbarScreenPadding;
        double leftMargin = ArrowScreenPadding + mediaQueryPadding.Left;
        double rightMargin = MediaQuery.WidthOf(context) - mediaQueryPadding.Right - ArrowScreenPadding;

        var anchorAboveAdjusted = new Point(
            Math.Clamp(AnchorAbove.X, leftMargin, Math.Max(leftMargin, rightMargin)),
            AnchorAbove.Y - ToolbarContentDistance - paddingAbove);
        var anchorBelowAdjusted = new Point(
            Math.Clamp(AnchorBelow.X, leftMargin, Math.Max(leftMargin, rightMargin)),
            AnchorBelow.Y + ToolbarContentDistance - paddingAbove);

        return new Padding(
            new Thickness(
                ToolbarScreenPadding,
                paddingAbove,
                ToolbarScreenPadding,
                ToolbarScreenPadding),
            new CustomSingleChildLayout(
                new TextSelectionToolbarLayoutDelegate(anchorAboveAdjusted, anchorBelowAdjusted),
                new CupertinoTextSelectionToolbarContent(
                    anchorAbove: anchorAboveAdjusted,
                    anchorBelow: anchorBelowAdjusted,
                    toolbarBuilder: ToolbarBuilder,
                    children: Children)));
    }
}

/// <summary>Dart's `_CupertinoTextSelectionToolbarContent`: paging, chevrons and the fade.</summary>
internal sealed class CupertinoTextSelectionToolbarContent : StatefulWidget
{
    public CupertinoTextSelectionToolbarContent(
        Point anchorAbove,
        Point anchorBelow,
        IReadOnlyList<Widget> children,
        CupertinoToolbarBuilder toolbarBuilder,
        Key? key = null) : base(key)
    {
        AnchorAbove = anchorAbove;
        AnchorBelow = anchorBelow;
        Children = children;
        ToolbarBuilder = toolbarBuilder;
    }

    public Point AnchorAbove { get; }

    public Point AnchorBelow { get; }

    public IReadOnlyList<Widget> Children { get; }

    public CupertinoToolbarBuilder ToolbarBuilder { get; }

    public override State CreateState() => new CupertinoTextSelectionToolbarContentState();

    internal sealed class CupertinoTextSelectionToolbarContentState : State
    {
        private readonly GlobalKey _toolbarItemsKey = new LabeledGlobalKey<State>("CupertinoToolbarItems");
        private AnimationController _controller = null!;
        private int? _nextPage;
        private int _page;

        private CupertinoTextSelectionToolbarContent Current =>
            (CupertinoTextSelectionToolbarContent)StateWidget;

        public override void InitState()
        {
            base.InitState();
            _controller = new AnimationController(
                value: 1.0,
                vsync: this,
                duration: CupertinoTextSelectionToolbar.ToolbarTransitionDuration);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);

            // If the children are changing, the current page may have been invalidated.
            var oldContent = (CupertinoTextSelectionToolbarContent)oldWidget;
            if (!ReferenceEquals(oldContent.Children, Current.Children))
            {
                _page = 0;
                _nextPage = null;
                _controller.Forward();
                _controller.RemoveStatusListener(StatusListener);
            }
        }

        public override void Dispose()
        {
            _controller.Dispose();
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            Color chevronColor = CupertinoTextSelectionToolbar.ToolbarTextColor.ResolveFrom(context);

            // Empty toolbars should not be handled.
            Widget backButton = new Center(
                widthFactor: 1.0,
                heightFactor: 1.0,
                child: new CupertinoTextSelectionToolbarButton(
                    onPressed: HandlePreviousPage,
                    child: new IgnorePointer(
                        child: new CustomPaint(
                            painter: new LeftCupertinoChevronPainter(chevronColor),
                            size: new Size(
                                CupertinoTextSelectionToolbar.ToolbarChevronSize,
                                CupertinoTextSelectionToolbar.ToolbarChevronSize)))));

            Widget nextButton = new Center(
                widthFactor: 1.0,
                heightFactor: 1.0,
                child: new CupertinoTextSelectionToolbarButton(
                    onPressed: HandleNextPage,
                    child: new IgnorePointer(
                        child: new CustomPaint(
                            painter: new RightCupertinoChevronPainter(chevronColor),
                            size: new Size(
                                CupertinoTextSelectionToolbar.ToolbarChevronSize,
                                CupertinoTextSelectionToolbar.ToolbarChevronSize)))));

            return Current.ToolbarBuilder(
                context,
                Current.AnchorAbove,
                Current.AnchorBelow,
                new FadeTransition(
                    opacity: _controller,
                    child: new AnimatedSize(
                        duration: CupertinoTextSelectionToolbar.ToolbarTransitionDuration,
                        curve: Curves.Decelerate,
                        child: new GestureDetector(
                            onHorizontalDragEnd: HandleHorizontalDragEnd,
                            child: new CupertinoTextSelectionToolbarItems(
                                page: _page,
                                backButton: backButton,
                                dividerColor: CupertinoTextSelectionToolbar.ToolbarDividerColor
                                    .ResolveFrom(context),
                                dividerWidth: 1.0 / MediaQuery.DevicePixelRatioOf(context),
                                nextButton: nextButton,
                                children: Current.Children
                                    .Select(child => (Widget)new Center(
                                        widthFactor: 1.0,
                                        heightFactor: 1.0,
                                        child: child))
                                    .ToList(),
                                key: _toolbarItemsKey)))));
        }

        private void HandleHorizontalDragEnd(DragEndDetails details)
        {
            double velocity = details.PrimaryVelocity ?? 0.0;
            if (velocity == 0.0)
            {
                return;
            }

            if (velocity > 0.0)
            {
                HandlePreviousPage();
            }
            else
            {
                HandleNextPage();
            }
        }

        private void HandleNextPage()
        {
            RenderObject? renderToolbar = _toolbarItemsKey.CurrentContext?.FindRenderObject();
            if (renderToolbar is RenderCupertinoTextSelectionToolbarItems { HasNextPage: true })
            {
                _controller.Reverse();
                _controller.AddStatusListener(StatusListener);
                _nextPage = _page + 1;
            }
        }

        private void HandlePreviousPage()
        {
            RenderObject? renderToolbar = _toolbarItemsKey.CurrentContext?.FindRenderObject();
            if (renderToolbar is RenderCupertinoTextSelectionToolbarItems { HasPreviousPage: true })
            {
                _controller.Reverse();
                _controller.AddStatusListener(StatusListener);
                _nextPage = _page - 1;
            }
        }

        private void StatusListener(AnimationStatus status)
        {
            if (status != AnimationStatus.Dismissed)
            {
                return;
            }

            SetState(() =>
            {
                _page = _nextPage!.Value;
                _nextPage = null;
            });
            _controller.Forward();
            _controller.RemoveStatusListener(StatusListener);
        }
    }
}

/// <summary>Dart's `_CupertinoTextSelectionToolbarShape`: clips the toolbar into an arrowed card.</summary>
internal sealed class CupertinoTextSelectionToolbarShape : SingleChildRenderObjectWidget
{
    public CupertinoTextSelectionToolbarShape(
        Point anchorAbove,
        Point anchorBelow,
        Color? shadowColor,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        AnchorAbove = anchorAbove;
        AnchorBelow = anchorBelow;
        ShadowColor = shadowColor;
    }

    public Point AnchorAbove { get; }

    public Point AnchorBelow { get; }

    public Color? ShadowColor { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderCupertinoTextSelectionToolbarShape(AnchorAbove, AnchorBelow, ShadowColor);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var shape = (RenderCupertinoTextSelectionToolbarShape)renderObject;
        shape.AnchorAbove = AnchorAbove;
        shape.AnchorBelow = AnchorBelow;
        shape.ShadowColor = ShadowColor;
    }
}

/// <summary>
/// Dart's `_RenderCupertinoTextSelectionToolbarShape` (a `RenderShiftedBox` in Flutter; Plumix folds
/// that role into <see cref="RenderProxyBox"/>).
/// </summary>
internal sealed class RenderCupertinoTextSelectionToolbarShape : RenderProxyBox
{
    private Point _anchorAbove;
    private Point _anchorBelow;
    private Color? _shadowColor;

    public RenderCupertinoTextSelectionToolbarShape(
        Point anchorAbove,
        Point anchorBelow,
        Color? shadowColor)
    {
        _anchorAbove = anchorAbove;
        _anchorBelow = anchorBelow;
        _shadowColor = shadowColor;
    }

    public override bool IsRepaintBoundary => true;

    public Point AnchorAbove
    {
        get => _anchorAbove;
        set
        {
            if (value == _anchorAbove)
            {
                return;
            }

            _anchorAbove = value;
            MarkNeedsLayout();
        }
    }

    public Point AnchorBelow
    {
        get => _anchorBelow;
        set
        {
            if (value == _anchorBelow)
            {
                return;
            }

            _anchorBelow = value;
            MarkNeedsLayout();
        }
    }

    public Color? ShadowColor
    {
        get => _shadowColor;
        set
        {
            if (value == _shadowColor)
            {
                return;
            }

            _shadowColor = value;
            MarkNeedsPaint();
        }
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        if (Child is null)
        {
            return constraints.Smallest;
        }

        Size childSize = Child.GetDryLayout(ConstraintsForChild(constraints));
        return new Size(
            childSize.Width,
            childSize.Height - CupertinoTextSelectionToolbar.ToolbarArrowSize.Height);
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        if (Child is null)
        {
            return null;
        }

        BoxConstraints enforcedConstraint = ConstraintsForChild(constraints);
        double? result = Child.GetDryBaseline(enforcedConstraint, baseline);
        return result is null
            ? null
            : result + ComputeChildOffset(Child.GetDryLayout(enforcedConstraint)).Y;
    }

    protected override void PerformLayout()
    {
        if (Child is null)
        {
            Size = Constraints.Smallest;
            return;
        }

        Child.Layout(ConstraintsForChild(Constraints), parentUsesSize: true);

        // The buttons are padded on both sides by _kToolbarArrowSize.height, and the toolbar's
        // height is the child's height minus one arrow height.
        var childParentData = (BoxParentData)Child.parentData!;
        childParentData.offset = ComputeChildOffset(Child.Size);
        Size = new Size(
            Child.Size.Width,
            Child.Size.Height - CupertinoTextSelectionToolbar.ToolbarArrowSize.Height);
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child is null)
        {
            return;
        }

        var childParentData = (BoxParentData)Child.parentData!;
        Plumix.UI.RRect rrect = ShapeRRect(Child);
        Plumix.UI.Path clipPath = ClipPath(Child, rrect);

        if (_shadowColor is { } shadowColor)
        {
            var boxShadow = new Plumix.Rendering.BoxShadow(color: shadowColor, blurRadius: 15.0);
            Point shadowOrigin = offset + (Vector)childParentData.offset;
            context.DrawRectangle(
                new SolidColorBrush(Colors.Transparent),
                null,
                new Rect(
                    shadowOrigin.X + rrect.Left,
                    shadowOrigin.Y + rrect.Top,
                    rrect.Width,
                    rrect.Height + CupertinoTextSelectionToolbar.ToolbarArrowSize.Height),
                BorderRadius.All(CupertinoTextSelectionToolbar.ToolbarBorderRadius),
                new BoxShadows(boxShadow.ToAvalonia()));
        }

        Point childOffset = offset + (Vector)childParentData.offset;
        context.PushClipPath(
            clipPath,
            childContext => childContext.PaintChild(Child, childOffset),
            geometryOffset: childOffset);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (Child is null)
        {
            return false;
        }

        // Positions outside of the clipped area of the child are not counted as hits.
        var childParentData = (BoxParentData)Child.parentData!;
        var hitBox = new Rect(
            childParentData.offset.X,
            childParentData.offset.Y + CupertinoTextSelectionToolbar.ToolbarArrowSize.Height,
            Child.Size.Width,
            Child.Size.Height - (CupertinoTextSelectionToolbar.ToolbarArrowSize.Height * 2.0));
        if (!hitBox.Contains(position))
        {
            return false;
        }

        return base.HitTestChildren(result, position);
    }

    /// <summary>Dart's `_shapeRRect`: the rounded body of the toolbar, in child coordinates.</summary>
    internal Plumix.UI.RRect ShapeRRect(RenderBox child)
    {
        var rect = new Rect(
            0.0,
            CupertinoTextSelectionToolbar.ToolbarArrowSize.Height,
            child.Size.Width,
            child.Size.Height - (CupertinoTextSelectionToolbar.ToolbarArrowSize.Height * 2.0));
        return Plumix.UI.RRect
            .FromRectAndRadius(rect, CupertinoTextSelectionToolbar.ToolbarBorderRadius)
            .ScaleRadii();
    }

    /// <summary>Dart's `_clipPath`: the rounded body plus the arrow pointing at the anchor.</summary>
    internal Plumix.UI.Path ClipPath(RenderBox child, Plumix.UI.RRect rrect)
    {
        var path = new Plumix.UI.Path();

        // If there isn't enough width for the arrow, don't draw it.
        double arrowWidth = CupertinoTextSelectionToolbar.ToolbarArrowSize.Width;
        if ((CupertinoTextSelectionToolbar.ToolbarBorderRadius.X * 2.0) + arrowWidth > Size.Width)
        {
            path.AddRRect(rrect);
            return path;
        }

        bool isAbove = IsAbove(child.Size.Height);
        Point localAnchor = GlobalToLocal(isAbove ? _anchorAbove : _anchorBelow);
        double lowerBound = CupertinoTextSelectionToolbar.ToolbarBorderRadius.X + (arrowWidth / 2.0);
        double upperBound = Size.Width - (arrowWidth / 2.0)
            - CupertinoTextSelectionToolbar.ToolbarBorderRadius.X;
        double arrowTipX = Math.Clamp(localAnchor.X, lowerBound, Math.Max(lowerBound, upperBound));

        if (isAbove)
        {
            double arrowBaseY = child.Size.Height - CupertinoTextSelectionToolbar.ToolbarArrowSize.Height;
            double arrowTipY = child.Size.Height;
            path.MoveTo(arrowTipX + (arrowWidth / 2.0), arrowBaseY);
            path.LineTo(arrowTipX, arrowTipY);
            path.LineTo(arrowTipX - (arrowWidth / 2.0), arrowBaseY);
        }
        else
        {
            double arrowBaseY = CupertinoTextSelectionToolbar.ToolbarArrowSize.Height;
            const double arrowTipY = 0.0;
            path.MoveTo(arrowTipX - (arrowWidth / 2.0), arrowBaseY);
            path.LineTo(arrowTipX, arrowTipY);
            path.LineTo(arrowTipX + (arrowWidth / 2.0), arrowBaseY);
        }

        AddRRectToPath(path, rrect, startAngle: isAbove ? Math.PI / 2.0 : -Math.PI / 2.0);
        path.Close();
        return path;
    }

    /// <summary>Dart's `_addRRectToPath`: appends the rounded rect starting at the given quadrant.</summary>
    private static Plumix.UI.Path AddRRectToPath(
        Plumix.UI.Path path,
        Plumix.UI.RRect rrect,
        double startAngle)
    {
        const double halfPi = Math.PI / 2.0;
        Rect outerRect = rrect.Rect;
        (Point Vertex, Radius CenterOffset)[] corners =
        [
            (outerRect.BottomRight, new Radius(-rrect.BottomRight.X, -rrect.BottomRight.Y)),
            (outerRect.BottomLeft, new Radius(rrect.BottomLeft.X, -rrect.BottomLeft.Y)),
            (outerRect.TopLeft, rrect.TopLeft),
            (outerRect.TopRight, new Radius(-rrect.TopRight.X, rrect.TopRight.Y)),
        ];

        int startQuadrantIndex = (int)(startAngle / halfPi);
        for (int index = startQuadrantIndex; index < 4 + startQuadrantIndex; index++)
        {
            (Point vertex, Radius centerOffset) = corners[((index % 4) + 4) % 4];
            var otherVertex = new Point(
                vertex.X + (2.0 * centerOffset.X),
                vertex.Y + (2.0 * centerOffset.Y));
            Rect cornerRect = RectFromPoints(vertex, otherVertex);
            path.ArcTo(cornerRect, halfPi * index, halfPi, forceMoveTo: false);
        }

        return path;
    }

    private static Rect RectFromPoints(Point a, Point b)
    {
        return new Rect(
            Math.Min(a.X, b.X),
            Math.Min(a.Y, b.Y),
            Math.Abs(a.X - b.X),
            Math.Abs(a.Y - b.Y));
    }

    private static BoxConstraints ConstraintsForChild(BoxConstraints constraints)
    {
        return new BoxConstraints(
            MinWidth: CupertinoTextSelectionToolbar.ToolbarArrowSize.Width
                + (CupertinoTextSelectionToolbar.ToolbarBorderRadius.X * 2.0))
            .Enforce(constraints.Loosen());
    }

    private Point ComputeChildOffset(Size childSize)
    {
        return new Point(
            0.0,
            IsAbove(childSize.Height) ? -CupertinoTextSelectionToolbar.ToolbarArrowSize.Height : 0.0);
    }

    private bool IsAbove(double childHeight)
    {
        return _anchorAbove.Y >= childHeight - CupertinoTextSelectionToolbar.ToolbarArrowSize.Height;
    }
}

/// <summary>Dart's `_CupertinoTextSelectionToolbarItemsSlot`.</summary>
internal enum CupertinoTextSelectionToolbarItemsSlot
{
    BackButton,
    NextButton,
}

/// <summary>Dart's `_CupertinoTextSelectionToolbarItems`: one page of items plus paging buttons.</summary>
internal sealed class CupertinoTextSelectionToolbarItems : RenderObjectWidget
{
    public CupertinoTextSelectionToolbarItems(
        int page,
        IReadOnlyList<Widget> children,
        Widget backButton,
        Color dividerColor,
        double dividerWidth,
        Widget nextButton,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(children);
        if (children.Count == 0)
        {
            throw new ArgumentException("Toolbar children must not be empty.", nameof(children));
        }

        Page = page;
        Children = children;
        BackButton = backButton;
        DividerColor = dividerColor;
        DividerWidth = dividerWidth;
        NextButton = nextButton;
    }

    public Widget BackButton { get; }

    public IReadOnlyList<Widget> Children { get; }

    public Color DividerColor { get; }

    public double DividerWidth { get; }

    public Widget NextButton { get; }

    public int Page { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderCupertinoTextSelectionToolbarItems(DividerColor, DividerWidth, Page);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var items = (RenderCupertinoTextSelectionToolbarItems)renderObject;
        items.Page = Page;
        items.DividerColor = DividerColor;
        items.DividerWidth = DividerWidth;
    }

    internal override Element CreateElement() => new CupertinoTextSelectionToolbarItemsElement(this);
}

/// <summary>Dart's `_CupertinoTextSelectionToolbarItemsElement`: two slots plus an indexed list.</summary>
internal sealed class CupertinoTextSelectionToolbarItemsElement : RenderObjectElement
{
    private readonly Dictionary<CupertinoTextSelectionToolbarItemsSlot, Element> _slotToChild = [];
    private readonly HashSet<Element> _forgottenChildren = [];
    private List<Element> _children = [];

    public CupertinoTextSelectionToolbarItemsElement(CupertinoTextSelectionToolbarItems widget)
        : base(widget)
    {
    }

    private CupertinoTextSelectionToolbarItems ToolbarItems =>
        (CupertinoTextSelectionToolbarItems)Widget;

    private RenderCupertinoTextSelectionToolbarItems Items =>
        (RenderCupertinoTextSelectionToolbarItems)RequireRenderObject();

    protected override void OnMount()
    {
        base.OnMount();
        MountChild(ToolbarItems.BackButton, CupertinoTextSelectionToolbarItemsSlot.BackButton);
        MountChild(ToolbarItems.NextButton, CupertinoTextSelectionToolbarItemsSlot.NextButton);

        _children = new List<Element>(ToolbarItems.Children.Count);
        Element? previousChild = null;
        for (int index = 0; index < ToolbarItems.Children.Count; index++)
        {
            Element newChild = InflateWidget(
                ToolbarItems.Children[index],
                new IndexedSlot<Element?>(index, previousChild));
            _children.Add(newChild);
            previousChild = newChild;
        }
    }

    internal override void Rebuild()
    {
        base.Rebuild();
        UpdateChildrenAndSlots();
    }

    internal override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        UpdateChildrenAndSlots();
    }

    internal override void ForgetChild(Element child)
    {
        if (child.Slot is CupertinoTextSelectionToolbarItemsSlot slot && _slotToChild.ContainsKey(slot))
        {
            _slotToChild.Remove(slot);
        }
        else
        {
            _forgottenChildren.Add(child);
        }
    }

    internal override void VisitChildren(Action<Element> visitor)
    {
        foreach (Element child in _slotToChild.Values)
        {
            visitor(child);
        }

        foreach (Element child in _children)
        {
            if (!_forgottenChildren.Contains(child))
            {
                visitor(child);
            }
        }
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        switch (slot)
        {
            case CupertinoTextSelectionToolbarItemsSlot toolbarSlot:
                UpdateSlottedRenderObject((RenderBox)child, toolbarSlot);
                return;
            case IndexedSlot<Element?> indexedSlot:
                Items.Insert((RenderBox)child, (RenderBox?)indexedSlot.Value?.RenderObject);
                return;
            default:
                throw new InvalidOperationException(
                    "slot must be CupertinoTextSelectionToolbarItemsSlot or IndexedSlot.");
        }
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        if (newSlot is not IndexedSlot<Element?> indexedSlot)
        {
            throw new InvalidOperationException("Only list children of the toolbar items can move.");
        }

        Items.Move((RenderBox)child, (RenderBox?)indexedSlot.Value?.RenderObject);
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        if (slot is CupertinoTextSelectionToolbarItemsSlot toolbarSlot)
        {
            UpdateSlottedRenderObject(null, toolbarSlot);
            return;
        }

        Items.Remove((RenderBox)child);
    }

    internal override void Unmount()
    {
        foreach (Element child in _slotToChild.Values.ToList())
        {
            UnmountChild(child);
        }

        foreach (Element child in _children)
        {
            if (!_forgottenChildren.Contains(child))
            {
                UnmountChild(child);
            }
        }

        _slotToChild.Clear();
        _children.Clear();
        _forgottenChildren.Clear();
        base.Unmount();
    }

    private void UpdateChildrenAndSlots()
    {
        MountChild(ToolbarItems.BackButton, CupertinoTextSelectionToolbarItemsSlot.BackButton);
        MountChild(ToolbarItems.NextButton, CupertinoTextSelectionToolbarItemsSlot.NextButton);
        _children = UpdateChildren(_children, ToolbarItems.Children, _forgottenChildren);
        _forgottenChildren.Clear();
    }

    private void MountChild(Widget widget, CupertinoTextSelectionToolbarItemsSlot slot)
    {
        _slotToChild.TryGetValue(slot, out Element? oldChild);
        Element? newChild = UpdateChild(oldChild, widget, slot);
        if (oldChild is not null)
        {
            _slotToChild.Remove(slot);
        }

        if (newChild is not null)
        {
            _slotToChild[slot] = newChild;
        }
    }

    private void UpdateSlottedRenderObject(RenderBox? child, CupertinoTextSelectionToolbarItemsSlot slot)
    {
        switch (slot)
        {
            case CupertinoTextSelectionToolbarItemsSlot.BackButton:
                Items.BackButton = child;
                break;
            case CupertinoTextSelectionToolbarItemsSlot.NextButton:
                Items.NextButton = child;
                break;
        }
    }
}

/// <summary>Dart's `_RenderCupertinoTextSelectionToolbarItems`: the greedy per-page layout.</summary>
internal sealed class RenderCupertinoTextSelectionToolbarItems : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, ToolbarItemsParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, ToolbarItemsParentData> _container;
    private readonly Dictionary<CupertinoTextSelectionToolbarItemsSlot, RenderBox> _slottedChildren = [];
    private RenderBox? _backButton;
    private RenderBox? _nextButton;
    private Color _dividerColor;
    private double _dividerWidth;
    private int _page;

    public RenderCupertinoTextSelectionToolbarItems(Color dividerColor, double dividerWidth, int page)
    {
        _dividerColor = dividerColor;
        _dividerWidth = dividerWidth;
        _page = page;
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, ToolbarItemsParentData>(this);
    }

    /// <summary>Whether a page after the current one exists.</summary>
    public bool HasNextPage { get; private set; }

    /// <summary>Whether a page before the current one exists.</summary>
    public bool HasPreviousPage { get; private set; }

    internal IReadOnlyDictionary<CupertinoTextSelectionToolbarItemsSlot, RenderBox> SlottedChildren =>
        _slottedChildren;

    public RenderBox? BackButton
    {
        get => _backButton;
        set => _backButton = UpdateChild(
            _backButton,
            value,
            CupertinoTextSelectionToolbarItemsSlot.BackButton);
    }

    public RenderBox? NextButton
    {
        get => _nextButton;
        set => _nextButton = UpdateChild(
            _nextButton,
            value,
            CupertinoTextSelectionToolbarItemsSlot.NextButton);
    }

    public int Page
    {
        get => _page;
        set
        {
            if (value == _page)
            {
                return;
            }

            _page = value;
            MarkNeedsLayout();
        }
    }

    public Color DividerColor
    {
        get => _dividerColor;
        set
        {
            if (value == _dividerColor)
            {
                return;
            }

            _dividerColor = value;
            MarkNeedsLayout();
        }
    }

    public double DividerWidth
    {
        get => _dividerWidth;
        set
        {
            if (value.Equals(_dividerWidth))
            {
                return;
            }

            _dividerWidth = value;
            MarkNeedsLayout();
        }
    }

    public RenderBox? FirstChild => _container.FirstChild;

    public RenderBox? LastChild => _container.LastChild;

    public int ChildCount => _container.ChildCount;

    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);

    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);

    public void AddAll(List<RenderBox> children) => _container.AddAll(children);

    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);

    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);

    public void Remove(RenderBox child) => _container.Remove(child);

    public void DefaultPaint(PaintingContext context, Point offset) => _container.DefaultPaint(context, offset);

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position) =>
        _container.DefaultHitTestChildren(result, position);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not ToolbarItemsParentData)
        {
            child.parentData = new ToolbarItemsParentData();
        }
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        foreach (RenderBox child in _slottedChildren.Values)
        {
            child.Attach(Owner!);
        }
    }

    protected override void OnDetach()
    {
        base.OnDetach();
        foreach (RenderBox child in _slottedChildren.Values)
        {
            child.Detach();
        }
    }

    protected override void RedepthChildren()
    {
        VisitChildren(child => RedepthChild(child));
    }

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        if (_backButton is not null)
        {
            visitor(_backButton);
        }

        if (_nextButton is not null)
        {
            visitor(_nextButton);
        }

        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            visitor(child);
        }
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject> visitor)
    {
        VisitChildren(child =>
        {
            if (((ToolbarItemsParentData)child.parentData!).ShouldPaint)
            {
                visitor(child);
            }
        });
    }

    protected override void PerformLayout()
    {
        if (FirstChild is null)
        {
            Size = Constraints.Smallest;
            return;
        }

        // Layout slotted children.
        double greatestHeight = 0.0;
        VisitChildren(renderObjectChild =>
        {
            var child = (RenderBox)renderObjectChild;
            double childHeight = child.GetMaxIntrinsicHeight(Constraints.MaxWidth);
            if (childHeight > greatestHeight)
            {
                greatestHeight = childHeight;
            }
        });

        var slottedConstraints = new BoxConstraints(
            MaxWidth: Constraints.MaxWidth,
            MinHeight: greatestHeight,
            MaxHeight: greatestHeight);
        _backButton!.Layout(slottedConstraints, parentUsesSize: true);
        _nextButton!.Layout(slottedConstraints, parentUsesSize: true);

        double subsequentPageButtonsWidth = _backButton.Size.Width + _nextButton.Size.Width;
        double currentButtonPosition = 0.0;
        double toolbarWidth = 0.0;
        int currentPage = 0;
        int index = -1;

        VisitChildren(renderObjectChild =>
        {
            index++;
            var child = (RenderBox)renderObjectChild;
            var childParentData = (ToolbarItemsParentData)child.parentData!;
            childParentData.ShouldPaint = false;

            // Skip slotted children and children on pages after the visible one.
            if (ReferenceEquals(child, _backButton)
                || ReferenceEquals(child, _nextButton)
                || currentPage > _page)
            {
                return;
            }

            double paginationButtonsWidth = currentPage == 0
                ? index == ChildCount + 1 ? 0.0 : _nextButton.Size.Width
                : subsequentPageButtonsWidth;

            // The width of the menu is set by the first page.
            child.Layout(
                new BoxConstraints(
                    MaxWidth: Constraints.MaxWidth - paginationButtonsWidth,
                    MinHeight: greatestHeight,
                    MaxHeight: greatestHeight),
                parentUsesSize: true);

            // If this child causes the current page to overflow, move to the next page and relayout
            // the child.
            double currentWidth = currentButtonPosition + paginationButtonsWidth + child.Size.Width;
            if (currentWidth > Constraints.MaxWidth)
            {
                currentPage++;
                currentButtonPosition = _backButton.Size.Width + DividerWidth;
                paginationButtonsWidth = _backButton.Size.Width + _nextButton.Size.Width;
                child.Layout(
                    new BoxConstraints(
                        MaxWidth: Constraints.MaxWidth - paginationButtonsWidth,
                        MinHeight: greatestHeight,
                        MaxHeight: greatestHeight),
                    parentUsesSize: true);
            }

            childParentData.offset = new Point(currentButtonPosition, 0.0);
            currentButtonPosition += child.Size.Width + DividerWidth;
            childParentData.ShouldPaint = currentPage == Page;

            if (currentPage == Page)
            {
                toolbarWidth = currentButtonPosition;
            }
        });

        // It shouldn't be possible to navigate beyond the last page.
        if (Page > currentPage)
        {
            throw new InvalidOperationException("The toolbar page is beyond the last page.");
        }

        // Position page nav buttons.
        if (currentPage > 0)
        {
            var nextButtonParentData = (ToolbarItemsParentData)_nextButton.parentData!;
            var backButtonParentData = (ToolbarItemsParentData)_backButton.parentData!;

            // The forward button only shows when there's a page after this one.
            if (Page != currentPage)
            {
                nextButtonParentData.offset = new Point(toolbarWidth, 0.0);
                nextButtonParentData.ShouldPaint = true;
                toolbarWidth += _nextButton.Size.Width;
            }

            if (Page > 0)
            {
                backButtonParentData.offset = new Point(0.0, 0.0);
                backButtonParentData.ShouldPaint = true;

                // No need to add the width of the back button to toolbarWidth here. It's already
                // been taken care of when laying out the children to assume the back button is
                // showing.
            }
        }
        else
        {
            // No divider for the next button when there's only one page.
            toolbarWidth -= DividerWidth;
        }

        HasNextPage = Page != currentPage;
        HasPreviousPage = Page > 0;
        Size = Constraints.Constrain(new Size(toolbarWidth, greatestHeight));
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        VisitChildren(renderObjectChild =>
        {
            var child = (RenderBox)renderObjectChild;
            var childParentData = (ToolbarItemsParentData)child.parentData!;
            if (!childParentData.ShouldPaint)
            {
                return;
            }

            Point childOffset = offset + (Vector)childParentData.offset;
            context.PaintChild(child, childOffset);

            if (childParentData.nextSibling is null && !ReferenceEquals(child, _backButton))
            {
                return;
            }

            // Dart paints a zero-width (device-pixel hairline) line; Avalonia has no hairline pen, so
            // this is one logical pixel wide.
            context.DrawLine(
                new Pen(new SolidColorBrush(DividerColor)),
                new Point(childOffset.X + child.Size.Width, childOffset.Y),
                new Point(childOffset.X + child.Size.Width, childOffset.Y + child.Size.Height));
        });
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        // Hit test list children.
        RenderBox? child = LastChild;
        while (child is not null)
        {
            var childParentData = (ToolbarItemsParentData)child.parentData!;
            if (!childParentData.ShouldPaint)
            {
                child = childParentData.previousSibling;
                continue;
            }

            if (HitTestChild(child, result, position))
            {
                return true;
            }

            child = childParentData.previousSibling;
        }

        // Hit test slotted children.
        return HitTestChild(_backButton, result, position) || HitTestChild(_nextButton, result, position);
    }

    private static bool HitTestChild(RenderBox? child, BoxHitTestResult result, Point position)
    {
        if (child is null)
        {
            return false;
        }

        var childParentData = (ToolbarItemsParentData)child.parentData!;
        if (!childParentData.ShouldPaint)
        {
            return false;
        }

        return child.HitTest(result, position - (Vector)childParentData.offset);
    }

    private RenderBox? UpdateChild(
        RenderBox? oldChild,
        RenderBox? newChild,
        CupertinoTextSelectionToolbarItemsSlot slot)
    {
        if (oldChild is not null)
        {
            DropChild(oldChild);
            _slottedChildren.Remove(slot);
        }

        if (newChild is not null)
        {
            _slottedChildren[slot] = newChild;
            AdoptChild(newChild);
        }

        return newChild;
    }

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) =>
        Insert((RenderBox)child, after as RenderBox);

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) =>
        Move((RenderBox)child, after as RenderBox);

    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);
}

/// <summary>Dart's `_LeftCupertinoChevronPainter`.</summary>
internal sealed class LeftCupertinoChevronPainter : CupertinoChevronPainter
{
    public LeftCupertinoChevronPainter(Color color) : base(color, isLeft: true)
    {
    }
}

/// <summary>Dart's `_RightCupertinoChevronPainter`.</summary>
internal sealed class RightCupertinoChevronPainter : CupertinoChevronPainter
{
    public RightCupertinoChevronPainter(Color color) : base(color, isLeft: false)
    {
    }
}

/// <summary>Dart's `_CupertinoChevronPainter`: the paging chevron glyph.</summary>
internal abstract class CupertinoChevronPainter : CustomPainter
{
    protected CupertinoChevronPainter(Color color, bool isLeft)
    {
        Color = color;
        IsLeft = isLeft;
    }

    public Color Color { get; }

    /// <summary>Whether the chevron points to the left.</summary>
    public bool IsLeft { get; }

    /// <summary>The three points of the chevron glyph, for a square of the given size.</summary>
    internal (Point First, Point Middle, Point Lower) ChevronPoints(Size size)
    {
        if (size.Height != size.Width)
        {
            throw new ArgumentException($"size must have the same height and width: {size}", nameof(size));
        }

        double iconSize = size.Height;

        // The chevron is half of a square rotated 45º, so it needs to be shifted so that it does not
        // appear off center.
        var centerOffset = new Vector(iconSize / 4.0 * (IsLeft ? 1.0 : -1.0), 0.0);

        return (
            new Point(iconSize / 2.0, 0.0) + centerOffset,
            new Point(IsLeft ? 0.0 : iconSize, iconSize / 2.0) + centerOffset,
            new Point(iconSize / 2.0, iconSize) + centerOffset);
    }

    public override void Paint(PaintingContext context, Size size)
    {
        (Point firstPoint, Point middlePoint, Point lowerPoint) = ChevronPoints(size);

        var pen = new Pen(
            new SolidColorBrush(Color),
            CupertinoTextSelectionToolbar.ToolbarChevronThickness,
            lineCap: PenLineCap.Round,
            lineJoin: PenLineJoin.Round);

        // `drawPath` is used here because it renders a smoother chevron than `drawLine`.
        context.DrawLine(pen, firstPoint, middlePoint);
        context.DrawLine(pen, middlePoint, lowerPoint);
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        return oldDelegate is not CupertinoChevronPainter oldPainter
               || oldPainter.Color != Color
               || oldPainter.IsLeft != IsLeft;
    }
}
