using System.Text.RegularExpressions;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit.Sdk;

// C#-only test infrastructure: the subset of flutter_test's `WidgetTester` and
// `TestWidgetsFlutterBinding` that the Dart-ported framework tests (`FrameworkDart*Tests`) use.

namespace Plumix.Tests;

/// <summary>
/// Drives a widget tree the way <c>flutter_test</c> does: every pump attaches the widget under a
/// <see cref="View"/> and a <c>[root]</c> <see cref="RootWidget"/>, then runs a full frame (build,
/// layout, paint, <see cref="BuildOwner.FinalizeTree"/>) from a persistent frame callback, so an
/// exception thrown during the frame is reported instead of escaping the pump.
/// </summary>
/// <remarks>
/// Reported errors are kept the way <c>TestWidgetsFlutterBinding</c> keeps them: the first one is
/// pending until <see cref="TakeException"/>, any further one is unexpected, and either left over at
/// the end of the test fails it.
/// </remarks>
internal sealed class FrameworkDartTester : IDisposable
{
    private static int _nextViewId = 7000;
    private static int _nextPointer = 7000;

    private readonly BuildOwner _owner = new();
    private readonly FlutterExceptionHandler? _previousOnError;
    private readonly List<FlutterErrorDetails> _unexpected = [];
    private readonly Action<TimeSpan> _drawFrame;
    private FlutterErrorDetails? _pending;
    private TimeSpan _clock;
    private RootElement _root;
    private bool _disposed;

    public FrameworkDartTester()
    {
        GestureBinding.Instance.ResetForTests();
        View = new FlutterView(new Size(800, 600), 1.0, Interlocked.Increment(ref _nextViewId));
        _previousOnError = FlutterError.OnError;
        FlutterError.OnError = HandleError;
        _drawFrame = _ => DrawFrame();
        double seconds = Math.Max(Scheduler.CurrentSeconds, Scheduler.CurrentSystemFrameTimeStamp.TotalSeconds);
        _clock = TimeSpan.FromSeconds(seconds + 1.0);

        // The Dart binding already has a root element when a test starts, so every pumpWidget goes
        // through the update path inside a frame. Bootstrap one here for the same reason.
        _root = new RootWidget(child: Wrap(new SizedBox()), debugShortDescription: "[root]").Attach(_owner);
        Pump();
    }

    public FlutterView View { get; }

    public BuildOwner Owner => _owner;

    public RootElement Root => _root;

    /// <summary>The render view the <see cref="View"/> widget created for <see cref="View"/>.</summary>
    public RenderView RenderView => RendererBinding.Instance.RenderViews.First(view => view.FlutterView == View);

    /// <summary>Dart's <c>WidgetTester.pumpWidget</c>.</summary>
    public void PumpWidget(Widget widget, TimeSpan? duration = null)
    {
        _root = new RootWidget(child: Wrap(widget), debugShortDescription: "[root]").Attach(_owner, _root);
        Pump(duration);
    }

    /// <summary>Dart's <c>WidgetTester.pump</c>: one frame, after advancing the clock.</summary>
    public void Pump(TimeSpan? duration = null)
    {
        _clock += duration ?? TimeSpan.Zero;
        Scheduler.AddPersistentFrameCallback(_drawFrame);
        try
        {
            Scheduler.HandleBeginFrame(_clock);
            Scheduler.FlushMicrotasks();
            Scheduler.HandleDrawFrame();
        }
        finally
        {
            Scheduler.RemovePersistentFrameCallback(_drawFrame);
        }
    }

    /// <summary>Dart's <c>WidgetTester.pumpAndSettle</c> with its default 100ms step.</summary>
    public int PumpAndSettle()
    {
        int count = 0;
        do
        {
            if (count > 1000)
            {
                throw new XunitException("pumpAndSettle timed out");
            }

            Pump(TimeSpan.FromMilliseconds(100));
            count += 1;
        }
        while (Scheduler.HasScheduledFrame || Scheduler.TransientCallbackCount > 0);

        return count;
    }

    /// <summary>Dart's <c>WidgetTester.takeException</c>.</summary>
    public object? TakeException()
    {
        object? exception = _pending?.Exception;
        _pending = null;
        return exception;
    }

    /// <summary>Every element below the root, depth-first in child order.</summary>
    public IReadOnlyList<Element> AllElements()
    {
        var elements = new List<Element>();
        void Visit(Element element)
        {
            elements.Add(element);
            element.VisitChildren(Visit);
        }

        _root.VisitChildren(Visit);
        return elements;
    }

    /// <summary>Dart's <c>find.byType(T)</c>: exact runtime type.</summary>
    public IReadOnlyList<Element> ElementsOfType<TWidget>() where TWidget : Widget
        => AllElements().Where(element => element.Widget.GetType() == typeof(TWidget)).ToList();

    /// <summary>Dart's <c>tester.element(find.byType(T))</c>: exactly one match.</summary>
    public Element ElementOfType<TWidget>() where TWidget : Widget => ElementsOfType<TWidget>().Single();

    /// <summary>Dart's <c>find.byKey(key)</c>.</summary>
    public IReadOnlyList<Element> ElementsWithKey(Key key)
        => AllElements().Where(element => Equals(element.Widget.Key, key)).ToList();

    /// <summary>Dart's <c>find.byWidget(widget)</c>.</summary>
    public Element ElementOfWidget(Widget widget)
        => AllElements().Single(element => ReferenceEquals(element.Widget, widget));

    /// <summary>Dart's <c>find.text(text)</c> over <see cref="Text"/> widgets.</summary>
    public IReadOnlyList<Element> ElementsWithText(string text)
        => AllElements().Where(element => element.Widget is Text { Data: var data } && data == text).ToList();

    /// <summary>Dart's <c>tester.stateList</c>, by state type.</summary>
    public IReadOnlyList<TState> StateList<TState>() where TState : State
        => AllElements().OfType<StatefulElement>().Select(element => element.State).OfType<TState>().ToList();

    /// <summary>Dart's <c>tester.state</c>, by state type: exactly one match.</summary>
    public TState State<TState>() where TState : State => StateList<TState>().Single();

    /// <summary>Dart's <c>tester.getCenter</c>.</summary>
    public Point GetCenter(Element element)
    {
        var box = (RenderBox)element.FindRenderObject()!;
        return box.LocalToGlobal(new Point(box.Size.Width / 2.0, box.Size.Height / 2.0));
    }

    /// <summary>Dart's <c>tester.startGesture</c>: dispatches the down event and returns the pointer.</summary>
    public int StartGesture(Point location)
    {
        int pointer = Interlocked.Increment(ref _nextPointer);
        GestureBinding.Instance.HandlePointerEvent(RenderView, new PointerDownEvent(
            pointer: pointer,
            kind: PointerDeviceKind.Touch,
            position: location,
            buttons: PointerButtons.Primary,
            timestampUtc: DateTime.UtcNow));
        return pointer;
    }

    /// <summary>Dart's <c>TestGesture.up</c>.</summary>
    public void Up(int pointer, Point location)
    {
        GestureBinding.Instance.HandlePointerEvent(RenderView, new PointerUpEvent(
            pointer: pointer,
            kind: PointerDeviceKind.Touch,
            position: location,
            buttons: PointerButtons.None,
            timestampUtc: DateTime.UtcNow));
    }

    /// <summary>Dart's <c>tester.tap</c>: a down and an up at the element's center, no pump.</summary>
    public void Tap(Element element)
    {
        Point center = GetCenter(element);
        int pointer = StartGesture(center);
        Up(pointer, center);
    }

    /// <summary>Flutter's <c>equalsIgnoringHashCodes</c> normalization.</summary>
    public static string IgnoringHashCodes(string value) => Regex.Replace(value, "#[0-9a-fA-F]{5}", "#00000");

    /// <summary>
    /// Ends the test the way <c>TestWidgetsFlutterBinding</c> does: an exception that was reported and
    /// never taken fails the test, then the tree is replaced and unmounted.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        FlutterErrorDetails? leftover = _pending;
        var unexpected = new List<FlutterErrorDetails>(_unexpected);
        try
        {
            try
            {
                PumpWidget(new SizedBox(key: new UniqueKey()));
                _root.UnmountRoot();
                Scheduler.FlushMicrotasks();
            }
            catch (Exception)
            {
                // Teardown of a deliberately broken tree may fail; the test's own assertions already ran.
            }
        }
        finally
        {
            FlutterError.OnError = _previousOnError;
        }

        if (leftover is not null || unexpected.Count > 0)
        {
            IEnumerable<string> messages = new[] { leftover }
                .Concat(unexpected)
                .Where(details => details is not null)
                .Select(details => details!.Exception.ToString() ?? string.Empty);
            throw new XunitException(
                "A test reported exceptions that were never taken with TakeException():\n"
                + string.Join("\n---\n", messages));
        }
    }

    private Widget Wrap(Widget widget) => new View(View, widget);

    private void DrawFrame()
    {
        // Dart's WidgetsBinding.drawFrame: build, the render pipeline, then finalizeTree. A throw
        // skips the rest of the frame and is reported by the scheduler's callback guard.
        _owner.BuildScope(_root);
        PipelineOwner pipeline = RendererBinding.Instance.RootPipelineOwner;
        pipeline.FlushLayout();
        pipeline.FlushCompositingBits();
        pipeline.FlushPaint();
        _owner.FinalizeTree();
    }

    private void HandleError(FlutterErrorDetails details)
    {
        if (_disposed)
        {
            return;
        }

        if (_pending is null)
        {
            _pending = details;
        }
        else
        {
            _unexpected.Add(details);
        }
    }
}

/// <summary>flutter/packages/flutter/test/widgets/test_widgets.dart: <c>FlipWidget</c>.</summary>
internal sealed class FrameworkDartFlipWidget(Widget left, Widget right, Key? key = null) : StatefulWidget(key)
{
    public Widget Left { get; } = left;

    public Widget Right { get; } = right;

    public override State CreateState() => new FrameworkDartFlipWidgetState();
}

/// <summary>flutter/packages/flutter/test/widgets/test_widgets.dart: <c>FlipWidgetState</c>.</summary>
internal sealed class FrameworkDartFlipWidgetState : State<FrameworkDartFlipWidget>
{
    private bool _showLeft = true;

    public void Flip() => SetState(() => _showLeft = !_showLeft);

    public override Widget Build(BuildContext context) => _showLeft ? Widget.Left : Widget.Right;
}

/// <summary>flutter/packages/flutter/test/widgets/test_widgets.dart: the shared constants and helpers.</summary>
internal static class FrameworkDartTestWidgets
{
    public static readonly BoxDecoration BoxDecorationA = new(Color: Avalonia.Media.Color.FromUInt32(0xFFFF0000));

    public static readonly BoxDecoration BoxDecorationB = new(Color: Avalonia.Media.Color.FromUInt32(0xFF00FF00));

    public static readonly BoxDecoration BoxDecorationC = new(Color: Avalonia.Media.Color.FromUInt32(0xFF0000FF));

    /// <summary>Dart's <c>flipStatefulWidget</c>.</summary>
    public static void FlipStatefulWidget(FrameworkDartTester tester)
        => tester.State<FrameworkDartFlipWidgetState>().Flip();
}
