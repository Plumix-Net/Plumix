using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Material;

internal sealed class TabBarParentData : ContainerBoxParentData<RenderBox>;

internal sealed class RenderTabBar : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, TabBarParentData>,
    IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, TabBarParentData> _container;
    private double _animationValue;
    private int _currentIndex;
    private int _previousIndex;
    private bool _indexIsChanging;
    private bool _isScrollable;
    private TabAlignment _alignment;
    private TabBarIndicatorSize _indicatorSize;
    private Color _indicatorColor;
    private double _indicatorWeight;
    private Thickness _indicatorPadding;
    private Thickness _labelPadding;
    private BoxDecoration? _indicator;
    private Color _dividerColor;
    private double _dividerHeight;
    private TabIndicatorAnimation _indicatorAnimation;
    private TextDirection _textDirection;
    private readonly List<Rect> _tabRects = [];
    private readonly List<double> _intrinsicWidths = [];
    private Action<IReadOnlyList<Rect>, double>? _onLayout;

    public RenderTabBar(
        double animationValue, int currentIndex, int previousIndex, bool indexIsChanging,
        bool isScrollable, TabAlignment alignment, TabBarIndicatorSize indicatorSize,
        Color indicatorColor, double indicatorWeight, Thickness indicatorPadding,
        Thickness labelPadding, BoxDecoration? indicator, Color dividerColor, double dividerHeight,
        TabIndicatorAnimation indicatorAnimation, TextDirection textDirection,
        Action<IReadOnlyList<Rect>, double>? onLayout)
    {
        _container = new RenderBoxContainerDefaultsMixin<RenderBox, TabBarParentData>(this);
        Update(animationValue, currentIndex, previousIndex, indexIsChanging, isScrollable, alignment,
            indicatorSize, indicatorColor, indicatorWeight, indicatorPadding, indicator, dividerColor,
            dividerHeight, indicatorAnimation, textDirection, onLayout, labelPadding);
    }

    internal Rect? IndicatorRect { get; private set; }
    internal IReadOnlyList<Rect> TabRects => _tabRects;
    internal Color IndicatorColor => _indicatorColor;
    internal Color DividerColor => _dividerColor;
    internal double DividerHeight => _dividerHeight;

    public int ChildCount => _container.ChildCount;
    public RenderBox? FirstChild => _container.FirstChild;
    public RenderBox? LastChild => _container.LastChild;
    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);
    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);
    public void AddAll(List<RenderBox> children) => _container.AddAll(children);

    internal void Update(
        double animationValue, int currentIndex, int previousIndex, bool indexIsChanging,
        bool isScrollable, TabAlignment alignment, TabBarIndicatorSize indicatorSize,
        Color indicatorColor, double indicatorWeight, Thickness indicatorPadding,
        Thickness labelPadding, BoxDecoration? indicator, Color dividerColor, double dividerHeight,
        TabIndicatorAnimation indicatorAnimation, TextDirection textDirection,
        Action<IReadOnlyList<Rect>, double>? onLayout)
        => Update(animationValue, currentIndex, previousIndex, indexIsChanging, isScrollable, alignment,
            indicatorSize, indicatorColor, indicatorWeight, indicatorPadding, indicator, dividerColor,
            dividerHeight, indicatorAnimation, textDirection, onLayout, labelPadding);

    private void Update(
        double animationValue, int currentIndex, int previousIndex, bool indexIsChanging,
        bool isScrollable, TabAlignment alignment, TabBarIndicatorSize indicatorSize,
        Color indicatorColor, double indicatorWeight, Thickness indicatorPadding,
        BoxDecoration? indicator, Color dividerColor, double dividerHeight,
        TabIndicatorAnimation indicatorAnimation, TextDirection textDirection,
        Action<IReadOnlyList<Rect>, double>? onLayout, Thickness labelPadding)
    {
        bool layoutChanged = _isScrollable != isScrollable || _alignment != alignment || _textDirection != textDirection;
        bool indicatorGeometryChanged = Math.Abs(_animationValue - animationValue) > 0.000001
                                        || _indicatorSize != indicatorSize
                                        || Math.Abs(_indicatorWeight - indicatorWeight) > 0.000001
                                        || _indicatorPadding != indicatorPadding
                                        || _labelPadding != labelPadding
                                        || _indicatorAnimation != indicatorAnimation;
        _animationValue = animationValue;
        _currentIndex = currentIndex;
        _previousIndex = previousIndex;
        _indexIsChanging = indexIsChanging;
        _isScrollable = isScrollable;
        _alignment = alignment;
        _indicatorSize = indicatorSize;
        _indicatorColor = indicatorColor;
        _indicatorWeight = indicatorWeight;
        _indicatorPadding = indicatorPadding;
        _labelPadding = labelPadding;
        _indicator = indicator;
        _dividerColor = dividerColor;
        _dividerHeight = dividerHeight;
        _indicatorAnimation = indicatorAnimation;
        _textDirection = textDirection;
        _onLayout = onLayout;
        if (layoutChanged)
        {
            MarkNeedsLayout();
        }
        else if (indicatorGeometryChanged)
        {
            UpdateIndicatorRect();
        }
        MarkNeedsPaint();
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not TabBarParentData) child.parentData = new TabBarParentData();
    }

    protected override void PerformLayout()
    {
        _tabRects.Clear();
        _intrinsicWidths.Clear();
        if (ChildCount == 0)
        {
            Size = Constraints.Constrain(new Size(0, 0));
            return;
        }

        double maxHeight = Constraints.HasBoundedHeight ? Constraints.MaxHeight : double.PositiveInfinity;
        var probe = new BoxConstraints(MaxHeight: maxHeight);
        var widths = new List<double>(ChildCount);
        double height = 0.0;
        for (var child = FirstChild; child is not null; child = ChildAfter(child))
        {
            child.Layout(probe, parentUsesSize: true);
            widths.Add(child.Size.Width);
            _intrinsicWidths.Add(child.Size.Width);
            height = Math.Max(height, child.Size.Height);
        }

        double availableWidth = Constraints.HasBoundedWidth ? Constraints.MaxWidth : widths.Sum();
        bool fill = !_isScrollable && _alignment == TabAlignment.Fill;
        double totalWidth = fill ? availableWidth : widths.Sum();
        double start = _alignment == TabAlignment.Center && totalWidth < availableWidth
            ? (availableWidth - totalWidth) / 2
            : 0;
        Size = Constraints.Constrain(new Size(_isScrollable ? totalWidth : availableWidth, height));

        double x = start;
        int index = 0;
        for (var child = FirstChild; child is not null; child = ChildAfter(child), index++)
        {
            double width = fill ? availableWidth / ChildCount : widths[index];
            child.Layout(BoxConstraints.Tight(new Size(width, height)), parentUsesSize: true);
            double logicalX = _textDirection == TextDirection.Rtl ? Size.Width - x - width : x;
            ((TabBarParentData)child.parentData!).offset = new Point(logicalX, 0);
            _tabRects.Add(new Rect(logicalX, 0, width, height));
            x += width;
        }

        UpdateIndicatorRect();
        _onLayout?.Invoke(_tabRects, Size.Width);
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        _container.DefaultPaint(context, offset);
        if (_dividerHeight > 0 && _dividerColor.A > 0)
        {
            context.DrawRectangle(
                new SolidColorBrush(_dividerColor),
                null,
                new Rect(offset.X, offset.Y + Size.Height - _dividerHeight, Size.Width, _dividerHeight));
        }
        if (IndicatorRect is not { } indicatorRect) return;
        var translated = new Rect(indicatorRect.Position + offset, indicatorRect.Size);
        if (_indicator is { } decoration)
        {
            double radius = decoration.EffectiveBorderRadius.Radius;
            context.DrawRectangle(
                decoration.Brush ?? new SolidColorBrush(decoration.Color ?? Colors.Transparent),
                decoration.Border is { } border ? new Pen(new SolidColorBrush(border.Color), border.Width) : null,
                translated,
                radius,
                radius);
            return;
        }

        double radiusValue = _indicatorSize == TabBarIndicatorSize.Label ? _indicatorWeight : 0;
        context.DrawRectangle(new SolidColorBrush(_indicatorColor), null, translated, radiusValue, radiusValue);
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position) =>
        _container.DefaultHitTestChildren(result, position);
    public void DefaultPaint(PaintingContext context, Point offset) => _container.DefaultPaint(context, offset);
    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position) =>
        _container.DefaultHitTestChildren(result, position);

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (var child = FirstChild; child is not null; child = ChildAfter(child)) visitor(child);
    }

    internal override void VisitChildrenForSemantics(Action<RenderObject, Point, Matrix> visitor)
    {
        for (var child = FirstChild; child is not null; child = ChildAfter(child))
            visitor(child, ((TabBarParentData)child.parentData!).offset, Matrix.Identity);
    }

    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);
    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);
    public void Remove(RenderBox child) => _container.Remove(child);
    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after) => Insert((RenderBox)child, after as RenderBox);
    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after) => Move((RenderBox)child, after as RenderBox);
    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);

    private void UpdateIndicatorRect()
    {
        if (_tabRects.Count == 0) { IndicatorRect = null; return; }
        double value = Math.Clamp(_animationValue, 0, _tabRects.Count - 1);
        int from = Math.Clamp((int)Math.Floor(value), 0, _tabRects.Count - 1);
        int to = Math.Clamp((int)Math.Ceiling(value), 0, _tabRects.Count - 1);
        double progress = value - from;
        var fromRect = IndicatorBounds(from);
        var toRect = IndicatorBounds(to);
        double left;
        double right;
        if (_indicatorAnimation == TabIndicatorAnimation.Elastic && from != to)
        {
            bool movingRight = toRect.Center.X > fromRect.Center.X;
            double accelerate = 1 - Math.Cos(progress * Math.PI / 2);
            double decelerate = Math.Sin(progress * Math.PI / 2);
            left = Lerp(fromRect.Left, toRect.Left, movingRight ? accelerate : decelerate);
            right = Lerp(fromRect.Right, toRect.Right, movingRight ? decelerate : accelerate);
        }
        else
        {
            left = Lerp(fromRect.Left, toRect.Left, progress);
            right = Lerp(fromRect.Right, toRect.Right, progress);
        }

        IndicatorRect = new Rect(left, Size.Height - _indicatorWeight, Math.Max(0, right - left), _indicatorWeight);
    }

    private Rect IndicatorBounds(int index)
    {
        var rect = _tabRects[index];
        if (_indicatorSize == TabBarIndicatorSize.Label)
        {
            double intrinsicWidth = index < _intrinsicWidths.Count ? _intrinsicWidths[index] : rect.Width;
            double labelWidth = Math.Min(
                rect.Width,
                Math.Max(0, intrinsicWidth - _labelPadding.Left - _labelPadding.Right));
            rect = new Rect(rect.Center.X - (labelWidth / 2), rect.Y, labelWidth, rect.Height);
        }
        double left = rect.Left + _indicatorPadding.Left;
        double right = rect.Right - _indicatorPadding.Right;
        if (right < left) throw new InvalidOperationException("Indicator padding exceeds tab bounds.");
        return new Rect(left, rect.Top + _indicatorPadding.Top, right - left,
            Math.Max(0, rect.Height - _indicatorPadding.Top - _indicatorPadding.Bottom));
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);
}
