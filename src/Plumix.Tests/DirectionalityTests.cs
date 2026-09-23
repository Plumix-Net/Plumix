using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/test/widgets/directionality_test.dart

namespace Plumix.Tests;

public sealed class DirectionalityTests
{
    [Fact]
    public void Directionality_NotifiesDependentsOnlyWhenDirectionChanges()
    {
        var log = new List<TextDirection>();
        var dependent = new Builder(context =>
        {
            log.Add(Directionality.Of(context));
            return new SizedBox();
        });
        using var harness = new WidgetHarness(new Directionality(TextDirection.Ltr, dependent));

        Assert.Equal([TextDirection.Ltr], log);
        harness.Update(new Directionality(TextDirection.Ltr, dependent));
        Assert.Equal([TextDirection.Ltr], log);
        harness.Update(new Directionality(TextDirection.Rtl, dependent));
        Assert.Equal([TextDirection.Ltr, TextDirection.Rtl], log);
        harness.Update(new Directionality(TextDirection.Rtl, dependent));
        Assert.Equal([TextDirection.Ltr, TextDirection.Rtl], log);
        harness.Update(new Directionality(TextDirection.Ltr, dependent));
        Assert.Equal([TextDirection.Ltr, TextDirection.Rtl, TextDirection.Ltr], log);
    }

    [Fact]
    public void MaybeOf_ReturnsNullOutsideAndDirectionInside()
    {
        BuildContext? outerContext = null;
        BuildContext? innerContext = null;
        using var harness = new WidgetHarness(new Builder(context =>
        {
            outerContext = context;
            return new Directionality(TextDirection.Rtl, new Builder(inner =>
            {
                innerContext = inner;
                return new SizedBox();
            }));
        }));

        Assert.Null(Directionality.MaybeOf(outerContext!));
        Assert.Equal(TextDirection.Rtl, Directionality.MaybeOf(innerContext!));
        Assert.Equal(TextDirection.Rtl, Directionality.Of(innerContext!));
    }

    [DebugOnlyFact]
    public void Of_WithoutAncestorReportsFlutterDiagnostic()
    {
        BuildContext? context = null;
        using var harness = new WidgetHarness(new Builder(builderContext =>
        {
            context = builderContext;
            return new SizedBox();
        }));

        var error = Assert.Throws<FlutterError>(() => Directionality.Of(context!));
        Assert.Contains("No Directionality widget found.", error.Message);
        Assert.Contains("Builder widgets require a Directionality widget ancestor", error.Message);
        Assert.Contains("The ownership chain for the affected widget is", error.Message);
    }

    [DebugOnlyFact]
    public void DebugCheckHasDirectionality_OrdersOptionalDiagnostics()
    {
        BuildContext? context = null;
        using var harness = new WidgetHarness(new Builder(builderContext =>
        {
            context = builderContext;
            return new SizedBox();
        }));

        var error = Assert.Throws<FlutterError>(() => WidgetsDebug.DebugCheckHasDirectionality(
            context!, why: "to resolve alignment", hint: "Supply a direction.",
            alternative: "Set an explicit text direction."));
        string message = error.Message;
        int summary = message.IndexOf("No Directionality widget found.", StringComparison.Ordinal);
        int reason = message.IndexOf("to resolve alignment", StringComparison.Ordinal);
        int hint = message.IndexOf("Supply a direction.", StringComparison.Ordinal);
        int widget = message.IndexOf("The specific widget", StringComparison.Ordinal);
        int guidance = message.IndexOf("Typically, the Directionality widget", StringComparison.Ordinal);
        int alternative = message.IndexOf("Set an explicit text direction.", StringComparison.Ordinal);
        Assert.True(summary >= 0 && summary < reason && reason < hint && hint < widget
            && widget < guidance && guidance < alternative);
    }

    private sealed class WidgetHarness : IDisposable
    {
        private readonly BuildOwner _owner = TestBuildOwner.Create();
        private readonly RootElement _root;

        public WidgetHarness(Widget widget)
        {
            _root = new RootElement(widget);
            _root.Attach(_owner);
            _owner.BuildScope(_root, () => _root.Mount(parent: null, newSlot: null));
            _owner.FlushBuild();
        }

        public void Update(Widget widget)
        {
            _root.Update(widget);
            _owner.FlushBuild();
        }

        public void Dispose() => _root.UnmountRoot();

        private sealed class RootElement : Element, IRenderObjectHost
        {
            private Element? _child;

            public RootElement(Widget widget) : base(widget)
            {
            }

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            protected override void PerformRebuild()
            {
                base.PerformRebuild();
                _child = UpdateChild(_child, Widget, Slot);
            }

            public override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Owner!.BuildScope(this, () => Rebuild(force: true));
            }

            public override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
            }
        }
    }
}
