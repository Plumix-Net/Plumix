using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/sliver.dart
// Dart parity source: flutter/packages/flutter/lib/src/widgets/scroll_notification.dart
// Dart parity source: flutter/packages/flutter/lib/src/widgets/automatic_keep_alive.dart
// Dart parity source: flutter/packages/flutter/lib/src/widgets/scroll_controller.dart
// Dart parity source: flutter/packages/flutter/lib/src/widgets/sliver_prototype_extent_list.dart

namespace Plumix.Widgets;

public delegate Widget IndexedWidgetBuilder(BuildContext context, int index);

public delegate int? ChildIndexGetter(Key key);

public abstract class ScrollNotification : LayoutChangedNotification, IViewportNotification
{
    protected ScrollNotification(
        IScrollMetrics metrics,
        int depth = 0,
        BuildContext? sourceContext = null)
    {
        Metrics = metrics;
        Depth = Math.Max(0, depth);
        if (sourceContext is BuildContext context)
        {
            SetContext(context);
        }
    }

    public IScrollMetrics Metrics { get; }

    public int Depth { get; private set; }

    void IViewportNotification.IncrementDepth()
    {
        Depth += 1;
    }
}

public sealed class ScrollMetricsNotification : Notification, IViewportNotification
{
    public ScrollMetricsNotification(
        IScrollMetrics metrics,
        BuildContext context,
        int depth = 0)
    {
        Metrics = metrics;
        Depth = Math.Max(0, depth);
        SetContext(context);
    }

    public IScrollMetrics Metrics { get; }

    public int Depth { get; private set; }

    public ScrollUpdateNotification AsScrollUpdate()
    {
        BuildContext sourceContext = Context
                                     ?? throw new InvalidOperationException(
                                         "ScrollMetricsNotification requires a source context.");
        return new ScrollUpdateNotification(
            Metrics,
            depth: Depth,
            sourceContext: sourceContext);
    }

    void IViewportNotification.IncrementDepth()
    {
        Depth += 1;
    }
}

public sealed class ScrollStartNotification : ScrollNotification
{
    public ScrollStartNotification(
        IScrollMetrics metrics,
        DragStartDetails? dragDetails = null,
        int depth = 0,
        BuildContext? sourceContext = null) : base(metrics, depth, sourceContext)
    {
        DragDetails = dragDetails;
    }

    public ScrollStartNotification(
        IScrollMetrics metrics,
        bool hasDragDetails,
        int depth = 0) : this(
        metrics,
        hasDragDetails ? new DragStartDetails(default) : null,
        depth)
    {
    }

    public DragStartDetails? DragDetails { get; }

    public bool HasDragDetails => DragDetails.HasValue;
}

public sealed class ScrollUpdateNotification : ScrollNotification
{
    public ScrollUpdateNotification(
        IScrollMetrics metrics,
        DragUpdateDetails? dragDetails = null,
        double? scrollDelta = null,
        int depth = 0,
        BuildContext? sourceContext = null) : base(metrics, depth, sourceContext)
    {
        DragDetails = dragDetails;
        ScrollDelta = scrollDelta;
    }

    public ScrollUpdateNotification(
        IScrollMetrics metrics,
        double? scrollDelta,
        bool hasDragDetails,
        int depth = 0) : this(
        metrics,
        hasDragDetails
            ? new DragUpdateDetails(default, default, default, 0.0)
            : null,
        scrollDelta,
        depth)
    {
    }

    public DragUpdateDetails? DragDetails { get; }

    public double? ScrollDelta { get; }

    public bool HasDragDetails => DragDetails.HasValue;
}

public sealed class OverscrollNotification : ScrollNotification
{
    public OverscrollNotification(
        IScrollMetrics metrics,
        double overscroll,
        DragUpdateDetails? dragDetails = null,
        double velocity = 0.0,
        int depth = 0,
        BuildContext? sourceContext = null) : base(metrics, depth, sourceContext)
    {
        if (!double.IsFinite(overscroll) || Math.Abs(overscroll) <= double.Epsilon)
        {
            throw new ArgumentOutOfRangeException(nameof(overscroll));
        }

        if (!double.IsFinite(velocity))
        {
            throw new ArgumentOutOfRangeException(nameof(velocity));
        }

        Overscroll = overscroll;
        DragDetails = dragDetails;
        Velocity = velocity;
    }

    public OverscrollNotification(
        IScrollMetrics metrics,
        double overscroll,
        bool hasDragDetails,
        int depth = 0) : this(
        metrics,
        overscroll,
        hasDragDetails
            ? new DragUpdateDetails(default, default, default, 0.0)
            : null,
        velocity: 0.0,
        depth)
    {
    }

    public double Overscroll { get; }

    public DragUpdateDetails? DragDetails { get; }

    public double Velocity { get; }

    public bool HasDragDetails => DragDetails.HasValue;
}

public sealed class ScrollEndNotification : ScrollNotification
{
    public ScrollEndNotification(
        IScrollMetrics metrics,
        DragEndDetails? dragDetails = null,
        int depth = 0,
        BuildContext? sourceContext = null) : base(metrics, depth, sourceContext)
    {
        DragDetails = dragDetails;
    }

    public ScrollEndNotification(
        IScrollMetrics metrics,
        int depth) : this(metrics, dragDetails: null, depth)
    {
    }

    public DragEndDetails? DragDetails { get; }
}

/// <summary>
/// A notification that the user has changed the direction in which they are scrolling.
/// </summary>
public sealed class UserScrollNotification : ScrollNotification
{
    public UserScrollNotification(
        IScrollMetrics metrics,
        ScrollDirection direction,
        int depth = 0,
        BuildContext? sourceContext = null) : base(metrics, depth, sourceContext)
    {
        Direction = direction;
    }

    /// <summary>The direction in which the user is scrolling.</summary>
    public ScrollDirection Direction { get; }
}

public sealed class KeepAliveNotification : Notification
{
    public KeepAliveNotification(KeepAliveHandle handle)
    {
        Handle = handle;
    }

    public KeepAliveHandle Handle { get; }
}

public sealed class KeepAliveHandle : ChangeNotifier
{
    private bool _released;

    public bool IsReleased => _released;

    public void Release()
    {
        if (_released)
        {
            return;
        }

        _released = true;
        NotifyListeners();
        base.Dispose();
    }

    public override void Dispose()
    {
        Release();
    }
}

public sealed class AutomaticKeepAlive : StatefulWidget
{
    public AutomaticKeepAlive(Widget child, Key? key = null) : base(key)
    {
        Child = child;
    }

    public Widget Child { get; }

    public override State CreateState()
    {
        return new AutomaticKeepAliveState();
    }

    private sealed class AutomaticKeepAliveState : State
    {
        private readonly Dictionary<KeepAliveHandle, Action> _releaseCallbacks = [];
        private bool _keepingAlive;

        private AutomaticKeepAlive CurrentWidget => (AutomaticKeepAlive)Element.Widget;

        public override Widget Build(BuildContext context)
        {
            return new NotificationListener<KeepAliveNotification>(
                onNotification: HandleKeepAliveNotification,
                child: new KeepAlive(
                    keepAlive: _keepingAlive,
                    child: CurrentWidget.Child));
        }

        public override void Dispose()
        {
            foreach (var (handle, callback) in _releaseCallbacks.ToArray())
            {
                handle.RemoveListener(callback);
            }

            _releaseCallbacks.Clear();

            base.Dispose();
        }

        private bool HandleKeepAliveNotification(KeepAliveNotification notification)
        {
            var handle = notification.Handle;
            if (!_releaseCallbacks.ContainsKey(handle))
            {
                Action callback = () => HandleReleased(handle);
                _releaseCallbacks[handle] = callback;
                handle.AddListener(callback);
            }

            if (!_keepingAlive)
            {
                SetState(() => _keepingAlive = true);
            }

            return true;
        }

        private void HandleReleased(KeepAliveHandle handle)
        {
            if (!_releaseCallbacks.Remove(handle, out var callback))
            {
                return;
            }

            handle.RemoveListener(callback);
            if (_releaseCallbacks.Count == 0 && _keepingAlive)
            {
                SetState(() => _keepingAlive = false);
            }
        }
    }
}

public abstract class AutomaticKeepAliveClientMixin : State
{
    private KeepAliveHandle? _keepAliveHandle;

    protected abstract bool WantKeepAlive { get; }

    public void UpdateKeepAlive()
    {
        if (WantKeepAlive)
        {
            EnsureKeepAlive();
        }
        else
        {
            ReleaseKeepAlive();
        }
    }

    protected void EnsureKeepAlive()
    {
        if (_keepAliveHandle != null)
        {
            return;
        }

        var handle = new KeepAliveHandle();
        _keepAliveHandle = handle;
        new KeepAliveNotification(handle).Dispatch(Context);
    }

    public override void InitState()
    {
        base.InitState();
        if (WantKeepAlive)
        {
            EnsureKeepAlive();
        }
    }

    public override void Deactivate()
    {
        ReleaseKeepAlive();
        base.Deactivate();
    }

    public override void Dispose()
    {
        ReleaseKeepAlive();
        base.Dispose();
    }

    private void ReleaseKeepAlive()
    {
        var handle = _keepAliveHandle;
        if (handle == null)
        {
            return;
        }

        _keepAliveHandle = null;
        handle.Release();
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/widgets/primary_scroll_controller.dart
public sealed class PrimaryScrollController : InheritedWidget
{
    private static readonly IReadOnlySet<TargetPlatform> MobilePlatforms = new HashSet<TargetPlatform>
    {
        TargetPlatform.Android,
        TargetPlatform.IOS,
        TargetPlatform.Fuchsia,
    };

    public PrimaryScrollController(
        ScrollController? controller,
        Widget child,
        IReadOnlySet<TargetPlatform>? automaticallyInheritForPlatforms = null,
        Axis? scrollDirection = Axis.Vertical,
        Key? key = null) : base(key)
    {
        Controller = controller;
        Child = child ?? throw new ArgumentNullException(nameof(child));
        AutomaticallyInheritForPlatforms = automaticallyInheritForPlatforms ?? MobilePlatforms;
        ScrollDirection = scrollDirection;
    }

    public ScrollController? Controller { get; }

    public Widget Child { get; }

    public IReadOnlySet<TargetPlatform> AutomaticallyInheritForPlatforms { get; }

    public Axis? ScrollDirection { get; }

    public static PrimaryScrollController None(Widget child, Key? key = null)
    {
        return new PrimaryScrollController(
            controller: null,
            child: child,
            automaticallyInheritForPlatforms: new HashSet<TargetPlatform>(),
            scrollDirection: null,
            key: key);
    }

    public static bool ShouldInherit(BuildContext context, Axis scrollDirection)
    {
        PrimaryScrollController? result = context.FindAncestorWidgetOfExactType<PrimaryScrollController>();
        if (result == null)
        {
            return false;
        }

        TargetPlatform platform = ScrollConfiguration.Of(context).GetPlatform(context);
        return result.AutomaticallyInheritForPlatforms.Contains(platform)
               && result.ScrollDirection == scrollDirection;
    }

    public static ScrollController? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<PrimaryScrollController>()?.Controller;
    }

    public static ScrollController Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException("PrimaryScrollController not found in context.");
    }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(((PrimaryScrollController)oldWidget).Controller, Controller);
    }
}

public class ScrollController : ChangeNotifier
{
    private readonly List<ScrollPosition> _positions = [];

    public ScrollController(
        double initialScrollOffset = 0.0,
        ScrollPhysics? physics = null,
        bool keepScrollOffset = true,
        string? debugLabel = null,
        Action<ScrollPosition>? onAttach = null,
        Action<ScrollPosition>? onDetach = null)
    {
        InitialScrollOffset = initialScrollOffset;
        KeepScrollOffset = keepScrollOffset;
        DebugLabel = debugLabel;
        OnAttach = onAttach;
        OnDetach = onDetach;
        Physics = physics ?? new ClampingScrollPhysics();
    }

    public double InitialScrollOffset { get; }

    public bool KeepScrollOffset { get; }

    /// <summary>A label that is used in the <see cref="ToString"/> output. Intended to aid with
    /// identifying scroll controller instances in debug output.</summary>
    public string? DebugLabel { get; }

    /// <summary>Called when a <see cref="ScrollPosition"/> is attached to the scroll controller.</summary>
    public Action<ScrollPosition>? OnAttach { get; }

    /// <summary>Called when a <see cref="ScrollPosition"/> is detached from the scroll controller.</summary>
    public Action<ScrollPosition>? OnDetach { get; }

    public ScrollPhysics Physics { get; }

    public bool HasClients => _positions.Count > 0;

    public IReadOnlyList<ScrollPosition> Positions => _positions;

    public double Offset => _positions.Count == 0 ? InitialScrollOffset : _positions[0].Pixels;

    public ScrollPosition? PrimaryPosition => _positions.Count == 0 ? null : _positions[0];

    public ScrollPosition Position => _positions.Count == 1
        ? _positions[0]
        : throw new InvalidOperationException(
            $"ScrollController.Position requires exactly one attached ScrollPosition; found {_positions.Count}.");

    /// <summary>
    /// Creates a <see cref="ScrollPosition"/> for use by a <see cref="Scrollable"/> widget.
    /// </summary>
    /// <remarks>
    /// Subclasses can override this function to customize the <see cref="ScrollPosition"/> used by
    /// the scrollable widgets they control. For example, <see cref="PageController"/> overrides this
    /// function to return a page-oriented scroll position subclass that keeps the same page visible
    /// when the scrollable widget resizes.
    /// <para>
    /// The <paramref name="context"/> is the scrollable's <see cref="IScrollContext"/>; the
    /// <paramref name="oldPosition"/> is the position that is being replaced, if any, whose state the
    /// new one absorbs.
    /// </para>
    /// </remarks>
    public virtual ScrollPosition CreateScrollPosition(
        ScrollPhysics physics,
        IScrollContext context,
        ScrollPosition? oldPosition)
    {
        return new ScrollPositionWithSingleContext(
            physics: physics,
            context: context,
            initialPixels: InitialScrollOffset,
            keepScrollOffset: KeepScrollOffset,
            oldPosition: oldPosition,
            debugLabel: DebugLabel);
    }

    internal virtual void Attach(ScrollPosition position)
    {
        if (_positions.Contains(position))
        {
            return;
        }

        _positions.Add(position);
        position.AddListener(NotifyListeners);
        OnAttach?.Invoke(position);
    }

    internal virtual void Detach(ScrollPosition position)
    {
        if (!_positions.Contains(position))
        {
            return;
        }

        OnDetach?.Invoke(position);
        position.RemoveListener(NotifyListeners);
        _positions.Remove(position);
    }

    public void JumpTo(double value)
    {
        foreach (var position in _positions.ToArray())
        {
            position.JumpTo(value);
        }
    }

    public void AnimateTo(double value, TimeSpan duration, Curve? curve = null)
    {
        foreach (var position in _positions.ToArray())
        {
            position.AnimateTo(value, duration, curve);
        }
    }

    public override void Dispose()
    {
        foreach (var position in _positions.ToArray())
        {
            position.RemoveListener(NotifyListeners);
        }

        _positions.Clear();
        base.Dispose();
    }

    public override string ToString()
    {
        var description = new List<string>();
        DebugFillDescription(description);
        return $"{Diagnostics.DescribeIdentity(this)}({string.Join(", ", description)})";
    }

    /// <summary>Add additional information to the given description for use by
    /// <see cref="ToString"/>.</summary>
    protected virtual void DebugFillDescription(List<string> description)
    {
        if (DebugLabel != null)
        {
            description.Add(DebugLabel);
        }

        if (InitialScrollOffset != 0.0)
        {
            description.Add(
                $"initialScrollOffset: {InitialScrollOffset.ToString("F1", CultureInfo.InvariantCulture)}, ");
        }

        if (_positions.Count == 0)
        {
            description.Add("no clients");
        }
        else if (_positions.Count == 1)
        {
            // Don't actually list the client itself, since its toString may refer to us.
            description.Add($"one client, offset {Offset.ToString("F1", CultureInfo.InvariantCulture)}");
        }
        else
        {
            description.Add($"{_positions.Count} clients");
        }
    }
}

public sealed class SliverToBoxAdapter : SingleChildRenderObjectWidget
{
    public SliverToBoxAdapter(Widget? child = null, Key? key = null) : base(child, key)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverToBoxAdapter();
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/widgets/sliver.dart
public sealed class SliverIgnorePointer : SingleChildRenderObjectWidget
{
    public SliverIgnorePointer(
        Widget? sliver = null,
        bool ignoring = true,
        bool? ignoringSemantics = null,
        Key? key = null) : base(sliver, key)
    {
        Ignoring = ignoring;
        IgnoringSemantics = ignoringSemantics;
    }

    public bool Ignoring { get; }

    public bool? IgnoringSemantics { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverIgnorePointer(
            ignoring: Ignoring,
            ignoringSemantics: IgnoringSemantics);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var ignorePointer = (RenderSliverIgnorePointer)renderObject;
        ignorePointer.Ignoring = Ignoring;
        ignorePointer.IgnoringSemantics = IgnoringSemantics;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<bool>("ignoring", Ignoring));
        properties.Add(new DiagnosticsProperty<bool?>("ignoringSemantics", IgnoringSemantics, defaultValue: null));
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/widgets/sliver.dart
public sealed class SliverOffstage : SingleChildRenderObjectWidget
{
    public SliverOffstage(
        Widget? sliver = null,
        bool offstage = true,
        Key? key = null) : base(sliver, key)
    {
        Offstage = offstage;
    }

    public bool Offstage { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverOffstage(offstage: Offstage);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderSliverOffstage)renderObject).Offstage = Offstage;
    }

    public override Element CreateElement() => new SliverOffstageElement(this);

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<bool>("offstage", Offstage));
    }
}

/// <summary>Hides an offstage sliver's children from the debug on-stage walk.</summary>
/// <remarks>Flutter's private <c>_SliverOffstageElement</c>.</remarks>
internal sealed class SliverOffstageElement : SingleChildRenderObjectElement
{
    public SliverOffstageElement(SliverOffstage widget) : base(widget)
    {
    }

    public override void DebugVisitOnstageChildren(Action<Element> visitor)
    {
        if (!((SliverOffstage)Widget).Offstage)
        {
            base.DebugVisitOnstageChildren(visitor);
        }
    }
}

// Dart parity source: flutter/packages/flutter/lib/src/widgets/sliver.dart
public sealed class SliverOpacity : SingleChildRenderObjectWidget
{
    public SliverOpacity(
        double opacity,
        Widget? sliver = null,
        bool alwaysIncludeSemantics = false,
        Key? key = null) : base(sliver, key)
    {
        if (!double.IsFinite(opacity) || opacity < 0.0 || opacity > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity), "Opacity must be between zero and one.");
        }

        Opacity = opacity;
        AlwaysIncludeSemantics = alwaysIncludeSemantics;
    }

    public double Opacity { get; }

    public bool AlwaysIncludeSemantics { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverOpacity(
            opacity: Opacity,
            alwaysIncludeSemantics: AlwaysIncludeSemantics);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var opacity = (RenderSliverOpacity)renderObject;
        opacity.Opacity = Opacity;
        opacity.AlwaysIncludeSemantics = AlwaysIncludeSemantics;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<double>("opacity", Opacity));
        properties.Add(new FlagProperty(
            "alwaysIncludeSemantics",
            value: AlwaysIncludeSemantics,
            ifTrue: "alwaysIncludeSemantics"));
    }
}

/// <summary>
/// Ensures its sliver child is included in the semantics tree even when it is outside the viewport
/// and its cache extent.
/// </summary>
/// <remarks>
/// The child may still be excluded when its <see cref="RenderSliver.SemanticBounds"/> is invalid,
/// and this widget does not guarantee that its child is laid out.
/// </remarks>
public sealed class SliverEnsureSemantics : SingleChildRenderObjectWidget
{
    public SliverEnsureSemantics(Widget sliver, Key? key = null) : base(sliver, key)
    {
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverEnsureSemantics();
    }
}

/// <remarks>Flutter's private <c>_RenderSliverEnsureSemantics</c>.</remarks>
internal sealed class RenderSliverEnsureSemantics : RenderProxySliver
{
    public override bool EnsureSemantics => true;
}

public sealed class SliverPadding : SingleChildRenderObjectWidget
{
    public SliverPadding(Thickness padding, Widget? sliver = null, Key? key = null) : base(sliver, key)
    {
        Padding = padding;
    }

    public Thickness Padding { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverPadding(Padding);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderSliverPadding)renderObject).Padding = Padding;
    }
}

public sealed class KeepAlive : ParentDataWidget<IKeepAliveParentData>
{
    public KeepAlive(
        bool keepAlive,
        Widget child,
        Key? key = null) : base(child, key)
    {
        Value = keepAlive;
    }

    public bool Value { get; }

    /// <remarks>
    /// Dart's <c>debugTypicalAncestorWidgetClass</c> throws here, because two ancestor types are
    /// valid; the name it reports comes from <see cref="DebugTypicalAncestorWidgetDescription"/>
    /// instead. C# keeps the type non-throwing and names the first of the two.
    /// </remarks>
    public override Type DebugTypicalAncestorWidgetType => typeof(SliverWithKeepAliveWidget);

    /// <inheritdoc />
    public override string DebugTypicalAncestorWidgetDescription =>
        "SliverWithKeepAliveWidget or TwoDimensionalViewport";

    /// <summary>
    /// Turning keep-alive <em>on</em> needs no layout — the child is alive already — so the write is
    /// allowed outside a build, which is what lets an <see cref="AutomaticKeepAlive"/> handle claim a
    /// child mid-layout.
    /// </summary>
    /// <remarks>Flutter's <c>KeepAlive.debugCanApplyOutOfTurn</c>.</remarks>
    public override bool DebugCanApplyOutOfTurn() => Value;

    protected override void ApplyParentData(RenderObject renderObject)
    {
        Debug.Assert(renderObject.parentData is IKeepAliveParentData);
        var parentData = (IKeepAliveParentData)renderObject.parentData!;
        if (parentData.KeepAlive == Value)
        {
            return;
        }

        parentData.KeepAlive = Value;
        if (!Value)
        {
            renderObject.Parent?.MarkNeedsLayout();
        }
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<bool>("keepAlive", Value));
    }
}

/// <summary>
/// A base class for slivers that have <see cref="KeepAlive"/> children.
/// </summary>
/// <remarks>Flutter's <c>SliverWithKeepAliveWidget</c>; its render object mixes in keep-alive support.</remarks>
public abstract class SliverWithKeepAliveWidget : RenderObjectWidget
{
    protected SliverWithKeepAliveWidget(Key? key = null) : base(key)
    {
    }
}

public abstract class SliverMultiBoxAdaptorWidget : SliverWithKeepAliveWidget
{
    protected SliverMultiBoxAdaptorWidget(SliverChildDelegate @delegate, Key? key = null) : base(key)
    {
        Delegate = @delegate;
    }

    public SliverChildDelegate Delegate { get; }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<SliverChildDelegate>("delegate", Delegate));
    }

    /// <summary>
    /// An estimate of the max scroll extent for all the children, or null to let the element
    /// extrapolate it. Subclasses override this when they know more than the delegate does.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>SliverMultiBoxAdaptorWidget.estimateMaxScrollOffset</c>: the default defers to
    /// <see cref="SliverChildDelegate.EstimateMaxScrollOffset"/>.
    /// </remarks>
    public virtual double? EstimateMaxScrollOffset(
        SliverConstraints? constraints,
        int firstIndex,
        int lastIndex,
        double leadingScrollOffset,
        double trailingScrollOffset)
    {
        Debug.Assert(lastIndex >= firstIndex);
        return Delegate.EstimateMaxScrollOffset(
            firstIndex,
            lastIndex,
            leadingScrollOffset,
            trailingScrollOffset);
    }

    public override Element CreateElement()
    {
        return new SliverMultiBoxAdaptorElement(this);
    }
}

internal class SliverMultiBoxAdaptorElement : RenderObjectElement, IRenderSliverBoxChildManager
{
    private readonly SortedDictionary<int, Element?> _childElements = [];
    private readonly bool _replaceMovedChildren;
    private RenderBox? _currentBeforeChild;
    private int? _currentlyUpdatingChildIndex;
    private bool _didUnderflow;

    public SliverMultiBoxAdaptorElement(SliverMultiBoxAdaptorWidget widget, bool replaceMovedChildren = false)
        : base(widget)
    {
        _replaceMovedChildren = replaceMovedChildren;
    }

    protected SliverMultiBoxAdaptorWidget TypedWidget => (SliverMultiBoxAdaptorWidget)Widget;

    protected RenderSliverMultiBoxAdaptor TypedRenderObject => (RenderSliverMultiBoxAdaptor)RequireRenderObject();

    /// <remarks>Flutter's <c>SliverMultiBoxAdaptorElement._extrapolateMaxScrollOffset</c>.</remarks>
    private static double ExtrapolateMaxScrollOffset(
        int firstIndex,
        int lastIndex,
        double leadingScrollOffset,
        double trailingScrollOffset,
        int childCount)
    {
        if (lastIndex == childCount - 1)
        {
            return trailingScrollOffset;
        }

        int reifiedCount = lastIndex - firstIndex + 1;
        double averageExtent = (trailingScrollOffset - leadingScrollOffset) / reifiedCount;
        int remainingCount = childCount - lastIndex - 1;
        return trailingScrollOffset + (averageExtent * remainingCount);
    }

    /// <inheritdoc />
    public double EstimateMaxScrollOffset(
        SliverConstraints constraints,
        int? firstIndex = null,
        int? lastIndex = null,
        double? leadingScrollOffset = null,
        double? trailingScrollOffset = null)
    {
        int? childCount = EstimatedChildCount;
        if (childCount is null)
        {
            return double.PositiveInfinity;
        }

        return TypedWidget.EstimateMaxScrollOffset(
                   constraints,
                   firstIndex!.Value,
                   lastIndex!.Value,
                   leadingScrollOffset!.Value,
                   trailingScrollOffset!.Value)
               ?? ExtrapolateMaxScrollOffset(
                   firstIndex.Value,
                   lastIndex.Value,
                   leadingScrollOffset.Value,
                   trailingScrollOffset.Value,
                   childCount.Value);
    }

    /// <inheritdoc />
    public int? EstimatedChildCount => TypedWidget.Delegate.EstimatedChildCount;

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>SliverMultiBoxAdaptorElement.childCount</c>: when the delegate cannot estimate
    /// the count, the fact that this getter was read means the builder already returned null once,
    /// so the list is finite and an open-ended binary search finds its end.
    /// </remarks>
    public int ChildCount
    {
        get
        {
            int? result = EstimatedChildCount;
            if (result is not null)
            {
                return result.Value;
            }

            int lo = 0;
            int hi = 1;
            SliverMultiBoxAdaptorWidget adaptorWidget = TypedWidget;
            const int max = int.MaxValue;
            while (BuildChildWidget(hi - 1, adaptorWidget) is not null)
            {
                lo = hi - 1;
                if (hi < max / 2)
                {
                    hi *= 2;
                }
                else if (hi < max)
                {
                    hi = max;
                }
                else
                {
                    throw new FlutterError(
                    [
                        new ErrorSummary(
                            $"Could not find the number of children in {adaptorWidget.Delegate}."),
                        new ErrorDescription(
                            "The childCount getter was called (implying that the delegate's builder returned "
                            + $"null for a positive index), but even building the child with index {hi} (the "
                            + "maximum possible integer) did not return null. Consider implementing childCount "
                            + "to avoid the cost of searching for the final child."),
                    ]);
                }
            }

            while (hi - lo > 1)
            {
                int mid = ((hi - lo) / 2) + lo;
                if (BuildChildWidget(mid - 1, adaptorWidget) is null)
                {
                    hi = mid;
                }
                else
                {
                    lo = mid;
                }
            }

            return lo;
        }
    }

    /// <inheritdoc />
    public void DidStartLayout()
    {
        Debug.Assert(DebugAssertChildListLocked());
    }

    /// <inheritdoc />
    public void DidFinishLayout()
    {
        Debug.Assert(DebugAssertChildListLocked());
        int firstIndex = _childElements.Count == 0 ? 0 : _childElements.Keys.First();
        int lastIndex = _childElements.Count == 0 ? 0 : _childElements.Keys.Last();
        TypedWidget.Delegate.DidFinishLayout(firstIndex, lastIndex);
    }

    /// <inheritdoc />
    public bool DebugAssertChildListLocked()
    {
        Debug.Assert(_currentlyUpdatingChildIndex is null);
        return true;
    }

    /// <remarks>Flutter's <c>SliverMultiBoxAdaptorElement._build</c>.</remarks>
    private Widget? BuildChildWidget(int index, SliverMultiBoxAdaptorWidget widget)
    {
        return widget.Delegate.Build(this, index);
    }

    protected override void OnMount()
    {
        base.OnMount();
        TypedRenderObject.ChildManager = this;
    }

    protected override void OnActivate()
    {
        base.OnActivate();
        TypedRenderObject.ChildManager = this;
    }

    protected override void OnDeactivate()
    {
        if (RenderObject is RenderSliverMultiBoxAdaptor renderObject
            && ReferenceEquals(renderObject.ChildManager, this))
        {
            renderObject.ChildManager = null;
        }

        base.OnDeactivate();
    }

    /// <remarks>Flutter's <c>SliverMultiBoxAdaptorElement.update</c>: a new delegate instance only
    /// rebuilds the children when it is of a different runtime type or says so itself.</remarks>
    public override void Update(Widget newWidget)
    {
        SliverChildDelegate oldDelegate = TypedWidget.Delegate;
        base.Update(newWidget);
        TypedRenderObject.ChildManager = this;
        SliverChildDelegate newDelegate = ((SliverMultiBoxAdaptorWidget)newWidget).Delegate;
        if (!ReferenceEquals(newDelegate, oldDelegate)
            && (newDelegate.GetType() != oldDelegate.GetType() || newDelegate.ShouldRebuild(oldDelegate)))
        {
            PerformRebuild();
        }
    }

    /// <remarks>Flutter's <c>SliverMultiBoxAdaptorElement.performRebuild</c>.</remarks>
    protected override void PerformRebuild()
    {
        base.PerformRebuild();
        _currentBeforeChild = null;
        bool childrenUpdated = false;
        Debug.Assert(_currentlyUpdatingChildIndex is null);
        try
        {
            var newChildren = new SortedDictionary<int, Element?>();
            var indexToLayoutOffset = new Dictionary<int, double>();
            SliverMultiBoxAdaptorWidget adaptorWidget = TypedWidget;

            void ProcessElement(int index)
            {
                _currentlyUpdatingChildIndex = index;
                newChildren.TryGetValue(index, out Element? reusedChild);
                if (_childElements.TryGetValue(index, out Element? oldChild)
                    && oldChild is not null
                    && !ReferenceEquals(oldChild, reusedChild))
                {
                    // This index has an old child that isn't used anywhere and should be deactivated.
                    _childElements[index] = UpdateChild(oldChild, null, index);
                    childrenUpdated = true;
                }

                Element? newChild = UpdateChild(reusedChild, BuildChildWidget(index, adaptorWidget), index);
                if (newChild is not null)
                {
                    _childElements.TryGetValue(index, out Element? previousChild);
                    childrenUpdated = childrenUpdated || !ReferenceEquals(previousChild, newChild);
                    _childElements[index] = newChild;
                    var parentData = (SliverMultiBoxAdaptorParentData)newChild.RenderObject!.parentData!;
                    if (index == 0)
                    {
                        parentData.LayoutOffset = 0.0;
                    }
                    else if (indexToLayoutOffset.TryGetValue(index, out double inheritedOffset))
                    {
                        parentData.LayoutOffset = inheritedOffset;
                    }

                    if (!parentData.KeptAlive)
                    {
                        _currentBeforeChild = (RenderBox?)newChild.RenderObject;
                    }
                }
                else
                {
                    childrenUpdated = true;
                    _childElements.Remove(index);
                }
            }

            foreach (int index in _childElements.Keys.ToArray())
            {
                Element child = _childElements[index]!;
                Key? key = child.Widget.Key;
                int? newIndex = key is null ? null : adaptorWidget.Delegate.FindIndexByKey(key);
                var childParentData = child.RenderObject?.parentData as SliverMultiBoxAdaptorParentData;

                if (childParentData?.LayoutOffset is { } layoutOffset)
                {
                    indexToLayoutOffset[index] = layoutOffset;
                }

                if (newIndex is not null && newIndex.Value != index)
                {
                    // The layout offset of the child being moved is no longer accurate.
                    if (childParentData is not null)
                    {
                        childParentData.LayoutOffset = null;
                    }

                    newChildren[newIndex.Value] = child;
                    if (_replaceMovedChildren)
                    {
                        // We need to make sure the original index gets processed.
                        newChildren.TryAdd(index, null);
                    }

                    // We do not want the remapped child to get deactivated during processElement.
                    _childElements.Remove(index);
                }
                else
                {
                    newChildren.TryAdd(index, child);
                }
            }

            // Moving children will temporarily violate the integrity.
            TypedRenderObject.DebugChildIntegrityEnabled = false;
            foreach (int index in newChildren.Keys.ToArray())
            {
                ProcessElement(index);
            }

            // An element rebuild only updates existing children. The underflow check is here to make
            // sure we look ahead one more child if we were at the end of the child list before the
            // update. By doing so, we can update the max scroll offset during the layout phase.
            // Otherwise, the layout phase may be skipped, and the scroll view may be stuck at the
            // previous max scroll offset.
            //
            // This logic is not needed if any existing children have been updated, because then the
            // layout phase will not be skipped.
            if (!childrenUpdated && _didUnderflow)
            {
                int lastKey = _childElements.Count == 0 ? -1 : _childElements.Keys.Last();
                int rightBoundary = lastKey + 1;
                _childElements.TryGetValue(rightBoundary, out Element? boundaryChild);
                newChildren[rightBoundary] = boundaryChild;
                ProcessElement(rightBoundary);
            }
        }
        finally
        {
            _currentlyUpdatingChildIndex = null;
            TypedRenderObject.DebugChildIntegrityEnabled = true;
        }
    }

    /// <remarks>
    /// Flutter's <c>SliverMultiBoxAdaptorElement.updateChild</c>: a rebuilt child that swapped its
    /// render object keeps the layout offset the old one had.
    /// </remarks>
    public override Element? UpdateChild(Element? child, Widget? newWidget, object? newSlot)
    {
        var oldParentData = child?.RenderObject?.parentData as SliverMultiBoxAdaptorParentData;
        Element? newChild = base.UpdateChild(child, newWidget, newSlot);
        var newParentData = newChild?.RenderObject?.parentData as SliverMultiBoxAdaptorParentData;

        // Preserve the old layoutOffset if the renderObject was swapped out.
        if (!ReferenceEquals(oldParentData, newParentData)
            && oldParentData is not null
            && newParentData is not null)
        {
            newParentData.LayoutOffset = oldParentData.LayoutOffset;
        }

        return newChild;
    }

    public override void VisitChildren(Action<Element> visitor)
    {
        Debug.Assert(_childElements.Values.All(static child => child is not null));
        foreach (Element child in _childElements.Values.Select(static child => child!).ToArray())
        {
            visitor(child);
        }
    }

    /// <remarks>
    /// Flutter's <c>SliverMultiBoxAdaptorElement.debugVisitOnstageChildren</c>: only the children
    /// that intersect the painted part of the viewport are on stage.
    /// </remarks>
    public override void DebugVisitOnstageChildren(Action<Element> visitor)
    {
        RenderSliverMultiBoxAdaptor renderObject = TypedRenderObject;
        SliverConstraints constraints = renderObject.ConstraintsForSliver;
        foreach (Element child in _childElements.Values.Select(static child => child!).ToArray())
        {
            var parentData = (SliverMultiBoxAdaptorParentData)child.RenderObject!.parentData!;
            double itemExtent = constraints.Axis == Axis.Horizontal
                ? child.RenderObject!.PaintBounds.Width
                : child.RenderObject!.PaintBounds.Height;
            if (parentData.LayoutOffset is { } layoutOffset
                && layoutOffset < constraints.ScrollOffset + constraints.RemainingPaintExtent
                && layoutOffset + itemExtent > constraints.ScrollOffset)
            {
                visitor(child);
            }
        }
    }

    /// <remarks>
    /// Flutter's <c>SliverMultiBoxAdaptorElement.forgetChild</c> asserts the slot is still
    /// registered, because Dart only reaches it through the global-key retake path. Plumix's
    /// <c>Element.DeactivateChild</c> also calls it, and a remapped child has already left the map
    /// by then, so the entry is dropped only when it still points at this child.
    /// </remarks>
    public override void ForgetChild(Element child)
    {
        if (child.Slot is int slot
            && _childElements.TryGetValue(slot, out Element? registered)
            && ReferenceEquals(registered, child))
        {
            _childElements.Remove(slot);
        }

        base.ForgetChild(child);
    }

    public override void Unmount()
    {
        foreach (Element child in _childElements.Values.Select(static child => child!).ToArray())
        {
            UnmountChild(child);
        }

        _childElements.Clear();
        base.Unmount();
    }

    /// <inheritdoc />
    public void CreateChild(int index, RenderBox? after)
    {
        Debug.Assert(_currentlyUpdatingChildIndex is null);
        Owner!.BuildScopeDuringLayout(
            this,
            () =>
            {
                bool insertFirst = after is null;
                Debug.Assert(insertFirst || _childElements[index - 1] is not null);
                _currentBeforeChild = insertFirst ? null : (RenderBox?)_childElements[index - 1]!.RenderObject;
                Element? newChild;
                try
                {
                    SliverMultiBoxAdaptorWidget adaptorWidget = TypedWidget;
                    _currentlyUpdatingChildIndex = index;
                    _childElements.TryGetValue(index, out Element? oldChild);
                    newChild = UpdateChild(oldChild, BuildChildWidget(index, adaptorWidget), index);
                }
                finally
                {
                    _currentlyUpdatingChildIndex = null;
                }

                if (newChild is not null)
                {
                    _childElements[index] = newChild;
                }
                else
                {
                    _childElements.Remove(index);
                }
            });
    }

    /// <inheritdoc />
    public void RemoveChild(RenderBox child)
    {
        int index = TypedRenderObject.IndexOf(child);
        Debug.Assert(_currentlyUpdatingChildIndex is null);
        Debug.Assert(index >= 0);
        Owner!.BuildScopeDuringLayout(
            this,
            () =>
            {
                Debug.Assert(_childElements.ContainsKey(index));
                try
                {
                    _currentlyUpdatingChildIndex = index;
                    Element? result = UpdateChild(_childElements[index], null, index);
                    Debug.Assert(result is null);
                }
                finally
                {
                    _currentlyUpdatingChildIndex = null;
                }

                _childElements.Remove(index);
                Debug.Assert(!_childElements.ContainsKey(index));
            });
    }

    /// <inheritdoc />
    public virtual void DidAdoptChild(RenderBox child)
    {
        Debug.Assert(_currentlyUpdatingChildIndex is not null);
        var childParentData = (SliverMultiBoxAdaptorParentData)child.parentData!;
        childParentData.Index = _currentlyUpdatingChildIndex;
    }

    /// <inheritdoc />
    public void SetDidUnderflow(bool value)
    {
        _didUnderflow = value;
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        Debug.Assert(Equals(_currentlyUpdatingChildIndex, slot));
        TypedRenderObject.Insert((RenderBox)child, _currentBeforeChild);
        Debug.Assert(Equals(slot, ((SliverMultiBoxAdaptorParentData)child.parentData!).Index));
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        Debug.Assert(Equals(_currentlyUpdatingChildIndex, newSlot));
        TypedRenderObject.Move((RenderBox)child, _currentBeforeChild);
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        Debug.Assert(_currentlyUpdatingChildIndex is not null);
        TypedRenderObject.Remove((RenderBox)child);
    }
}


public sealed class SliverList : SliverMultiBoxAdaptorWidget
{
    public SliverList(SliverChildDelegate @delegate, Key? key = null) : base(@delegate, key)
    {
    }

    /// <remarks>
    /// Flutter's <c>SliverList.createElement</c> passes <c>replaceMovedChildren: true</c>: this
    /// sliver dead-reckons its layout offsets, so a vacated index has to be re-inflated to give the
    /// leading edge an anchor. The fixed-extent and grid slivers derive offsets from the index and
    /// keep the default.
    /// </remarks>
    public override Element CreateElement()
    {
        return new SliverMultiBoxAdaptorElement(this, replaceMovedChildren: true);
    }

    /// <remarks>Flutter's <c>SliverList.list</c>.</remarks>
    public static SliverList FromChildren(
        IReadOnlyList<Widget> children,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        Key? key = null)
    {
        return new SliverList(
            new SliverChildListDelegate(
                children,
                addAutomaticKeepAlives: addAutomaticKeepAlives,
                addRepaintBoundaries: addRepaintBoundaries,
                addSemanticIndexes: addSemanticIndexes),
            key);
    }

    /// <remarks>Flutter's <c>SliverList.builder</c>; a null <paramref name="itemCount"/> is unbounded.</remarks>
    public static SliverList Builder(
        NullableIndexedWidgetBuilder itemBuilder,
        int? itemCount = null,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        SemanticIndexCallback? semanticIndexCallback = null,
        int semanticIndexOffset = 0,
        ChildIndexGetter? findChildIndexCallback = null,
        Key? key = null)
    {
        return new SliverList(
            new SliverChildBuilderDelegate(
                itemBuilder,
                itemCount,
                addAutomaticKeepAlives: addAutomaticKeepAlives,
                addRepaintBoundaries: addRepaintBoundaries,
                addSemanticIndexes: addSemanticIndexes,
                semanticIndexCallback: semanticIndexCallback,
                semanticIndexOffset: semanticIndexOffset,
                findChildIndexCallback: findChildIndexCallback),
            key);
    }

    /// <summary>
    /// Places box children in a linear array, separated by box widgets built by
    /// <paramref name="separatorBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>SliverList.separated</c>. The delegate holds two children per item, so the child
    /// count is <c>max(0, itemCount * 2 - 1)</c>, an even child index <c>2k</c> is item <c>k</c> and
    /// an odd child index <c>2k + 1</c> is the separator after item <c>k</c>. Separators get no
    /// semantic index at all, so item <c>k</c> keeps index <c>k</c>.
    /// <paramref name="findItemIndexCallback"/> returns an *item* index and is doubled here;
    /// <paramref name="findChildIndexCallback"/> is Dart's deprecated form, which returns a child
    /// index and is passed through unchanged.
    /// </remarks>
    public static SliverList Separated(
        NullableIndexedWidgetBuilder itemBuilder,
        NullableIndexedWidgetBuilder separatorBuilder,
        int? itemCount = null,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        ChildIndexGetter? findItemIndexCallback = null,
        ChildIndexGetter? findChildIndexCallback = null,
        Key? key = null)
    {
        if (findItemIndexCallback is not null && findChildIndexCallback is not null)
        {
            throw new ArgumentException(
                "Cannot provide both findItemIndexCallback and findChildIndexCallback. "
                + "Use findItemIndexCallback as findChildIndexCallback is deprecated.");
        }

        ChildIndexGetter? effectiveFindChildIndexCallback = findItemIndexCallback is null
            ? findChildIndexCallback
            : childKey => findItemIndexCallback(childKey) is { } itemIndex ? itemIndex * 2 : null;

        return new SliverList(
            new SliverChildBuilderDelegate(
                (context, index) =>
                {
                    int itemIndex = index / 2;
                    if (index % 2 == 0)
                    {
                        return itemBuilder(context, itemIndex);
                    }

                    Widget? separator = separatorBuilder(context, itemIndex);
                    if (Constants.KDebugMode && separator is null)
                    {
                        throw new FlutterError("separatorBuilder cannot return null.");
                    }

                    return separator;
                },
                itemCount is null ? null : Math.Max(0, (itemCount.Value * 2) - 1),
                addAutomaticKeepAlives: addAutomaticKeepAlives,
                addRepaintBoundaries: addRepaintBoundaries,
                addSemanticIndexes: addSemanticIndexes,
                semanticIndexCallback: static (_, index) => index % 2 == 0 ? index / 2 : null,
                findChildIndexCallback: effectiveFindChildIndexCallback),
            key);
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverList();
    }
}

public sealed class SliverFixedExtentList : SliverMultiBoxAdaptorWidget
{
    public SliverFixedExtentList(
        SliverChildDelegate @delegate,
        double itemExtent,
        Key? key = null) : base(@delegate, key)
    {
        if (itemExtent <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemExtent), "itemExtent must be greater than 0.");
        }

        ItemExtent = itemExtent;
    }

    public double ItemExtent { get; }

    /// <remarks>Flutter's <c>SliverFixedExtentList.list</c>.</remarks>
    public static SliverFixedExtentList FromChildren(
        IReadOnlyList<Widget> children,
        double itemExtent,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        Key? key = null)
    {
        return new SliverFixedExtentList(
            new SliverChildListDelegate(
                children,
                addAutomaticKeepAlives: addAutomaticKeepAlives,
                addRepaintBoundaries: addRepaintBoundaries,
                addSemanticIndexes: addSemanticIndexes),
            itemExtent,
            key);
    }

    /// <remarks>
    /// Flutter's <c>SliverFixedExtentList.builder</c>; a null <paramref name="itemCount"/> is unbounded.
    /// </remarks>
    public static SliverFixedExtentList Builder(
        NullableIndexedWidgetBuilder itemBuilder,
        double itemExtent,
        int? itemCount = null,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        SemanticIndexCallback? semanticIndexCallback = null,
        int semanticIndexOffset = 0,
        ChildIndexGetter? findChildIndexCallback = null,
        Key? key = null)
    {
        return new SliverFixedExtentList(
            new SliverChildBuilderDelegate(
                itemBuilder,
                itemCount,
                addAutomaticKeepAlives: addAutomaticKeepAlives,
                addRepaintBoundaries: addRepaintBoundaries,
                addSemanticIndexes: addSemanticIndexes,
                semanticIndexCallback: semanticIndexCallback,
                semanticIndexOffset: semanticIndexOffset,
                findChildIndexCallback: findChildIndexCallback),
            itemExtent,
            key);
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverFixedExtentList(ItemExtent);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderSliverFixedExtentList)renderObject).SetItemExtent(ItemExtent);
    }
}

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/sliver.dart (SliverVariedExtentList)
// flutter/packages/flutter/lib/src/widgets/sliver_prototype_extent_list.dart (SliverPrototypeExtentList)

/// <summary>
/// Places box children in a linear array and forces each child to the main-axis
/// extent returned by <see cref="ItemExtentBuilder"/>.
/// </summary>
public sealed class SliverVariedExtentList : SliverMultiBoxAdaptorWidget
{
    public SliverVariedExtentList(
        SliverChildDelegate @delegate,
        ItemExtentBuilder itemExtentBuilder,
        Key? key = null) : base(@delegate, key)
    {
        ItemExtentBuilder = itemExtentBuilder ?? throw new ArgumentNullException(nameof(itemExtentBuilder));
    }

    public ItemExtentBuilder ItemExtentBuilder { get; }

    /// <remarks>Flutter's <c>SliverVariedExtentList.list</c>.</remarks>
    public static SliverVariedExtentList FromChildren(
        IReadOnlyList<Widget> children,
        ItemExtentBuilder itemExtentBuilder,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        Key? key = null)
    {
        return new SliverVariedExtentList(
            new SliverChildListDelegate(
                children,
                addAutomaticKeepAlives: addAutomaticKeepAlives,
                addRepaintBoundaries: addRepaintBoundaries,
                addSemanticIndexes: addSemanticIndexes),
            itemExtentBuilder,
            key);
    }

    /// <remarks>
    /// Flutter's <c>SliverVariedExtentList.builder</c>; a null <paramref name="itemCount"/> is unbounded.
    /// </remarks>
    public static SliverVariedExtentList Builder(
        NullableIndexedWidgetBuilder itemBuilder,
        ItemExtentBuilder itemExtentBuilder,
        int? itemCount = null,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        ChildIndexGetter? findChildIndexCallback = null,
        Key? key = null)
    {
        return new SliverVariedExtentList(
            new SliverChildBuilderDelegate(
                itemBuilder,
                itemCount,
                addAutomaticKeepAlives: addAutomaticKeepAlives,
                addRepaintBoundaries: addRepaintBoundaries,
                addSemanticIndexes: addSemanticIndexes,
                findChildIndexCallback: findChildIndexCallback),
            itemExtentBuilder,
            key);
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverVariedExtentList(ItemExtentBuilder);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderSliverVariedExtentList)renderObject).SetItemExtentBuilder(ItemExtentBuilder);
    }
}

/// <summary>
/// Places box children in a linear array and derives their common main-axis
/// extent from an offstage prototype child.
/// </summary>
public sealed class SliverPrototypeExtentList : SliverMultiBoxAdaptorWidget
{
    public SliverPrototypeExtentList(
        SliverChildDelegate @delegate,
        Widget prototypeItem,
        Key? key = null) : base(@delegate, key)
    {
        PrototypeItem = prototypeItem ?? throw new ArgumentNullException(nameof(prototypeItem));
    }

    public Widget PrototypeItem { get; }

    /// <remarks>Flutter's <c>SliverPrototypeExtentList.list</c>.</remarks>
    public static SliverPrototypeExtentList FromChildren(
        IReadOnlyList<Widget> children,
        Widget prototypeItem,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        Key? key = null)
    {
        return new SliverPrototypeExtentList(
            new SliverChildListDelegate(
                children,
                addAutomaticKeepAlives: addAutomaticKeepAlives,
                addRepaintBoundaries: addRepaintBoundaries,
                addSemanticIndexes: addSemanticIndexes),
            prototypeItem,
            key);
    }

    /// <remarks>
    /// Flutter's <c>SliverPrototypeExtentList.builder</c>; a null <paramref name="itemCount"/> is unbounded.
    /// </remarks>
    public static SliverPrototypeExtentList Builder(
        NullableIndexedWidgetBuilder itemBuilder,
        Widget prototypeItem,
        int? itemCount = null,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        ChildIndexGetter? findChildIndexCallback = null,
        Key? key = null)
    {
        return new SliverPrototypeExtentList(
            new SliverChildBuilderDelegate(
                itemBuilder,
                itemCount,
                addAutomaticKeepAlives: addAutomaticKeepAlives,
                addRepaintBoundaries: addRepaintBoundaries,
                addSemanticIndexes: addSemanticIndexes,
                findChildIndexCallback: findChildIndexCallback),
            prototypeItem,
            key);
    }

    public override Element CreateElement()
    {
        return new SliverPrototypeExtentListElement(this);
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverPrototypeExtentList();
    }
}

internal sealed class SliverPrototypeExtentListElement : SliverMultiBoxAdaptorElement
{
    private static readonly object PrototypeSlot = new();
    private Element? _prototype;

    public SliverPrototypeExtentListElement(SliverPrototypeExtentList widget) : base(widget)
    {
    }

    private SliverPrototypeExtentList PrototypeWidget => (SliverPrototypeExtentList)Widget;

    private RenderSliverPrototypeExtentList PrototypeRenderObject =>
        (RenderSliverPrototypeExtentList)TypedRenderObject;

    /// <remarks>
    /// Flutter's <c>_SliverPrototypeExtentListElement.didAdoptChild</c>: the prototype is adopted
    /// outside the lazy child list, so it carries no child index.
    /// </remarks>
    public override void DidAdoptChild(RenderBox child)
    {
        if (!ReferenceEquals(child, PrototypeRenderObject.PrototypeChild))
        {
            base.DidAdoptChild(child);
        }
    }

    protected override void OnMount()
    {
        base.OnMount();
        _prototype = UpdateChild(_prototype, PrototypeWidget.PrototypeItem, PrototypeSlot);
    }

    public override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        _prototype = UpdateChild(_prototype, PrototypeWidget.PrototypeItem, PrototypeSlot);
    }

    public override void VisitChildren(Action<Element> visitor)
    {
        if (_prototype != null)
        {
            visitor(_prototype);
        }

        base.VisitChildren(visitor);
    }

    public override void ForgetChild(Element child)
    {
        if (ReferenceEquals(child, _prototype))
        {
            _prototype = null;
            return;
        }

        base.ForgetChild(child);
    }

    public override void InsertRenderObjectChild(RenderObject child, object? slot)
    {
        if (ReferenceEquals(slot, PrototypeSlot))
        {
            PrototypeRenderObject.PrototypeChild = (RenderBox)child;
            return;
        }

        base.InsertRenderObjectChild(child, slot);
    }

    public override void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
    {
        if (ReferenceEquals(newSlot, PrototypeSlot))
        {
            throw new InvalidOperationException("A SliverPrototypeExtentList prototype cannot move.");
        }

        base.MoveRenderObjectChild(child, oldSlot, newSlot);
    }

    public override void RemoveRenderObjectChild(RenderObject child, object? slot)
    {
        if (ReferenceEquals(child, PrototypeRenderObject.PrototypeChild))
        {
            PrototypeRenderObject.PrototypeChild = null;
            return;
        }

        base.RemoveRenderObjectChild(child, slot);
    }

    public override void Unmount()
    {
        if (_prototype != null)
        {
            UnmountChild(_prototype);
            _prototype = null;
        }

        base.Unmount();
    }
}

public sealed class SliverGrid : SliverMultiBoxAdaptorWidget
{
    public SliverGrid(
        SliverChildDelegate @delegate,
        SliverGridDelegate gridDelegate,
        Key? key = null) : base(@delegate, key)
    {
        GridDelegate = gridDelegate ?? throw new ArgumentNullException(nameof(gridDelegate));
    }

    public SliverGridDelegate GridDelegate { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Flutter's <c>SliverGrid.estimateMaxScrollOffset</c>: when the delegate has no estimate of its
    /// own, the grid layout knows the exact extent of a known number of children.
    /// </remarks>
    public override double? EstimateMaxScrollOffset(
        SliverConstraints? constraints,
        int firstIndex,
        int lastIndex,
        double leadingScrollOffset,
        double trailingScrollOffset)
    {
        return base.EstimateMaxScrollOffset(
                   constraints,
                   firstIndex,
                   lastIndex,
                   leadingScrollOffset,
                   trailingScrollOffset)
               ?? GridDelegate
                   .GetLayout(constraints!.Value)
                   .ComputeMaxScrollOffset(Delegate.EstimatedChildCount!.Value);
    }

    /// <remarks>Flutter's <c>SliverGrid.list</c>.</remarks>
    public static SliverGrid FromChildren(
        IReadOnlyList<Widget> children,
        SliverGridDelegate gridDelegate,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        int semanticIndexOffset = 0,
        Key? key = null)
    {
        return new SliverGrid(
            new SliverChildListDelegate(
                children,
                addAutomaticKeepAlives: addAutomaticKeepAlives,
                addRepaintBoundaries: addRepaintBoundaries,
                addSemanticIndexes: addSemanticIndexes,
                semanticIndexOffset: semanticIndexOffset),
            gridDelegate,
            key);
    }

    /// <remarks>Flutter's <c>SliverGrid.builder</c>; a null <paramref name="itemCount"/> is unbounded.</remarks>
    public static SliverGrid Builder(
        NullableIndexedWidgetBuilder itemBuilder,
        SliverGridDelegate gridDelegate,
        int? itemCount = null,
        bool addAutomaticKeepAlives = true,
        bool addRepaintBoundaries = true,
        bool addSemanticIndexes = true,
        int semanticIndexOffset = 0,
        ChildIndexGetter? findChildIndexCallback = null,
        Key? key = null)
    {
        return new SliverGrid(
            new SliverChildBuilderDelegate(
                itemBuilder,
                itemCount,
                addAutomaticKeepAlives: addAutomaticKeepAlives,
                addRepaintBoundaries: addRepaintBoundaries,
                addSemanticIndexes: addSemanticIndexes,
                semanticIndexOffset: semanticIndexOffset,
                findChildIndexCallback: findChildIndexCallback),
            gridDelegate,
            key);
    }

    public static SliverGrid Count(
        int crossAxisCount,
        IReadOnlyList<Widget> children,
        double mainAxisSpacing = 0,
        double crossAxisSpacing = 0,
        double childAspectRatio = 1,
        bool addAutomaticKeepAlives = true,
        Key? key = null)
    {
        return FromChildren(
            children,
            new SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: crossAxisCount,
                mainAxisSpacing: mainAxisSpacing,
                crossAxisSpacing: crossAxisSpacing,
                childAspectRatio: childAspectRatio),
            addAutomaticKeepAlives: addAutomaticKeepAlives,
            key: key);
    }

    public static SliverGrid Extent(
        double maxCrossAxisExtent,
        IReadOnlyList<Widget> children,
        double mainAxisSpacing = 0,
        double crossAxisSpacing = 0,
        double childAspectRatio = 1,
        bool addAutomaticKeepAlives = true,
        Key? key = null)
    {
        return FromChildren(
            children,
            new SliverGridDelegateWithMaxCrossAxisExtent(
                maxCrossAxisExtent: maxCrossAxisExtent,
                mainAxisSpacing: mainAxisSpacing,
                crossAxisSpacing: crossAxisSpacing,
                childAspectRatio: childAspectRatio),
            addAutomaticKeepAlives: addAutomaticKeepAlives,
            key: key);
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverGrid(GridDelegate);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderSliverGrid)renderObject).GridDelegate = GridDelegate;
    }
}
