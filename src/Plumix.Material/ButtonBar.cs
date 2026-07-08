using Avalonia;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/button_bar.dart

public sealed class ButtonBar : StatelessWidget
{
    public ButtonBar(
        IReadOnlyList<Widget>? children = null,
        MainAxisAlignment? alignment = null,
        MainAxisSize? mainAxisSize = null,
        ButtonTextTheme? buttonTextTheme = null,
        double? buttonMinWidth = null,
        double? buttonHeight = null,
        Thickness? buttonPadding = null,
        bool? buttonAlignedDropdown = null,
        ButtonBarLayoutBehavior? layoutBehavior = null,
        VerticalDirection? overflowDirection = null,
        double? overflowButtonSpacing = null,
        Key? key = null) : base(key)
    {
        ValidateNonNegative(nameof(buttonMinWidth), buttonMinWidth);
        ValidateNonNegative(nameof(buttonHeight), buttonHeight);
        ValidateNonNegative(nameof(overflowButtonSpacing), overflowButtonSpacing);
        Children = children ?? [];
        Alignment = alignment;
        MainAxisSize = mainAxisSize;
        ButtonTextTheme = buttonTextTheme;
        ButtonMinWidth = buttonMinWidth;
        ButtonHeight = buttonHeight;
        ButtonPadding = buttonPadding;
        ButtonAlignedDropdown = buttonAlignedDropdown;
        LayoutBehavior = layoutBehavior;
        OverflowDirection = overflowDirection;
        OverflowButtonSpacing = overflowButtonSpacing;
    }

    public IReadOnlyList<Widget> Children { get; }
    public MainAxisAlignment? Alignment { get; }
    public MainAxisSize? MainAxisSize { get; }
    public ButtonTextTheme? ButtonTextTheme { get; }
    public double? ButtonMinWidth { get; }
    public double? ButtonHeight { get; }
    public Thickness? ButtonPadding { get; }
    public bool? ButtonAlignedDropdown { get; }
    public ButtonBarLayoutBehavior? LayoutBehavior { get; }
    public VerticalDirection? OverflowDirection { get; }
    public double? OverflowButtonSpacing { get; }

    public override Widget Build(BuildContext context)
    {
        var parentTheme = ButtonTheme.Of(context);
        var barTheme = ButtonBarTheme.Of(context);
        var effectivePadding = ButtonPadding ?? barTheme.ButtonPadding ?? new Thickness(8, 0);
        var effectiveButtonTheme = parentTheme with
        {
            TextTheme = ButtonTextTheme ?? barTheme.ButtonTextTheme ?? global::Plumix.Material.ButtonTextTheme.Primary,
            MinWidth = ButtonMinWidth ?? barTheme.ButtonMinWidth ?? 64,
            Height = ButtonHeight ?? barTheme.ButtonHeight ?? 36,
            Padding = effectivePadding,
            AlignedDropdown = ButtonAlignedDropdown ?? barTheme.ButtonAlignedDropdown ?? false,
            LayoutBehavior = LayoutBehavior ?? barTheme.LayoutBehavior ?? ButtonBarLayoutBehavior.Padded,
        };
        double paddingUnit = (effectivePadding.Left + effectivePadding.Right) / 4.0;
        var paddedChildren = Children
            .Select(child => (Widget)new Padding(new Thickness(paddingUnit, 0), child))
            .ToList();

        Widget row = new ButtonBarRow(
            children: paddedChildren,
            mainAxisAlignment: Alignment ?? barTheme.Alignment ?? MainAxisAlignment.End,
            mainAxisSize: MainAxisSize ?? barTheme.MainAxisSize ?? global::Plumix.Rendering.MainAxisSize.Max,
            overflowDirection: OverflowDirection ?? barTheme.OverflowDirection ?? VerticalDirection.Down,
            overflowButtonSpacing: OverflowButtonSpacing ?? 0,
            textDirection: Directionality.Of(context));
        row = new ButtonTheme(effectiveButtonTheme, row);

        return effectiveButtonTheme.LayoutBehavior switch
        {
            ButtonBarLayoutBehavior.Constrained => new ConstrainedBox(
                new BoxConstraints(MinHeight: 52),
                new Padding(
                    new Thickness(paddingUnit, 0),
                    new Center(child: row))),
            _ => new Padding(
                new Thickness(paddingUnit, 2 * paddingUnit),
                row),
        };
    }

    private static void ValidateNonNegative(string name, double? value)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}

internal sealed class ButtonBarRow : MultiChildRenderObjectWidget
{
    public ButtonBarRow(
        IReadOnlyList<Widget> children,
        MainAxisAlignment mainAxisAlignment,
        MainAxisSize mainAxisSize,
        VerticalDirection overflowDirection,
        double overflowButtonSpacing,
        TextDirection textDirection) : base(children)
    {
        MainAxisAlignment = mainAxisAlignment;
        MainAxisSize = mainAxisSize;
        OverflowDirection = overflowDirection;
        OverflowButtonSpacing = overflowButtonSpacing;
        TextDirection = textDirection;
    }

    public MainAxisAlignment MainAxisAlignment { get; }
    public MainAxisSize MainAxisSize { get; }
    public VerticalDirection OverflowDirection { get; }
    public double OverflowButtonSpacing { get; }
    public TextDirection TextDirection { get; }

    internal override RenderObject CreateRenderObject(BuildContext context) => new RenderButtonBarRow(
        MainAxisAlignment,
        MainAxisSize,
        OverflowDirection,
        OverflowButtonSpacing,
        TextDirection);

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var row = (RenderButtonBarRow)renderObject;
        row.MainAxisAlignment = MainAxisAlignment;
        row.MainAxisSize = MainAxisSize;
        row.OverflowDirection = OverflowDirection;
        row.OverflowButtonSpacing = OverflowButtonSpacing;
        row.TextDirection = TextDirection;
    }
}

internal sealed class ButtonBarParentData : ContainerBoxParentData<RenderBox>;

internal sealed class RenderButtonBarRow : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, ButtonBarParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, ButtonBarParentData> _container;
    private MainAxisAlignment _mainAxisAlignment;
    private MainAxisSize _mainAxisSize;
    private VerticalDirection _overflowDirection;
    private double _overflowButtonSpacing;
    private TextDirection _textDirection;

    public RenderButtonBarRow(
        MainAxisAlignment mainAxisAlignment,
        MainAxisSize mainAxisSize,
        VerticalDirection overflowDirection,
        double overflowButtonSpacing,
        TextDirection textDirection)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, ButtonBarParentData>(this);
        _mainAxisAlignment = mainAxisAlignment;
        _mainAxisSize = mainAxisSize;
        _overflowDirection = overflowDirection;
        _overflowButtonSpacing = overflowButtonSpacing;
        _textDirection = textDirection;
    }

    public MainAxisAlignment MainAxisAlignment { get => _mainAxisAlignment; set { if (_mainAxisAlignment != value) { _mainAxisAlignment = value; MarkNeedsLayout(); } } }
    public MainAxisSize MainAxisSize { get => _mainAxisSize; set { if (_mainAxisSize != value) { _mainAxisSize = value; MarkNeedsLayout(); } } }
    public VerticalDirection OverflowDirection { get => _overflowDirection; set { if (_overflowDirection != value) { _overflowDirection = value; MarkNeedsLayout(); } } }
    public double OverflowButtonSpacing { get => _overflowButtonSpacing; set { if (Math.Abs(_overflowButtonSpacing - value) > 0.0001) { _overflowButtonSpacing = value; MarkNeedsLayout(); } } }
    public TextDirection TextDirection { get => _textDirection; set { if (_textDirection != value) { _textDirection = value; MarkNeedsLayout(); } } }
    public int ChildCount => _container.ChildCount;
    public RenderBox? FirstChild => _container.FirstChild;
    public RenderBox? LastChild => _container.LastChild;
    public void AddAll(List<RenderBox> children) => _container.AddAll(children);
    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);
    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not ButtonBarParentData) child.parentData = new ButtonBarParentData();
    }

    protected override void PerformLayout()
    {
        if (ChildCount == 0)
        {
            Size = Constraints.Smallest;
            return;
        }

        var children = new List<RenderBox>(ChildCount);
        var childConstraints = new BoxConstraints(MaxWidth: double.PositiveInfinity, MaxHeight: Constraints.MaxHeight);
        double idealWidth = 0.0;
        double maxHeight = 0.0;
        for (var child = FirstChild; child is not null; child = ChildAfter(child))
        {
            child.Layout(childConstraints, parentUsesSize: true);
            children.Add(child);
            idealWidth += child.Size.Width;
            maxHeight = Math.Max(maxHeight, child.Size.Height);
        }

        if (!Constraints.HasBoundedWidth || idealWidth <= Constraints.MaxWidth)
        {
            double width = MainAxisSize == MainAxisSize.Max && Constraints.HasBoundedWidth
                ? Constraints.MaxWidth
                : idealWidth;
            Size = Constraints.Constrain(new Size(width, maxHeight));
            PositionHorizontal(children, idealWidth);
            return;
        }

        double height = children.Sum(child => child.Size.Height)
                        + (OverflowButtonSpacing * Math.Max(0, children.Count - 1));
        Size = Constraints.Constrain(new Size(Constraints.MaxWidth, height));
        PositionVertical(children);
    }

    private void PositionHorizontal(IReadOnlyList<RenderBox> children, double childrenWidth)
    {
        double free = Math.Max(0, Size.Width - childrenWidth);
        (double leading, double between) = MainAxisAlignment switch
        {
            MainAxisAlignment.Center => (free / 2, 0.0),
            MainAxisAlignment.End => (free, 0.0),
            MainAxisAlignment.SpaceBetween when children.Count > 1 => (0.0, free / (children.Count - 1)),
            MainAxisAlignment.SpaceAround => (free / children.Count / 2, free / children.Count),
            MainAxisAlignment.SpaceEvenly => (free / (children.Count + 1), free / (children.Count + 1)),
            _ => (0.0, 0.0),
        };
        bool rtl = TextDirection == TextDirection.Rtl;
        double x = rtl ? Size.Width - leading : leading;
        foreach (var child in children)
        {
            if (rtl) x -= child.Size.Width;
            ((ButtonBarParentData)child.parentData!).offset = new Point(x, (Size.Height - child.Size.Height) / 2);
            if (rtl) x -= between;
            else x += child.Size.Width + between;
        }
    }

    private void PositionVertical(IReadOnlyList<RenderBox> children)
    {
        var ordered = OverflowDirection == VerticalDirection.Down ? children : children.Reverse().ToList();
        double y = 0.0;
        foreach (var child in ordered)
        {
            double x = ResolveOverflowX(child.Size.Width);
            ((ButtonBarParentData)child.parentData!).offset = new Point(x, y);
            y += child.Size.Height + OverflowButtonSpacing;
        }
    }

    private double ResolveOverflowX(double width)
    {
        var logical = MainAxisAlignment is MainAxisAlignment.SpaceAround or MainAxisAlignment.SpaceBetween or MainAxisAlignment.SpaceEvenly
            ? MainAxisAlignment.Start
            : MainAxisAlignment;
        if (logical == MainAxisAlignment.Center) return (Size.Width - width) / 2;
        double start = TextDirection == TextDirection.Ltr ? 0 : Size.Width - width;
        double end = TextDirection == TextDirection.Ltr ? Size.Width - width : 0;
        return logical == MainAxisAlignment.End ? end : start;
    }

    public override void Paint(PaintingContext context, Point offset) => DefaultPaint(context, offset);
    protected override bool HitTestChildren(BoxHitTestResult result, Point position) => DefaultHitTestChildren(result, position);
    public void DefaultPaint(PaintingContext context, Point offset) => _container.DefaultPaint(context, offset);
    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position) => _container.DefaultHitTestChildren(result, position);
    public override void VisitChildren(Action<RenderObject> visitor) { for (var child = FirstChild; child is not null; child = ChildAfter(child)) visitor(child); }
    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor) { for (var child = FirstChild; child is not null; child = ChildAfter(child)) { var data = (ButtonBarParentData)child.parentData!; visitor(child, data.offset, Matrix.Identity); } }
    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);
    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);
    public void Remove(RenderBox child) => _container.Remove(child);
    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) => Insert((RenderBox)child, after as RenderBox);
    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) => Move((RenderBox)child, after as RenderBox);
    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);
}
