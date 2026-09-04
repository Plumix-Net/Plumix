using System.Diagnostics;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Avalonia;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/scrollable.dart (2D SCROLLING)

namespace Plumix.Widgets;

/// <summary>
/// How the drag gesture recognizers of a <see cref="TwoDimensionalScrollable"/> are configured.
/// </summary>
/// <remarks>Flutter's <c>DiagonalDragBehavior</c>.</remarks>
public enum DiagonalDragBehavior
{
    /// <summary>
    /// No diagonal scrolling: a drag in one direction locks the input axis until it is released.
    /// </summary>
    None,

    /// <summary>
    /// Diagonal scrolling on a weighted scale, evaluated once per gesture: after the drag is first
    /// evaluated, the weighted result stands until the gesture is released.
    /// </summary>
    WeightedEvent,

    /// <summary>
    /// Diagonal scrolling on a weighted scale that is re-evaluated on every drag update, so the
    /// locked axis can change during one gesture.
    /// </summary>
    WeightedContinuous,

    /// <summary>Free movement in any and all directions while dragging.</summary>
    Free,
}

/// <summary>
/// Builds the viewport a <see cref="TwoDimensionalScrollable"/> scrolls, from both of its offsets.
/// </summary>
/// <remarks>Flutter's <c>TwoDimensionalViewportBuilder</c>.</remarks>
public delegate Widget TwoDimensionalViewportBuilder(
    BuildContext context,
    ViewportOffset verticalPosition,
    ViewportOffset horizontalPosition);

/// <summary>
/// A widget that scrolls on two axes at once, by nesting a horizontal <see cref="Scrollable"/> inside
/// a vertical one and handing both offsets to one viewport.
/// </summary>
/// <remarks>Flutter's <c>TwoDimensionalScrollable</c>.</remarks>
public class TwoDimensionalScrollable : StatefulWidget
{
    public TwoDimensionalScrollable(
        ScrollableDetails horizontalDetails,
        ScrollableDetails verticalDetails,
        TwoDimensionalViewportBuilder viewportBuilder,
        ScrollIncrementCalculator? incrementCalculator = null,
        string? restorationId = null,
        bool excludeFromSemantics = false,
        DiagonalDragBehavior diagonalDragBehavior = DiagonalDragBehavior.None,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(horizontalDetails);
        ArgumentNullException.ThrowIfNull(verticalDetails);
        ArgumentNullException.ThrowIfNull(viewportBuilder);
        DiagonalDragBehavior = diagonalDragBehavior;
        HorizontalDetails = horizontalDetails;
        VerticalDetails = verticalDetails;
        ViewportBuilder = viewportBuilder;
        IncrementCalculator = incrementCalculator;
        RestorationId = restorationId;
        ExcludeFromSemantics = excludeFromSemantics;
        HitTestBehavior = hitTestBehavior;
        DragStartBehavior = dragStartBehavior;
    }

    /// <summary>How scrolling gestures lock to one axis, or move freely in both.</summary>
    public DiagonalDragBehavior DiagonalDragBehavior { get; }

    /// <summary>The configuration of the horizontal <see cref="Scrollable"/>.</summary>
    public ScrollableDetails HorizontalDetails { get; }

    /// <summary>The configuration of the vertical <see cref="Scrollable"/>.</summary>
    public ScrollableDetails VerticalDetails { get; }

    /// <summary>Builds the viewport through which the content is seen.</summary>
    public TwoDimensionalViewportBuilder ViewportBuilder { get; }

    /// <summary>Computes a keyboard-driven scroll increment; applies to both axes.</summary>
    public ScrollIncrementCalculator? IncrementCalculator { get; }

    /// <summary>
    /// The restoration ID of the scope this widget introduces; both inner scrollables get their own
    /// unique IDs inside it.
    /// </summary>
    public string? RestorationId { get; }

    /// <summary>Whether both scrollables contribute no semantics of their own.</summary>
    public bool ExcludeFromSemantics { get; }

    /// <summary>How both scrollables behave during hit testing.</summary>
    public HitTestBehavior HitTestBehavior { get; }

    /// <summary>When drag gestures start; applies to both axes.</summary>
    public DragStartBehavior DragStartBehavior { get; }

    public override State CreateState() => new TwoDimensionalScrollableState();

    /// <summary>The state of the closest enclosing <see cref="TwoDimensionalScrollable"/>, or null.</summary>
    public static TwoDimensionalScrollableState? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<TwoDimensionalScrollableScope>()?.TwoDimensionalScrollable;
    }

    /// <summary>The state of the closest enclosing <see cref="TwoDimensionalScrollable"/>.</summary>
    public static TwoDimensionalScrollableState Of(BuildContext context)
    {
        return MaybeOf(context)
               ?? throw new InvalidOperationException(
                   "TwoDimensionalScrollable.of() was called with a context that does not contain a "
                   + "TwoDimensionalScrollable widget.\n"
                   + "No TwoDimensionalScrollable widget ancestor could be found starting from the "
                   + "context that was passed to TwoDimensionalScrollable.of(). This can happen "
                   + "because you are using a widget that looks for a TwoDimensionalScrollable "
                   + "ancestor, but no such ancestor exists.");
    }
}

/// <remarks>
/// Flutter's private <c>_TwoDimensionalScrollableScope</c>: it lets
/// <see cref="TwoDimensionalScrollable.Of"/> work as if the state were an inherited widget. The state
/// object never changes identity, so it never notifies.
/// </remarks>
internal sealed class TwoDimensionalScrollableScope : InheritedWidget
{
    public TwoDimensionalScrollableScope(
        TwoDimensionalScrollableState twoDimensionalScrollable,
        Widget child,
        Key? key = null) : base(key)
    {
        TwoDimensionalScrollable = twoDimensionalScrollable;
        Child = child;
    }

    public TwoDimensionalScrollableState TwoDimensionalScrollable { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget) => false;
}

/// <summary>State for a <see cref="TwoDimensionalScrollable"/>.</summary>
public class TwoDimensionalScrollableState : State
{
    private readonly GlobalObjectKey<Scrollable.ScrollableState> _verticalOuterScrollableKey =
        new(new object());
    private readonly GlobalObjectKey<Scrollable.ScrollableState> _horizontalInnerScrollableKey =
        new(new object());

    private ScrollController? _verticalFallbackController;
    private ScrollController? _horizontalFallbackController;

    private TwoDimensionalScrollable CurrentWidget => (TwoDimensionalScrollable)StateWidget;

    /// <summary>The <see cref="ScrollableState"/> of the vertical axis.</summary>
    public Scrollable.ScrollableState VerticalScrollable
    {
        get
        {
            Debug.Assert(_verticalOuterScrollableKey.CurrentState != null);
            return _verticalOuterScrollableKey.CurrentState!;
        }
    }

    /// <summary>The <see cref="ScrollableState"/> of the horizontal axis.</summary>
    public Scrollable.ScrollableState HorizontalScrollable
    {
        get
        {
            Debug.Assert(_horizontalInnerScrollableKey.CurrentState != null);
            return _horizontalInnerScrollableKey.CurrentState!;
        }
    }

    public override void InitState()
    {
        if (CurrentWidget.VerticalDetails.Controller is null)
        {
            _verticalFallbackController = new ScrollController();
        }

        if (CurrentWidget.HorizontalDetails.Controller is null)
        {
            _horizontalFallbackController = new ScrollController();
        }

        base.InitState();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var previous = (TwoDimensionalScrollable)oldWidget;
        TwoDimensionalScrollable current = CurrentWidget;

        // Handle changes in the provided/fallback scroll controllers.
        if (!ReferenceEquals(previous.VerticalDetails.Controller, current.VerticalDetails.Controller))
        {
            if (previous.VerticalDetails.Controller is null)
            {
                Debug.Assert(_verticalFallbackController != null);
                Debug.Assert(current.VerticalDetails.Controller != null);
                _verticalFallbackController!.Dispose();
                _verticalFallbackController = null;
            }
            else if (current.VerticalDetails.Controller is null)
            {
                Debug.Assert(_verticalFallbackController is null);
                _verticalFallbackController = new ScrollController();
            }
        }

        if (!ReferenceEquals(previous.HorizontalDetails.Controller, current.HorizontalDetails.Controller))
        {
            if (previous.HorizontalDetails.Controller is null)
            {
                Debug.Assert(_horizontalFallbackController != null);
                Debug.Assert(current.HorizontalDetails.Controller != null);
                _horizontalFallbackController!.Dispose();
                _horizontalFallbackController = null;
            }
            else if (current.HorizontalDetails.Controller is null)
            {
                Debug.Assert(_horizontalFallbackController is null);
                _horizontalFallbackController = new ScrollController();
            }
        }
    }

    public override Widget Build(BuildContext context)
    {
        TwoDimensionalScrollable widget = CurrentWidget;
        if (ScrollDirectionUtils.AxisDirectionToAxis(widget.VerticalDetails.Direction) != Axis.Vertical)
        {
            throw new AssertionError("TwoDimensionalScrollable.verticalDetails are not Axis.vertical.");
        }

        if (ScrollDirectionUtils.AxisDirectionToAxis(widget.HorizontalDetails.Direction) != Axis.Horizontal)
        {
            throw new AssertionError("TwoDimensionalScrollable.horizontalDetails are not Axis.horizontal.");
        }

        Widget result = new RestorationScope(
            restorationId: widget.RestorationId,
            child: new VerticalOuterDimension(
                key: _verticalOuterScrollableKey,
                // For gesture forwarding.
                horizontalKey: _horizontalInnerScrollableKey,
                axisDirection: widget.VerticalDetails.Direction,
                controller: widget.VerticalDetails.Controller ?? _verticalFallbackController!,
                physics: widget.VerticalDetails.Physics,
                clipBehavior: widget.VerticalDetails.DecorationClipBehavior ?? Clip.HardEdge,
                incrementCalculator: widget.IncrementCalculator,
                excludeFromSemantics: widget.ExcludeFromSemantics,
                restorationId: "OuterVerticalTwoDimensionalScrollable",
                dragStartBehavior: widget.DragStartBehavior,
                diagonalDragBehavior: widget.DiagonalDragBehavior,
                hitTestBehavior: widget.HitTestBehavior,
                viewportBuilder: (_, verticalOffset) => new HorizontalInnerDimension(
                    key: _horizontalInnerScrollableKey,
                    verticalOuterKey: _verticalOuterScrollableKey,
                    axisDirection: widget.HorizontalDetails.Direction,
                    controller: widget.HorizontalDetails.Controller ?? _horizontalFallbackController!,
                    physics: widget.HorizontalDetails.Physics,
                    clipBehavior: widget.HorizontalDetails.DecorationClipBehavior ?? Clip.HardEdge,
                    incrementCalculator: widget.IncrementCalculator,
                    excludeFromSemantics: widget.ExcludeFromSemantics,
                    restorationId: "InnerHorizontalTwoDimensionalScrollable",
                    dragStartBehavior: widget.DragStartBehavior,
                    diagonalDragBehavior: widget.DiagonalDragBehavior,
                    hitTestBehavior: widget.HitTestBehavior,
                    viewportBuilder: (innerContext, horizontalOffset) =>
                        widget.ViewportBuilder(innerContext, verticalOffset, horizontalOffset))));

        // TODO(Piinks): Build scrollbars for 2 dimensions instead of 1,
        // https://github.com/flutter/flutter/issues/122348
        return new TwoDimensionalScrollableScope(this, result);
    }

    public override void Dispose()
    {
        _verticalFallbackController?.Dispose();
        _horizontalFallbackController?.Dispose();
        base.Dispose();
    }
}

/// <remarks>Flutter's private <c>_VerticalOuterDimension</c>: the outer scrollable of 2D scrolling.</remarks>
internal sealed class VerticalOuterDimension : Scrollable
{
    public VerticalOuterDimension(
        GlobalObjectKey<Scrollable.ScrollableState> horizontalKey,
        Func<BuildContext, ViewportOffset, Widget> viewportBuilder,
        AxisDirection axisDirection,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        Clip clipBehavior = Clip.HardEdge,
        ScrollIncrementCalculator? incrementCalculator = null,
        bool excludeFromSemantics = false,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        string? restorationId = null,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        DiagonalDragBehavior diagonalDragBehavior = DiagonalDragBehavior.None,
        Key? key = null) : base(
            axisDirection: axisDirection,
            controller: controller,
            physics: physics,
            clipBehavior: clipBehavior,
            incrementCalculator: incrementCalculator,
            excludeFromSemantics: excludeFromSemantics,
            dragStartBehavior: dragStartBehavior,
            restorationId: restorationId,
            hitTestBehavior: hitTestBehavior,
            key: key)
    {
        Debug.Assert(axisDirection is AxisDirection.Up or AxisDirection.Down);
        DiagonalDragBehavior = diagonalDragBehavior;
        HorizontalKey = horizontalKey;
        ViewportBuilder = viewportBuilder;
    }

    public DiagonalDragBehavior DiagonalDragBehavior { get; }

    public GlobalObjectKey<Scrollable.ScrollableState> HorizontalKey { get; }

    public override State CreateState() => new VerticalOuterDimensionState();
}

internal sealed class VerticalOuterDimensionState : Scrollable.ScrollableState
{
    private Axis? _lockedAxis;
    private Point? _lastDragOffset;

    private VerticalOuterDimension TypedWidget => (VerticalOuterDimension)CurrentWidget;

    private DiagonalDragBehavior DiagonalDragBehavior => TypedWidget.DiagonalDragBehavior;

    private Scrollable.ScrollableState HorizontalScrollable => TypedWidget.HorizontalKey.CurrentState!;

    /// <inheritdoc />
    /// <remarks>Implemented in <see cref="HorizontalInnerDimensionState"/> instead.</remarks>
    private protected override (IReadOnlyList<Task> Futures, Scrollable.ScrollableState Next) PerformEnsureVisible(
        RenderObject renderObject,
        double alignment,
        TimeSpan duration,
        Curve? curve,
        ScrollPositionAlignmentPolicy alignmentPolicy,
        RenderObject? targetRenderObject)
    {
        Debug.Fail(
            "The PerformEnsureVisible method was called for the vertical scrollable of a "
            + "TwoDimensionalScrollable. This should not happen as the horizontal scrollable handles "
            + "both axes.");
        return ([], this);
    }

    private void EvaluateLockedAxis(Point offset)
    {
        Debug.Assert(_lastDragOffset != null);
        Point last = _lastDragOffset!.Value;
        double deltaX = last.X - offset.X;
        double deltaY = last.Y - offset.Y;
        double axisDifferential = Math.Abs(deltaX) - Math.Abs(deltaY);
        if (Math.Abs(axisDifferential) >= GestureConstants.TouchSlop)
        {
            // We have a single axis winner.
            _lockedAxis = axisDifferential > 0.0 ? Axis.Horizontal : Axis.Vertical;
        }
        else
        {
            _lockedAxis = null;
        }
    }

    private protected override void HandleDragDown(DragDownDetails details)
    {
        if (DiagonalDragBehavior != DiagonalDragBehavior.None)
        {
            // Initiate the hold on both axes. If one or the other wins the gesture, the opposite
            // axis is cancelled.
            HorizontalScrollable.ForwardDragDown(details);
        }

        base.HandleDragDown(details);
    }

    private protected override void HandleDragStart(DragStartDetails details)
    {
        _lastDragOffset = details.GlobalPosition;
        switch (DiagonalDragBehavior)
        {
            case DiagonalDragBehavior.None:
                break;
            case DiagonalDragBehavior.Free:
                // Prepare to scroll both; the vertical axis is handled by the base call below.
                HorizontalScrollable.ForwardDragStart(details);
                break;
            default:
                // See if one axis wins the drag.
                EvaluateLockedAxis(details.GlobalPosition);
                if (_lockedAxis is null)
                {
                    // Prepare to scroll both; null means no winner yet.
                    HorizontalScrollable.ForwardDragStart(details);
                }
                else if (_lockedAxis == Axis.Horizontal)
                {
                    // Prepare to scroll horizontally only.
                    HorizontalScrollable.ForwardDragStart(details);
                    return;
                }

                break;
        }

        base.HandleDragStart(details);
    }

    private protected override void HandleDragUpdate(DragUpdateDetails details)
    {
        var verticalDragDetails = new DragUpdateDetails(
            GlobalPosition: details.GlobalPosition,
            LocalPosition: details.LocalPosition,
            Delta: new Point(0.0, details.Delta.Y),
            PrimaryDelta: details.Delta.Y,
            SourceTimeStampUtc: details.SourceTimeStampUtc,
            Kind: details.Kind);
        var horizontalDragDetails = new DragUpdateDetails(
            GlobalPosition: details.GlobalPosition,
            LocalPosition: details.LocalPosition,
            Delta: new Point(details.Delta.X, 0.0),
            PrimaryDelta: details.Delta.X,
            SourceTimeStampUtc: details.SourceTimeStampUtc,
            Kind: details.Kind);

        switch (DiagonalDragBehavior)
        {
            case DiagonalDragBehavior.None:
                // Default gesture handling for one axis.
                base.HandleDragUpdate(verticalDragDetails);
                return;
            case DiagonalDragBehavior.Free:
                HorizontalScrollable.ForwardDragUpdate(horizontalDragDetails);
                base.HandleDragUpdate(verticalDragDetails);
                return;
            case DiagonalDragBehavior.WeightedContinuous:
                // Re-evaluate the locked axis for every update.
                EvaluateLockedAxis(details.GlobalPosition);
                _lastDragOffset = details.GlobalPosition;
                break;
            default:
                // Lock the axis only once per gesture.
                if (_lockedAxis is null && _lastDragOffset != null)
                {
                    EvaluateLockedAxis(details.GlobalPosition);
                }

                break;
        }

        if (_lockedAxis is null)
        {
            HorizontalScrollable.ForwardDragUpdate(horizontalDragDetails);
        }
        else if (_lockedAxis == Axis.Horizontal)
        {
            HorizontalScrollable.ForwardDragUpdate(horizontalDragDetails);
            return;
        }

        base.HandleDragUpdate(verticalDragDetails);
    }

    private protected override void HandleDragEnd(DragEndDetails details)
    {
        _lastDragOffset = null;
        _lockedAxis = null;
        double dx = details.Velocity.PixelsPerSecond.X;
        double dy = details.Velocity.PixelsPerSecond.Y;
        var verticalDragDetails = new DragEndDetails(
            velocity: new Velocity(new Vector(0.0, dy)),
            primaryVelocity: dy,
            globalPosition: details.GlobalPosition,
            localPosition: details.LocalPosition);
        var horizontalDragDetails = new DragEndDetails(
            velocity: new Velocity(new Vector(dx, 0.0)),
            primaryVelocity: dx,
            globalPosition: details.GlobalPosition,
            localPosition: details.LocalPosition);

        if (DiagonalDragBehavior != DiagonalDragBehavior.None)
        {
            HorizontalScrollable.ForwardDragEnd(horizontalDragDetails);
        }

        base.HandleDragEnd(verticalDragDetails);
    }

    private protected override void HandleDragCancel()
    {
        _lastDragOffset = null;
        _lockedAxis = null;
        if (DiagonalDragBehavior != DiagonalDragBehavior.None)
        {
            HorizontalScrollable.ForwardDragCancel();
        }

        base.HandleDragCancel();
    }

    public override void SetCanDrag(bool value)
    {
        if (DiagonalDragBehavior == DiagonalDragBehavior.None)
        {
            // Without diagonal scrolling the default drag gesture recognizer is used.
            base.SetCanDrag(value);
            return;
        }

        if (!value)
        {
            // Flutter leaves the pan recognizer installed here, so an axis with no scrollable
            // content does not take the other axis's gestures down with it.
            return;
        }

        _gestureRecognizers = new Dictionary<Type, IGestureRecognizerFactory>
        {
            [typeof(PanGestureRecognizer)] =
                new GestureRecognizerFactoryWithHandlers<PanGestureRecognizer>(
                    () => new PanGestureRecognizer { SupportedDevices = Configuration.DragDevices },
                    instance =>
                    {
                        instance.OnDown = HandleDragDown;
                        instance.OnStart = HandleDragStart;
                        instance.OnUpdate = HandleDragUpdate;
                        instance.OnEnd = HandleDragEnd;
                        instance.OnCancel = HandleDragCancel;
                        instance.MinFlingDistance = EffectivePhysics.MinFlingDistance;
                        instance.MinFlingVelocity = EffectivePhysics.MinFlingVelocity;
                        instance.MaxFlingVelocity = EffectivePhysics.MaxFlingVelocity;
                        instance.VelocityTrackerBuilder = Configuration.VelocityTrackerBuilder(Context);
                        instance.DragStartBehavior = CurrentWidget.DragStartBehavior;
                        instance.GestureSettings = MediaQuery.MaybeGestureSettingsOf(Context);
                    }),
        };

        // Cancel the active hold/drag (if any) because the gesture recognizers are about to be
        // disposed by the RawGestureDetector, so no pointer up will arrive to cancel them.
        HandleDragCancel();
        _lastCanDrag = value;
        _lastAxis = CurrentWidget.Axis;
        _gestureDetectorKey.CurrentState?.ReplaceGestureRecognizers(_gestureRecognizers);
    }

    /// <inheritdoc />
    /// <remarks>The dual scrollbar is the 2D scrollable's job, so only the indicator is built here.</remarks>
    private protected override Widget BuildChrome(BuildContext context, Widget child)
    {
        var details = new ScrollableDetails(
            Direction: AxisDirection,
            Controller: EffectiveScrollController,
            DecorationClipBehavior: CurrentWidget.ClipBehavior);
        return Configuration.BuildOverscrollIndicator(context, child, details);
    }
}

/// <remarks>Flutter's private <c>_HorizontalInnerDimension</c>: the inner scrollable of 2D scrolling.</remarks>
internal sealed class HorizontalInnerDimension : Scrollable
{
    public HorizontalInnerDimension(
        GlobalObjectKey<Scrollable.ScrollableState> verticalOuterKey,
        Func<BuildContext, ViewportOffset, Widget> viewportBuilder,
        AxisDirection axisDirection,
        ScrollController? controller = null,
        ScrollPhysics? physics = null,
        Clip clipBehavior = Clip.HardEdge,
        ScrollIncrementCalculator? incrementCalculator = null,
        bool excludeFromSemantics = false,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        string? restorationId = null,
        HitTestBehavior hitTestBehavior = HitTestBehavior.Opaque,
        DiagonalDragBehavior diagonalDragBehavior = DiagonalDragBehavior.None,
        Key? key = null) : base(
            axisDirection: axisDirection,
            controller: controller,
            physics: physics,
            clipBehavior: clipBehavior,
            incrementCalculator: incrementCalculator,
            excludeFromSemantics: excludeFromSemantics,
            dragStartBehavior: dragStartBehavior,
            restorationId: restorationId,
            hitTestBehavior: hitTestBehavior,
            key: key)
    {
        Debug.Assert(axisDirection is AxisDirection.Left or AxisDirection.Right);
        VerticalOuterKey = verticalOuterKey;
        DiagonalDragBehavior = diagonalDragBehavior;
        ViewportBuilder = viewportBuilder;
    }

    public GlobalObjectKey<Scrollable.ScrollableState> VerticalOuterKey { get; }

    public DiagonalDragBehavior DiagonalDragBehavior { get; }

    public override State CreateState() => new HorizontalInnerDimensionState();
}

internal sealed class HorizontalInnerDimensionState : Scrollable.ScrollableState
{
    private Scrollable.ScrollableState _verticalScrollable = null!;

    private HorizontalInnerDimension TypedWidget => (HorizontalInnerDimension)CurrentWidget;

    private GlobalObjectKey<Scrollable.ScrollableState> VerticalOuterKey => TypedWidget.VerticalOuterKey;

    private DiagonalDragBehavior DiagonalDragBehavior => TypedWidget.DiagonalDragBehavior;

    public override void DidChangeDependencies()
    {
        _verticalScrollable = Scrollable.Of(Context);
        Debug.Assert(ScrollDirectionUtils.AxisDirectionToAxis(_verticalScrollable.AxisDirection)
                     == Axis.Vertical);
        base.DidChangeDependencies();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reveals the target on both axes and hands the walk back to the vertical scrollable, so the
    /// enclosing scrollables are found from its context rather than visiting this one twice.
    /// </remarks>
    private protected override (IReadOnlyList<Task> Futures, Scrollable.ScrollableState Next) PerformEnsureVisible(
        RenderObject renderObject,
        double alignment,
        TimeSpan duration,
        Curve? curve,
        ScrollPositionAlignmentPolicy alignmentPolicy,
        RenderObject? targetRenderObject)
    {
        return (
            [
                Position.EnsureVisible(renderObject, alignment, duration, curve, alignmentPolicy),
                _verticalScrollable.Position.EnsureVisible(
                    renderObject,
                    alignment,
                    duration,
                    curve,
                    alignmentPolicy),
            ],
            _verticalScrollable);
    }

    public override void SetCanDrag(bool value)
    {
        if (DiagonalDragBehavior == DiagonalDragBehavior.None)
        {
            // Without diagonal scrolling the default drag gesture recognizer is used.
            base.SetCanDrag(value);
            return;
        }

        if (!value)
        {
            return;
        }

        // With diagonal scrolling the pan recognizer lives on the outer dimension, so this one needs
        // no recognizer of its own; the outer dimension still has to be updated in case it did not
        // have enough content to enable dragging.
        _gestureRecognizers = RawGestureDetector.NoGestures;
        VerticalOuterKey.CurrentState!.SetCanDrag(value);
        // Cancel the active hold/drag (if any) because the gesture recognizers are about to be
        // disposed by the RawGestureDetector, so no pointer up will arrive to cancel them.
        HandleDragCancel();
        _lastCanDrag = value;
        _lastAxis = CurrentWidget.Axis;
        _gestureDetectorKey.CurrentState?.ReplaceGestureRecognizers(_gestureRecognizers);
    }

    /// <inheritdoc />
    private protected override Widget BuildChrome(BuildContext context, Widget child)
    {
        var details = new ScrollableDetails(
            Direction: AxisDirection,
            Controller: EffectiveScrollController,
            DecorationClipBehavior: CurrentWidget.ClipBehavior);
        return Configuration.BuildOverscrollIndicator(context, child, details);
    }
}
