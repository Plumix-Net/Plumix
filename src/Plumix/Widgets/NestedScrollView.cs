using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Physics;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/nested_scroll_view.dart

namespace Plumix.Widgets;

/// <summary>
/// Builds the slivers that scroll above the body of a <see cref="NestedScrollView"/>.
/// </summary>
/// <param name="innerBoxIsScrolled">
/// Whether any of the inner scroll views is scrolled away from its leading edge, which is what a
/// header uses to decide whether to show its shadow.
/// </param>
public delegate IReadOnlyList<Widget> NestedScrollViewHeaderSliversBuilder(
    BuildContext context,
    bool innerBoxIsScrolled);

/// <summary>
/// A scrolling view in which a header and a body scroll together as one, with the header scrolling
/// out of the way before the body starts scrolling.
/// </summary>
public class NestedScrollView : StatefulWidget
{
    public NestedScrollView(
        NestedScrollViewHeaderSliversBuilder headerSliverBuilder,
        Widget body,
        ScrollController? controller = null,
        Axis scrollDirection = Axis.Vertical,
        bool reverse = false,
        ScrollPhysics? physics = null,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        bool floatHeaderSlivers = false,
        Clip clipBehavior = Clip.HardEdge,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        string? restorationId = null,
        ScrollBehavior? scrollBehavior = null,
        Key? key = null) : base(key)
    {
        HeaderSliverBuilder = headerSliverBuilder
                              ?? throw new ArgumentNullException(nameof(headerSliverBuilder));
        Body = body ?? throw new ArgumentNullException(nameof(body));
        Controller = controller;
        ScrollDirection = scrollDirection;
        Reverse = reverse;
        Physics = physics;
        DragStartBehavior = dragStartBehavior;
        FloatHeaderSlivers = floatHeaderSlivers;
        ClipBehavior = clipBehavior;
        HitTestBehavior = hitTestBehavior;
        RestorationId = restorationId;
        ScrollBehavior = scrollBehavior;
    }

    /// <summary>An object that can be used to control the position of the outer scroll view.</summary>
    public ScrollController? Controller { get; }

    /// <summary>The axis along which the scroll view scrolls.</summary>
    public Axis ScrollDirection { get; }

    /// <summary>Whether the scroll view scrolls in the reading direction.</summary>
    public bool Reverse { get; }

    /// <summary>How the scroll view should respond to user input.</summary>
    public ScrollPhysics? Physics { get; }

    /// <summary>Builds the slivers that appear above the body.</summary>
    public NestedScrollViewHeaderSliversBuilder HeaderSliverBuilder { get; }

    /// <summary>The widget shown below the header slivers.</summary>
    public Widget Body { get; }

    public DragStartBehavior DragStartBehavior { get; }

    /// <summary>
    /// Whether a floating header should be scrolled back in before the body scrolls, which is what a
    /// <c>SliverAppBar</c> with <c>floating: true</c> needs to reappear on a downward drag.
    /// </summary>
    public bool FloatHeaderSlivers { get; }

    public Clip ClipBehavior { get; }

    public HitTestBehavior HitTestBehavior { get; }

    public string? RestorationId { get; }

    public ScrollBehavior? ScrollBehavior { get; }

    /// <summary>
    /// Returns the <see cref="SliverOverlapAbsorberHandle"/> of the nearest ancestor
    /// <see cref="NestedScrollView"/>, registering <paramref name="context"/> as a dependent.
    /// </summary>
    public static SliverOverlapAbsorberHandle SliverOverlapAbsorberHandleFor(BuildContext context)
    {
        InheritedNestedScrollView? target = context.DependOnInherited<InheritedNestedScrollView>();
        if (target == null)
        {
            throw new InvalidOperationException(
                "NestedScrollView.SliverOverlapAbsorberHandleFor must be called with a context that "
                + "contains a NestedScrollView.");
        }

        return target.State.AbsorberHandle;
    }

    internal IReadOnlyList<Widget> BuildSlivers(
        BuildContext context,
        ScrollController innerController,
        bool bodyIsScrolled)
    {
        var slivers = new List<Widget>(HeaderSliverBuilder(context, bodyIsScrolled))
        {
            new SliverFillRemaining(
                child: new PrimaryScrollController(
                    automaticallyInheritForPlatforms: new HashSet<TargetPlatform>(
                        Enum.GetValues<TargetPlatform>()),
                    controller: innerController,
                    child: Body)),
        };
        return slivers;
    }

    public override State CreateState() => new NestedScrollViewState();
}

/// <summary>The state of a <see cref="NestedScrollView"/>, exposing its two scroll controllers.</summary>
public class NestedScrollViewState : State
{
    private NestedScrollCoordinator? _coordinator;
    private bool? _lastHasScrolledBody;

    internal SliverOverlapAbsorberHandle AbsorberHandle { get; } = new();

    private NestedScrollView CurrentWidget => (NestedScrollView)Element.Widget;

    /// <summary>The <see cref="ScrollController"/> provided to the body's scroll views.</summary>
    public ScrollController InnerController => _coordinator!.InnerController;

    /// <summary>The <see cref="ScrollController"/> provided to the header slivers' scroll view.</summary>
    public ScrollController OuterController => _coordinator!.OuterController;

    public override void InitState()
    {
        base.InitState();
        _coordinator = new NestedScrollCoordinator(
            this,
            CurrentWidget.Controller,
            HandleHasScrolledBodyChanged,
            CurrentWidget.FloatHeaderSlivers);
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        _coordinator!.SetParent(CurrentWidget.Controller);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        if (!ReferenceEquals(((NestedScrollView)oldWidget).Controller, CurrentWidget.Controller))
        {
            _coordinator!.SetParent(CurrentWidget.Controller);
        }
    }

    public override void Dispose()
    {
        _coordinator!.Dispose();
        _coordinator = null;
        AbsorberHandle.Dispose();
        base.Dispose();
    }

    private void HandleHasScrolledBodyChanged()
    {
        if (!Mounted)
        {
            return;
        }

        bool newHasScrolledBody = _coordinator!.HasScrolledBody;
        if (_lastHasScrolledBody != newHasScrolledBody)
        {
            SetState(static () => { });
        }
    }

    public override Widget Build(BuildContext context)
    {
        NestedScrollView widget = CurrentWidget;
        ScrollPhysics scrollPhysics =
            widget.Physics?.ApplyTo(new ClampingScrollPhysics())
            ?? widget.ScrollBehavior?.GetScrollPhysics(context).ApplyTo(new ClampingScrollPhysics())
            ?? new ClampingScrollPhysics();

        return new InheritedNestedScrollView(
            state: this,
            child: new Builder(builderContext =>
            {
                _lastHasScrolledBody = _coordinator!.HasScrolledBody;
                return new NestedScrollViewCustomScrollView(
                    scrollDirection: widget.ScrollDirection,
                    reverse: widget.Reverse,
                    physics: scrollPhysics,
                    scrollBehavior: widget.ScrollBehavior
                                    ?? ScrollConfiguration.Of(builderContext).CopyWith(scrollbars: false),
                    controller: _coordinator.OuterController,
                    slivers: widget.BuildSlivers(
                        builderContext,
                        _coordinator.InnerController,
                        _lastHasScrolledBody.Value),
                    handle: AbsorberHandle,
                    clipBehavior: widget.ClipBehavior,
                    restorationId: widget.RestorationId,
                    hitTestBehavior: widget.HitTestBehavior,
                    dragStartBehavior: widget.DragStartBehavior);
            }));
    }
}

/// <summary>The outer scroll view of a <see cref="NestedScrollView"/>.</summary>
internal sealed class NestedScrollViewCustomScrollView : CustomScrollView
{
    public NestedScrollViewCustomScrollView(
        Axis scrollDirection,
        bool reverse,
        ScrollPhysics physics,
        ScrollBehavior scrollBehavior,
        ScrollController controller,
        IReadOnlyList<Widget> slivers,
        SliverOverlapAbsorberHandle handle,
        Clip clipBehavior,
        HitTestBehavior hitTestBehavior,
        DragStartBehavior dragStartBehavior,
        string? restorationId = null) : base(
        slivers: slivers,
        scrollDirection: scrollDirection,
        reverse: reverse,
        controller: controller,
        physics: physics,
        scrollBehavior: scrollBehavior,
        dragStartBehavior: dragStartBehavior,
        restorationId: restorationId,
        hitTestBehavior: hitTestBehavior,
        clipBehavior: clipBehavior)
    {
        Handle = handle;
    }

    public SliverOverlapAbsorberHandle Handle { get; }

    protected override bool HasCustomViewport => true;

    protected override Widget BuildViewport(
        BuildContext context,
        ViewportOffset offset,
        AxisDirection axisDirection,
        IReadOnlyList<Widget> slivers)
    {
        if (ShrinkWrap)
        {
            throw new InvalidOperationException("A NestedScrollView's outer view cannot shrink wrap.");
        }

        return new NestedScrollViewViewport(
            axisDirection: axisDirection,
            offset: offset,
            slivers: slivers,
            handle: Handle,
            clipBehavior: ClipBehavior);
    }
}

/// <summary>Carries the <see cref="NestedScrollViewState"/> down to its header slivers.</summary>
internal sealed class InheritedNestedScrollView : InheritedWidget
{
    public InheritedNestedScrollView(NestedScrollViewState state, Widget child, Key? key = null)
        : base(key)
    {
        State = state;
        Child = child;
    }

    public NestedScrollViewState State { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !ReferenceEquals(((InheritedNestedScrollView)oldWidget).State, State);
    }
}

/// <summary>
/// The metrics of the outer scroll view of a <see cref="NestedScrollView"/>, extended with the range
/// the outer position may travel through before the inner positions take over.
/// </summary>
internal sealed record NestedScrollMetrics(
    double Pixels,
    double MinScrollExtent,
    double MaxScrollExtent,
    double ViewportDimension,
    AxisDirection AxisDirection,
    double MinRange,
    double MaxRange,
    double CorrectionOffset,
    double DevicePixelRatio = 1.0) : IScrollMetrics;

/// <summary>Which of the two scroll views a ballistic activity belongs to.</summary>
internal enum NestedBallisticScrollActivityMode
{
    /// <summary>The activity is for the outer scroll view.</summary>
    Outer,

    /// <summary>The activity is for an inner scroll view.</summary>
    Inner,

    /// <summary>The activity is for a position settling on its own.</summary>
    Independent,
}

internal delegate ScrollActivity NestedScrollActivityGetter(NestedScrollPosition position);

/// <summary>
/// Splits every user or programmatic scroll between the outer scroll view of a
/// <see cref="NestedScrollView"/> and the scroll views inside its body.
/// </summary>
internal sealed class NestedScrollCoordinator : IScrollActivityDelegate, IScrollHoldController, IDisposable
{
    private readonly NestedScrollViewState _state;
    private readonly Action _onHasScrolledBodyChanged;
    private readonly bool _floatHeaderSlivers;
    private ScrollController? _parent;
    private ScrollDirection _userScrollDirection = ScrollDirection.Idle;
    private ScrollDragController? _currentDrag;

    public NestedScrollCoordinator(
        NestedScrollViewState state,
        ScrollController? parent,
        Action onHasScrolledBodyChanged,
        bool floatHeaderSlivers)
    {
        _state = state;
        _parent = parent;
        _onHasScrolledBodyChanged = onHasScrolledBodyChanged;
        _floatHeaderSlivers = floatHeaderSlivers;
        double initialScrollOffset = _parent?.InitialScrollOffset ?? 0.0;
        OuterController = new NestedScrollController(this, initialScrollOffset);
        InnerController = new NestedScrollController(this);
    }

    public NestedScrollController OuterController { get; }

    public NestedScrollController InnerController { get; }

    private NestedScrollPosition? OuterPosition => OuterController.HasClients
        ? OuterController.NestedPositions.Single()
        : null;

    private IEnumerable<NestedScrollPosition> InnerPositions => InnerController.NestedPositions;

    public bool OutOfRange => (OuterPosition?.OutOfRange ?? false)
                              || InnerPositions.Any(position => position.OutOfRange);

    /// <summary>Whether the body may scroll, which it may only once the header is fully out.</summary>
    public bool CanScrollBody
    {
        get
        {
            NestedScrollPosition? outer = OuterPosition;
            if (outer == null)
            {
                return true;
            }

            return outer.HaveDimensions && outer.ExtentAfter == 0.0;
        }
    }

    /// <summary>Whether any inner scroll view has scrolled away from its leading edge.</summary>
    public bool HasScrolledBody
    {
        get
        {
            foreach (NestedScrollPosition position in InnerPositions)
            {
                if (!position.HasContentDimensions || !position.HasPixels)
                {
                    // The position is not laid out yet, so it cannot have scrolled. This happens when
                    // a rebuild is scheduled before the first layout of a warm-up frame.
                    continue;
                }

                if (position.Pixels > position.MinScrollExtent)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public ScrollDirection UserScrollDirection => _userScrollDirection;

    public AxisDirection AxisDirection => OuterPosition!.AxisDirection;

    public void UpdateShadow() => _onHasScrolledBodyChanged();

    public void UpdateUserScrollDirection(ScrollDirection value)
    {
        if (_userScrollDirection == value)
        {
            return;
        }

        _userScrollDirection = value;
        OuterPosition!.DidUpdateScrollDirection(value);
        foreach (NestedScrollPosition position in InnerPositions.ToList())
        {
            position.DidUpdateScrollDirection(value);
        }
    }

    public void BeginActivity(
        ScrollActivity newOuterActivity,
        NestedScrollActivityGetter innerActivityGetter)
    {
        OuterPosition!.BeginActivity(newOuterActivity);
        bool scrolling = newOuterActivity.IsScrolling;
        foreach (NestedScrollPosition position in InnerPositions.ToList())
        {
            ScrollActivity newInnerActivity = innerActivityGetter(position);
            position.BeginActivity(newInnerActivity);
            scrolling = scrolling && newInnerActivity.IsScrolling;
        }

        ScrollDragController? drag = _currentDrag;
        _currentDrag = null;
        drag?.Dispose();
        if (!scrolling)
        {
            UpdateUserScrollDirection(ScrollDirection.Idle);
        }
    }

    private static IdleScrollActivity CreateIdleScrollActivity(NestedScrollPosition position)
    {
        return new IdleScrollActivity(position);
    }

    public void GoIdle()
    {
        BeginActivity(CreateIdleScrollActivity(OuterPosition!), CreateIdleScrollActivity);
    }

    public void GoBallistic(double velocity)
    {
        BeginActivity(
            CreateOuterBallisticScrollActivity(velocity),
            position => CreateInnerBallisticScrollActivity(position, velocity));
    }

    public ScrollActivity CreateInnerBallisticScrollActivity(NestedScrollPosition position, double velocity)
    {
        return position.CreateBallisticScrollActivity(
            position.Physics.CreateBallisticSimulation(GetMetrics(position, velocity), velocity),
            NestedBallisticScrollActivityMode.Inner);
    }

    public ScrollActivity CreateOuterBallisticScrollActivity(double velocity)
    {
        // Pick the inner position that is farthest away from the infinity that we are heading towards,
        // and use that to determine how much the outer scroll view may still travel.
        NestedScrollPosition? innerPosition = null;
        if (velocity != 0.0)
        {
            foreach (NestedScrollPosition position in InnerPositions)
            {
                if (innerPosition != null)
                {
                    if (velocity > 0.0)
                    {
                        if (innerPosition.Pixels < position.Pixels)
                        {
                            continue;
                        }
                    }
                    else if (innerPosition.Pixels > position.Pixels)
                    {
                        continue;
                    }
                }

                innerPosition = position;
            }
        }

        if (innerPosition == null)
        {
            // It's either just us or a velocity=0 situation.
            return OuterPosition!.CreateBallisticScrollActivity(
                OuterPosition.Physics.CreateBallisticSimulation(OuterPosition, velocity),
                NestedBallisticScrollActivityMode.Independent);
        }

        NestedScrollMetrics metrics = GetMetrics(innerPosition, velocity);
        return OuterPosition!.CreateBallisticScrollActivity(
            OuterPosition.Physics.CreateBallisticSimulation(metrics, velocity),
            NestedBallisticScrollActivityMode.Outer,
            metrics);
    }

    public NestedScrollMetrics GetMetrics(NestedScrollPosition innerPosition, double velocity)
    {
        NestedScrollPosition outer = OuterPosition!;
        double pixels;
        double minRange;
        double maxRange;
        double correctionOffset;
        double extra = 0.0;
        if (innerPosition.Pixels == innerPosition.MinScrollExtent)
        {
            pixels = Math.Clamp(outer.Pixels, outer.MinScrollExtent, outer.MaxScrollExtent);
            minRange = outer.MinScrollExtent;
            maxRange = outer.MaxScrollExtent;
            correctionOffset = 0.0;
        }
        else
        {
            pixels = innerPosition.Pixels < innerPosition.MinScrollExtent
                ? innerPosition.Pixels - innerPosition.MinScrollExtent + outer.MinScrollExtent
                : innerPosition.Pixels - innerPosition.MinScrollExtent + outer.MaxScrollExtent;

            if (velocity > 0.0 && innerPosition.Pixels > innerPosition.MinScrollExtent)
            {
                // Scrolling up: the inner is scrolled past its leading edge, so the outer may take the
                // pixels it still has left right away.
                extra = outer.MaxScrollExtent - outer.Pixels;
                minRange = pixels;
                maxRange = pixels + extra;
                correctionOffset = outer.Pixels - pixels;
            }
            else if (velocity < 0.0 && innerPosition.Pixels < innerPosition.MinScrollExtent)
            {
                // Scrolling down: the inner is underscrolled, so the outer may grow right away.
                extra = outer.Pixels - outer.MinScrollExtent;
                minRange = pixels - extra;
                maxRange = pixels;
                correctionOffset = outer.Pixels - pixels;
            }
            else
            {
                if (velocity > 0.0)
                {
                    extra = outer.MinScrollExtent - outer.Pixels;
                }
                else if (velocity < 0.0)
                {
                    extra = outer.Pixels - (outer.MaxScrollExtent - outer.MinScrollExtent);
                }

                minRange = outer.MinScrollExtent;
                maxRange = outer.MaxScrollExtent + extra;
                correctionOffset = 0.0;
            }
        }

        return new NestedScrollMetrics(
            Pixels: pixels,
            MinScrollExtent: outer.MinScrollExtent,
            MaxScrollExtent: outer.MaxScrollExtent
                             + innerPosition.MaxScrollExtent
                             - innerPosition.MinScrollExtent
                             + extra,
            ViewportDimension: outer.ViewportDimension,
            AxisDirection: outer.AxisDirection,
            MinRange: minRange,
            MaxRange: maxRange,
            CorrectionOffset: correctionOffset,
            DevicePixelRatio: outer.DevicePixelRatio);
    }

    /// <summary>Converts an offset in <paramref name="source"/>'s coordinates to outer coordinates.</summary>
    public double UnnestOffset(double value, NestedScrollPosition source)
    {
        NestedScrollPosition outer = OuterPosition!;
        if (ReferenceEquals(source, outer))
        {
            return Math.Clamp(value, outer.MinScrollExtent, outer.MaxScrollExtent);
        }

        if (value < source.MinScrollExtent)
        {
            return value - source.MinScrollExtent + outer.MinScrollExtent;
        }

        return value - source.MinScrollExtent + outer.MaxScrollExtent;
    }

    /// <summary>Converts an offset in outer coordinates to <paramref name="target"/>'s coordinates.</summary>
    public double NestOffset(double value, NestedScrollPosition target)
    {
        NestedScrollPosition outer = OuterPosition!;
        if (ReferenceEquals(target, outer))
        {
            return Math.Clamp(value, outer.MinScrollExtent, outer.MaxScrollExtent);
        }

        if (value < outer.MinScrollExtent)
        {
            return value - outer.MinScrollExtent + target.MinScrollExtent;
        }

        if (value > outer.MaxScrollExtent)
        {
            return value - outer.MaxScrollExtent + target.MinScrollExtent;
        }

        return target.MinScrollExtent;
    }

    public void UpdateCanDrag()
    {
        if (!OuterPosition!.HaveDimensions)
        {
            return;
        }

        bool innerCanDrag = false;
        foreach (NestedScrollPosition position in InnerPositions)
        {
            if (!position.HaveDimensions)
            {
                return;
            }

            innerCanDrag = innerCanDrag || position.Physics.ShouldAcceptUserOffset(position);
        }

        OuterPosition.UpdateCanDrag(innerCanDrag);
    }

    public Task AnimateTo(double to, TimeSpan duration, Curve curve)
    {
        DrivenScrollActivity outerActivity = OuterPosition!.CreateDrivenScrollActivity(
            NestOffset(to, OuterPosition),
            duration,
            curve);
        var resultFutures = new List<Task> { outerActivity.Done };
        BeginActivity(
            outerActivity,
            position =>
            {
                DrivenScrollActivity innerActivity = position.CreateDrivenScrollActivity(
                    NestOffset(to, position),
                    duration,
                    curve);
                resultFutures.Add(innerActivity.Done);
                return innerActivity;
            });
        return Task.WhenAll(resultFutures);
    }

    public void JumpTo(double to)
    {
        GoIdle();
        OuterPosition!.LocalJumpTo(NestOffset(to, OuterPosition));
        foreach (NestedScrollPosition position in InnerPositions.ToList())
        {
            position.LocalJumpTo(NestOffset(to, position));
        }

        GoBallistic(0.0);
    }

    public void PointerScroll(double delta)
    {
        // A pointer scroll uses the opposite sign convention to a drag: a positive delta scrolls the
        // content up, the way a positive drag delta scrolls it down.
        if (delta == 0.0)
        {
            GoBallistic(0.0);
            return;
        }

        GoIdle();
        UpdateUserScrollDirection(delta < 0.0 ? ScrollDirection.Forward : ScrollDirection.Reverse);
        NestedScrollPosition outer = OuterPosition!;
        List<NestedScrollPosition> innerPositions = InnerPositions.ToList();
        outer.IsScrollingNotifier.Value = true;
        outer.DidStartScroll();
        foreach (NestedScrollPosition position in innerPositions)
        {
            position.IsScrollingNotifier.Value = true;
            position.DidStartScroll();
        }

        if (innerPositions.Count == 0)
        {
            // Does not enter overscroll.
            outer.ApplyClampedPointerSignalUpdate(delta);
        }
        else if (delta > 0.0)
        {
            // Dragging "up" - delta is positive.
            double outerDelta = delta;
            foreach (NestedScrollPosition position in innerPositions)
            {
                if (position.Pixels < 0.0)
                {
                    // This inner position is in overscroll.
                    double potentialOuterDelta = position.ApplyClampedPointerSignalUpdate(delta);
                    outerDelta = Math.Max(outerDelta, potentialOuterDelta);
                }
            }

            if (outerDelta != 0.0)
            {
                double innerDelta = outer.ApplyClampedPointerSignalUpdate(outerDelta);
                if (innerDelta != 0.0)
                {
                    foreach (NestedScrollPosition position in innerPositions)
                    {
                        position.ApplyClampedPointerSignalUpdate(innerDelta);
                    }
                }
            }
        }
        else
        {
            // Dragging "down" - delta is negative.
            double innerDelta = delta;
            if (_floatHeaderSlivers)
            {
                innerDelta = outer.ApplyClampedPointerSignalUpdate(delta);
            }

            if (innerDelta != 0.0)
            {
                double outerDelta = 0.0;
                foreach (NestedScrollPosition position in innerPositions)
                {
                    outerDelta = Math.Min(outerDelta, position.ApplyClampedPointerSignalUpdate(innerDelta));
                }

                if (outerDelta != 0.0)
                {
                    outer.ApplyClampedPointerSignalUpdate(outerDelta);
                }
            }
        }

        outer.DidEndScroll();
        foreach (NestedScrollPosition position in innerPositions)
        {
            position.DidEndScroll();
        }

        GoBallistic(0.0);
    }

    double IScrollActivityDelegate.SetPixels(double pixels)
    {
        throw new InvalidOperationException(
            "A NestedScrollView coordinator does not own a scroll offset of its own.");
    }

    public IScrollHoldController Hold(Action? holdCancelCallback)
    {
        BeginActivity(
            new HoldScrollActivity(OuterPosition!, holdCancelCallback),
            position => new HoldScrollActivity(position));
        return this;
    }

    /// <summary>Ends the hold this coordinator handed out.</summary>
    public void Cancel() => GoBallistic(0.0);

    public ScrollDragController Drag(DragStartDetails details, Action? dragCancelCallback)
    {
        var drag = new ScrollDragController(
            @delegate: this,
            details: details,
            onDragCanceled: dragCancelCallback);
        BeginActivity(
            new DragScrollActivity(OuterPosition!, drag),
            position => new DragScrollActivity(position, drag));
        _currentDrag = drag;
        return drag;
    }

    public void ApplyUserOffset(double delta)
    {
        UpdateUserScrollDirection(delta > 0.0 ? ScrollDirection.Forward : ScrollDirection.Reverse);
        NestedScrollPosition outer = OuterPosition!;
        List<NestedScrollPosition> innerPositions = InnerPositions.ToList();
        if (innerPositions.Count == 0)
        {
            outer.ApplyFullDragUpdate(delta);
        }
        else if (delta < 0.0)
        {
            // Dragging "up" - delta is negative.
            double outerDelta = delta;
            foreach (NestedScrollPosition position in innerPositions)
            {
                if (position.Pixels < 0.0)
                {
                    // This inner position is in overscroll.
                    double potentialOuterDelta = position.ApplyClampedDragUpdate(delta);
                    // Only the first inner scroll view to reach the outer gets to move it.
                    outerDelta = Math.Max(outerDelta, potentialOuterDelta);
                }
            }

            if (Math.Abs(outerDelta) > Constants.PrecisionErrorTolerance)
            {
                double innerDelta = outer.ApplyClampedDragUpdate(outerDelta);
                if (innerDelta != 0.0)
                {
                    foreach (NestedScrollPosition position in innerPositions)
                    {
                        position.ApplyFullDragUpdate(innerDelta);
                    }
                }
            }
        }
        else
        {
            // Dragging "down" - delta is positive.
            double innerDelta = delta;
            // Apply delta to the outer header first if it is configured to float.
            if (_floatHeaderSlivers)
            {
                innerDelta = outer.ApplyClampedDragUpdate(delta);
            }

            if (innerDelta != 0.0)
            {
                // Apply the delta to the innerPositions.
                double outerDelta = 0.0; // it will go positive if it changes
                var overscrolls = new List<double>();
                foreach (NestedScrollPosition position in innerPositions)
                {
                    double overscroll = position.ApplyClampedDragUpdate(innerDelta);
                    outerDelta = Math.Max(outerDelta, overscroll);
                    overscrolls.Add(overscroll);
                }

                if (outerDelta != 0.0)
                {
                    outerDelta -= outer.ApplyClampedDragUpdate(outerDelta);
                }

                // Now deal with any overscroll.
                for (int i = 0; i < innerPositions.Count; i += 1)
                {
                    double remainingDelta = overscrolls[i] - outerDelta;
                    if (remainingDelta > 0.0)
                    {
                        innerPositions[i].ApplyFullDragUpdate(remainingDelta);
                    }
                }
            }
        }
    }

    public void SetParent(ScrollController? value)
    {
        _parent = value;
        UpdateParent();
    }

    public void UpdateParent()
    {
        OuterPosition?.SetParent(_parent ?? PrimaryScrollController.MaybeOf(_state.Context));
    }

    public void Dispose()
    {
        ScrollDragController? drag = _currentDrag;
        _currentDrag = null;
        drag?.Dispose();
        OuterController.Dispose();
        InnerController.Dispose();
    }

    public override string ToString()
    {
        return $"{nameof(NestedScrollCoordinator)}(outer={OuterController}; inner={InnerController})";
    }
}

/// <summary>One of the two controllers a <see cref="NestedScrollCoordinator"/> owns.</summary>
internal sealed class NestedScrollController : ScrollController
{
    private readonly NestedScrollCoordinator _coordinator;

    public NestedScrollController(NestedScrollCoordinator coordinator, double initialScrollOffset = 0.0)
        : base(initialScrollOffset)
    {
        _coordinator = coordinator;
    }

    public IEnumerable<NestedScrollPosition> NestedPositions => Positions.Cast<NestedScrollPosition>();

    public override ScrollPosition CreateScrollPosition(ScrollPhysics? physics = null)
    {
        return new NestedScrollPosition(
            _coordinator,
            physics ?? Physics,
            InitialScrollOffset,
            KeepScrollOffset);
    }

    internal override void Attach(ScrollPosition position)
    {
        base.Attach(position);
        _coordinator.UpdateParent();
        _coordinator.UpdateCanDrag();
        position.AddListener(ScheduleUpdateShadow);
        ScheduleUpdateShadow();
    }

    internal override void Detach(ScrollPosition position)
    {
        ((NestedScrollPosition)position).SetParent(null);
        position.RemoveListener(ScheduleUpdateShadow);
        base.Detach(position);
        ScheduleUpdateShadow();
    }

    private void ScheduleUpdateShadow()
    {
        // We do this asynchronously because the shadow is driven by whether the body has scrolled,
        // which cannot be answered while the body is still being laid out.
        Scheduler.AddPostFrameCallback(_ => _coordinator.UpdateShadow());
    }
}

/// <summary>
/// One of the scroll positions a <see cref="NestedScrollCoordinator"/> drives; every scroll it
/// receives is forwarded to the coordinator, which splits it across the whole nested view.
/// </summary>
internal sealed class NestedScrollPosition : ScrollPosition
{
    private readonly NestedScrollCoordinator _coordinator;
    private ScrollController? _parent;

    public NestedScrollPosition(
        NestedScrollCoordinator coordinator,
        ScrollPhysics? physics,
        double initialPixels = 0.0,
        bool keepScrollOffset = true) : base(initialPixels, physics, keepScrollOffset)
    {
        _coordinator = coordinator;
        // In case we did not restore but could, so that we do not restore it later.
        SaveScrollOffset();
    }

    public void SetParent(ScrollController? value)
    {
        _parent?.Detach(this);
        _parent = value;
        _parent?.Attach(this);
    }

    public override void RestoreScrollOffset()
    {
        if (_coordinator.CanScrollBody)
        {
            base.RestoreScrollOffset();
        }
    }

    public override ScrollDirection UserScrollDirection => _coordinator.UserScrollDirection;

    public override void ApplyUserOffset(double delta)
    {
        throw new InvalidOperationException(
            "A NestedScrollView's positions are driven through its coordinator.");
    }

    protected override void ApplyNewDimensions()
    {
        base.ApplyNewDimensions();
        _coordinator.UpdateCanDrag();
    }

    public override IScrollHoldController Hold(Action? holdCancelCallback = null)
    {
        return _coordinator.Hold(holdCancelCallback);
    }

    public override ScrollDragController Drag(DragStartDetails details, Action? dragCancelCallback = null)
    {
        return _coordinator.Drag(details, dragCancelCallback);
    }

    public override Task AnimateTo(double to, TimeSpan duration, Curve? curve = null)
    {
        return _coordinator.AnimateTo(
            _coordinator.UnnestOffset(to, this),
            duration,
            curve ?? Curves.Linear);
    }

    public override void JumpTo(double pixels)
    {
        _coordinator.JumpTo(_coordinator.UnnestOffset(pixels, this));
    }

    public override void ApplyPointerScrollDelta(double delta)
    {
        _coordinator.PointerScroll(delta);
    }

    /// <summary>
    /// Applies a drag update that can reduce an existing overscroll but never enter one, returning
    /// the part of the delta this position did not use.
    /// </summary>
    public double ApplyClampedDragUpdate(double delta)
    {
        // If we are going towards the maxScrollExtent (negative scroll offset), then the furthest we
        // can be in the minScrollExtent direction is negative infinity.
        double min = delta < 0.0 ? double.NegativeInfinity : Math.Min(MinScrollExtent, Pixels);
        // The logic for max is equivalent but on the other side.
        double max = delta > 0.0
            ? double.PositiveInfinity
            : Pixels < 0.0
                ? 0.0
                : Math.Max(MaxScrollExtent, Pixels);
        double oldPixels = Pixels;
        double newPixels = Math.Clamp(Pixels - delta, min, max);
        double clampedDelta = newPixels - Pixels;
        if (clampedDelta == 0.0)
        {
            return delta;
        }

        double overscroll = Physics.ApplyBoundaryConditions(this, newPixels);
        double actualNewPixels = newPixels - overscroll;
        double offset = actualNewPixels - oldPixels;
        if (offset != 0.0)
        {
            ForcePixels(actualNewPixels);
            DidUpdateScrollPositionBy(offset);
        }

        double result = delta + offset;
        return Math.Abs(result) < Constants.PrecisionErrorTolerance ? 0.0 : result;
    }

    /// <summary>Applies a drag update through the physics, returning the overscroll it produced.</summary>
    public double ApplyFullDragUpdate(double delta)
    {
        double oldPixels = Pixels;
        // Apply friction.
        double newPixels = Pixels - Physics.ApplyPhysicsToUserOffset(this, delta);
        if (Math.Abs(oldPixels - newPixels) < Constants.PrecisionErrorTolerance)
        {
            // Delta must have been so small as to be discarded.
            return 0.0;
        }

        // Check for overscroll.
        double overscroll = Physics.ApplyBoundaryConditions(this, newPixels);
        double actualNewPixels = newPixels - overscroll;
        if (actualNewPixels != oldPixels)
        {
            ForcePixels(actualNewPixels);
            DidUpdateScrollPositionBy(actualNewPixels - oldPixels);
        }

        if (overscroll != 0.0)
        {
            DidOverscrollBy(overscroll);
            return overscroll;
        }

        return 0.0;
    }

    /// <summary>
    /// Applies a pointer-signal update, which never overscrolls and never consults the physics,
    /// returning the part of the delta this position did not use.
    /// </summary>
    public double ApplyClampedPointerSignalUpdate(double delta)
    {
        double min = delta > 0.0 ? double.NegativeInfinity : Math.Min(MinScrollExtent, Pixels);
        // The logic for max is equivalent but on the other side.
        double max = delta < 0.0 ? double.PositiveInfinity : Math.Max(MaxScrollExtent, Pixels);
        double newPixels = Math.Clamp(Pixels + delta, min, max);
        double clampedDelta = newPixels - Pixels;
        if (clampedDelta == 0.0)
        {
            return delta;
        }

        ForcePixels(newPixels);
        DidUpdateScrollPositionBy(clampedDelta);
        return delta - clampedDelta;
    }

    public DrivenScrollActivity CreateDrivenScrollActivity(double to, TimeSpan duration, Curve curve)
    {
        return new DrivenScrollActivity(
            this,
            from: Pixels,
            to: to,
            duration: duration,
            curve: curve,
            vsync: TickerProvider);
    }

    public override void GoIdle()
    {
        BeginActivity(new IdleScrollActivity(this));
        _coordinator.UpdateUserScrollDirection(ScrollDirection.Idle);
    }

    public override void GoBallistic(double velocity)
    {
        Simulation? simulation = null;
        if (velocity != 0.0 || OutOfRange)
        {
            simulation = Physics.CreateBallisticSimulation(this, velocity);
        }

        BeginActivity(
            CreateBallisticScrollActivity(simulation, NestedBallisticScrollActivityMode.Independent));
    }

    public ScrollActivity CreateBallisticScrollActivity(
        Simulation? simulation,
        NestedBallisticScrollActivityMode mode,
        NestedScrollMetrics? metrics = null)
    {
        if (simulation == null)
        {
            return new IdleScrollActivity(this);
        }

        switch (mode)
        {
            case NestedBallisticScrollActivityMode.Outer:
                ArgumentNullException.ThrowIfNull(metrics);
                if (metrics.MinRange == metrics.MaxRange)
                {
                    return new IdleScrollActivity(this);
                }

                return new NestedOuterBallisticScrollActivity(
                    _coordinator,
                    this,
                    metrics,
                    simulation,
                    TickerProvider);
            case NestedBallisticScrollActivityMode.Inner:
                return new NestedInnerBallisticScrollActivity(
                    _coordinator,
                    this,
                    simulation,
                    TickerProvider);
            default:
                return new BallisticScrollActivity(this, simulation, TickerProvider);
        }
    }

    /// <summary>Moves this position without letting the coordinator redistribute the jump.</summary>
    public void LocalJumpTo(double value)
    {
        if (Pixels == value)
        {
            return;
        }

        double oldPixels = Pixels;
        ForcePixels(value);
        DidStartScroll();
        DidUpdateScrollPositionBy(Pixels - oldPixels);
        DidEndScroll();
    }

    /// <summary>
    /// Tells the outer scrollable whether it may be dragged, which it may whenever either scroll view
    /// still accepts a user offset.
    /// </summary>
    public void UpdateCanDrag(bool innerCanDrag)
    {
        CanDragChanged?.Invoke(Physics.ShouldAcceptUserOffset(this) || innerCanDrag);
    }
}

/// <summary>The ballistic activity an inner position of a nested view runs.</summary>
internal sealed class NestedInnerBallisticScrollActivity : BallisticScrollActivity
{
    private readonly NestedScrollCoordinator _coordinator;

    public NestedInnerBallisticScrollActivity(
        NestedScrollCoordinator coordinator,
        NestedScrollPosition position,
        Simulation simulation,
        ITickerProvider? vsync) : base(position, simulation, vsync)
    {
        _coordinator = coordinator;
    }

    private NestedScrollPosition Position => (NestedScrollPosition)Delegate;

    public override void ResetActivity()
    {
        Position.BeginActivity(_coordinator.CreateInnerBallisticScrollActivity(Position, Velocity));
    }

    public override void ApplyNewDimensions()
    {
        Position.BeginActivity(_coordinator.CreateInnerBallisticScrollActivity(Position, Velocity));
    }

    protected override bool ApplyMoveTo(double value)
    {
        return base.ApplyMoveTo(_coordinator.NestOffset(value, Position));
    }
}

/// <summary>
/// The ballistic activity the outer position of a nested view runs, which stops as soon as the
/// simulation leaves the range the inner positions left for it.
/// </summary>
internal sealed class NestedOuterBallisticScrollActivity : BallisticScrollActivity
{
    private readonly NestedScrollCoordinator _coordinator;
    private readonly NestedScrollMetrics _metrics;

    public NestedOuterBallisticScrollActivity(
        NestedScrollCoordinator coordinator,
        NestedScrollPosition position,
        NestedScrollMetrics metrics,
        Simulation simulation,
        ITickerProvider? vsync) : base(position, simulation, vsync)
    {
        if (metrics.MinRange >= metrics.MaxRange)
        {
            throw new ArgumentOutOfRangeException(
                nameof(metrics),
                "An outer ballistic activity needs a non-empty range to travel through.");
        }

        _coordinator = coordinator;
        _metrics = metrics;
    }

    private NestedScrollPosition Position => (NestedScrollPosition)Delegate;

    public override void ResetActivity()
    {
        Position.BeginActivity(_coordinator.CreateOuterBallisticScrollActivity(Velocity));
    }

    public override void ApplyNewDimensions()
    {
        Position.BeginActivity(_coordinator.CreateOuterBallisticScrollActivity(Velocity));
    }

    protected override bool ApplyMoveTo(double value)
    {
        bool done = false;
        if (Velocity > 0.0)
        {
            if (value < _metrics.MinRange)
            {
                return true;
            }

            if (value > _metrics.MaxRange)
            {
                value = _metrics.MaxRange;
                done = true;
            }
        }
        else if (Velocity < 0.0)
        {
            if (value > _metrics.MaxRange)
            {
                return true;
            }

            if (value < _metrics.MinRange)
            {
                value = _metrics.MinRange;
                done = true;
            }
        }
        else
        {
            value = Math.Clamp(value, _metrics.MinRange, _metrics.MaxRange);
            done = true;
        }

        // Since we tried to pass an in-range value, this should never overflow.
        base.ApplyMoveTo(value + _metrics.CorrectionOffset);
        return !done;
    }

    public override string ToString()
    {
        return $"{nameof(NestedOuterBallisticScrollActivity)}({_metrics.MinRange} .. "
               + $"{_metrics.MaxRange}; correcting by {_metrics.CorrectionOffset})";
    }
}

/// <summary>
/// A sliver that wraps another sliver and takes the overlap that sliver applies to the slivers after
/// it out of the outer scroll view, reporting it through <see cref="Handle"/>.
/// </summary>
public class SliverOverlapAbsorber : SingleChildRenderObjectWidget
{
    public SliverOverlapAbsorber(
        SliverOverlapAbsorberHandle handle,
        Widget? sliver = null,
        Key? key = null) : base(sliver, key)
    {
        Handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>The handle the absorbed overlap is reported through.</summary>
    public SliverOverlapAbsorberHandle Handle { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverOverlapAbsorber(Handle);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderSliverOverlapAbsorber)renderObject).Handle = Handle;
    }
}

/// <summary>
/// A sliver that reproduces the overlap a <see cref="SliverOverlapAbsorber"/> removed, so a sliver
/// inside the body of a <see cref="NestedScrollView"/> starts below the header pinned over it.
/// </summary>
public class SliverOverlapInjector : SingleChildRenderObjectWidget
{
    public SliverOverlapInjector(
        SliverOverlapAbsorberHandle handle,
        Widget? sliver = null,
        Key? key = null) : base(sliver, key)
    {
        Handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>The handle whose absorbed overlap is injected.</summary>
    public SliverOverlapAbsorberHandle Handle { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSliverOverlapInjector(Handle);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderSliverOverlapInjector)renderObject).Handle = Handle;
    }
}

/// <summary>The viewport the outer scroll view of a <see cref="NestedScrollView"/> uses.</summary>
public class NestedScrollViewViewport : Viewport
{
    public NestedScrollViewViewport(
        ViewportOffset offset,
        SliverOverlapAbsorberHandle handle,
        IReadOnlyList<Widget>? slivers = null,
        AxisDirection axisDirection = AxisDirection.Down,
        AxisDirection? crossAxisDirection = null,
        double anchor = 0.0,
        Key? center = null,
        Clip clipBehavior = Clip.HardEdge,
        Key? key = null) : base(
        offset: offset,
        slivers: slivers ?? [],
        axisDirection: axisDirection,
        crossAxisDirection: crossAxisDirection,
        anchor: anchor,
        center: center,
        clipBehavior: clipBehavior,
        key: key)
    {
        Handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>The handle the injectors in the body listen to.</summary>
    public SliverOverlapAbsorberHandle Handle { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderNestedScrollViewViewport(
            offset: Offset,
            handle: Handle,
            axisDirection: AxisDirection,
            crossAxisDirection: CrossAxisDirection
                                ?? GetDefaultCrossAxisDirection(context, AxisDirection),
            anchor: Anchor,
            clipBehavior: ClipBehavior);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var viewport = (RenderNestedScrollViewViewport)renderObject;
        viewport.AxisDirection = AxisDirection;
        viewport.CrossAxisDirection = CrossAxisDirection
                                      ?? GetDefaultCrossAxisDirection(context, AxisDirection);
        viewport.Anchor = Anchor;
        viewport.Offset = Offset;
        viewport.Handle = Handle;
        viewport.ClipBehavior = ClipBehavior;
    }
}
