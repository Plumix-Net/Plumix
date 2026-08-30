using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/gesture_detector.dart

namespace Plumix.Widgets;

/// <summary>
/// A widget that detects gestures, described in <see cref="RawGestureDetector.Gestures"/> terms by
/// the recognizers it builds for the callbacks it was given.
/// </summary>
/// <remarks>
/// Flutter's <c>GestureDetector</c>. It is stateless: it maps its callbacks onto a
/// <c>Map&lt;Type, GestureRecognizerFactory&gt;</c> and hands that to a
/// <see cref="RawGestureDetector"/>.
/// </remarks>
public sealed class GestureDetector : StatelessWidget
{
    public GestureDetector(
        Widget? child = null,
        Action<TapDownDetails>? onTapDown = null,
        Action<TapUpDetails>? onTapUp = null,
        Action? onTap = null,
        Action<TapMoveDetails>? onTapMove = null,
        Action? onTapCancel = null,
        Action? onSecondaryTap = null,
        Action<TapDownDetails>? onSecondaryTapDown = null,
        Action<TapUpDetails>? onSecondaryTapUp = null,
        Action? onSecondaryTapCancel = null,
        Action<TapDownDetails>? onTertiaryTapDown = null,
        Action<TapUpDetails>? onTertiaryTapUp = null,
        Action? onTertiaryTapCancel = null,
        Action<TapDownDetails>? onDoubleTapDown = null,
        Action? onDoubleTap = null,
        Action? onDoubleTapCancel = null,
        Action<LongPressDownDetails>? onLongPressDown = null,
        Action? onLongPressCancel = null,
        Action? onLongPress = null,
        Action<LongPressStartDetails>? onLongPressStart = null,
        Action<LongPressMoveUpdateDetails>? onLongPressMoveUpdate = null,
        Action? onLongPressUp = null,
        Action<LongPressEndDetails>? onLongPressEnd = null,
        Action<LongPressDownDetails>? onSecondaryLongPressDown = null,
        Action? onSecondaryLongPressCancel = null,
        Action? onSecondaryLongPress = null,
        Action<LongPressStartDetails>? onSecondaryLongPressStart = null,
        Action<LongPressMoveUpdateDetails>? onSecondaryLongPressMoveUpdate = null,
        Action? onSecondaryLongPressUp = null,
        Action<LongPressEndDetails>? onSecondaryLongPressEnd = null,
        Action<LongPressDownDetails>? onTertiaryLongPressDown = null,
        Action? onTertiaryLongPressCancel = null,
        Action? onTertiaryLongPress = null,
        Action<LongPressStartDetails>? onTertiaryLongPressStart = null,
        Action<LongPressMoveUpdateDetails>? onTertiaryLongPressMoveUpdate = null,
        Action? onTertiaryLongPressUp = null,
        Action<LongPressEndDetails>? onTertiaryLongPressEnd = null,
        Action<DragDownDetails>? onVerticalDragDown = null,
        Action<DragStartDetails>? onVerticalDragStart = null,
        Action<DragUpdateDetails>? onVerticalDragUpdate = null,
        Action<DragEndDetails>? onVerticalDragEnd = null,
        Action? onVerticalDragCancel = null,
        Action<DragDownDetails>? onHorizontalDragDown = null,
        Action<DragStartDetails>? onHorizontalDragStart = null,
        Action<DragUpdateDetails>? onHorizontalDragUpdate = null,
        Action<DragEndDetails>? onHorizontalDragEnd = null,
        Action? onHorizontalDragCancel = null,
        Action<ForcePressDetails>? onForcePressStart = null,
        Action<ForcePressDetails>? onForcePressPeak = null,
        Action<ForcePressDetails>? onForcePressUpdate = null,
        Action<ForcePressDetails>? onForcePressEnd = null,
        Action<DragDownDetails>? onPanDown = null,
        Action<DragStartDetails>? onPanStart = null,
        Action<DragUpdateDetails>? onPanUpdate = null,
        Action<DragEndDetails>? onPanEnd = null,
        Action? onPanCancel = null,
        Action<ScaleStartDetails>? onScaleStart = null,
        Action<ScaleUpdateDetails>? onScaleUpdate = null,
        Action<ScaleEndDetails>? onScaleEnd = null,
        HitTestBehavior? behavior = null,
        bool excludeFromSemantics = false,
        DragStartBehavior dragStartBehavior = DragStartBehavior.Start,
        bool trackpadScrollCausesScale = false,
        Point? trackpadScrollToScaleFactor = null,
        IReadOnlySet<PointerDeviceKind>? supportedDevices = null,
        Key? key = null) : base(key)
    {
        AssertRecognizerCombination(
            haveVerticalDrag: onVerticalDragStart != null
                              || onVerticalDragUpdate != null
                              || onVerticalDragEnd != null,
            haveHorizontalDrag: onHorizontalDragStart != null
                                || onHorizontalDragUpdate != null
                                || onHorizontalDragEnd != null,
            havePan: onPanStart != null || onPanUpdate != null || onPanEnd != null,
            haveScale: onScaleStart != null || onScaleUpdate != null || onScaleEnd != null);

        Child = child;
        OnTapDown = onTapDown;
        OnTapUp = onTapUp;
        OnTap = onTap;
        OnTapMove = onTapMove;
        OnTapCancel = onTapCancel;
        OnSecondaryTap = onSecondaryTap;
        OnSecondaryTapDown = onSecondaryTapDown;
        OnSecondaryTapUp = onSecondaryTapUp;
        OnSecondaryTapCancel = onSecondaryTapCancel;
        OnTertiaryTapDown = onTertiaryTapDown;
        OnTertiaryTapUp = onTertiaryTapUp;
        OnTertiaryTapCancel = onTertiaryTapCancel;
        OnDoubleTapDown = onDoubleTapDown;
        OnDoubleTap = onDoubleTap;
        OnDoubleTapCancel = onDoubleTapCancel;
        OnLongPressDown = onLongPressDown;
        OnLongPressCancel = onLongPressCancel;
        OnLongPress = onLongPress;
        OnLongPressStart = onLongPressStart;
        OnLongPressMoveUpdate = onLongPressMoveUpdate;
        OnLongPressUp = onLongPressUp;
        OnLongPressEnd = onLongPressEnd;
        OnSecondaryLongPressDown = onSecondaryLongPressDown;
        OnSecondaryLongPressCancel = onSecondaryLongPressCancel;
        OnSecondaryLongPress = onSecondaryLongPress;
        OnSecondaryLongPressStart = onSecondaryLongPressStart;
        OnSecondaryLongPressMoveUpdate = onSecondaryLongPressMoveUpdate;
        OnSecondaryLongPressUp = onSecondaryLongPressUp;
        OnSecondaryLongPressEnd = onSecondaryLongPressEnd;
        OnTertiaryLongPressDown = onTertiaryLongPressDown;
        OnTertiaryLongPressCancel = onTertiaryLongPressCancel;
        OnTertiaryLongPress = onTertiaryLongPress;
        OnTertiaryLongPressStart = onTertiaryLongPressStart;
        OnTertiaryLongPressMoveUpdate = onTertiaryLongPressMoveUpdate;
        OnTertiaryLongPressUp = onTertiaryLongPressUp;
        OnTertiaryLongPressEnd = onTertiaryLongPressEnd;
        OnVerticalDragDown = onVerticalDragDown;
        OnVerticalDragStart = onVerticalDragStart;
        OnVerticalDragUpdate = onVerticalDragUpdate;
        OnVerticalDragEnd = onVerticalDragEnd;
        OnVerticalDragCancel = onVerticalDragCancel;
        OnHorizontalDragDown = onHorizontalDragDown;
        OnHorizontalDragStart = onHorizontalDragStart;
        OnHorizontalDragUpdate = onHorizontalDragUpdate;
        OnHorizontalDragEnd = onHorizontalDragEnd;
        OnHorizontalDragCancel = onHorizontalDragCancel;
        OnForcePressStart = onForcePressStart;
        OnForcePressPeak = onForcePressPeak;
        OnForcePressUpdate = onForcePressUpdate;
        OnForcePressEnd = onForcePressEnd;
        OnPanDown = onPanDown;
        OnPanStart = onPanStart;
        OnPanUpdate = onPanUpdate;
        OnPanEnd = onPanEnd;
        OnPanCancel = onPanCancel;
        OnScaleStart = onScaleStart;
        OnScaleUpdate = onScaleUpdate;
        OnScaleEnd = onScaleEnd;
        Behavior = behavior;
        ExcludeFromSemantics = excludeFromSemantics;
        DragStartBehavior = dragStartBehavior;
        SupportedDevices = supportedDevices;
        TrackpadScrollCausesScale = trackpadScrollCausesScale;
        TrackpadScrollToScaleFactor = trackpadScrollToScaleFactor
                                      ?? ScaleGestureRecognizer.KDefaultTrackpadScrollToScaleFactor;
    }

    /// <summary>
    /// Dart's constructor assert: scale is a superset of pan, so the two are redundant together, and
    /// either of them is swallowed by having both drag axes. The check is gated on
    /// <see cref="Constants.KDebugMode"/> so that it is elided exactly where Dart strips the assert.
    /// </summary>
    private static void AssertRecognizerCombination(
        bool haveVerticalDrag,
        bool haveHorizontalDrag,
        bool havePan,
        bool haveScale)
    {
        if (!Constants.KDebugMode || (!havePan && !haveScale))
        {
            return;
        }

        if (havePan && haveScale)
        {
            throw new FlutterError([
                new ErrorSummary("Incorrect GestureDetector arguments."),
                new ErrorDescription(
                    "Having both a pan gesture recognizer and a scale gesture recognizer is "
                    + "redundant; scale is a superset of pan."),
                new ErrorHint("Just use the scale gesture recognizer."),
            ]);
        }

        string recognizer = havePan ? "pan" : "scale";
        if (haveVerticalDrag && haveHorizontalDrag)
        {
            throw new FlutterError(
                "Incorrect GestureDetector arguments.\n"
                + "Simultaneously having a vertical drag gesture recognizer, a horizontal drag "
                + $"gesture recognizer, and a {recognizer} gesture recognizer will result in the "
                + $"{recognizer} gesture recognizer being ignored, since the other two will catch "
                + "all drags.");
        }
    }

    public Widget? Child { get; }

    /// <summary>A pointer that might cause a tap with a primary button has contacted the screen.</summary>
    public Action<TapDownDetails>? OnTapDown { get; }

    /// <summary>A pointer that will trigger a tap with a primary button has stopped contacting.</summary>
    public Action<TapUpDetails>? OnTapUp { get; }

    /// <summary>A tap with a primary button has occurred.</summary>
    public Action? OnTap { get; }

    /// <summary>
    /// Accepted but not wired to the recognizer: Dart's `GestureDetector.build` neither gates on nor
    /// assigns `onTapMove` at Flutter 3.47.0, so a detector configured only with it creates no tap
    /// recognizer. Use <see cref="TapGestureRecognizer.OnTapMove"/> through
    /// <see cref="RawGestureDetector"/> instead.
    /// </summary>
    public Action<TapMoveDetails>? OnTapMove { get; }

    /// <summary>The pointer that previously triggered <see cref="OnTapDown"/> will not tap.</summary>
    public Action? OnTapCancel { get; }

    /// <summary>A tap with a secondary button has occurred.</summary>
    public Action? OnSecondaryTap { get; }

    public Action<TapDownDetails>? OnSecondaryTapDown { get; }

    public Action<TapUpDetails>? OnSecondaryTapUp { get; }

    public Action? OnSecondaryTapCancel { get; }

    public Action<TapDownDetails>? OnTertiaryTapDown { get; }

    public Action<TapUpDetails>? OnTertiaryTapUp { get; }

    public Action? OnTertiaryTapCancel { get; }

    public Action<TapDownDetails>? OnDoubleTapDown { get; }

    public Action? OnDoubleTap { get; }

    public Action? OnDoubleTapCancel { get; }

    public Action<LongPressDownDetails>? OnLongPressDown { get; }

    public Action? OnLongPressCancel { get; }

    public Action? OnLongPress { get; }

    public Action<LongPressStartDetails>? OnLongPressStart { get; }

    public Action<LongPressMoveUpdateDetails>? OnLongPressMoveUpdate { get; }

    public Action? OnLongPressUp { get; }

    public Action<LongPressEndDetails>? OnLongPressEnd { get; }

    public Action<LongPressDownDetails>? OnSecondaryLongPressDown { get; }

    public Action? OnSecondaryLongPressCancel { get; }

    public Action? OnSecondaryLongPress { get; }

    public Action<LongPressStartDetails>? OnSecondaryLongPressStart { get; }

    public Action<LongPressMoveUpdateDetails>? OnSecondaryLongPressMoveUpdate { get; }

    public Action? OnSecondaryLongPressUp { get; }

    public Action<LongPressEndDetails>? OnSecondaryLongPressEnd { get; }

    public Action<LongPressDownDetails>? OnTertiaryLongPressDown { get; }

    public Action? OnTertiaryLongPressCancel { get; }

    public Action? OnTertiaryLongPress { get; }

    public Action<LongPressStartDetails>? OnTertiaryLongPressStart { get; }

    public Action<LongPressMoveUpdateDetails>? OnTertiaryLongPressMoveUpdate { get; }

    public Action? OnTertiaryLongPressUp { get; }

    public Action<LongPressEndDetails>? OnTertiaryLongPressEnd { get; }

    public Action<DragDownDetails>? OnVerticalDragDown { get; }

    public Action<DragStartDetails>? OnVerticalDragStart { get; }

    public Action<DragUpdateDetails>? OnVerticalDragUpdate { get; }

    public Action<DragEndDetails>? OnVerticalDragEnd { get; }

    public Action? OnVerticalDragCancel { get; }

    public Action<DragDownDetails>? OnHorizontalDragDown { get; }

    public Action<DragStartDetails>? OnHorizontalDragStart { get; }

    public Action<DragUpdateDetails>? OnHorizontalDragUpdate { get; }

    public Action<DragEndDetails>? OnHorizontalDragEnd { get; }

    public Action? OnHorizontalDragCancel { get; }

    public Action<DragDownDetails>? OnPanDown { get; }

    public Action<DragStartDetails>? OnPanStart { get; }

    public Action<DragUpdateDetails>? OnPanUpdate { get; }

    public Action<DragEndDetails>? OnPanEnd { get; }

    public Action? OnPanCancel { get; }

    /// <summary>The pointers established a focal point and an initial scale of 1.0.</summary>
    public Action<ScaleStartDetails>? OnScaleStart { get; }

    /// <summary>The pointers indicated a new focal point and/or scale.</summary>
    public Action<ScaleUpdateDetails>? OnScaleUpdate { get; }

    /// <summary>The pointers are no longer in contact with the screen.</summary>
    public Action<ScaleEndDetails>? OnScaleEnd { get; }

    /// <summary>The pointer is in contact with the screen and has pressed with sufficient force.</summary>
    public Action<ForcePressDetails>? OnForcePressStart { get; }

    /// <summary>The pointer has pressed with the maximum force.</summary>
    public Action<ForcePressDetails>? OnForcePressPeak { get; }

    /// <summary>The pointer is moving, changing force, or both, after a force press started.</summary>
    public Action<ForcePressDetails>? OnForcePressUpdate { get; }

    /// <summary>The pointer that triggered a force press is no longer in contact with the screen.</summary>
    public Action<ForcePressDetails>? OnForcePressEnd { get; }

    /// <summary>
    /// How this detector participates in hit testing. Null defers to
    /// <see cref="RawGestureDetectorState.DefaultBehavior"/>.
    /// </summary>
    public HitTestBehavior? Behavior { get; }

    /// <summary>Whether the detector's gestures are hidden from the semantics tree.</summary>
    public bool ExcludeFromSemantics { get; }

    public DragStartBehavior DragStartBehavior { get; }

    /// <summary>Whether scrolling up/down on a trackpad scales instead of panning.</summary>
    public bool TrackpadScrollCausesScale { get; }

    /// <summary>Controls the direction and magnitude of the scale a trackpad scroll converts to.</summary>
    public Point TrackpadScrollToScaleFactor { get; }

    /// <summary>The device kinds this detector recognizes gestures from; null means every kind.</summary>
    public IReadOnlySet<PointerDeviceKind>? SupportedDevices { get; }

    public override Widget Build(BuildContext context)
    {
        var gestures = new Dictionary<Type, IGestureRecognizerFactory>();
        DeviceGestureSettings? gestureSettings = MediaQuery.MaybeGestureSettingsOf(context);
        ScrollBehavior configuration = ScrollConfiguration.Of(context);

        if (OnTapDown != null
            || OnTapUp != null
            || OnTap != null
            || OnTapCancel != null
            || OnSecondaryTap != null
            || OnSecondaryTapDown != null
            || OnSecondaryTapUp != null
            || OnSecondaryTapCancel != null
            || OnTertiaryTapDown != null
            || OnTertiaryTapUp != null
            || OnTertiaryTapCancel != null)
        {
            gestures[typeof(TapGestureRecognizer)] =
                new GestureRecognizerFactoryWithHandlers<TapGestureRecognizer>(
                    () => new TapGestureRecognizer
                    {
                        DebugOwner = this,
                        SupportedDevices = SupportedDevices,
                    },
                    instance =>
                    {
                        instance.OnTapDown = OnTapDown;
                        instance.OnTapUp = OnTapUp;
                        instance.OnTap = OnTap;
                        instance.OnTapCancel = OnTapCancel;
                        instance.OnSecondaryTap = OnSecondaryTap;
                        instance.OnSecondaryTapDown = OnSecondaryTapDown;
                        instance.OnSecondaryTapUp = OnSecondaryTapUp;
                        instance.OnSecondaryTapCancel = OnSecondaryTapCancel;
                        instance.OnTertiaryTapDown = OnTertiaryTapDown;
                        instance.OnTertiaryTapUp = OnTertiaryTapUp;
                        instance.OnTertiaryTapCancel = OnTertiaryTapCancel;
                        instance.GestureSettings = gestureSettings;
                        instance.SupportedDevices = SupportedDevices;
                    });
        }

        if (OnDoubleTap != null || OnDoubleTapDown != null || OnDoubleTapCancel != null)
        {
            gestures[typeof(DoubleTapGestureRecognizer)] =
                new GestureRecognizerFactoryWithHandlers<DoubleTapGestureRecognizer>(
                    () => new DoubleTapGestureRecognizer
                    {
                        DebugOwner = this,
                        SupportedDevices = SupportedDevices,
                    },
                    instance =>
                    {
                        instance.OnDoubleTapDown = OnDoubleTapDown;
                        instance.OnDoubleTap = OnDoubleTap;
                        instance.OnDoubleTapCancel = OnDoubleTapCancel;
                        instance.GestureSettings = gestureSettings;
                        instance.SupportedDevices = SupportedDevices;
                    });
        }

        if (OnLongPressDown != null
            || OnLongPressCancel != null
            || OnLongPress != null
            || OnLongPressStart != null
            || OnLongPressMoveUpdate != null
            || OnLongPressUp != null
            || OnLongPressEnd != null
            || OnSecondaryLongPressDown != null
            || OnSecondaryLongPressCancel != null
            || OnSecondaryLongPress != null
            || OnSecondaryLongPressStart != null
            || OnSecondaryLongPressMoveUpdate != null
            || OnSecondaryLongPressUp != null
            || OnSecondaryLongPressEnd != null
            || OnTertiaryLongPressDown != null
            || OnTertiaryLongPressCancel != null
            || OnTertiaryLongPress != null
            || OnTertiaryLongPressStart != null
            || OnTertiaryLongPressMoveUpdate != null
            || OnTertiaryLongPressUp != null
            || OnTertiaryLongPressEnd != null)
        {
            gestures[typeof(LongPressGestureRecognizer)] =
                new GestureRecognizerFactoryWithHandlers<LongPressGestureRecognizer>(
                    () => new LongPressGestureRecognizer
                    {
                        DebugOwner = this,
                        SupportedDevices = SupportedDevices,
                    },
                    instance =>
                    {
                        instance.OnLongPressDown = OnLongPressDown;
                        instance.OnLongPressCancel = OnLongPressCancel;
                        instance.OnLongPress = OnLongPress;
                        instance.OnLongPressStart = OnLongPressStart;
                        instance.OnLongPressMoveUpdate = OnLongPressMoveUpdate;
                        instance.OnLongPressUp = OnLongPressUp;
                        instance.OnLongPressEnd = OnLongPressEnd;
                        instance.OnSecondaryLongPressDown = OnSecondaryLongPressDown;
                        instance.OnSecondaryLongPressCancel = OnSecondaryLongPressCancel;
                        instance.OnSecondaryLongPress = OnSecondaryLongPress;
                        instance.OnSecondaryLongPressStart = OnSecondaryLongPressStart;
                        instance.OnSecondaryLongPressMoveUpdate = OnSecondaryLongPressMoveUpdate;
                        instance.OnSecondaryLongPressUp = OnSecondaryLongPressUp;
                        instance.OnSecondaryLongPressEnd = OnSecondaryLongPressEnd;
                        instance.OnTertiaryLongPressDown = OnTertiaryLongPressDown;
                        instance.OnTertiaryLongPressCancel = OnTertiaryLongPressCancel;
                        instance.OnTertiaryLongPress = OnTertiaryLongPress;
                        instance.OnTertiaryLongPressStart = OnTertiaryLongPressStart;
                        instance.OnTertiaryLongPressMoveUpdate = OnTertiaryLongPressMoveUpdate;
                        instance.OnTertiaryLongPressUp = OnTertiaryLongPressUp;
                        instance.OnTertiaryLongPressEnd = OnTertiaryLongPressEnd;
                        instance.GestureSettings = gestureSettings;
                        instance.SupportedDevices = SupportedDevices;
                    });
        }

        if (OnVerticalDragDown != null
            || OnVerticalDragStart != null
            || OnVerticalDragUpdate != null
            || OnVerticalDragEnd != null
            || OnVerticalDragCancel != null)
        {
            gestures[typeof(VerticalDragGestureRecognizer)] =
                new GestureRecognizerFactoryWithHandlers<VerticalDragGestureRecognizer>(
                    () => new VerticalDragGestureRecognizer
                    {
                        DebugOwner = this,
                        SupportedDevices = SupportedDevices,
                    },
                    instance =>
                    {
                        instance.OnDown = OnVerticalDragDown;
                        instance.OnStart = OnVerticalDragStart;
                        instance.OnUpdate = OnVerticalDragUpdate;
                        instance.OnEnd = OnVerticalDragEnd;
                        instance.OnCancel = OnVerticalDragCancel;
                        instance.DragStartBehavior = DragStartBehavior;
                        instance.MultitouchDragStrategy = configuration.GetMultitouchDragStrategy(context);
                        instance.GestureSettings = gestureSettings;
                        instance.SupportedDevices = SupportedDevices;
                    });
        }

        if (OnHorizontalDragDown != null
            || OnHorizontalDragStart != null
            || OnHorizontalDragUpdate != null
            || OnHorizontalDragEnd != null
            || OnHorizontalDragCancel != null)
        {
            gestures[typeof(HorizontalDragGestureRecognizer)] =
                new GestureRecognizerFactoryWithHandlers<HorizontalDragGestureRecognizer>(
                    () => new HorizontalDragGestureRecognizer
                    {
                        DebugOwner = this,
                        SupportedDevices = SupportedDevices,
                    },
                    instance =>
                    {
                        instance.OnDown = OnHorizontalDragDown;
                        instance.OnStart = OnHorizontalDragStart;
                        instance.OnUpdate = OnHorizontalDragUpdate;
                        instance.OnEnd = OnHorizontalDragEnd;
                        instance.OnCancel = OnHorizontalDragCancel;
                        instance.DragStartBehavior = DragStartBehavior;
                        instance.MultitouchDragStrategy = configuration.GetMultitouchDragStrategy(context);
                        instance.GestureSettings = gestureSettings;
                        instance.SupportedDevices = SupportedDevices;
                    });
        }

        if (OnPanDown != null
            || OnPanStart != null
            || OnPanUpdate != null
            || OnPanEnd != null
            || OnPanCancel != null)
        {
            gestures[typeof(PanGestureRecognizer)] =
                new GestureRecognizerFactoryWithHandlers<PanGestureRecognizer>(
                    () => new PanGestureRecognizer
                    {
                        DebugOwner = this,
                        SupportedDevices = SupportedDevices,
                    },
                    instance =>
                    {
                        instance.OnDown = OnPanDown;
                        instance.OnStart = OnPanStart;
                        instance.OnUpdate = OnPanUpdate;
                        instance.OnEnd = OnPanEnd;
                        instance.OnCancel = OnPanCancel;
                        instance.DragStartBehavior = DragStartBehavior;
                        instance.MultitouchDragStrategy = configuration.GetMultitouchDragStrategy(context);
                        instance.GestureSettings = gestureSettings;
                        instance.SupportedDevices = SupportedDevices;
                    });
        }

        if (OnScaleStart != null || OnScaleUpdate != null || OnScaleEnd != null)
        {
            gestures[typeof(ScaleGestureRecognizer)] =
                new GestureRecognizerFactoryWithHandlers<ScaleGestureRecognizer>(
                    () => new ScaleGestureRecognizer
                    {
                        DebugOwner = this,
                        SupportedDevices = SupportedDevices,
                    },
                    instance =>
                    {
                        instance.OnStart = OnScaleStart;
                        instance.OnUpdate = OnScaleUpdate;
                        instance.OnEnd = OnScaleEnd;
                        instance.DragStartBehavior = DragStartBehavior;
                        instance.GestureSettings = gestureSettings;
                        instance.TrackpadScrollCausesScale = TrackpadScrollCausesScale;
                        instance.TrackpadScrollToScaleFactor = TrackpadScrollToScaleFactor;
                        instance.SupportedDevices = SupportedDevices;
                    });
        }

        if (OnForcePressStart != null
            || OnForcePressPeak != null
            || OnForcePressUpdate != null
            || OnForcePressEnd != null)
        {
            gestures[typeof(ForcePressGestureRecognizer)] =
                new GestureRecognizerFactoryWithHandlers<ForcePressGestureRecognizer>(
                    () => new ForcePressGestureRecognizer(
                        debugOwner: this,
                        supportedDevices: SupportedDevices),
                    instance =>
                    {
                        instance.OnStart = OnForcePressStart;
                        instance.OnPeak = OnForcePressPeak;
                        instance.OnUpdate = OnForcePressUpdate;
                        instance.OnEnd = OnForcePressEnd;
                        instance.GestureSettings = gestureSettings;
                        instance.SupportedDevices = SupportedDevices;
                    });
        }

        return new RawGestureDetector(
            gestures: gestures,
            behavior: Behavior,
            excludeFromSemantics: ExcludeFromSemantics,
            child: Child);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new EnumProperty<DragStartBehavior>("startBehavior", DragStartBehavior));
    }
}

/// <summary>
/// A widget that detects gestures described by the given gesture factories.
/// </summary>
/// <remarks>
/// Flutter's <c>RawGestureDetector</c>. Unlike <see cref="GestureDetector"/> it takes the recognizers
/// directly, which lets a caller configure them fully and swap them from layout through
/// <see cref="RawGestureDetectorState.ReplaceGestureRecognizers"/>.
/// </remarks>
public sealed class RawGestureDetector : StatefulWidget
{
    /// <summary>Dart's `const <Type, GestureRecognizerFactory>{}` default.</summary>
    public static readonly IReadOnlyDictionary<Type, IGestureRecognizerFactory> NoGestures =
        new Dictionary<Type, IGestureRecognizerFactory>();

    public RawGestureDetector(
        Widget? child = null,
        IReadOnlyDictionary<Type, IGestureRecognizerFactory>? gestures = null,
        HitTestBehavior? behavior = null,
        bool excludeFromSemantics = false,
        SemanticsGestureDelegate? semantics = null,
        Key? key = null) : base(key)
    {
        Child = child;
        Gestures = gestures ?? NoGestures;
        Behavior = behavior;
        ExcludeFromSemantics = excludeFromSemantics;
        Semantics = semantics;
    }

    public Widget? Child { get; }

    /// <summary>The recognizers this detector owns, keyed by recognizer type.</summary>
    public IReadOnlyDictionary<Type, IGestureRecognizerFactory> Gestures { get; }

    /// <summary>
    /// How this detector participates in hit testing. Null falls back to
    /// <see cref="RawGestureDetectorState.DefaultBehavior"/>, exactly as Dart's
    /// `behavior ?? _defaultBehavior`.
    /// </summary>
    public HitTestBehavior? Behavior { get; }

    /// <summary>Whether the detector's gestures are hidden from the semantics tree.</summary>
    public bool ExcludeFromSemantics { get; }

    /// <summary>
    /// Describes the semantics notations that should be added to the underlying render object. Null
    /// uses <see cref="DefaultSemanticsGestureDelegate"/>.
    /// </summary>
    public SemanticsGestureDelegate? Semantics { get; }

    public override State CreateState()
    {
        return new RawGestureDetectorState();
    }
}

/// <summary>State for a <see cref="RawGestureDetector"/>.</summary>
public sealed class RawGestureDetectorState : State
{
    // Dart initializes this to a const empty map and nulls it on dispose; the null is the disposed
    // sentinel `debugFillProperties` reports as DISPOSED.
    private Dictionary<Type, GestureRecognizer>? _recognizers = [];
    private SemanticsGestureDelegate? _semantics;

    internal IReadOnlyDictionary<Type, GestureRecognizer> Recognizers =>
        _recognizers ?? throw new InvalidOperationException("The RawGestureDetectorState is disposed.");

    private RawGestureDetector CurrentWidget => (RawGestureDetector)Element.Widget;

    /// <summary>Dart's `RawGestureDetectorState._defaultBehavior`.</summary>
    public HitTestBehavior DefaultBehavior =>
        CurrentWidget.Child == null ? HitTestBehavior.Translucent : HitTestBehavior.DeferToChild;

    public override void InitState()
    {
        base.InitState();
        _semantics = CurrentWidget.Semantics ?? new DefaultSemanticsGestureDelegate(this);
        SyncAll(CurrentWidget.Gestures);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var previous = (RawGestureDetector)oldWidget;
        if (!(previous.Semantics == null && CurrentWidget.Semantics == null))
        {
            _semantics = CurrentWidget.Semantics ?? new DefaultSemanticsGestureDelegate(this);
        }

        SyncAll(CurrentWidget.Gestures);
    }

    /// <summary>
    /// This method can be called after the build phase, during the layout of the nearest descendant
    /// <see cref="RenderObject"/> of the gesture detector, to change the set of recognizers.
    /// </summary>
    /// <remarks>
    /// Flutter's <c>replaceGestureRecognizers</c>. The new recognizers stay in place only until the
    /// next build, which restores the widget's own <see cref="RawGestureDetector.Gestures"/>.
    /// </remarks>
    public void ReplaceGestureRecognizers(IReadOnlyDictionary<Type, IGestureRecognizerFactory> gestures)
    {
        if (Constants.KDebugMode && Context.FindRenderObject()?.Owner is not { DebugDoingLayout: true })
        {
            throw new FlutterError([
                new ErrorSummary(
                    "Unexpected call to ReplaceGestureRecognizers() method of RawGestureDetectorState."),
                new ErrorDescription(
                    "The ReplaceGestureRecognizers() method can only be called during the layout phase."),
                new ErrorHint(
                    "To set the gesture recognizers at other times, trigger a new build using SetState() "
                    + "and provide the new gesture recognizers as constructor arguments to the "
                    + "corresponding RawGestureDetector or GestureDetector object."),
            ]);
        }

        SyncAll(gestures);
        if (!CurrentWidget.ExcludeFromSemantics)
        {
            var handler = (RenderSemanticsGestureHandler)Context.FindRenderObject()!;
            UpdateSemanticsForRenderObject(handler);
        }
    }

    /// <summary>
    /// Filters the semantics actions this detector exposes. <c>Scrollable</c> calls it so that
    /// assistive tools only see the directions the list can still be scrolled in.
    /// </summary>
    /// <remarks>Flutter's <c>RawGestureDetectorState.replaceSemanticsActions</c>.</remarks>
    public void ReplaceSemanticsActions(SemanticsActions actions)
    {
        if (CurrentWidget.ExcludeFromSemantics)
        {
            return;
        }

        if (Context.FindRenderObject() is not RenderSemanticsGestureHandler handler)
        {
            if (Constants.KDebugMode)
            {
                throw new FlutterError(
                    "Unexpected call to ReplaceSemanticsActions() method of RawGestureDetectorState.\n"
                    + "The ReplaceSemanticsActions() method can only be called after the "
                    + "RenderSemanticsGestureHandler has been created.");
            }

            return;
        }

        handler.ValidActions = actions;
    }

    public override void Dispose()
    {
        foreach (GestureRecognizer recognizer in Recognizers.Values)
        {
            recognizer.Dispose();
        }

        _recognizers = null;
        base.Dispose();
    }

    private void SyncAll(IReadOnlyDictionary<Type, IGestureRecognizerFactory> gestures)
    {
        Dictionary<Type, GestureRecognizer> oldRecognizers = _recognizers
            ?? throw new InvalidOperationException("The RawGestureDetectorState is disposed.");
        var recognizers = new Dictionary<Type, GestureRecognizer>();
        _recognizers = recognizers;
        foreach ((Type type, IGestureRecognizerFactory factory) in gestures)
        {
            if (Constants.KDebugMode && !factory.HandlesType(type))
            {
                throw new FlutterError(
                    $"GestureRecognizerFactory of type {factory.RecognizerType} was used where type "
                    + $"{type} was specified.");
            }

            if (!oldRecognizers.TryGetValue(type, out GestureRecognizer? recognizer))
            {
                recognizer = factory.ConstructorRaw();
                if (Constants.KDebugMode && recognizer.GetType() != type)
                {
                    throw new FlutterError(
                        $"GestureRecognizerFactory of type {type} created a GestureRecognizer of type "
                        + $"{recognizer.GetType()}. The GestureRecognizerFactory must be specialized "
                        + "with the type of the class that it returns from its constructor method.");
                }
            }

            recognizers[type] = recognizer;
            factory.InitializerRaw(recognizer);
        }

        foreach ((Type type, GestureRecognizer recognizer) in oldRecognizers)
        {
            if (!recognizers.ContainsKey(type))
            {
                recognizer.Dispose();
            }
        }
    }

    private void HandlePointerDown(PointerDownEvent @event)
    {
        foreach (GestureRecognizer recognizer in Recognizers.Values)
        {
            recognizer.AddPointer(@event);
        }
    }

    private void HandlePointerPanZoomStart(PointerPanZoomStartEvent @event)
    {
        foreach (GestureRecognizer recognizer in Recognizers.Values)
        {
            recognizer.AddPointerPanZoom(@event);
        }
    }

    private void UpdateSemanticsForRenderObject(RenderSemanticsGestureHandler renderObject)
    {
        _semantics!.AssignSemantics(renderObject);
    }

    public override Widget Build(BuildContext context)
    {
        RawGestureDetector widget = CurrentWidget;
        HitTestBehavior behavior = widget.Behavior ?? DefaultBehavior;
        Widget result = new Listener(
            onPointerDown: HandlePointerDown,
            onPointerPanZoomStart: HandlePointerPanZoomStart,
            behavior: behavior,
            child: widget.Child);

        if (!widget.ExcludeFromSemantics)
        {
            result = new GestureSemantics(
                behavior: behavior,
                assignSemantics: UpdateSemanticsForRenderObject,
                child: result);
        }

        return result;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        if (_recognizers is null)
        {
            properties.Add(DiagnosticsNode.Message("DISPOSED"));
        }
        else
        {
            List<string> gestures = [.. _recognizers.Values.Select(recognizer => recognizer.DebugDescription)];
            properties.Add(new IterableProperty<string>("gestures", gestures, ifEmpty: "<none>"));
            properties.Add(new IterableProperty<GestureRecognizer>(
                "recognizers",
                _recognizers.Values,
                level: DiagnosticLevel.Fine));
            properties.Add(new DiagnosticsProperty<bool>(
                "excludeFromSemantics",
                CurrentWidget.ExcludeFromSemantics,
                defaultValue: false));
            if (!CurrentWidget.ExcludeFromSemantics)
            {
                properties.Add(new DiagnosticsProperty<SemanticsGestureDelegate>(
                    "semantics",
                    CurrentWidget.Semantics,
                    defaultValue: DiagnosticsDefaults.NullValue));
            }
        }

        properties.Add(new EnumProperty<HitTestBehavior>(
            "behavior",
            CurrentWidget.Behavior,
            defaultValue: DiagnosticsDefaults.NullValue));
    }
}

/// <summary>
/// A base class that describes what semantics notations a <see cref="RawGestureDetector"/> should
/// add to the render object.
/// </summary>
/// <remarks>Flutter's <c>SemanticsGestureDelegate</c>.</remarks>
public abstract class SemanticsGestureDelegate
{
    /// <summary>Assigns semantics notations to the given <see cref="RenderSemanticsGestureHandler"/>.</summary>
    public abstract void AssignSemantics(RenderSemanticsGestureHandler renderObject);

    /// <inheritdoc />
    public override string ToString() =>
        $"{Diagnostics.ObjectRuntimeType(this, "SemanticsGestureDelegate")}()";
}

/// <summary>
/// The default semantics delegate: it replays the recognizers a detector owns as semantic actions.
/// </summary>
/// <remarks>Flutter's private <c>_DefaultSemanticsGestureDelegate</c>.</remarks>
internal sealed class DefaultSemanticsGestureDelegate : SemanticsGestureDelegate
{
    private readonly RawGestureDetectorState _detectorState;

    public DefaultSemanticsGestureDelegate(RawGestureDetectorState detectorState)
    {
        _detectorState = detectorState;
    }

    public override void AssignSemantics(RenderSemanticsGestureHandler renderObject)
    {
        IReadOnlyDictionary<Type, GestureRecognizer> recognizers = _detectorState.Recognizers;
        renderObject.OnTap = GetTapHandler(renderObject, recognizers);
        renderObject.OnLongPress = GetLongPressHandler(renderObject, recognizers);
        renderObject.OnHorizontalDragUpdate = GetHorizontalDragUpdateHandler(renderObject, recognizers);
        renderObject.OnVerticalDragUpdate = GetVerticalDragUpdateHandler(renderObject, recognizers);
    }

    private static Rect GetLocalRectFromRenderObject(RenderObject renderObject)
    {
        return renderObject is RenderBox box
            ? new Rect(0.0, 0.0, box.Size.Width, box.Size.Height)
            : default;
    }

    private static Point TransformOffsetToGlobal(RenderObject renderObject, Point local)
    {
        return MatrixUtils.TransformPoint(renderObject.GetTransformTo(null), local);
    }

    private static T? Lookup<T>(IReadOnlyDictionary<Type, GestureRecognizer> recognizers)
        where T : GestureRecognizer
    {
        return recognizers.TryGetValue(typeof(T), out GestureRecognizer? recognizer) ? (T)recognizer : null;
    }

    private static Action? GetTapHandler(
        RenderObject renderObject,
        IReadOnlyDictionary<Type, GestureRecognizer> recognizers)
    {
        if (Lookup<TapGestureRecognizer>(recognizers) is not { } tap)
        {
            return null;
        }

        return () =>
        {
            Point localCenter = GetLocalRectFromRenderObject(renderObject).Center;
            Point globalCenter = TransformOffsetToGlobal(renderObject, localCenter);
            tap.OnTapDown?.Invoke(new TapDownDetails(
                globalPosition: globalCenter,
                localPosition: localCenter,
                kind: PointerDeviceKind.Unknown));
            tap.OnTapUp?.Invoke(new TapUpDetails(
                kind: PointerDeviceKind.Unknown,
                globalPosition: globalCenter,
                localPosition: localCenter));
            tap.OnTap?.Invoke();
        };
    }

    private static Action? GetLongPressHandler(
        RenderObject renderObject,
        IReadOnlyDictionary<Type, GestureRecognizer> recognizers)
    {
        if (Lookup<LongPressGestureRecognizer>(recognizers) is not { } longPress)
        {
            return null;
        }

        return () =>
        {
            Point localCenter = GetLocalRectFromRenderObject(renderObject).Center;
            Point globalCenter = TransformOffsetToGlobal(renderObject, localCenter);
            longPress.OnLongPressDown?.Invoke(new LongPressDownDetails(
                GlobalPosition: globalCenter,
                LocalPosition: localCenter));
            longPress.OnLongPressStart?.Invoke(new LongPressStartDetails(
                GlobalPosition: globalCenter,
                LocalPosition: localCenter));
            longPress.OnLongPress?.Invoke();
            longPress.OnLongPressEnd?.Invoke(new LongPressEndDetails(
                GlobalPosition: globalCenter,
                LocalPosition: localCenter));
            longPress.OnLongPressUp?.Invoke();
        };
    }

    private static Action<DragUpdateDetails>? GetHorizontalDragUpdateHandler(
        RenderObject renderObject,
        IReadOnlyDictionary<Type, GestureRecognizer> recognizers)
    {
        return ComposeDragUpdateHandler(
            renderObject,
            Lookup<HorizontalDragGestureRecognizer>(recognizers),
            Lookup<PanGestureRecognizer>(recognizers));
    }

    private static Action<DragUpdateDetails>? GetVerticalDragUpdateHandler(
        RenderObject renderObject,
        IReadOnlyDictionary<Type, GestureRecognizer> recognizers)
    {
        return ComposeDragUpdateHandler(
            renderObject,
            Lookup<VerticalDragGestureRecognizer>(recognizers),
            Lookup<PanGestureRecognizer>(recognizers));
    }

    /// <summary>
    /// Builds the axis handler and the pan handler and, when both exist, runs the axis one first —
    /// exactly as Dart's `_get{Horizontal,Vertical}DragUpdateHandler` does. The pan recognizer's
    /// synthesized end details carry no primary velocity, where the axis recognizer's carry 0.0.
    /// </summary>
    private static Action<DragUpdateDetails>? ComposeDragUpdateHandler(
        RenderObject renderObject,
        DragGestureRecognizer? axis,
        PanGestureRecognizer? pan)
    {
        Action<DragUpdateDetails>? axisHandler = axis is null
            ? null
            : BuildDragUpdateHandler(renderObject, axis, primaryVelocity: 0.0);
        Action<DragUpdateDetails>? panHandler = pan is null
            ? null
            : BuildDragUpdateHandler(renderObject, pan, primaryVelocity: null);

        if (axisHandler is null && panHandler is null)
        {
            return null;
        }

        return details =>
        {
            axisHandler?.Invoke(details);
            panHandler?.Invoke(details);
        };
    }

    private static Action<DragUpdateDetails> BuildDragUpdateHandler(
        RenderObject renderObject,
        DragGestureRecognizer recognizer,
        double? primaryVelocity)
    {
        return details =>
        {
            Point localCenter = GetLocalRectFromRenderObject(renderObject).Center;
            Point globalCenter = TransformOffsetToGlobal(renderObject, localCenter);
            Point newLocalOffset = localCenter + details.Delta;
            Point newGlobalOffset = TransformOffsetToGlobal(renderObject, newLocalOffset);
            recognizer.OnDown?.Invoke(new DragDownDetails(
                GlobalPosition: globalCenter,
                LocalPosition: localCenter));
            recognizer.OnStart?.Invoke(new DragStartDetails(
                GlobalPosition: globalCenter,
                LocalPosition: localCenter));
            recognizer.OnUpdate?.Invoke(details);
            recognizer.OnEnd?.Invoke(new DragEndDetails(
                globalPosition: newGlobalOffset,
                localPosition: newLocalOffset,
                velocity: Velocity.Zero,
                primaryVelocity: primaryVelocity));
        };
    }
}

/// <summary>
/// Hosts the <see cref="RenderSemanticsGestureHandler"/> a <see cref="RawGestureDetector"/> puts
/// between its listener and the semantics tree.
/// </summary>
/// <remarks>Flutter's private <c>_GestureSemantics</c>.</remarks>
internal sealed class GestureSemantics : SingleChildRenderObjectWidget
{
    public GestureSemantics(
        HitTestBehavior behavior,
        Action<RenderSemanticsGestureHandler> assignSemantics,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        Behavior = behavior;
        AssignSemantics = assignSemantics;
    }

    public HitTestBehavior Behavior { get; }

    public Action<RenderSemanticsGestureHandler> AssignSemantics { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        var renderObject = new RenderSemanticsGestureHandler { Behavior = Behavior };
        AssignSemantics(renderObject);
        return renderObject;
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var handler = (RenderSemanticsGestureHandler)renderObject;
        handler.Behavior = Behavior;
        AssignSemantics(handler);
    }
}
