using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/mergeable_material.dart

internal static class MaterialElevation
{
    private static readonly HashSet<double> ShadowElevations = [0, 1, 2, 3, 4, 6, 8, 9, 12, 16, 24];

    public static bool HasDefinedShadow(double elevation) => ShadowElevations.Contains(elevation);
}

public abstract class MergeableMaterialItem
{
    protected MergeableMaterialItem(LocalKey key)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
    }

    public LocalKey Key { get; }
}

public sealed class MaterialSlice : MergeableMaterialItem
{
    public MaterialSlice(LocalKey key, Widget child, Color? color = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Color = color;
    }

    public Widget Child { get; }

    public Color? Color { get; }

    public override string ToString() => $"MergeableSlice(key: {Key}, child: {Child}, color: {Color})";
}

public sealed class MaterialGap : MergeableMaterialItem
{
    public MaterialGap(LocalKey key, double size = 16.0) : base(key)
    {
        Size = size;
    }

    public double Size { get; }

    public override string ToString() => $"MaterialGap(key: {Key}, child: {Size})";
}

public sealed class MergeableMaterial : StatefulWidget
{
    public MergeableMaterial(
        Axis mainAxis = Axis.Vertical,
        double elevation = 2.0,
        bool hasDividers = false,
        IReadOnlyList<MergeableMaterialItem>? children = null,
        Color? dividerColor = null,
        Key? key = null) : base(key)
    {
        MainAxis = mainAxis;
        Elevation = elevation;
        HasDividers = hasDividers;
        Children = children ?? [];
        DividerColor = dividerColor;
    }

    public IReadOnlyList<MergeableMaterialItem> Children { get; }

    public Axis MainAxis { get; }

    public double Elevation { get; }

    public bool HasDividers { get; }

    public Color? DividerColor { get; }

    public override State CreateState() => new MergeableMaterialState();

    private sealed class AnimationTuple : IDisposable
    {
        public AnimationTuple(AnimationController controller)
        {
            Controller = controller;
        }

        public AnimationController Controller { get; }

        public double GapStart { get; set; }

        public void Dispose()
        {
            Controller.Dispose();
        }
    }

    private sealed class MergeableMaterialState : State
    {
        private readonly Dictionary<LocalKey, AnimationTuple?> _animationTuples = [];
        private List<MergeableMaterialItem> _children = [];

        private MergeableMaterial CurrentWidget => (MergeableMaterial)StateWidget;

        public override void InitState()
        {
            _children = [.. CurrentWidget.Children];
            foreach (MergeableMaterialItem child in _children)
            {
                if (child is not MaterialGap gap)
                {
                    continue;
                }

                InitGap(gap);
                _animationTuples[gap.Key]!.Controller.SetValue(1.0);
            }

            Debug.Assert(GapsAreValid(_children));
        }

        public override void Dispose()
        {
            foreach (MergeableMaterialItem child in _children)
            {
                if (child is MaterialGap gap)
                {
                    _animationTuples[gap.Key]!.Dispose();
                }
            }

            _animationTuples.Clear();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var previous = (MergeableMaterial)oldWidget;
            var oldKeys = previous.Children.Select(child => child.Key).ToHashSet();
            var newKeys = CurrentWidget.Children.Select(child => child.Key).ToHashSet();
            var newOnly = newKeys.Except(oldKeys).ToHashSet();
            var oldOnly = oldKeys.Except(newKeys).ToHashSet();
            IReadOnlyList<MergeableMaterialItem> newChildren = CurrentWidget.Children;
            int i = 0;
            int j = 0;

            Debug.Assert(GapsAreValid(newChildren));
            RemoveEmptyGaps();

            while (i < newChildren.Count && j < _children.Count)
            {
                if (newOnly.Contains(newChildren[i].Key) || oldOnly.Contains(_children[j].Key))
                {
                    int startNew = i;
                    int startOld = j;

                    while (i < newChildren.Count && newOnly.Contains(newChildren[i].Key))
                    {
                        i++;
                    }

                    while (j < _children.Count && (oldOnly.Contains(_children[j].Key) || IsClosingGap(j)))
                    {
                        j++;
                    }

                    int newLength = i - startNew;
                    int oldLength = j - startOld;
                    ReconcileChangedRange(newChildren, startNew, startOld, newLength, oldLength, ref j);
                    continue;
                }

                if ((_children[j] is MaterialGap) == (newChildren[i] is MaterialGap))
                {
                    _children[j] = newChildren[i];
                    i++;
                    j++;
                }
                else
                {
                    Debug.Assert(_children[j] is MaterialGap);
                    j++;
                }
            }

            while (j < _children.Count)
            {
                RemoveChild(j);
            }

            while (i < newChildren.Count)
            {
                MergeableMaterialItem newChild = newChildren[i];
                InsertChild(j, newChild);
                if (newChild is MaterialGap)
                {
                    _animationTuples[newChild.Key]!.Controller.Forward();
                }

                i++;
                j++;
            }
        }

        public override Widget Build(BuildContext context)
        {
            RemoveEmptyGaps();

            var widgets = new List<Widget>();
            var slices = new List<Widget>();
            for (int i = 0; i < _children.Count; i++)
            {
                if (_children[i] is MaterialGap)
                {
                    Debug.Assert(slices.Count > 0);
                    widgets.Add(new ListBody(mainAxis: CurrentWidget.MainAxis, children: slices));
                    slices = [];
                    widgets.Add(CurrentWidget.MainAxis == Axis.Horizontal
                        ? new SizedBox(width: GetGapSize(i))
                        : new SizedBox(height: GetGapSize(i)));
                    continue;
                }

                var slice = (MaterialSlice)_children[i];
                Widget child = slice.Child;
                if (CurrentWidget.HasDividers)
                {
                    bool hasTopDivider = WillNeedDivider(i - 1);
                    bool hasBottomDivider = WillNeedDivider(i + 1);
                    BorderSide divider = Divider.CreateBorderSide(
                        context,
                        color: CurrentWidget.DividerColor,
                        width: 0.5);
                    Border border;
                    if (i == 0)
                    {
                        border = new Border(bottom: hasBottomDivider ? divider : BorderSide.None);
                    }
                    else if (i == _children.Count - 1)
                    {
                        border = new Border(top: hasTopDivider ? divider : BorderSide.None);
                    }
                    else
                    {
                        border = new Border(
                            top: hasTopDivider ? divider : BorderSide.None,
                            bottom: hasBottomDivider ? divider : BorderSide.None);
                    }

                    child = new AnimatedContainer(
                        duration: MaterialConstants.ThemeAnimationDuration,
                        key: new MergeableMaterialSliceKey(_children[i].Key),
                        decoration: new BoxDecoration(Border: border),
                        curve: Curves.FastOutSlowIn,
                        child: child);
                }

                slices.Add(new Container(
                    decoration: new BoxDecoration(
                        Color: slice.Color ?? Theme.Of(context).CardColor,
                        BorderRadius: ResolveBorderRadius(i, i == 0, i == _children.Count - 1)),
                    child: new Material(type: MaterialType.Transparency, child: child)));
            }

            if (slices.Count > 0)
            {
                widgets.Add(new ListBody(mainAxis: CurrentWidget.MainAxis, children: slices));
            }

            return new MergeableMaterialListBody(
                children: widgets,
                mainAxis: CurrentWidget.MainAxis,
                elevation: CurrentWidget.Elevation);
        }

        private void ReconcileChangedRange(
            IReadOnlyList<MergeableMaterialItem> newChildren,
            int startNew,
            int startOld,
            int newLength,
            int oldLength,
            ref int oldIndex)
        {
            if (newLength > 0)
            {
                ReconcileInsertedRange(newChildren, startNew, startOld, newLength, oldLength, ref oldIndex);
                return;
            }

            if (oldLength > 1 || oldLength == 1 && _children[startOld] is MaterialSlice)
            {
                double gapSizeSum = 0.0;
                while (startOld < oldIndex)
                {
                    if (_children[startOld] is MaterialGap gap)
                    {
                        gapSizeSum += gap.Size;
                    }

                    RemoveChild(startOld);
                    oldIndex--;
                }

                if (gapSizeSum != 0.0)
                {
                    var gap = new MaterialGap(new UniqueKey(), gapSizeSum);
                    InsertChild(startOld, gap);
                    AnimationTuple tuple = _animationTuples[gap.Key]!;
                    tuple.GapStart = 0.0;
                    tuple.Controller.SetValue(1.0);
                    tuple.Controller.Reverse();
                    oldIndex++;
                }

                return;
            }

            if (oldLength == 1)
            {
                var gap = (MaterialGap)_children[startOld];
                _animationTuples[gap.Key]!.GapStart = 0.0;
                _animationTuples[gap.Key]!.Controller.Reverse();
            }
        }

        private void ReconcileInsertedRange(
            IReadOnlyList<MergeableMaterialItem> newChildren,
            int startNew,
            int startOld,
            int newLength,
            int oldLength,
            ref int oldIndex)
        {
            if (oldLength > 1 || oldLength == 1 && _children[startOld] is MaterialSlice)
            {
                if (newLength == 1 && newChildren[startNew] is MaterialGap)
                {
                    double gapSizeSum = 0.0;
                    while (startOld < oldIndex)
                    {
                        if (_children[startOld] is MaterialGap gap)
                        {
                            gapSizeSum += gap.Size;
                        }

                        RemoveChild(startOld);
                        oldIndex--;
                    }

                    InsertChild(startOld, newChildren[startNew]);
                    AnimationTuple tuple = _animationTuples[newChildren[startNew].Key]!;
                    tuple.GapStart = gapSizeSum;
                    tuple.Controller.Forward();
                    oldIndex++;
                    return;
                }

                for (int k = 0; k < oldLength; k++)
                {
                    RemoveChild(startOld);
                }

                for (int k = 0; k < newLength; k++)
                {
                    InsertChild(startOld + k, newChildren[startNew + k]);
                }

                oldIndex += newLength - oldLength;
                return;
            }

            if (oldLength == 1)
            {
                if (newLength == 1
                    && newChildren[startNew] is MaterialGap
                    && _children[startOld].Key == newChildren[startNew].Key)
                {
                    _animationTuples[newChildren[startNew].Key]!.Controller.Forward();
                    return;
                }

                double gapSize = GetGapSize(startOld);
                RemoveChild(startOld);
                for (int k = 0; k < newLength; k++)
                {
                    InsertChild(startOld + k, newChildren[startNew + k]);
                }

                oldIndex += newLength - 1;
                double gapSizeSum = 0.0;
                for (int k = startNew; k < startNew + newLength; k++)
                {
                    if (newChildren[k] is MaterialGap gap)
                    {
                        gapSizeSum += gap.Size;
                    }
                }

                for (int k = startNew; k < startNew + newLength; k++)
                {
                    if (newChildren[k] is not MaterialGap gap)
                    {
                        continue;
                    }

                    AnimationTuple tuple = _animationTuples[gap.Key]!;
                    tuple.GapStart = gapSizeSum == 0.0 ? 0.0 : gapSize * gap.Size / gapSizeSum;
                    tuple.Controller.SetValue(0.0);
                    tuple.Controller.Forward();
                }

                return;
            }

            for (int k = 0; k < newLength; k++)
            {
                MergeableMaterialItem newChild = newChildren[startNew + k];
                InsertChild(startOld + k, newChild);
                if (newChild is MaterialGap)
                {
                    _animationTuples[newChild.Key]!.Controller.Forward();
                }
            }

            oldIndex += newLength;
        }

        private void InitGap(MaterialGap gap)
        {
            var controller = new AnimationController(MaterialConstants.ThemeAnimationDuration, this)
            {
                Curve = Curves.FastOutSlowIn
            };
            controller.Changed += HandleTick;
            _animationTuples[gap.Key] = new AnimationTuple(controller);
        }

        private void InsertChild(int index, MergeableMaterialItem child)
        {
            _children.Insert(index, child);
            if (child is MaterialGap gap)
            {
                InitGap(gap);
            }
        }

        private void RemoveChild(int index)
        {
            MergeableMaterialItem child = _children[index];
            _children.RemoveAt(index);
            if (child is not MaterialGap gap)
            {
                return;
            }

            AnimationTuple? tuple = _animationTuples[gap.Key];
            if (tuple is not null)
            {
                tuple.Controller.Changed -= HandleTick;
                tuple.Dispose();
            }

            _animationTuples[gap.Key] = null;
        }

        private bool IsClosingGap(int index)
        {
            return index < _children.Count - 1
                   && _children[index] is MaterialGap gap
                   && _animationTuples[gap.Key]!.Controller.Status == AnimationStatus.Reverse;
        }

        private void RemoveEmptyGaps()
        {
            for (int index = _children.Count - 1; index >= 0; index--)
            {
                if (_children[index] is MaterialGap gap
                    && _animationTuples[gap.Key]!.Controller.Status == AnimationStatus.Dismissed)
                {
                    RemoveChild(index);
                }
            }
        }

        private BorderRadius ResolveBorderRadius(int index, bool start, bool end)
        {
            BorderRadius cardBorderRadius = MaterialEdges.ForType(MaterialType.Card) ?? BorderRadius.Zero;
            double cardRadius = cardBorderRadius.Radius;
            double startRadius = 0.0;
            double endRadius = 0.0;

            if (index > 0 && _children[index - 1] is MaterialGap startGap)
            {
                startRadius = cardRadius * _animationTuples[startGap.Key]!.Controller.Evaluate();
            }

            if (index < _children.Count - 2 && _children[index + 1] is MaterialGap endGap)
            {
                endRadius = cardRadius * _animationTuples[endGap.Key]!.Controller.Evaluate();
            }

            if (CurrentWidget.MainAxis == Axis.Vertical)
            {
                double top = start ? cardRadius : startRadius;
                double bottom = end ? cardRadius : endRadius;
                return BorderRadius.Only(top, top, bottom, bottom);
            }

            double left = start ? cardRadius : startRadius;
            double right = end ? cardRadius : endRadius;
            return BorderRadius.Only(left, right, right, left);
        }

        private double GetGapSize(int index)
        {
            var gap = (MaterialGap)_children[index];
            AnimationTuple tuple = _animationTuples[gap.Key]!;
            double t = tuple.Controller.Evaluate();
            return tuple.GapStart + ((gap.Size - tuple.GapStart) * t);
        }

        private bool WillNeedDivider(int index)
        {
            return index >= 0
                   && index < _children.Count
                   && (_children[index] is MaterialSlice || IsClosingGap(index));
        }

        private static bool GapsAreValid(IReadOnlyList<MergeableMaterialItem> children)
        {
            for (int i = 0; i < children.Count - 1; i++)
            {
                if (children[i] is MaterialGap && children[i + 1] is MaterialGap)
                {
                    return false;
                }
            }

            return children.Count == 0
                   || children[0] is not MaterialGap && children[^1] is not MaterialGap;
        }

        private void HandleTick()
        {
            SetState(() => { });
        }
    }
}

internal sealed record MergeableMaterialSliceKey(LocalKey Value) : GlobalKey;

internal sealed class MergeableMaterialListBody : MultiChildRenderObjectWidget
{
    public MergeableMaterialListBody(
        IReadOnlyList<Widget> children,
        Axis mainAxis,
        double elevation,
        Key? key = null) : base(children, key)
    {
        MainAxis = mainAxis;
        Elevation = elevation;
    }

    public Axis MainAxis { get; }

    public double Elevation { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderMergeableMaterialListBody(ResolveAxisDirection(context), Elevation);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var listBody = (RenderMergeableMaterialListBody)renderObject;
        listBody.AxisDirection = ResolveAxisDirection(context);
        listBody.Elevation = Elevation;
    }

    private AxisDirection ResolveAxisDirection(BuildContext context)
    {
        if (MainAxis == Axis.Vertical)
        {
            return AxisDirection.Down;
        }

        return Directionality.Of(context) == TextDirection.Rtl
            ? AxisDirection.Left
            : AxisDirection.Right;
    }
}

internal sealed class RenderMergeableMaterialListBody : RenderListBody
{
    private double _elevation;

    public RenderMergeableMaterialListBody(
        AxisDirection axisDirection = AxisDirection.Down,
        double elevation = 0.0) : base(axisDirection)
    {
        _elevation = elevation;
    }

    public double Elevation
    {
        get => _elevation;
        set
        {
            if (_elevation == value)
            {
                return;
            }

            _elevation = value;
            MarkNeedsPaint();
        }
    }

    protected override void PerformLayout()
    {
        double mainAxisExtent = 0.0;
        RenderBox? child = FirstChild;
        if (MainAxis == Axis.Horizontal)
        {
            double crossAxisExtent = Constraints.HasBoundedHeight
                ? Constraints.MaxHeight
                : MeasureCrossAxis(horizontal: true);
            var childConstraints = BoxConstraints.TightFor(height: crossAxisExtent);
            while (child is not null)
            {
                child.Layout(childConstraints, parentUsesSize: true);
                ((ListBodyParentData)child.parentData!).offset = new Point(mainAxisExtent, 0.0);
                mainAxisExtent += child.Size.Width;
                child = ChildAfter(child);
            }

            Size = Constraints.Constrain(new Size(mainAxisExtent, crossAxisExtent));
            if (AxisDirection == AxisDirection.Left)
            {
                ReverseOffsets(horizontal: true, mainAxisExtent);
            }

            return;
        }

        double verticalCrossAxisExtent = Constraints.HasBoundedWidth
            ? Constraints.MaxWidth
            : MeasureCrossAxis(horizontal: false);
        var verticalChildConstraints = BoxConstraints.TightFor(width: verticalCrossAxisExtent);
        while (child is not null)
        {
            child.Layout(verticalChildConstraints, parentUsesSize: true);
            ((ListBodyParentData)child.parentData!).offset = new Point(0.0, mainAxisExtent);
            mainAxisExtent += child.Size.Height;
            child = ChildAfter(child);
        }

        Size = Constraints.Constrain(new Size(verticalCrossAxisExtent, mainAxisExtent));
        if (AxisDirection == AxisDirection.Up)
        {
            ReverseOffsets(horizontal: false, mainAxisExtent);
        }
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        RenderBox? child = FirstChild;
        int index = 0;
        while (child is not null)
        {
            var parentData = (ListBodyParentData)child.parentData!;
            var rect = new Rect(parentData.offset + offset, child.Size);
            if (index % 2 == 0 && Elevation != 0.0)
            {
                double radius = (MaterialEdges.ForType(MaterialType.Card) ?? BorderRadius.Zero).Radius;
                context.DrawShadow(
                    new RectangleGeometry(rect, radius, radius),
                    Colors.Black,
                    Elevation,
                    transparentOccluder: true);
            }

            child = ChildAfter(child);
            index++;
        }

        DefaultPaint(context, offset);
    }

    private double MeasureCrossAxis(bool horizontal)
    {
        double extent = 0.0;
        var probe = new BoxConstraints();
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            child.Layout(probe, parentUsesSize: true);
            extent = Math.Max(extent, horizontal ? child.Size.Height : child.Size.Width);
        }

        return extent;
    }

    private void ReverseOffsets(bool horizontal, double mainAxisExtent)
    {
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            var parentData = (ListBodyParentData)child.parentData!;
            parentData.offset = horizontal
                ? new Point(mainAxisExtent - parentData.offset.X - child.Size.Width, 0.0)
                : new Point(0.0, mainAxisExtent - parentData.offset.Y - child.Size.Height);
        }
    }
}
