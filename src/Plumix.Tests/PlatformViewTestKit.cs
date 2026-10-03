using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

/// <summary>The shapes platform_view_test.dart repeats in almost every test.</summary>
internal static class PlatformViewTestKit
{
    /// <summary><c>Center(child: SizedBox(width: w, height: h, child: child))</c>.</summary>
    public static Widget Centered(double width, double height, Widget? child) =>
        new Center(child: new SizedBox(width: width, height: height, child: child));

    /// <summary>
    /// <c>Align(alignment: Alignment.topLeft, child: SizedBox(width: w, height: h, child: child))</c>.
    /// </summary>
    public static Widget TopLeft(double width, double height, Widget child) =>
        new Align(alignment: Alignment.TopLeft, child: new SizedBox(width: width, height: height, child: child));

    /// <summary>The hit-test-behavior harness: a full-screen opaque <see cref="Listener"/> under the view.</summary>
    public static Widget ParentListenerStack(Widget view, Action onParentDown) =>
        new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Stack(
                children:
                [
                    new Listener(
                        behavior: Plumix.Rendering.HitTestBehavior.Opaque,
                        onPointerDown: _ => onParentDown()),
                    new Positioned(child: new SizedBox(width: 200.0, height: 100.0, child: view)),
                ]));

    public static IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> Recognizers(
        params IFactory<OneSequenceGestureRecognizer>[] factories) =>
        new HashSet<IFactory<OneSequenceGestureRecognizer>>(factories);

    public static IReadOnlySet<IFactory<OneSequenceGestureRecognizer>> NoRecognizers() =>
        new HashSet<IFactory<OneSequenceGestureRecognizer>>();

    /// <summary>flutter_test's <c>tester.layers</c>: the layer tree in pre-order.</summary>
    public static List<Layer> Layers(FrameworkDartTester tester)
    {
        var layers = new List<Layer>();
        void Visit(Layer layer)
        {
            layers.Add(layer);
            if (layer is ContainerLayer container)
            {
                foreach (Layer child in container.Children)
                {
                    Visit(child);
                }
            }
        }

        Visit(tester.RenderView.DebugLayer!);
        return layers;
    }

    /// <summary>flutter_test's <c>tester.tapAt</c>.</summary>
    public static void TapAt(FrameworkDartTester tester, Point location)
    {
        TestGesture gesture = tester.StartGesture(location, PointerDeviceKind.Touch);
        gesture.Up();
    }

    /// <summary>flutter_test's <c>tester.longPressAt</c>: down, wait out the long-press timeout, up.</summary>
    public static void LongPressAt(FrameworkDartTester tester, Point location)
    {
        TestGesture gesture = tester.StartGesture(location, PointerDeviceKind.Touch);
        tester.Pump(TimeSpan.FromSeconds(1));
        gesture.Up();
    }

    /// <summary>The focus node of the <see cref="Focus"/> below the first <typeparamref name="TWidget"/>.</summary>
    public static FocusNode FocusNodeBelow<TWidget>(FrameworkDartTester tester) where TWidget : Widget
    {
        Element root = tester.ElementOfType<TWidget>();
        Focus? focus = null;
        void Visit(Element element)
        {
            if (focus is not null)
            {
                return;
            }

            if (element.Widget is Focus found)
            {
                focus = found;
                return;
            }

            element.VisitChildren(Visit);
        }

        root.VisitChildren(Visit);
        return focus!.FocusNode!;
    }

    /// <summary>The render object of the only <typeparamref name="TWidget"/>.</summary>
    public static TRender RenderOf<TWidget, TRender>(FrameworkDartTester tester)
        where TWidget : Widget
        where TRender : RenderObject =>
        (TRender)tester.ElementOfType<TWidget>().FindRenderObject()!;

    /// <summary>Records every call on <see cref="SystemChannels.TextInput"/> and answers through
    /// <paramref name="respond"/>.</summary>
    public static MockMethodCallHandler MockTextInput(Func<MethodCall, object?>? respond = null) =>
        new(SystemChannels.TextInput, respond);

    /// <summary>The arguments of the last <c>TextInput.setPlatformViewClient</c> call.</summary>
    public static System.Collections.IDictionary? LastSetPlatformViewClient(MockMethodCallHandler handler) =>
        handler.Log.LastOrDefault(call => call.Method == "TextInput.setPlatformViewClient")?.Arguments
            as System.Collections.IDictionary;

    /// <summary>The Dart tests' <c>FlutterError.onError = errors.add</c>, restored on dispose.</summary>
    public sealed class ErrorCapture : IDisposable
    {
        private readonly FlutterExceptionHandler? _previous = FlutterError.OnError;

        public ErrorCapture()
        {
            FlutterError.OnError = Errors.Add;
        }

        public List<FlutterErrorDetails> Errors { get; } = [];

        public void Dispose() => FlutterError.OnError = _previous;
    }

    /// <summary>A recognizer factory counting its invocations (the tests' <c>constructRecognizer</c>).</summary>
    public sealed class CountingEagerFactory
    {
        public int Invocations { get; private set; }

        public EagerGestureRecognizer Construct()
        {
            Invocations++;
            return new EagerGestureRecognizer();
        }
    }

    /// <summary>Asserts that the semantics node of <paramref name="renderObject"/> matches Dart's
    /// expectations for a platform view at the bottom right of the 800x600 view.</summary>
    public static void AssertPlatformViewSemantics(RenderObject renderObject, int? platformViewId)
    {
        SemanticsNode node = renderObject.DebugSemantics!;
        Assert.Equal(platformViewId, node.PlatformViewId);
        Assert.Equal(new Rect(0, 0, 200, 100), node.Rect);
        Assert.Equal(Matrix4.TranslationValues(600, 500, 0), node.Transform);
        Assert.Empty(node.Children);
    }
}
