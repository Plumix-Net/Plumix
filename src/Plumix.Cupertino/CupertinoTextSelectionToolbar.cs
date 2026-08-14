using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/text_selection_toolbar.dart

public delegate Widget CupertinoToolbarBuilder(
    BuildContext context,
    Point anchorAbove,
    Point anchorBelow,
    Widget child);

/// <summary>An iOS-style text-selection toolbar with horizontal overflow pages.</summary>
public sealed class CupertinoTextSelectionToolbar : StatelessWidget
{
    public const double ToolbarScreenPadding = 8.0;

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

    public Point AnchorAbove { get; }

    public Point AnchorBelow { get; }

    public IReadOnlyList<Widget> Children { get; }

    public CupertinoToolbarBuilder ToolbarBuilder { get; }

    public override Widget Build(BuildContext context)
    {
        MediaQueryData mediaQuery = MediaQuery.Of(context);
        double paddingAbove = mediaQuery.Padding.Top + ToolbarScreenPadding;
        double leftLimit = 26.0 + mediaQuery.Padding.Left;
        double rightLimit = mediaQuery.Size.Width - mediaQuery.Padding.Right - 26.0;
        Point anchorAbove = new(
            Math.Clamp(AnchorAbove.X, leftLimit, Math.Max(leftLimit, rightLimit)),
            AnchorAbove.Y - ToolbarScreenPadding - paddingAbove);
        Point anchorBelow = new(
            Math.Clamp(AnchorBelow.X, leftLimit, Math.Max(leftLimit, rightLimit)),
            AnchorBelow.Y + ToolbarScreenPadding - paddingAbove);
        Widget child = new CupertinoTextSelectionToolbarContent(
            anchorAbove,
            anchorBelow,
            Children,
            ToolbarBuilder);

        return new Padding(
            new Thickness(
                ToolbarScreenPadding,
                paddingAbove,
                ToolbarScreenPadding,
                ToolbarScreenPadding),
            new CustomSingleChildLayout(
                new TextSelectionToolbarLayoutDelegate(anchorAbove, anchorBelow),
                child));
    }

    private static Widget DefaultToolbarBuilder(
        BuildContext context,
        Point anchorAbove,
        Point anchorBelow,
        Widget child)
    {
        bool dark = CupertinoTheme.BrightnessOf(context) == PlatformBrightness.Dark;
        Color background = dark ? Color.Parse("#FF222222") : Color.Parse("#FFF6F6F6");
        Color? shadowColor = dark ? null : Color.FromArgb(51, 0, 0, 0);
        return new CupertinoTextSelectionToolbarShape(
            anchorAbove: anchorAbove,
            anchorBelow: anchorBelow,
            backgroundColor: background,
            shadowColor: shadowColor,
            child: child);
    }
}

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

    private sealed class CupertinoTextSelectionToolbarContentState : State
    {
        private int _page;
        private int? _nextPage;
        private double _opacity = 1.0;
        private readonly GlobalKey<State> _itemsKey = new LabeledGlobalKey<State>("CupertinoToolbarItems");

        private CupertinoTextSelectionToolbarContent Current =>
            (CupertinoTextSelectionToolbarContent)StateWidget;

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldToolbar = (CupertinoTextSelectionToolbarContent)oldWidget;
            if (!ReferenceEquals(oldToolbar.Children, Current.Children))
            {
                _page = 0;
            }
        }

        public override Widget Build(BuildContext context)
        {
            Color dividerColor = CupertinoTheme.BrightnessOf(context) == PlatformBrightness.Dark
                ? Color.Parse("#FF424242")
                : Color.Parse("#FFD6D6D6");
            Widget backButton = BuildChevronButton(pointsRight: false, () => ChangePage(-1));
            Widget nextButton = BuildChevronButton(pointsRight: true, () => ChangePage(1));
            Widget contents = new CupertinoTextSelectionToolbarItems(
                page: _page,
                dividerColor: dividerColor,
                key: _itemsKey,
                children:
                [
                    backButton,
                    .. Current.Children,
                    nextButton,
                ]);
            contents = new GestureDetector(
                excludeFromSemantics: true,
                onHorizontalDragEnd: details =>
                {
                    if (details.PrimaryVelocity is > 0.0)
                    {
                        ChangePage(-1);
                    }
                    else if (details.PrimaryVelocity is < 0.0)
                    {
                        ChangePage(1);
                    }
                },
                child: contents);
            contents = new AnimatedSize(
                duration: TimeSpan.FromMilliseconds(125),
                curve: Curves.Decelerate,
                child: contents);
            contents = new AnimatedOpacity(
                opacity: _opacity,
                duration: TimeSpan.FromMilliseconds(125),
                onEnd: HandleOpacityEnd,
                child: contents);
            return Current.ToolbarBuilder(
                context,
                Current.AnchorAbove,
                Current.AnchorBelow,
                contents);
        }

        private static Widget BuildChevronButton(bool pointsRight, Action onPressed)
        {
            return new CupertinoTextSelectionToolbarButton(
                onPressed,
                new IgnorePointer(
                    child: new CustomPaint(
                        painter: new CupertinoChevronPainter(pointsRight),
                        size: new Size(10.0, 10.0))));
        }

        private void ChangePage(int delta)
        {
            RenderCupertinoTextSelectionToolbarItems? render =
                _itemsKey.CurrentContext?.FindRenderObject() as RenderCupertinoTextSelectionToolbarItems;
            if (render is null
                || delta < 0 && !render.HasPreviousPage
                || delta > 0 && !render.HasNextPage)
            {
                return;
            }

            SetState(() =>
            {
                _nextPage = _page + delta;
                _opacity = 0.0;
            });
        }

        private void HandleOpacityEnd()
        {
            if (_opacity != 0.0 || !_nextPage.HasValue)
            {
                return;
            }

            SetState(() =>
            {
                _page = _nextPage.Value;
                _nextPage = null;
                _opacity = 1.0;
            });
        }
    }
}

internal sealed class CupertinoTextSelectionToolbarShape : SingleChildRenderObjectWidget
{
    public CupertinoTextSelectionToolbarShape(
        Point anchorAbove,
        Point anchorBelow,
        Color backgroundColor,
        Color? shadowColor,
        Widget child,
        Key? key = null) : base(child, key)
    {
        AnchorAbove = anchorAbove;
        AnchorBelow = anchorBelow;
        BackgroundColor = backgroundColor;
        ShadowColor = shadowColor;
    }

    public Point AnchorAbove { get; }

    public Point AnchorBelow { get; }

    public Color BackgroundColor { get; }

    public Color? ShadowColor { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderCupertinoTextSelectionToolbarShape(
            AnchorAbove,
            AnchorBelow,
            BackgroundColor,
            ShadowColor);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var shape = (RenderCupertinoTextSelectionToolbarShape)renderObject;
        shape.AnchorAbove = AnchorAbove;
        shape.AnchorBelow = AnchorBelow;
        shape.BackgroundColor = BackgroundColor;
        shape.ShadowColor = ShadowColor;
    }
}

internal sealed class RenderCupertinoTextSelectionToolbarShape : RenderProxyBox
{
    private const double ArrowSize = 14.0;
    private const double BorderRadiusValue = 8.0;
    private Point _anchorAbove;
    private Point _anchorBelow;
    private Color _backgroundColor;
    private Color? _shadowColor;
    private bool _isAbove;

    public RenderCupertinoTextSelectionToolbarShape(
        Point anchorAbove,
        Point anchorBelow,
        Color backgroundColor,
        Color? shadowColor)
    {
        _anchorAbove = anchorAbove;
        _anchorBelow = anchorBelow;
        _backgroundColor = backgroundColor;
        _shadowColor = shadowColor;
    }

    public Point AnchorAbove
    {
        get => _anchorAbove;
        set
        {
            if (_anchorAbove == value) return;
            _anchorAbove = value;
            MarkNeedsLayout();
        }
    }

    public Point AnchorBelow
    {
        get => _anchorBelow;
        set
        {
            if (_anchorBelow == value) return;
            _anchorBelow = value;
            MarkNeedsLayout();
        }
    }

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if (_backgroundColor == value) return;
            _backgroundColor = value;
            MarkNeedsPaint();
        }
    }

    public Color? ShadowColor
    {
        get => _shadowColor;
        set
        {
            if (_shadowColor == value) return;
            _shadowColor = value;
            MarkNeedsPaint();
        }
    }

    protected override void PerformLayout()
    {
        if (Child is null)
        {
            Size = Constraints.Smallest;
            return;
        }

        BoxConstraints childConstraints = new BoxConstraints(MinWidth: 30.0)
            .Enforce(Constraints.Loosen());
        Child.Layout(childConstraints, parentUsesSize: true);
        _isAbove = AnchorAbove.Y >= Child.Size.Height - (ArrowSize / 2.0);
        ((BoxParentData)Child.parentData!).offset = new Point(0.0, _isAbove ? -ArrowSize / 2.0 : 0.0);
        Size = Constraints.Constrain(new Size(Child.Size.Width, Math.Max(0.0, Child.Size.Height - 7.0)));
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        if (Child is null)
        {
            return;
        }

        var childData = (BoxParentData)Child.parentData!;
        Plumix.UI.Path path = BuildPath();
        if (ShadowColor.HasValue)
        {
            var shadow = new BoxShadow
            {
                Color = ShadowColor.Value,
                Blur = 15.0,
            };
            context.DrawRectangle(
                new SolidColorBrush(Colors.Transparent),
                null,
                new Rect(offset.X, offset.Y, Size.Width, Size.Height),
                BorderRadius.Circular(BorderRadiusValue),
                new BoxShadows(shadow));
        }

        context.PushTransform(
            Matrix.CreateTranslation(offset.X, offset.Y),
            childContext => childContext.DrawPath(path, new SolidColorBrush(BackgroundColor), null));
        context.PushClipPath(
            path,
            childContext => childContext.PaintChild(Child, offset + (Vector)childData.offset),
            geometryOffset: offset);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        if (Child is null)
        {
            return false;
        }

        var childData = (BoxParentData)Child.parentData!;
        Point local = position - (Vector)childData.offset;
        return local.X >= 0.0
               && local.X <= Child.Size.Width
               && local.Y >= 7.0
               && local.Y <= Child.Size.Height - 7.0
               && Child.HitTest(result, local);
    }

    private Plumix.UI.Path BuildPath()
    {
        double childHeight = Child!.Size.Height;
        double top = 7.0;
        double bottom = childHeight - 7.0;
        Point rootOffset = GetPaintOffsetToRoot();
        double anchorX = (_isAbove ? AnchorAbove.X : AnchorBelow.X) - rootOffset.X;
        double arrowX = Math.Clamp(
            anchorX,
            BorderRadiusValue + (ArrowSize / 2.0),
            Math.Max(
                BorderRadiusValue + (ArrowSize / 2.0),
                Size.Width - BorderRadiusValue - (ArrowSize / 2.0)));
        var path = new Plumix.UI.Path();
        path.AddRRect(Plumix.UI.RRect.FromRectAndRadius(
            new Rect(0.0, top, Child.Size.Width, Math.Max(0.0, bottom - top)),
            BorderRadiusValue));
        path.MoveTo(arrowX - (ArrowSize / 2.0), _isAbove ? bottom : top);
        path.LineTo(arrowX, _isAbove ? childHeight : 0.0);
        path.LineTo(arrowX + (ArrowSize / 2.0), _isAbove ? bottom : top);
        path.Close();
        return path;
    }
}

internal sealed class CupertinoToolbarItemParentData : ContainerBoxParentData<RenderBox>
{
    public bool ShouldPaint { get; set; }
}

internal sealed class CupertinoTextSelectionToolbarItems : MultiChildRenderObjectWidget
{
    public CupertinoTextSelectionToolbarItems(
        int page,
        Color dividerColor,
        IReadOnlyList<Widget> children,
        Key? key = null) : base(children, key)
    {
        Page = page;
        DividerColor = dividerColor;
    }

    public int Page { get; }

    public Color DividerColor { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderCupertinoTextSelectionToolbarItems(Page, DividerColor);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var toolbar = (RenderCupertinoTextSelectionToolbarItems)renderObject;
        toolbar.Page = Page;
        toolbar.DividerColor = DividerColor;
    }
}

internal sealed class RenderCupertinoTextSelectionToolbarItems : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, CupertinoToolbarItemParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, CupertinoToolbarItemParentData> _container;
    private int _page;
    private Color _dividerColor;
    private int _lastPage;

    public RenderCupertinoTextSelectionToolbarItems(int page, Color dividerColor)
    {
        _page = page;
        _dividerColor = dividerColor;
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, CupertinoToolbarItemParentData>(this);
    }

    public int Page
    {
        get => _page;
        set
        {
            if (_page == value)
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
            if (_dividerColor == value)
            {
                return;
            }

            _dividerColor = value;
            MarkNeedsPaint();
        }
    }

    public bool HasNextPage => Page < _lastPage;

    public bool HasPreviousPage => Page > 0 && Page <= _lastPage;

    public RenderBox? FirstChild => _container.FirstChild;

    public RenderBox? LastChild => _container.LastChild;

    public int ChildCount => _container.ChildCount;

    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);

    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);

    public void AddAll(List<RenderBox> children) => _container.AddAll(children);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not CupertinoToolbarItemParentData)
        {
            child.parentData = new CupertinoToolbarItemParentData();
        }
    }

    protected override void PerformLayout()
    {
        if (ChildCount < 3 || FirstChild is null || LastChild is null)
        {
            Size = Constraints.Smallest;
            return;
        }

        var children = Children().ToList();
        RenderBox back = children[0];
        RenderBox next = children[^1];
        IReadOnlyList<RenderBox> items = children.Skip(1).Take(children.Count - 2).ToList();
        double height = items.Count == 0
            ? 0.0
            : items.Max(child => child.GetMaxIntrinsicHeight(Constraints.MaxWidth));
        var navigationConstraints = new BoxConstraints(
            MaxWidth: Constraints.MaxWidth,
            MinHeight: height,
            MaxHeight: height);
        back.Layout(navigationConstraints, parentUsesSize: true);
        next.Layout(navigationConstraints, parentUsesSize: true);
        double divider = 1.0;
        double x = 0.0;
        int calculatedPage = 0;
        double pageWidth = 0.0;
        foreach (RenderBox child in children)
        {
            ((CupertinoToolbarItemParentData)child.parentData!).ShouldPaint = false;
        }

        foreach (RenderBox child in items)
        {
            double reserved = calculatedPage == 0 ? next.Size.Width : back.Size.Width + next.Size.Width;
            child.Layout(
                new BoxConstraints(
                    MaxWidth: Math.Max(0.0, Constraints.MaxWidth - reserved),
                    MinHeight: height,
                    MaxHeight: height),
                parentUsesSize: true);
            if (x > 0.0 && x + child.Size.Width + reserved > Constraints.MaxWidth)
            {
                calculatedPage++;
                x = back.Size.Width + divider;
            }

            var parentData = (CupertinoToolbarItemParentData)child.parentData!;
            parentData.offset = new Point(x, 0.0);
            parentData.ShouldPaint = calculatedPage == Page;
            x += child.Size.Width + divider;
            if (calculatedPage == Page)
            {
                pageWidth = Math.Max(pageWidth, x);
            }
        }

        int lastPage = calculatedPage;
        _lastPage = lastPage;
        bool showBack = Page > 0 && Page <= lastPage;
        bool showNext = Page < lastPage;
        var backData = (CupertinoToolbarItemParentData)back.parentData!;
        backData.ShouldPaint = showBack;
        backData.offset = new Point(0.0, 0.0);
        var nextData = (CupertinoToolbarItemParentData)next.parentData!;
        nextData.ShouldPaint = showNext;
        nextData.offset = new Point(Math.Max(0.0, pageWidth - divider), 0.0);
        if (showNext)
        {
            pageWidth += next.Size.Width;
        }

        Size = Constraints.Constrain(new Size(Math.Max(0.0, pageWidth - divider), height));
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        RenderBox? child = FirstChild;
        while (child is not null)
        {
            var parentData = (CupertinoToolbarItemParentData)child.parentData!;
            if (parentData.ShouldPaint)
            {
                context.PaintChild(child, offset + (Vector)parentData.offset);
                RenderBox? nextSibling = parentData.nextSibling;
                if (nextSibling is not null
                    && ((CupertinoToolbarItemParentData)nextSibling.parentData!).ShouldPaint)
                {
                    double x = offset.X + parentData.offset.X + child.Size.Width;
                    context.DrawLine(
                        new Pen(new SolidColorBrush(DividerColor)),
                        new Point(x, offset.Y),
                        new Point(x, offset.Y + Size.Height));
                }
            }

            child = parentData.nextSibling;
        }
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        RenderBox? child = LastChild;
        while (child is not null)
        {
            var parentData = (CupertinoToolbarItemParentData)child.parentData!;
            if (parentData.ShouldPaint
                && child.HitTest(result, position - (Vector)parentData.offset))
            {
                return true;
            }

            child = parentData.previousSibling;
        }

        return false;
    }

    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);

    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);

    public void Remove(RenderBox child) => _container.Remove(child);

    public void DefaultPaint(PaintingContext context, Point offset) => _container.DefaultPaint(context, offset);

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position) =>
        _container.DefaultHitTestChildren(result, position);

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
            var parentData = (CupertinoToolbarItemParentData)child.parentData!;
            if (parentData.ShouldPaint)
            {
                visitor(child);
            }
        }
    }

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) =>
        Insert((RenderBox)child, after as RenderBox);

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) =>
        Move((RenderBox)child, after as RenderBox);

    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);

    private IEnumerable<RenderBox> Children()
    {
        RenderBox? child = FirstChild;
        while (child is not null)
        {
            yield return child;
            child = ((CupertinoToolbarItemParentData)child.parentData!).nextSibling;
        }
    }
}

internal sealed class CupertinoChevronPainter : CustomPainter
{
    private readonly bool _pointsRight;

    public CupertinoChevronPainter(bool pointsRight)
    {
        _pointsRight = pointsRight;
    }

    public override void Paint(PaintingContext context, Size size)
    {
        var pen = new Pen(new SolidColorBrush(Colors.Black), 2.0, lineCap: PenLineCap.Round);
        double outer = _pointsRight ? 2.5 : 7.5;
        double inner = _pointsRight ? 7.5 : 2.5;
        context.DrawLine(pen, new Point(outer, 0.0), new Point(inner, 5.0));
        context.DrawLine(pen, new Point(inner, 5.0), new Point(outer, 10.0));
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        return oldDelegate is not CupertinoChevronPainter oldPainter
               || oldPainter._pointsRight != _pointsRight;
    }
}
