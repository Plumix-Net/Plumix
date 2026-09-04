using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources:
// - material_ui/lib/src/text_selection_toolbar.dart
// - material_ui/lib/src/text_selection_toolbar_text_button.dart

public delegate Widget ToolbarBuilder(BuildContext context, Widget child);

/// <summary>A Material text-selection toolbar with automatic horizontal overflow handling.</summary>
public sealed class TextSelectionToolbar : StatelessWidget
{
    internal const double ToolbarHeight = 44.0;
    internal const double ToolbarContentDistance = 8.0;
    internal const double ToolbarScreenPadding = 8.0;

    public const double HandleSize = 22.0;
    public const double ToolbarContentDistanceBelow = HandleSize - 2.0;

    public TextSelectionToolbar(
        Point anchorAbove,
        Point anchorBelow,
        IReadOnlyList<Widget> children,
        ToolbarBuilder? toolbarBuilder = null,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(children);
        if (children.Count == 0)
        {
            throw new ArgumentException("TextSelectionToolbar children must not be empty.", nameof(children));
        }

        AnchorAbove = anchorAbove;
        AnchorBelow = anchorBelow;
        Children = children;
        ToolbarBuilder = toolbarBuilder ?? DefaultToolbarBuilder;
    }

    public Point AnchorAbove { get; }

    public Point AnchorBelow { get; }

    public IReadOnlyList<Widget> Children { get; }

    public ToolbarBuilder ToolbarBuilder { get; }

    public override Widget Build(BuildContext context)
    {
        var anchorAbovePadded = AnchorAbove - new Vector(0.0, ToolbarContentDistance);
        var anchorBelowPadded = AnchorBelow + new Vector(0.0, ToolbarContentDistanceBelow);
        double paddingAbove = MediaQuery.PaddingOf(context).Top + ToolbarScreenPadding;
        double availableHeight = anchorAbovePadded.Y - ToolbarContentDistance - paddingAbove;
        bool fitsAbove = ToolbarHeight <= availableHeight;
        var localAdjustment = new Vector(ToolbarScreenPadding, paddingAbove);

        return new Padding(
            new Thickness(ToolbarScreenPadding, paddingAbove, ToolbarScreenPadding, ToolbarScreenPadding),
            new CustomSingleChildLayout(
                new TextSelectionToolbarLayoutDelegate(
                    anchorAbove: anchorAbovePadded - localAdjustment,
                    anchorBelow: anchorBelowPadded - localAdjustment,
                    fitsAbove: fitsAbove),
                new TextSelectionToolbarOverflowable(
                    isAbove: fitsAbove,
                    toolbarBuilder: ToolbarBuilder,
                    children: Children)));
    }

    private static Widget DefaultToolbarBuilder(BuildContext context, Widget child)
    {
        ThemeData theme = Theme.Of(context);
        Color defaultSurface = theme.Brightness == Brightness.Dark
            ? ThemeData.Dark.ColorScheme.Surface
            : ThemeData.Light.ColorScheme.Surface;
        Color color = theme.ColorScheme.Surface != defaultSurface
            ? theme.ColorScheme.Surface
            : theme.Brightness == Brightness.Dark
                ? Color.Parse("#FF424242")
                : Colors.White;

        return new Material(
            type: MaterialType.Card,
            elevation: 1.0,
            color: color,
            borderRadius: BorderRadius.Circular(ToolbarHeight / 2.0),
            clipBehavior: Clip.AntiAlias,
            child: child);
    }
}

/// <summary>A text button styled like an Android Material text-selection toolbar item.</summary>
public sealed class TextSelectionToolbarTextButton : StatelessWidget
{
    private const double MiddlePadding = 9.5;
    private const double EndPadding = 14.5;

    public TextSelectionToolbarTextButton(
        Widget child,
        Thickness padding,
        Action? onPressed = null,
        Alignment? alignment = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Padding = padding;
        OnPressed = onPressed;
        Alignment = alignment;
    }

    public Widget Child { get; }

    public Action? OnPressed { get; }

    public Thickness Padding { get; }

    public Alignment? Alignment { get; }

    public static Thickness GetPadding(int index, int total, TextDirection textDirection = TextDirection.Ltr)
    {
        if (total <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(total), "A toolbar must contain at least one item.");
        }

        if (index < 0 || index >= total)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index must identify an item in a non-empty toolbar.");
        }

        double start = index == 0 ? EndPadding : MiddlePadding;
        double end = index == total - 1 ? EndPadding : MiddlePadding;
        return textDirection == TextDirection.Rtl
            ? new Thickness(end, 0.0, start, 0.0)
            : new Thickness(start, 0.0, end, 0.0);
    }

    public TextSelectionToolbarTextButton CopyWith(
        Widget? child = null,
        Action? onPressed = null,
        Thickness? padding = null,
        Alignment? alignment = null)
    {
        return new TextSelectionToolbarTextButton(
            child: child ?? Child,
            onPressed: onPressed ?? OnPressed,
            padding: padding ?? Padding,
            alignment: alignment ?? Alignment);
    }

    public override Widget Build(BuildContext context)
    {
        ThemeData theme = Theme.Of(context);
        Color defaultOnSurface = theme.Brightness == Brightness.Dark
            ? ThemeData.Dark.ColorScheme.OnSurface
            : ThemeData.Light.ColorScheme.OnSurface;
        Color foregroundColor = theme.ColorScheme.OnSurface != defaultOnSurface
            ? theme.ColorScheme.OnSurface
            : theme.Brightness == Brightness.Dark
                ? Colors.White
                : Colors.Black;
        ButtonStyle style = TextButton.StyleFrom(
            backgroundColor: Colors.Transparent,
            foregroundColor: foregroundColor,
            shape: new RoundedRectangleBorder(borderRadius: BorderRadius.Zero),
            minimumSize: new Size(48.0, 48.0),
            padding: Padding,
            alignment: Alignment,
            textStyle: new TextStyle(FontWeight: FontWeight.Normal));

        return new TextButton(
            child: Child,
            onPressed: OnPressed,
            style: style);
    }
}

internal sealed class TextSelectionToolbarOverflowable : StatefulWidget
{
    public TextSelectionToolbarOverflowable(
        bool isAbove,
        ToolbarBuilder toolbarBuilder,
        IReadOnlyList<Widget> children,
        Key? key = null) : base(key)
    {
        IsAbove = isAbove;
        ToolbarBuilder = toolbarBuilder;
        Children = children;
    }

    public bool IsAbove { get; }

    public ToolbarBuilder ToolbarBuilder { get; }

    public IReadOnlyList<Widget> Children { get; }

    public override State CreateState() => new TextSelectionToolbarOverflowableState();

    private sealed class TextSelectionToolbarOverflowableState : State
    {
        private bool _overflowOpen;
        private Key _containerKey = new UniqueKey();

        private TextSelectionToolbarOverflowable CurrentWidget =>
            (TextSelectionToolbarOverflowable)StateWidget;

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldToolbar = (TextSelectionToolbarOverflowable)oldWidget;
            if (!ChildrenEqual(CurrentWidget.Children, oldToolbar.Children))
            {
                _containerKey = new UniqueKey();
                _overflowOpen = false;
            }
        }

        public override Widget Build(BuildContext context)
        {
            MaterialLocalizations localizations = MaterialLocalizations.Of(context);
            TextDirection textDirection = Directionality.Of(context);
            string navigationTooltip = _overflowOpen
                ? localizations.BackButtonTooltip
                : localizations.MoreButtonTooltip;
            Widget navigationButton = new Material(
                type: MaterialType.Card,
                color: Colors.Transparent,
                child: new Tooltip(
                    message: navigationTooltip,
                    child: new IconButton(
                        icon: new Icon(_overflowOpen ? Icons.ArrowBack : Icons.MoreVert),
                        onPressed: ToggleOverflow)));
            var children = new List<Widget>(CurrentWidget.Children.Count + 1)
            {
                navigationButton,
            };
            children.AddRange(CurrentWidget.Children);

            Widget contents = new TextSelectionToolbarItemsLayout(
                isAbove: CurrentWidget.IsAbove,
                overflowOpen: _overflowOpen,
                textDirection: textDirection,
                children: children);
            Widget toolbar = CurrentWidget.ToolbarBuilder(context, contents);
            toolbar = new AnimatedSize(
                duration: TimeSpan.FromMilliseconds(140),
                child: toolbar);
            return new TextSelectionToolbarTrailingEdgeAlign(
                overflowOpen: _overflowOpen,
                textDirection: textDirection,
                child: toolbar,
                key: _containerKey);
        }

        private void ToggleOverflow() => SetState(() => _overflowOpen = !_overflowOpen);

        private static bool ChildrenEqual(IReadOnlyList<Widget> first, IReadOnlyList<Widget> second)
        {
            if (first.Count != second.Count)
            {
                return false;
            }

            for (int index = 0; index < first.Count; index++)
            {
                if (!ReferenceEquals(first[index], second[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

internal sealed class TextSelectionToolbarTrailingEdgeAlign : SingleChildRenderObjectWidget
{
    public TextSelectionToolbarTrailingEdgeAlign(
        bool overflowOpen,
        TextDirection textDirection,
        Widget child,
        Key? key = null) : base(child, key)
    {
        OverflowOpen = overflowOpen;
        TextDirection = textDirection;
    }

    public bool OverflowOpen { get; }

    public TextDirection TextDirection { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderTextSelectionToolbarTrailingEdgeAlign(OverflowOpen, TextDirection);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var align = (RenderTextSelectionToolbarTrailingEdgeAlign)renderObject;
        align.OverflowOpen = OverflowOpen;
        align.TextDirection = TextDirection;
    }
}

internal sealed class RenderTextSelectionToolbarTrailingEdgeAlign : RenderProxyBox
{
    private bool _overflowOpen;
    private TextDirection _textDirection;
    private double? _closedWidth;

    public RenderTextSelectionToolbarTrailingEdgeAlign(bool overflowOpen, TextDirection textDirection)
    {
        _overflowOpen = overflowOpen;
        _textDirection = textDirection;
    }

    public bool OverflowOpen
    {
        get => _overflowOpen;
        set
        {
            if (_overflowOpen == value)
            {
                return;
            }

            _overflowOpen = value;
            MarkNeedsLayout();
        }
    }

    public TextDirection TextDirection
    {
        get => _textDirection;
        set
        {
            if (_textDirection == value)
            {
                return;
            }

            _textDirection = value;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        if (Child is null)
        {
            Size = Constraints.Smallest;
            return;
        }

        Child.Layout(Constraints.Loosen(), parentUsesSize: true);
        if (!OverflowOpen && !_closedWidth.HasValue)
        {
            _closedWidth = Child.Size.Width;
        }

        double width = !_closedWidth.HasValue || Child.Size.Width > _closedWidth.Value
            ? Child.Size.Width
            : _closedWidth.Value;
        Size = Constraints.Constrain(new Size(width, Child.Size.Height));
        ((BoxParentData)Child.parentData!).offset = new Point(
            TextDirection == TextDirection.Rtl ? 0.0 : Size.Width - Child.Size.Width,
            0.0);
    }
}

internal sealed class TextSelectionToolbarItemsLayout : MultiChildRenderObjectWidget
{
    public TextSelectionToolbarItemsLayout(
        bool isAbove,
        bool overflowOpen,
        TextDirection textDirection,
        IReadOnlyList<Widget> children,
        Key? key = null) : base(children, key)
    {
        IsAbove = isAbove;
        OverflowOpen = overflowOpen;
        TextDirection = textDirection;
    }

    public bool IsAbove { get; }

    public bool OverflowOpen { get; }

    public TextDirection TextDirection { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderTextSelectionToolbarItemsLayout(IsAbove, OverflowOpen, TextDirection);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var layout = (RenderTextSelectionToolbarItemsLayout)renderObject;
        layout.IsAbove = IsAbove;
        layout.OverflowOpen = OverflowOpen;
        layout.TextDirection = TextDirection;
    }
}

internal sealed class RenderTextSelectionToolbarItemsLayout : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, ToolbarItemsParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, ToolbarItemsParentData> _container;
    private bool _isAbove;
    private bool _overflowOpen;
    private TextDirection _textDirection;
    private int _lastIndexThatFits = -1;

    public RenderTextSelectionToolbarItemsLayout(
        bool isAbove,
        bool overflowOpen,
        TextDirection textDirection)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, ToolbarItemsParentData>(this);
        _isAbove = isAbove;
        _overflowOpen = overflowOpen;
        _textDirection = textDirection;
    }

    public bool IsAbove
    {
        get => _isAbove;
        set
        {
            if (_isAbove == value)
            {
                return;
            }

            _isAbove = value;
            MarkNeedsLayout();
        }
    }

    public bool OverflowOpen
    {
        get => _overflowOpen;
        set
        {
            if (_overflowOpen == value)
            {
                return;
            }

            _overflowOpen = value;
            MarkNeedsLayout();
        }
    }

    public TextDirection TextDirection
    {
        get => _textDirection;
        set
        {
            if (_textDirection == value)
            {
                return;
            }

            _textDirection = value;
            MarkNeedsLayout();
        }
    }

    public int ChildCount => _container.ChildCount;

    public RenderBox? FirstChild => _container.FirstChild;

    public RenderBox? LastChild => _container.LastChild;

    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);

    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);

    public void AddAll(List<RenderBox>? children) => _container.AddAll(children);

    public void RemoveAll() => _container.RemoveAll();

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not ToolbarItemsParentData)
        {
            child.parentData = new ToolbarItemsParentData();
        }
    }

    protected override void PerformLayout()
    {
        _lastIndexThatFits = -1;
        if (FirstChild is null)
        {
            Size = Constraints.Smallest;
            return;
        }

        LayoutChildren();
        Size = Constraints.Constrain(OverflowOpen ? PlaceChildrenVertically() : PlaceChildrenHorizontally());
        ResizeChildrenWhenOverflow();
    }

    private void LayoutChildren()
    {
        BoxConstraints sizedConstraints = OverflowOpen
            ? Constraints
            : new BoxConstraints(MaxWidth: Constraints.MaxWidth, MaxHeight: TextSelectionToolbar.ToolbarHeight);
        BoxConstraints childConstraints = sizedConstraints.Loosen();
        double width = 0.0;
        int index = -1;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            index++;
            if (_lastIndexThatFits != -1 && !OverflowOpen)
            {
                continue;
            }

            child.Layout(childConstraints, parentUsesSize: true);
            width += child.Size.Width;
            if (width > sizedConstraints.MaxWidth && _lastIndexThatFits == -1)
            {
                _lastIndexThatFits = index - 1;
            }
        }

        RenderBox navigationButton = FirstChild!;
        if (_lastIndexThatFits != -1
            && _lastIndexThatFits == ChildCount - 2
            && width - navigationButton.Size.Width <= sizedConstraints.MaxWidth)
        {
            _lastIndexThatFits = -1;
        }
    }

    private bool ShouldPaintChild(RenderBox child, int index)
    {
        if (ReferenceEquals(child, FirstChild))
        {
            return _lastIndexThatFits != -1;
        }

        if (_lastIndexThatFits == -1)
        {
            return true;
        }

        return (index > _lastIndexThatFits) == OverflowOpen;
    }

    private Size PlaceChildrenHorizontally()
    {
        RenderBox navigationButton = FirstChild!;
        bool isRtl = TextDirection == TextDirection.Rtl;
        var contentItems = new List<RenderBox>();
        double totalWidth = 0.0;
        double maxHeight = 0.0;
        int index = -1;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            index++;
            var parentData = (ToolbarItemsParentData)child.parentData!;
            parentData.ShouldPaint = ShouldPaintChild(child, index);
            if (!parentData.ShouldPaint)
            {
                continue;
            }

            totalWidth += child.Size.Width;
            maxHeight = Math.Max(maxHeight, child.Size.Height);
            if (!ReferenceEquals(child, navigationButton))
            {
                contentItems.Add(child);
            }
        }

        bool showNavigationButton = _lastIndexThatFits >= 0;
        if (isRtl)
        {
            if (showNavigationButton)
            {
                ((ToolbarItemsParentData)navigationButton.parentData!).offset = default;
            }

            double rightEdge = totalWidth;
            foreach (RenderBox item in contentItems)
            {
                rightEdge -= item.Size.Width;
                ((ToolbarItemsParentData)item.parentData!).offset = new Point(rightEdge, 0.0);
            }
        }
        else
        {
            double currentX = 0.0;
            foreach (RenderBox item in contentItems)
            {
                ((ToolbarItemsParentData)item.parentData!).offset = new Point(currentX, 0.0);
                currentX += item.Size.Width;
            }

            if (showNavigationButton)
            {
                ((ToolbarItemsParentData)navigationButton.parentData!).offset = new Point(currentX, 0.0);
            }
        }

        return new Size(totalWidth, maxHeight);
    }

    private Size PlaceChildrenVertically()
    {
        RenderBox navigationButton = FirstChild!;
        var navigationParentData = (ToolbarItemsParentData)navigationButton.parentData!;
        double currentY = 0.0;
        double maxWidth = 0.0;
        navigationParentData.ShouldPaint = ShouldPaintChild(navigationButton, 0);
        if (navigationParentData.ShouldPaint && !IsAbove)
        {
            navigationParentData.offset = default;
            currentY += navigationButton.Size.Height;
            maxWidth = Math.Max(maxWidth, navigationButton.Size.Width);
        }

        int index = -1;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            index++;
            if (ReferenceEquals(child, navigationButton))
            {
                continue;
            }

            var parentData = (ToolbarItemsParentData)child.parentData!;
            parentData.ShouldPaint = ShouldPaintChild(child, index);
            if (!parentData.ShouldPaint)
            {
                continue;
            }

            parentData.offset = new Point(0.0, currentY);
            currentY += child.Size.Height;
            maxWidth = Math.Max(maxWidth, child.Size.Width);
        }

        if (IsAbove && navigationParentData.ShouldPaint)
        {
            navigationParentData.offset = new Point(0.0, currentY);
            currentY += navigationButton.Size.Height;
            maxWidth = Math.Max(maxWidth, navigationButton.Size.Width);
        }

        return new Size(maxWidth, currentY);
    }

    private void ResizeChildrenWhenOverflow()
    {
        if (!OverflowOpen)
        {
            return;
        }

        RenderBox navigationButton = FirstChild!;
        int index = -1;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            index++;
            if (ReferenceEquals(child, navigationButton) || !ShouldPaintChild(child, index))
            {
                continue;
            }

            child.Layout(BoxConstraints.TightFor(width: Size.Width), parentUsesSize: true);
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            var parentData = (ToolbarItemsParentData)child.parentData!;
            if (parentData.ShouldPaint)
            {
                context.PaintChild(child, parentData.offset + offset);
            }
        }
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        for (RenderBox? child = LastChild; child is not null; child = ChildBefore(child))
        {
            var parentData = (ToolbarItemsParentData)child.parentData!;
            if (!parentData.ShouldPaint)
            {
                continue;
            }

            RenderBox localChild = child;
            bool isHit = result.AddWithPaintOffset(
                parentData.offset,
                position,
                (hitResult, transformed) => localChild.HitTest(hitResult, transformed));
            if (isHit)
            {
                return true;
            }
        }

        return false;
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
            var parentData = (ToolbarItemsParentData)child.parentData!;
            if (parentData.ShouldPaint)
            {
                visitor(child);
            }
        }
    }

    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);

    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);

    public void Remove(RenderBox child) => _container.Remove(child);

    public void DefaultPaint(PaintingContext context, Point offset) => _container.DefaultPaint(context, offset);

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position) =>
        _container.DefaultHitTestChildren(result, position);

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) =>
        Insert((RenderBox)child, after as RenderBox);

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) =>
        Move((RenderBox)child, after as RenderBox);

    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);
}
