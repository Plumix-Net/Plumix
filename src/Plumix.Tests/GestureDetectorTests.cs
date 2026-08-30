using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/test/widgets/gesture_detector_test.dart;
// flutter/packages/flutter/test/widgets/gesture_detector_semantics_test.dart

namespace Plumix.Tests;

/// <summary>
/// Ports the behaviors Flutter's own `gesture_detector_test.dart` and
/// `gesture_detector_semantics_test.dart` assert against `GestureDetector`, `RawGestureDetector` and
/// the default semantics delegate.
/// </summary>
public sealed class GestureDetectorTests
{
    private static readonly Size SurfaceSize = new(800.0, 600.0);

    /// <summary>Runs a detector's gestures map through the factory the way `_syncAll` does.</summary>
    private static T BuildRecognizer<T>(IReadOnlyDictionary<Type, IGestureRecognizerFactory> gestures)
        where T : GestureRecognizer
    {
        IGestureRecognizerFactory factory = Assert.Contains(typeof(T), gestures);
        var recognizer = (T)factory.ConstructorRaw();
        factory.InitializerRaw(recognizer);
        return recognizer;
    }

    private static IReadOnlyDictionary<Type, IGestureRecognizerFactory> GesturesOf(GestureDetector detector)
    {
        var harness = new ScrollSemanticsHarness(detector);
        harness.Pump(SurfaceSize);
        return FindWidget<RawGestureDetector>(harness.RootElement).Gestures;
    }

    private static T FindWidget<T>(Element root) where T : Widget
    {
        return Assert.Single(FindWidgets<T>(root));
    }

    private static List<T> FindWidgets<T>(Element root) where T : Widget
    {
        var found = new List<T>();
        Visit(root);
        return found;

        void Visit(Element element)
        {
            if (element.Widget is T widget)
            {
                found.Add(widget);
            }

            element.VisitChildren(Visit);
        }
    }

    private static RawGestureDetectorState FindDetectorState(Element root)
    {
        RawGestureDetectorState? result = null;
        Visit(root);
        return result ?? throw new InvalidOperationException("No RawGestureDetectorState in the tree.");

        void Visit(Element element)
        {
            if (element is StatefulElement { State: RawGestureDetectorState state })
            {
                result ??= state;
            }

            element.VisitChildren(Visit);
        }
    }

    private static RenderSemanticsGestureHandler FindHandler(Element root)
    {
        RenderSemanticsGestureHandler? result = null;
        Visit(root);
        return result ?? throw new InvalidOperationException("No RenderSemanticsGestureHandler in the tree.");

        void Visit(Element element)
        {
            if (element.RenderObject is RenderSemanticsGestureHandler handler)
            {
                result ??= handler;
            }

            element.VisitChildren(Visit);
        }
    }

    private static T FindRenderObject<T>(Element root) where T : RenderObject
    {
        T? result = null;
        Visit(root);
        return result ?? throw new InvalidOperationException($"No {typeof(T).Name} in the tree.");

        void Visit(Element element)
        {
            if (element.RenderObject is T match)
            {
                result ??= match;
            }

            element.VisitChildren(Visit);
        }
    }

    /// <summary>The deepest semantics node exposing any of the four gesture actions.</summary>
    private static SemanticsNode? FindGestureNode(SemanticsNode? node)
    {
        const SemanticsActions gestureActions = SemanticsActions.Tap
                                                | SemanticsActions.LongPress
                                                | SemanticsActions.ScrollLeft
                                                | SemanticsActions.ScrollRight
                                                | SemanticsActions.ScrollUp
                                                | SemanticsActions.ScrollDown;
        if (node is null)
        {
            return null;
        }

        foreach (SemanticsNode child in node.Children)
        {
            if (FindGestureNode(child) is { } match)
            {
                return match;
            }
        }

        return (node.Actions & gestureActions) != SemanticsActions.None ? node : null;
    }

    /// <summary>A 20x20 detector centred in the 800x600 surface: local centre (10,10), global (400,300).</summary>
    private static Widget CenteredDetector(Widget detector)
    {
        return new Center(child: new SizedBox(width: 20.0, height: 20.0, child: detector));
    }

    // ---------------------------------------------------------------- constructor asserts

    [DebugOnlyFact]
    public void Constructor_RejectsRedundantPanAndScaleCallbacks()
    {
        FlutterError error = Assert.Throws<FlutterError>(() => new GestureDetector(
            onPanUpdate: _ => { },
            onScaleUpdate: _ => { }));
        Assert.Contains("Incorrect GestureDetector arguments.", error.Message, StringComparison.Ordinal);
        Assert.Contains("scale is a superset of pan", error.Message, StringComparison.Ordinal);
        // Dart's last diagnostic of this error is an ErrorHint.
        Assert.Equal(DiagnosticLevel.Hint, error.Diagnostics[^1].Level);
        Assert.Contains("Just use the scale gesture recognizer.", error.Diagnostics[^1].ToString(),
            StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void Constructor_RejectsPanOrScaleAlongsideBothDragAxes()
    {
        FlutterError scaleError = Assert.Throws<FlutterError>(() => new GestureDetector(
            onHorizontalDragUpdate: _ => { },
            onVerticalDragUpdate: _ => { },
            onScaleUpdate: _ => { }));
        Assert.Contains("scale gesture recognizer being ignored", scaleError.Message, StringComparison.Ordinal);

        FlutterError panError = Assert.Throws<FlutterError>(() => new GestureDetector(
            onHorizontalDragUpdate: _ => { },
            onVerticalDragUpdate: _ => { },
            onPanUpdate: _ => { }));
        Assert.Contains("pan gesture recognizer being ignored", panError.Message, StringComparison.Ordinal);

        // Only Start/Update/End participate: Down and Cancel never trip the assert.
        _ = new GestureDetector(
            onHorizontalDragDown: _ => { },
            onVerticalDragCancel: () => { },
            onPanUpdate: _ => { });
        // Either drag axis on its own is fine.
        _ = new GestureDetector(onHorizontalDragUpdate: _ => { }, onScaleUpdate: _ => { });
    }

    // ---------------------------------------------------------------- GestureDetector.build

    [Fact]
    public void Build_RegistersNoRecognizersWithoutCallbacks()
    {
        Assert.Empty(GesturesOf(new GestureDetector()));
    }

    [Fact]
    public void Build_GatesTheTapRecognizerOnTheElevenTapCallbacks()
    {
        Assert.Contains(typeof(TapGestureRecognizer), GesturesOf(new GestureDetector(onTap: () => { })));
        Assert.Contains(
            typeof(TapGestureRecognizer),
            GesturesOf(new GestureDetector(onTertiaryTapCancel: () => { })));

        // Upstream gap at 3.47.0: `onTapMove` is declared but never gates or configures the
        // recognizer, so a detector carrying only it registers nothing.
        Assert.Empty(GesturesOf(new GestureDetector(onTapMove: _ => { })));
    }

    [Fact]
    public void Build_ConfiguresTheTapRecognizerFromEveryTapCallback()
    {
        var devices = new HashSet<PointerDeviceKind> { PointerDeviceKind.Mouse };
        var detector = new GestureDetector(
            onTapDown: _ => { },
            onTapUp: _ => { },
            onTap: () => { },
            onTapCancel: () => { },
            onSecondaryTap: () => { },
            onSecondaryTapDown: _ => { },
            onSecondaryTapUp: _ => { },
            onSecondaryTapCancel: () => { },
            onTertiaryTapDown: _ => { },
            onTertiaryTapUp: _ => { },
            onTertiaryTapCancel: () => { },
            supportedDevices: devices);
        TapGestureRecognizer tap = BuildRecognizer<TapGestureRecognizer>(GesturesOf(detector));

        Assert.NotNull(tap.OnTapDown);
        Assert.NotNull(tap.OnTapUp);
        Assert.NotNull(tap.OnTap);
        Assert.NotNull(tap.OnTapCancel);
        Assert.NotNull(tap.OnSecondaryTap);
        Assert.NotNull(tap.OnSecondaryTapDown);
        Assert.NotNull(tap.OnSecondaryTapUp);
        Assert.NotNull(tap.OnSecondaryTapCancel);
        Assert.NotNull(tap.OnTertiaryTapDown);
        Assert.NotNull(tap.OnTertiaryTapUp);
        Assert.NotNull(tap.OnTertiaryTapCancel);
        Assert.Same(devices, tap.SupportedDevices);
        // `onTapMove` is not assigned by `GestureDetector.build`.
        Assert.Null(tap.OnTapMove);
    }

    [Fact]
    public void Build_GatesAndConfiguresTheDoubleTapRecognizer()
    {
        Assert.Empty(GesturesOf(new GestureDetector()));
        var detector = new GestureDetector(
            onDoubleTapDown: _ => { },
            onDoubleTap: () => { },
            onDoubleTapCancel: () => { });
        DoubleTapGestureRecognizer doubleTap =
            BuildRecognizer<DoubleTapGestureRecognizer>(GesturesOf(detector));

        Assert.NotNull(doubleTap.OnDoubleTapDown);
        Assert.NotNull(doubleTap.OnDoubleTap);
        Assert.NotNull(doubleTap.OnDoubleTapCancel);

        // Each of the three callbacks alone is enough to register the recognizer.
        Assert.Contains(
            typeof(DoubleTapGestureRecognizer),
            GesturesOf(new GestureDetector(onDoubleTap: () => { })));
        Assert.Contains(
            typeof(DoubleTapGestureRecognizer),
            GesturesOf(new GestureDetector(onDoubleTapDown: _ => { })));
    }

    [Fact]
    public void Build_ConfiguresTheWholePrimarySecondaryTertiaryLongPressMatrix()
    {
        var detector = new GestureDetector(
            onLongPressDown: _ => { },
            onLongPressCancel: () => { },
            onLongPress: () => { },
            onLongPressStart: _ => { },
            onLongPressMoveUpdate: _ => { },
            onLongPressUp: () => { },
            onLongPressEnd: _ => { },
            onSecondaryLongPressDown: _ => { },
            onSecondaryLongPressCancel: () => { },
            onSecondaryLongPress: () => { },
            onSecondaryLongPressStart: _ => { },
            onSecondaryLongPressMoveUpdate: _ => { },
            onSecondaryLongPressUp: () => { },
            onSecondaryLongPressEnd: _ => { },
            onTertiaryLongPressDown: _ => { },
            onTertiaryLongPressCancel: () => { },
            onTertiaryLongPress: () => { },
            onTertiaryLongPressStart: _ => { },
            onTertiaryLongPressMoveUpdate: _ => { },
            onTertiaryLongPressUp: () => { },
            onTertiaryLongPressEnd: _ => { });
        LongPressGestureRecognizer longPress =
            BuildRecognizer<LongPressGestureRecognizer>(GesturesOf(detector));

        Assert.NotNull(longPress.OnLongPressDown);
        Assert.NotNull(longPress.OnLongPressCancel);
        Assert.NotNull(longPress.OnLongPress);
        Assert.NotNull(longPress.OnLongPressStart);
        Assert.NotNull(longPress.OnLongPressMoveUpdate);
        Assert.NotNull(longPress.OnLongPressUp);
        Assert.NotNull(longPress.OnLongPressEnd);
        Assert.NotNull(longPress.OnSecondaryLongPressDown);
        Assert.NotNull(longPress.OnSecondaryLongPressCancel);
        Assert.NotNull(longPress.OnSecondaryLongPress);
        Assert.NotNull(longPress.OnSecondaryLongPressStart);
        Assert.NotNull(longPress.OnSecondaryLongPressMoveUpdate);
        Assert.NotNull(longPress.OnSecondaryLongPressUp);
        Assert.NotNull(longPress.OnSecondaryLongPressEnd);
        Assert.NotNull(longPress.OnTertiaryLongPressDown);
        Assert.NotNull(longPress.OnTertiaryLongPressCancel);
        Assert.NotNull(longPress.OnTertiaryLongPress);
        Assert.NotNull(longPress.OnTertiaryLongPressStart);
        Assert.NotNull(longPress.OnTertiaryLongPressMoveUpdate);
        Assert.NotNull(longPress.OnTertiaryLongPressUp);
        Assert.NotNull(longPress.OnTertiaryLongPressEnd);

        // Any single one of the 21 registers the recognizer.
        Assert.Contains(
            typeof(LongPressGestureRecognizer),
            GesturesOf(new GestureDetector(onTertiaryLongPressMoveUpdate: _ => { })));
    }

    [Fact]
    public void Build_ConfiguresEachDragRecognizerWithItsAxisCallbacks()
    {
        var vertical = new GestureDetector(
            onVerticalDragDown: _ => { },
            onVerticalDragStart: _ => { },
            onVerticalDragUpdate: _ => { },
            onVerticalDragEnd: _ => { },
            onVerticalDragCancel: () => { },
            dragStartBehavior: DragStartBehavior.Down);
        VerticalDragGestureRecognizer verticalDrag =
            BuildRecognizer<VerticalDragGestureRecognizer>(GesturesOf(vertical));
        Assert.NotNull(verticalDrag.OnDown);
        Assert.NotNull(verticalDrag.OnStart);
        Assert.NotNull(verticalDrag.OnUpdate);
        Assert.NotNull(verticalDrag.OnEnd);
        Assert.NotNull(verticalDrag.OnCancel);
        Assert.Equal(DragStartBehavior.Down, verticalDrag.DragStartBehavior);

        var horizontal = new GestureDetector(onHorizontalDragUpdate: _ => { });
        HorizontalDragGestureRecognizer horizontalDrag =
            BuildRecognizer<HorizontalDragGestureRecognizer>(GesturesOf(horizontal));
        Assert.NotNull(horizontalDrag.OnUpdate);

        var pan = new GestureDetector(onPanDown: _ => { });
        PanGestureRecognizer panDrag = BuildRecognizer<PanGestureRecognizer>(GesturesOf(pan));
        Assert.NotNull(panDrag.OnDown);
    }

    [Fact]
    public void Build_TakesTheMultitouchDragStrategyFromTheAmbientScrollBehavior()
    {
        var harness = new ScrollSemanticsHarness(new ScrollConfiguration(
            behavior: new SumAllPointersScrollBehavior(),
            child: new GestureDetector(
                onVerticalDragUpdate: _ => { },
                child: new SizedBox(width: 4.0, height: 4.0))));
        harness.Pump(SurfaceSize);

        VerticalDragGestureRecognizer drag = BuildRecognizer<VerticalDragGestureRecognizer>(
            FindWidget<RawGestureDetector>(harness.RootElement).Gestures);
        Assert.Equal(MultitouchDragStrategy.SumAllPointers, drag.MultitouchDragStrategy);

        // The scale recognizer deliberately does not receive the strategy.
        harness.UpdateRoot(new ScrollConfiguration(
            behavior: new SumAllPointersScrollBehavior(),
            child: new GestureDetector(
                onScaleUpdate: _ => { },
                child: new SizedBox(width: 4.0, height: 4.0))));
        harness.Pump(SurfaceSize);
        ScaleGestureRecognizer scale = BuildRecognizer<ScaleGestureRecognizer>(
            FindWidget<RawGestureDetector>(harness.RootElement).Gestures);
        Assert.NotNull(scale.OnUpdate);
    }

    [Fact]
    public void Build_ConfiguresTheScaleRecognizerIncludingTheTrackpadKnobs()
    {
        var factor = new Point(0.0, -0.01);
        var detector = new GestureDetector(
            onScaleStart: _ => { },
            onScaleUpdate: _ => { },
            onScaleEnd: _ => { },
            dragStartBehavior: DragStartBehavior.Start,
            trackpadScrollCausesScale: true,
            trackpadScrollToScaleFactor: factor);
        ScaleGestureRecognizer scale = BuildRecognizer<ScaleGestureRecognizer>(GesturesOf(detector));

        Assert.NotNull(scale.OnStart);
        Assert.NotNull(scale.OnUpdate);
        Assert.NotNull(scale.OnEnd);
        Assert.Equal(DragStartBehavior.Start, scale.DragStartBehavior);
        Assert.True(scale.TrackpadScrollCausesScale);
        Assert.Equal(factor, scale.TrackpadScrollToScaleFactor);
    }

    [Fact]
    public void Build_DefaultsTrackpadScrollToScaleFactorToFlutterConstant()
    {
        var detector = new GestureDetector(onScaleUpdate: _ => { });
        Assert.Equal(ScaleGestureRecognizer.KDefaultTrackpadScrollToScaleFactor,
            detector.TrackpadScrollToScaleFactor);
        Assert.Equal(new Point(0.0, -1.0 / 200.0), detector.TrackpadScrollToScaleFactor);
        Assert.False(detector.TrackpadScrollCausesScale);
        Assert.Equal(DragStartBehavior.Start, detector.DragStartBehavior);
        Assert.False(detector.ExcludeFromSemantics);
        Assert.Null(detector.Behavior);
        Assert.Null(detector.SupportedDevices);
    }

    [Fact]
    public void Build_GatesAndConfiguresTheForcePressRecognizer()
    {
        var detector = new GestureDetector(
            onForcePressStart: _ => { },
            onForcePressPeak: _ => { },
            onForcePressUpdate: _ => { },
            onForcePressEnd: _ => { });
        ForcePressGestureRecognizer force =
            BuildRecognizer<ForcePressGestureRecognizer>(GesturesOf(detector));

        Assert.NotNull(force.OnStart);
        Assert.NotNull(force.OnPeak);
        Assert.NotNull(force.OnUpdate);
        Assert.NotNull(force.OnEnd);
        Assert.Equal(0.4, force.StartPressure);
        Assert.Equal(0.85, force.PeakPressure);

        Assert.Contains(
            typeof(ForcePressGestureRecognizer),
            GesturesOf(new GestureDetector(onForcePressEnd: _ => { })));
    }

    [Fact]
    public void Build_ForwardsBehaviorExcludeFromSemanticsAndChildToTheRawDetector()
    {
        var child = new SizedBox(width: 4.0, height: 4.0);
        var detector = new GestureDetector(
            child: child,
            behavior: HitTestBehavior.Opaque,
            excludeFromSemantics: true,
            onTap: () => { });
        var harness = new ScrollSemanticsHarness(detector);
        harness.Pump(SurfaceSize);

        RawGestureDetector raw = FindWidget<RawGestureDetector>(harness.RootElement);
        Assert.Equal(HitTestBehavior.Opaque, raw.Behavior);
        Assert.True(raw.ExcludeFromSemantics);
        Assert.Same(child, raw.Child);
        // GestureDetector never passes a delegate, so the default one is used.
        Assert.Null(raw.Semantics);
    }

    [DebugOnlyFact]
    public void DebugFillProperties_ReportsTheDragStartBehaviorAsStartBehavior()
    {
        var properties = new DiagnosticPropertiesBuilder();
        new GestureDetector(dragStartBehavior: DragStartBehavior.Down).DebugFillProperties(properties);
        Assert.Contains(
            properties.Properties,
            property => property.Name == "startBehavior" && property.ToString()!.Contains("down",
                StringComparison.OrdinalIgnoreCase));
    }

    // ---------------------------------------------------------------- RawGestureDetector

    [Fact]
    public void RawGestureDetector_DefaultsMatchFlutter()
    {
        var detector = new RawGestureDetector();
        Assert.Empty(detector.Gestures);
        Assert.Null(detector.Behavior);
        Assert.False(detector.ExcludeFromSemantics);
        Assert.Null(detector.Semantics);
        Assert.Null(detector.Child);
    }

    [Fact]
    public void DefaultBehavior_IsTranslucentWithoutAChildAndDeferToChildWithOne()
    {
        var childless = new ScrollSemanticsHarness(new RawGestureDetector());
        childless.Pump(SurfaceSize);
        Assert.Equal(HitTestBehavior.Translucent, FindDetectorState(childless.RootElement).DefaultBehavior);
        // Both the semantics render object and the Listener take the resolved behavior.
        Assert.Equal(HitTestBehavior.Translucent, FindHandler(childless.RootElement).Behavior);
        Assert.Equal(HitTestBehavior.Translucent, FindWidget<Listener>(childless.RootElement).Behavior);

        var withChild = new ScrollSemanticsHarness(
            new RawGestureDetector(child: new SizedBox(width: 4.0, height: 4.0)));
        withChild.Pump(SurfaceSize);
        Assert.Equal(HitTestBehavior.DeferToChild, FindDetectorState(withChild.RootElement).DefaultBehavior);
        Assert.Equal(HitTestBehavior.DeferToChild, FindHandler(withChild.RootElement).Behavior);

        var explicitBehavior = new ScrollSemanticsHarness(new RawGestureDetector(
            child: new SizedBox(width: 4.0, height: 4.0),
            behavior: HitTestBehavior.Opaque));
        explicitBehavior.Pump(SurfaceSize);
        Assert.Equal(HitTestBehavior.Opaque, FindHandler(explicitBehavior.RootElement).Behavior);
        Assert.Equal(HitTestBehavior.Opaque, FindWidget<Listener>(explicitBehavior.RootElement).Behavior);
    }

    [Fact]
    public void ExcludeFromSemantics_OmitsTheSemanticsRenderObject()
    {
        var harness = new ScrollSemanticsHarness(new RawGestureDetector(
            excludeFromSemantics: true,
            child: new SizedBox(width: 4.0, height: 4.0)));
        harness.Pump(SurfaceSize);
        Assert.Throws<InvalidOperationException>(() => FindHandler(harness.RootElement));
    }

    [Fact]
    public void SyncAll_ReusesTheRecognizerOfAGivenTypeAcrossRebuilds()
    {
        GestureRecognizer? constructed = null;
        int initializerRuns = 0;
        Widget Build(IReadOnlySet<PointerDeviceKind>? devices) => new RawGestureDetector(
            child: new SizedBox(width: 4.0, height: 4.0),
            gestures: new Dictionary<Type, IGestureRecognizerFactory>
            {
                [typeof(TapGestureRecognizer)] =
                    new GestureRecognizerFactoryWithHandlers<TapGestureRecognizer>(
                        () =>
                        {
                            var recognizer = new TapGestureRecognizer();
                            constructed = recognizer;
                            return recognizer;
                        },
                        instance =>
                        {
                            initializerRuns++;
                            instance.SupportedDevices = devices;
                        }),
            });

        var mouse = new HashSet<PointerDeviceKind> { PointerDeviceKind.Mouse };
        var stylus = new HashSet<PointerDeviceKind> { PointerDeviceKind.Stylus };
        var harness = new ScrollSemanticsHarness(Build(mouse));
        harness.Pump(SurfaceSize);
        GestureRecognizer? first = constructed;
        Assert.NotNull(first);
        Assert.Equal(1, initializerRuns);

        harness.UpdateRoot(Build(stylus));
        harness.Pump(SurfaceSize);
        // The same instance is reused and re-initialized, so the new supportedDevices take effect.
        Assert.Same(first, constructed);
        Assert.Equal(2, initializerRuns);
        Assert.Same(stylus, FindDetectorState(harness.RootElement).Recognizers[typeof(TapGestureRecognizer)]
            .SupportedDevices);
    }

    [Fact]
    public void SyncAll_DisposesARecognizerWhoseTypeLeavesTheMap()
    {
        Widget Build(bool tap) => new RawGestureDetector(
            child: new SizedBox(width: 4.0, height: 4.0),
            gestures: tap
                ? new Dictionary<Type, IGestureRecognizerFactory>
                {
                    [typeof(TapGestureRecognizer)] =
                        new GestureRecognizerFactoryWithHandlers<TapGestureRecognizer>(
                            () => new TapGestureRecognizer(),
                            _ => { }),
                }
                : new Dictionary<Type, IGestureRecognizerFactory>
                {
                    [typeof(LongPressGestureRecognizer)] =
                        new GestureRecognizerFactoryWithHandlers<LongPressGestureRecognizer>(
                            () => new LongPressGestureRecognizer(),
                            _ => { }),
                });

        var harness = new ScrollSemanticsHarness(Build(tap: true));
        harness.Pump(SurfaceSize);
        RawGestureDetectorState state = FindDetectorState(harness.RootElement);
        Assert.Equal([typeof(TapGestureRecognizer)], state.Recognizers.Keys);

        harness.UpdateRoot(Build(tap: false));
        harness.Pump(SurfaceSize);
        Assert.Equal([typeof(LongPressGestureRecognizer)], state.Recognizers.Keys);
        // The semantics notations follow the recognizer set.
        RenderSemanticsGestureHandler handler = FindHandler(harness.RootElement);
        Assert.Null(handler.OnTap);
        Assert.NotNull(handler.OnLongPress);
    }

    [DebugOnlyFact]
    public void SyncAll_RejectsAFactoryRegisteredUnderTheWrongType()
    {
        var harness = new ScrollSemanticsHarness(new SizedBox(width: 4.0, height: 4.0));
        harness.Pump(SurfaceSize);
        Assert.Throws<FlutterError>(() =>
        {
            harness.UpdateRoot(new RawGestureDetector(
                child: new SizedBox(width: 4.0, height: 4.0),
                gestures: new Dictionary<Type, IGestureRecognizerFactory>
                {
                    [typeof(LongPressGestureRecognizer)] =
                        new GestureRecognizerFactoryWithHandlers<TapGestureRecognizer>(
                            () => new TapGestureRecognizer(),
                            _ => { }),
                }));
        });
    }

    [Fact]
    public void BuildModeGates_TheGestureDetectorContractChecksRunOnlyInADebugBuild()
    {
        // Dart wraps the constructor combination check, the `replaceGestureRecognizers` layout-phase
        // check, `_debugAssertTypeMatches` and the `replaceSemanticsActions` render-object check in
        // `assert(...)`, so a release build runs none of them. The `[DebugOnlyFact]` tests above
        // cover the debug half; this covers the two observable elided halves in every configuration
        // (a mismatched factory type still breaks the semantics delegate's cast, in Dart as here).
        if (Constants.KDebugMode)
        {
            Assert.Throws<FlutterError>(() => new GestureDetector(
                onPanUpdate: _ => { },
                onScaleUpdate: _ => { }));
            return;
        }

        var detector = new GestureDetector(onPanUpdate: _ => { }, onScaleUpdate: _ => { });
        Assert.NotNull(detector);

        var harness = new ScrollSemanticsHarness(new RawGestureDetector(
            child: new SizedBox(width: 4.0, height: 4.0)));
        harness.Pump(SurfaceSize);
        RawGestureDetectorState state = FindDetectorState(harness.RootElement);

        // Called outside the layout phase: silently accepted.
        state.ReplaceGestureRecognizers(new Dictionary<Type, IGestureRecognizerFactory>
        {
            [typeof(LongPressGestureRecognizer)] =
                new GestureRecognizerFactoryWithHandlers<LongPressGestureRecognizer>(
                    () => new LongPressGestureRecognizer(),
                    _ => { }),
        });
        Assert.Equal([typeof(LongPressGestureRecognizer)], state.Recognizers.Keys);
    }

    [DebugOnlyFact]
    public void ReplaceGestureRecognizers_ThrowsOutsideTheLayoutPhase()
    {
        var harness = new ScrollSemanticsHarness(new RawGestureDetector(
            child: new SizedBox(width: 4.0, height: 4.0)));
        harness.Pump(SurfaceSize);
        RawGestureDetectorState state = FindDetectorState(harness.RootElement);

        FlutterError error = Assert.Throws<FlutterError>(
            () => state.ReplaceGestureRecognizers(RawGestureDetector.NoGestures));
        Assert.Contains("can only be called during the layout phase", error.Message, StringComparison.Ordinal);
        Assert.Equal(DiagnosticLevel.Hint, error.Diagnostics[^1].Level);
    }

    [Fact]
    public void ReplaceGestureRecognizers_SwapsRecognizersAndSemanticsDuringLayout()
    {
        RawGestureDetectorState? state = null;
        int taps = 0;
        var replacement = new Dictionary<Type, IGestureRecognizerFactory>
        {
            [typeof(TapGestureRecognizer)] = new GestureRecognizerFactoryWithHandlers<TapGestureRecognizer>(
                () => new TapGestureRecognizer(),
                instance => instance.OnTap = () => taps++),
        };

        var harness = new ScrollSemanticsHarness(new RawGestureDetector(
            gestures: new Dictionary<Type, IGestureRecognizerFactory>
            {
                [typeof(HorizontalDragGestureRecognizer)] =
                    new GestureRecognizerFactoryWithHandlers<HorizontalDragGestureRecognizer>(
                        () => new HorizontalDragGestureRecognizer(),
                        instance => instance.OnUpdate = _ => { }),
            },
            child: new LayoutCallback(
                onPerformLayout: () => state?.ReplaceGestureRecognizers(replacement),
                child: new SizedBox(width: 20.0, height: 20.0))));
        harness.Pump(SurfaceSize);
        state = FindDetectorState(harness.RootElement);
        RenderSemanticsGestureHandler handler = FindHandler(harness.RootElement);
        Assert.NotNull(handler.OnHorizontalDragUpdate);

        FindRenderObject<RenderLayoutCallback>(harness.RootElement).MarkNeedsLayout();
        harness.Pump(SurfaceSize);

        Assert.Equal([typeof(TapGestureRecognizer)], state.Recognizers.Keys);
        Assert.Null(handler.OnHorizontalDragUpdate);
        Assert.NotNull(handler.OnTap);
        handler.OnTap!();
        Assert.Equal(1, taps);
    }

    [Fact]
    public void ReplaceSemanticsActions_FiltersTheExposedActionsAndIsANoOpWhenExcluded()
    {
        var harness = new ScrollSemanticsHarness(CenteredDetector(new RawGestureDetector(
            gestures: new Dictionary<Type, IGestureRecognizerFactory>
            {
                [typeof(TapGestureRecognizer)] =
                    new GestureRecognizerFactoryWithHandlers<TapGestureRecognizer>(
                        () => new TapGestureRecognizer(),
                        instance => instance.OnTap = () => { }),
                [typeof(LongPressGestureRecognizer)] =
                    new GestureRecognizerFactoryWithHandlers<LongPressGestureRecognizer>(
                        () => new LongPressGestureRecognizer(),
                        instance => instance.OnLongPress = () => { }),
            })));
        harness.Pump(SurfaceSize);
        SemanticsNode node = Assert.IsType<SemanticsNode>(FindGestureNode(harness.SemanticsRoot));
        Assert.True(node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.True(node.Actions.HasFlag(SemanticsActions.LongPress));

        FindDetectorState(harness.RootElement).ReplaceSemanticsActions(SemanticsActions.LongPress);
        harness.Pump(SurfaceSize);
        node = Assert.IsType<SemanticsNode>(FindGestureNode(harness.SemanticsRoot));
        Assert.False(node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.True(node.Actions.HasFlag(SemanticsActions.LongPress));

        var excluded = new ScrollSemanticsHarness(new RawGestureDetector(
            excludeFromSemantics: true,
            child: new SizedBox(width: 4.0, height: 4.0)));
        excluded.Pump(SurfaceSize);
        // Returns without looking for the render object, which does not exist.
        FindDetectorState(excluded.RootElement).ReplaceSemanticsActions(SemanticsActions.Tap);
    }

    [DebugOnlyFact]
    public void Dispose_DisposesEveryRecognizerAndMarksTheStateDisposed()
    {
        var harness = new ScrollSemanticsHarness(new RawGestureDetector(
            child: new SizedBox(width: 4.0, height: 4.0),
            gestures: new Dictionary<Type, IGestureRecognizerFactory>
            {
                [typeof(TapGestureRecognizer)] =
                    new GestureRecognizerFactoryWithHandlers<TapGestureRecognizer>(
                        () => new TapGestureRecognizer(),
                        _ => { }),
            }));
        harness.Pump(SurfaceSize);
        RawGestureDetectorState state = FindDetectorState(harness.RootElement);

        harness.UpdateRoot(new SizedBox(width: 4.0, height: 4.0));
        harness.Pump(SurfaceSize);

        var properties = new DiagnosticPropertiesBuilder();
        state.DebugFillProperties(properties);
        Assert.Contains(properties.Properties, property => property.ToString() == "DISPOSED");
    }

    [DebugOnlyFact]
    public void DebugFillProperties_ReportsGesturesSemanticsAndBehavior()
    {
        var bare = new ScrollSemanticsHarness(new RawGestureDetector());
        bare.Pump(SurfaceSize);
        var properties = new DiagnosticPropertiesBuilder();
        FindDetectorState(bare.RootElement).DebugFillProperties(properties);
        Assert.Equal(
            ["gestures: <none>"],
            properties.Properties
                .Where(property => property.Level >= DiagnosticLevel.Info)
                .Select(property => property.ToString())
                .ToArray());

        var configured = new ScrollSemanticsHarness(new RawGestureDetector(
            behavior: HitTestBehavior.DeferToChild,
            semantics: new EmptySemanticsGestureDelegate(),
            gestures: new Dictionary<Type, IGestureRecognizerFactory>
            {
                [typeof(TapGestureRecognizer)] =
                    new GestureRecognizerFactoryWithHandlers<TapGestureRecognizer>(
                        () => new TapGestureRecognizer(),
                        _ => { }),
                [typeof(LongPressGestureRecognizer)] =
                    new GestureRecognizerFactoryWithHandlers<LongPressGestureRecognizer>(
                        () => new LongPressGestureRecognizer(),
                        _ => { }),
            }));
        configured.Pump(SurfaceSize);
        properties = new DiagnosticPropertiesBuilder();
        FindDetectorState(configured.RootElement).DebugFillProperties(properties);
        Assert.Equal(
            [
                "gestures: tap, long press",
                "semantics: EmptySemanticsGestureDelegate()",
                "behavior: deferToChild",
            ],
            properties.Properties
                .Where(property => property.Level >= DiagnosticLevel.Info)
                .Select(property => property.ToString())
                .ToArray());

        var noSemantics = new ScrollSemanticsHarness(new RawGestureDetector(excludeFromSemantics: true));
        noSemantics.Pump(SurfaceSize);
        properties = new DiagnosticPropertiesBuilder();
        FindDetectorState(noSemantics.RootElement).DebugFillProperties(properties);
        Assert.Equal(
            ["gestures: <none>", "excludeFromSemantics: true"],
            properties.Properties
                .Where(property => property.Level >= DiagnosticLevel.Info)
                .Select(property => property.ToString())
                .ToArray());
    }

    // ---------------------------------------------------------------- default semantics delegate

    [Fact]
    public void DefaultDelegate_LeavesEveryHandlerNullWithoutMatchingRecognizers()
    {
        var harness = new ScrollSemanticsHarness(CenteredDetector(new RawGestureDetector()));
        harness.Pump(SurfaceSize);
        RenderSemanticsGestureHandler handler = FindHandler(harness.RootElement);

        Assert.Null(handler.OnTap);
        Assert.Null(handler.OnLongPress);
        Assert.Null(handler.OnHorizontalDragUpdate);
        Assert.Null(handler.OnVerticalDragUpdate);
        Assert.Null(FindGestureNode(harness.SemanticsRoot));
    }

    [Fact]
    public void DefaultDelegate_ExposesActionsForCallbackLessRecognizers()
    {
        RenderSemanticsGestureHandler HandlerFor<T>(Func<T> constructor) where T : GestureRecognizer
        {
            var harness = new ScrollSemanticsHarness(CenteredDetector(new RawGestureDetector(
                gestures: new Dictionary<Type, IGestureRecognizerFactory>
                {
                    [typeof(T)] = new GestureRecognizerFactoryWithHandlers<T>(constructor, _ => { }),
                })));
            harness.Pump(SurfaceSize);
            return FindHandler(harness.RootElement);
        }

        Assert.NotNull(HandlerFor(() => new TapGestureRecognizer()).OnTap);
        Assert.NotNull(HandlerFor(() => new LongPressGestureRecognizer()).OnLongPress);

        RenderSemanticsGestureHandler horizontal =
            HandlerFor(() => new HorizontalDragGestureRecognizer());
        Assert.NotNull(horizontal.OnHorizontalDragUpdate);
        Assert.Null(horizontal.OnVerticalDragUpdate);

        RenderSemanticsGestureHandler vertical = HandlerFor(() => new VerticalDragGestureRecognizer());
        Assert.Null(vertical.OnHorizontalDragUpdate);
        Assert.NotNull(vertical.OnVerticalDragUpdate);

        // A lone pan recognizer answers on both axes, so all four scroll actions become available.
        RenderSemanticsGestureHandler pan = HandlerFor(() => new PanGestureRecognizer());
        Assert.NotNull(pan.OnHorizontalDragUpdate);
        Assert.NotNull(pan.OnVerticalDragUpdate);
    }

    [Fact]
    public void DefaultDelegate_ReplaysTapAtTheRenderObjectCentre()
    {
        var log = new List<string>();
        Point downGlobal = default;
        Point downLocal = default;
        var harness = new ScrollSemanticsHarness(CenteredDetector(new RawGestureDetector(
            gestures: new Dictionary<Type, IGestureRecognizerFactory>
            {
                [typeof(TapGestureRecognizer)] =
                    new GestureRecognizerFactoryWithHandlers<TapGestureRecognizer>(
                        () => new TapGestureRecognizer(),
                        instance =>
                        {
                            instance.OnTapDown = details =>
                            {
                                downGlobal = details.GlobalPosition;
                                downLocal = details.LocalPosition;
                                log.Add("tapDown");
                            };
                            instance.OnTapUp = _ => log.Add("tapUp");
                            instance.OnTap = () => log.Add("tap");
                            instance.OnTapCancel = () => log.Add("tapCancel");
                            instance.OnSecondaryTapDown = _ => log.Add("secondaryTapDown");
                            instance.OnTertiaryTapDown = _ => log.Add("tertiaryTapDown");
                        }),
            })));
        harness.Pump(SurfaceSize);

        SemanticsNode node = Assert.IsType<SemanticsNode>(FindGestureNode(harness.SemanticsRoot));
        Assert.True(harness.PerformSemanticsAction(node.Id, SemanticsActions.Tap));

        Assert.Equal(["tapDown", "tapUp", "tap"], log);
        Assert.Equal(new Point(10.0, 10.0), downLocal);
        Assert.Equal(new Point(400.0, 300.0), downGlobal);
    }

    [Fact]
    public void DefaultDelegate_ReplaysTheLongPressSequenceWithoutMoveUpdates()
    {
        var log = new List<string>();
        Point startLocal = default;
        Point startGlobal = default;
        var harness = new ScrollSemanticsHarness(CenteredDetector(new RawGestureDetector(
            gestures: new Dictionary<Type, IGestureRecognizerFactory>
            {
                [typeof(LongPressGestureRecognizer)] =
                    new GestureRecognizerFactoryWithHandlers<LongPressGestureRecognizer>(
                        () => new LongPressGestureRecognizer(),
                        instance =>
                        {
                            instance.OnLongPressDown = _ => log.Add("LPDown");
                            instance.OnLongPressCancel = () => log.Add("LPCancel");
                            instance.OnLongPressStart = details =>
                            {
                                startLocal = details.LocalPosition;
                                startGlobal = details.GlobalPosition;
                                log.Add("LPStart");
                            };
                            instance.OnLongPress = () => log.Add("LP");
                            instance.OnLongPressMoveUpdate = _ => log.Add("LPMove");
                            instance.OnLongPressEnd = _ => log.Add("LPEnd");
                            instance.OnLongPressUp = () => log.Add("LPUp");
                        }),
            })));
        harness.Pump(SurfaceSize);

        SemanticsNode node = Assert.IsType<SemanticsNode>(FindGestureNode(harness.SemanticsRoot));
        Assert.True(harness.PerformSemanticsAction(node.Id, SemanticsActions.LongPress));

        Assert.Equal(["LPDown", "LPStart", "LP", "LPEnd", "LPUp"], log);
        Assert.Equal(new Point(10.0, 10.0), startLocal);
        Assert.Equal(new Point(400.0, 300.0), startGlobal);
    }

    [Fact]
    public void DefaultDelegate_RunsTheAxisRecognizerBeforeThePanRecognizerOnBothAxes()
    {
        var log = new List<string>();
        DragEndDetails horizontalEnd = default;
        DragEndDetails panEnd = default;

        GestureRecognizerFactoryWithHandlers<T> Drag<T>(Func<T> constructor, string prefix)
            where T : DragGestureRecognizer
        {
            return new GestureRecognizerFactoryWithHandlers<T>(
                constructor,
                instance =>
                {
                    instance.OnDown = _ => log.Add(prefix + "Down");
                    instance.OnStart = _ => log.Add(prefix + "Start");
                    instance.OnUpdate = _ => log.Add(prefix + "Update");
                    instance.OnEnd = details =>
                    {
                        if (prefix == "P")
                        {
                            panEnd = details;
                        }
                        else
                        {
                            horizontalEnd = details;
                        }

                        log.Add(prefix + "End");
                    };
                    instance.OnCancel = () => log.Add(prefix + "Cancel");
                });
        }

        var harness = new ScrollSemanticsHarness(CenteredDetector(new RawGestureDetector(
            gestures: new Dictionary<Type, IGestureRecognizerFactory>
            {
                [typeof(HorizontalDragGestureRecognizer)] =
                    Drag(() => new HorizontalDragGestureRecognizer(), "H"),
                [typeof(PanGestureRecognizer)] = Drag(() => new PanGestureRecognizer(), "P"),
            })));
        harness.Pump(SurfaceSize);

        SemanticsNode node = Assert.IsType<SemanticsNode>(FindGestureNode(harness.SemanticsRoot));
        Assert.True(harness.PerformSemanticsAction(node.Id, SemanticsActions.ScrollLeft));
        Assert.Equal(["HDown", "HStart", "HUpdate", "HEnd", "PDown", "PStart", "PUpdate", "PEnd"], log);

        // The axis recognizer's synthesized end carries a zero primary velocity; the pan's carries none.
        Assert.Equal(0.0, horizontalEnd.PrimaryVelocity);
        Assert.Null(panEnd.PrimaryVelocity);
        Assert.Equal(Velocity.Zero, horizontalEnd.Velocity);
        // The end position is the centre plus the update delta the scroll action synthesized.
        Assert.Equal(new Point(10.0 + (20.0 * -0.8), 10.0), horizontalEnd.LocalPosition);

        // Running the action again produces the identical sequence — no state carries over.
        log.Clear();
        Assert.True(harness.PerformSemanticsAction(node.Id, SemanticsActions.ScrollLeft));
        Assert.Equal(["HDown", "HStart", "HUpdate", "HEnd", "PDown", "PStart", "PUpdate", "PEnd"], log);

        log.Clear();
        Assert.True(harness.PerformSemanticsAction(node.Id, SemanticsActions.ScrollUp));
        Assert.Equal(["PDown", "PStart", "PUpdate", "PEnd"], log);
    }

    [Fact]
    public void DefaultDelegate_ExposesTheVerticalAxisBeforeThePan()
    {
        var log = new List<string>();
        var harness = new ScrollSemanticsHarness(CenteredDetector(new RawGestureDetector(
            gestures: new Dictionary<Type, IGestureRecognizerFactory>
            {
                [typeof(VerticalDragGestureRecognizer)] =
                    new GestureRecognizerFactoryWithHandlers<VerticalDragGestureRecognizer>(
                        () => new VerticalDragGestureRecognizer(),
                        instance =>
                        {
                            instance.OnDown = _ => log.Add("VDown");
                            instance.OnStart = _ => log.Add("VStart");
                            instance.OnUpdate = _ => log.Add("VUpdate");
                            instance.OnEnd = _ => log.Add("VEnd");
                        }),
                [typeof(PanGestureRecognizer)] =
                    new GestureRecognizerFactoryWithHandlers<PanGestureRecognizer>(
                        () => new PanGestureRecognizer(),
                        instance =>
                        {
                            instance.OnDown = _ => log.Add("PDown");
                            instance.OnStart = _ => log.Add("PStart");
                            instance.OnUpdate = _ => log.Add("PUpdate");
                            instance.OnEnd = _ => log.Add("PEnd");
                        }),
            })));
        harness.Pump(SurfaceSize);

        SemanticsNode node = Assert.IsType<SemanticsNode>(FindGestureNode(harness.SemanticsRoot));
        Assert.True(harness.PerformSemanticsAction(node.Id, SemanticsActions.ScrollUp));
        Assert.Equal(["VDown", "VStart", "VUpdate", "VEnd", "PDown", "PStart", "PUpdate", "PEnd"], log);

        log.Clear();
        Assert.True(harness.PerformSemanticsAction(node.Id, SemanticsActions.ScrollDown));
        Assert.Equal(["VDown", "VStart", "VUpdate", "VEnd", "PDown", "PStart", "PUpdate", "PEnd"], log);
    }

    [Fact]
    public void VerticalDetector_ExposesOnlyTheUpAndDownActions()
    {
        int starts = 0;
        var harness = new ScrollSemanticsHarness(
            CenteredDetector(new GestureDetector(onVerticalDragStart: _ => starts++)));
        harness.Pump(SurfaceSize);

        SemanticsNode node = Assert.IsType<SemanticsNode>(FindGestureNode(harness.SemanticsRoot));
        Assert.True(node.Actions.HasFlag(SemanticsActions.ScrollUp));
        Assert.True(node.Actions.HasFlag(SemanticsActions.ScrollDown));
        Assert.False(node.Actions.HasFlag(SemanticsActions.ScrollLeft));
        Assert.False(node.Actions.HasFlag(SemanticsActions.ScrollRight));

        Assert.False(harness.PerformSemanticsAction(node.Id, SemanticsActions.ScrollLeft));
        Assert.Equal(0, starts);
        Assert.True(harness.PerformSemanticsAction(node.Id, SemanticsActions.ScrollUp));
        Assert.Equal(1, starts);
        Assert.True(harness.PerformSemanticsAction(node.Id, SemanticsActions.ScrollDown));
        Assert.Equal(2, starts);
    }

    [Fact]
    public void HorizontalDetector_ExposesOnlyTheLeftAndRightActions()
    {
        int starts = 0;
        var harness = new ScrollSemanticsHarness(
            CenteredDetector(new GestureDetector(onHorizontalDragStart: _ => starts++)));
        harness.Pump(SurfaceSize);

        SemanticsNode node = Assert.IsType<SemanticsNode>(FindGestureNode(harness.SemanticsRoot));
        Assert.True(node.Actions.HasFlag(SemanticsActions.ScrollLeft));
        Assert.True(node.Actions.HasFlag(SemanticsActions.ScrollRight));
        Assert.False(node.Actions.HasFlag(SemanticsActions.ScrollUp));
        Assert.False(node.Actions.HasFlag(SemanticsActions.ScrollDown));

        Assert.False(harness.PerformSemanticsAction(node.Id, SemanticsActions.ScrollUp));
        Assert.Equal(0, starts);
        Assert.True(harness.PerformSemanticsAction(node.Id, SemanticsActions.ScrollLeft));
        Assert.Equal(1, starts);
    }

    // ---------------------------------------------------------------- custom semantics delegate

    [Fact]
    public void CustomDelegate_ReplacesTheDefaultNotationsAndIsSwappedBackOnUpdate()
    {
        Widget Build(SemanticsGestureDelegate? semantics) => CenteredDetector(new RawGestureDetector(
            semantics: semantics,
            gestures: new Dictionary<Type, IGestureRecognizerFactory>
            {
                [typeof(TapGestureRecognizer)] =
                    new GestureRecognizerFactoryWithHandlers<TapGestureRecognizer>(
                        () => new TapGestureRecognizer(),
                        instance => instance.OnTap = () => { }),
                [typeof(LongPressGestureRecognizer)] =
                    new GestureRecognizerFactoryWithHandlers<LongPressGestureRecognizer>(
                        () => new LongPressGestureRecognizer(),
                        instance => instance.OnLongPress = () => { }),
            }));

        var harness = new ScrollSemanticsHarness(Build(null));
        harness.Pump(SurfaceSize);
        SemanticsNode node = Assert.IsType<SemanticsNode>(FindGestureNode(harness.SemanticsRoot));
        Assert.True(node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.True(node.Actions.HasFlag(SemanticsActions.LongPress));

        int customTaps = 0;
        harness.UpdateRoot(Build(new TapOnlySemanticsGestureDelegate(() => customTaps++)));
        harness.Pump(SurfaceSize);
        node = Assert.IsType<SemanticsNode>(FindGestureNode(harness.SemanticsRoot));
        Assert.True(node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.False(node.Actions.HasFlag(SemanticsActions.LongPress));
        Assert.True(harness.PerformSemanticsAction(node.Id, SemanticsActions.Tap));
        Assert.Equal(1, customTaps);

        // Switching back to the default delegate restores the long press notation.
        harness.UpdateRoot(Build(null));
        harness.Pump(SurfaceSize);
        node = Assert.IsType<SemanticsNode>(FindGestureNode(harness.SemanticsRoot));
        Assert.True(node.Actions.HasFlag(SemanticsActions.Tap));
        Assert.True(node.Actions.HasFlag(SemanticsActions.LongPress));
    }

    [DebugOnlyFact]
    public void SemanticsGestureDelegate_ToStringNamesItsRuntimeType()
    {
        Assert.Equal("EmptySemanticsGestureDelegate()", new EmptySemanticsGestureDelegate().ToString());
    }

    private sealed class SumAllPointersScrollBehavior : ScrollBehavior
    {
        public override MultitouchDragStrategy GetMultitouchDragStrategy(BuildContext context)
        {
            return MultitouchDragStrategy.SumAllPointers;
        }
    }

    private sealed class EmptySemanticsGestureDelegate : SemanticsGestureDelegate
    {
        public override void AssignSemantics(RenderSemanticsGestureHandler renderObject)
        {
        }
    }

    private sealed class TapOnlySemanticsGestureDelegate : SemanticsGestureDelegate
    {
        private readonly Action _onTap;

        public TapOnlySemanticsGestureDelegate(Action onTap)
        {
            _onTap = onTap;
        }

        public override void AssignSemantics(RenderSemanticsGestureHandler renderObject)
        {
            renderObject.OnTap = _onTap;
            renderObject.OnLongPress = null;
            renderObject.OnHorizontalDragUpdate = null;
            renderObject.OnVerticalDragUpdate = null;
        }
    }

    /// <summary>Runs a callback from its own <c>PerformLayout</c>, the way Flutter's test does.</summary>
    private sealed class LayoutCallback : SingleChildRenderObjectWidget
    {
        public LayoutCallback(Action onPerformLayout, Widget? child = null) : base(child)
        {
            OnPerformLayout = onPerformLayout;
        }

        public Action OnPerformLayout { get; }

        internal override RenderObject CreateRenderObject(BuildContext context)
        {
            return new RenderLayoutCallback { OnPerformLayout = OnPerformLayout };
        }

        internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
        {
            ((RenderLayoutCallback)renderObject).OnPerformLayout = OnPerformLayout;
        }
    }

    private sealed class RenderLayoutCallback : RenderProxyBox
    {
        public Action? OnPerformLayout { get; set; }

        protected override void PerformLayout()
        {
            base.PerformLayout();
            OnPerformLayout?.Invoke();
        }
    }
}
