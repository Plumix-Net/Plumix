using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/mergeable_material.dart
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
}

public sealed class MaterialGap : MergeableMaterialItem
{
    public MaterialGap(LocalKey key, double size = 16) : base(key)
    {
        if (!double.IsFinite(size) || size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Material gap size must be finite and non-negative.");
        }

        Size = size;
    }

    public double Size { get; }
}

public sealed class MergeableMaterial : StatefulWidget
{
    public MergeableMaterial(
        IReadOnlyList<MergeableMaterialItem>? children = null,
        Axis mainAxis = Axis.Vertical,
        double elevation = 2,
        bool hasDividers = false,
        Color? dividerColor = null,
        Key? key = null) : base(key)
    {
        if (!double.IsFinite(elevation) || !MaterialElevation.HasDefinedShadow(elevation))
        {
            throw new ArgumentOutOfRangeException(
                nameof(elevation),
                "Elevation must be one of 0, 1, 2, 3, 4, 6, 8, 9, 12, 16, or 24.");
        }

        Children = children ?? [];
        MainAxis = mainAxis;
        Elevation = elevation;
        HasDividers = hasDividers;
        DividerColor = dividerColor;
        ValidateChildren(Children);
    }

    public IReadOnlyList<MergeableMaterialItem> Children { get; }

    public Axis MainAxis { get; }

    public double Elevation { get; }

    public bool HasDividers { get; }

    public Color? DividerColor { get; }

    public override State CreateState() => new MergeableMaterialState();

    private static void ValidateChildren(IReadOnlyList<MergeableMaterialItem> children)
    {
        var keys = new HashSet<LocalKey>();
        for (int i = 0; i < children.Count; i++)
        {
            if (!keys.Add(children[i].Key))
            {
                throw new ArgumentException("MergeableMaterial children must have unique keys.", nameof(children));
            }

            if (children[i] is MaterialGap
                && (i == 0 || i == children.Count - 1 || children[i - 1] is MaterialGap))
            {
                throw new ArgumentException(
                    "Material gaps cannot be first, last, or consecutive.",
                    nameof(children));
            }
        }
    }

    private sealed class MergeableMaterialState : State
    {
        private readonly Dictionary<LocalKey, GapAnimation> _gaps = [];

        private MergeableMaterial CurrentWidget => (MergeableMaterial)StateWidget;

        public override void InitState()
        {
            foreach (var descriptor in DescribeGaps(CurrentWidget.Children))
            {
                AddGap(descriptor, initiallyOpen: true);
            }
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var desired = DescribeGaps(CurrentWidget.Children).ToDictionary(gap => gap.Gap.Key);
            foreach (var descriptor in desired.Values)
            {
                if (_gaps.TryGetValue(descriptor.Gap.Key, out var existing))
                {
                    existing.Descriptor = descriptor;
                    existing.Controller.Forward();
                }
                else
                {
                    AddGap(descriptor, initiallyOpen: false);
                }
            }

            foreach (var entry in _gaps.Values.ToArray())
            {
                if (!desired.ContainsKey(entry.Descriptor.Gap.Key))
                {
                    entry.Controller.Reverse();
                }
            }
        }

        public override void Dispose()
        {
            foreach (var entry in _gaps.Values)
            {
                entry.Controller.Changed -= HandleAnimationChanged;
                entry.Controller.Dismissed -= HandleAnimationSettled;
                entry.Controller.Dispose();
            }

            _gaps.Clear();
        }

        public override Widget Build(BuildContext context)
        {
            var theme = Theme.Of(context);
            var slices = CurrentWidget.Children.OfType<MaterialSlice>().ToArray();
            var groups = new List<Widget>();
            var currentGroup = new List<MaterialSlice>();

            for (int i = 0; i < slices.Length; i++)
            {
                currentGroup.Add(slices[i]);
                if (i == slices.Length - 1)
                {
                    continue;
                }

                var gap = FindGapBetween(slices[i].Key, slices[i + 1].Key);
                if (gap is null || gap.Controller.Value <= 0.0001)
                {
                    continue;
                }

                groups.Add(BuildSliceGroup(currentGroup, theme));
                currentGroup = [];
                double extent = gap.Descriptor.Gap.Size * gap.Controller.Evaluate();
                groups.Add(CurrentWidget.MainAxis == Axis.Vertical
                    ? new SizedBox(height: extent)
                    : new SizedBox(width: extent));
            }

            if (currentGroup.Count > 0)
            {
                groups.Add(BuildSliceGroup(currentGroup, theme));
            }

            return CurrentWidget.MainAxis == Axis.Vertical
                ? new Column(
                    mainAxisSize: MainAxisSize.Min,
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    children: groups)
                : new Row(mainAxisSize: MainAxisSize.Min, children: groups);
        }

        private Widget BuildSliceGroup(IReadOnlyList<MaterialSlice> slices, ThemeData theme)
        {
            var children = new List<Widget>();
            for (int i = 0; i < slices.Count; i++)
            {
                if (i > 0 && CurrentWidget.HasDividers)
                {
                    children.Add(CurrentWidget.MainAxis == Axis.Vertical
                        ? new Divider(height: 0.5, thickness: 0.5, color: CurrentWidget.DividerColor)
                        : new VerticalDivider(width: 0.5, thickness: 0.5, color: CurrentWidget.DividerColor));
                }

                children.Add(new ColoredBox(slices[i].Color ?? theme.CardColor, slices[i].Child));
            }

            Widget body = CurrentWidget.MainAxis == Axis.Vertical
                ? new Column(
                    mainAxisSize: MainAxisSize.Min,
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    children: children)
                : new Row(mainAxisSize: MainAxisSize.Min, children: children);
            return new Material(
                type: MaterialType.Card,
                color: Colors.Transparent,
                elevation: CurrentWidget.Elevation,
                shadowColor: theme.ShadowColor,
                clipBehavior: Clip.AntiAlias,
                child: body);
        }

        private GapAnimation? FindGapBetween(LocalKey previous, LocalKey next)
        {
            return _gaps.Values
                .Where(entry => Equals(entry.Descriptor.PreviousSlice, previous)
                                && Equals(entry.Descriptor.NextSlice, next))
                .OrderByDescending(entry => entry.Controller.Value)
                .FirstOrDefault();
        }

        private void AddGap(GapDescriptor descriptor, bool initiallyOpen)
        {
            var controller = new AnimationController(TimeSpan.FromMilliseconds(200), this)
            {
                Curve = Curves.EaseInOut
            };
            controller.Changed += HandleAnimationChanged;
            controller.Dismissed += HandleAnimationSettled;
            var entry = new GapAnimation(descriptor, controller);
            _gaps.Add(descriptor.Gap.Key, entry);
            if (initiallyOpen)
            {
                controller.Forward(from: 1);
                controller.Stop();
            }
            else
            {
                controller.Forward(from: 0);
            }
        }

        private void HandleAnimationChanged()
        {
            SetState(() => { });
        }

        private void HandleAnimationSettled()
        {
            var dismissed = _gaps
                .Where(pair => pair.Value.Controller.Value <= 0.0001
                               && !CurrentWidget.Children.Any(item => Equals(item.Key, pair.Key)))
                .Select(pair => pair.Key)
                .ToArray();
            if (dismissed.Length == 0)
            {
                return;
            }

            SetState(() =>
            {
                foreach (var key in dismissed)
                {
                    var entry = _gaps[key];
                    entry.Controller.Changed -= HandleAnimationChanged;
                    entry.Controller.Dismissed -= HandleAnimationSettled;
                    entry.Controller.Dispose();
                    _gaps.Remove(key);
                }
            });
        }

        private static IEnumerable<GapDescriptor> DescribeGaps(IReadOnlyList<MergeableMaterialItem> children)
        {
            for (int i = 1; i < children.Count - 1; i++)
            {
                if (children[i] is MaterialGap gap
                    && children[i - 1] is MaterialSlice previous
                    && children[i + 1] is MaterialSlice next)
                {
                    yield return new GapDescriptor(gap, previous.Key, next.Key);
                }
            }
        }

        private sealed class GapAnimation(GapDescriptor descriptor, AnimationController controller)
        {
            public GapDescriptor Descriptor { get; set; } = descriptor;

            public AnimationController Controller { get; } = controller;
        }

        private sealed record GapDescriptor(MaterialGap Gap, LocalKey PreviousSlice, LocalKey NextSlice);
    }
}
