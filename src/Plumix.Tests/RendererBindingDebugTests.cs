using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/binding.dart
// Dart parity source: flutter/packages/flutter/lib/src/rendering/object.dart
// Collector coverage complements Flutter's service_extensions_test.dart dump tests.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class RendererBindingDebugTests : IDisposable
{
    private readonly RenderView[] _previousViews;

    public RendererBindingDebugTests()
    {
        // Host tests may keep reusable views alive. Isolate the ambient registry and restore it
        // afterwards rather than making the dump tests depend on test execution order.
        RendererBinding binding = RendererBinding.Instance;
        _previousViews = binding.RenderViews.ToArray();
        foreach (RenderView view in _previousViews)
        {
            binding.RemoveRenderView(view);
        }
    }

    public void Dispose()
    {
        RendererBinding binding = RendererBinding.Instance;
        foreach (RenderView view in _previousViews)
        {
            binding.AddRenderView(view);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Dumps_EmptyRegistry_PrintTheExactMessageOnce(int kind)
    {
        Assert.Empty(RendererBinding.Instance.RenderViews);

        Assert.Equal("No render tree root was added to the binding.", Capture(() => Dump(kind)));
    }

    [Fact]
    public void RenderDump_PreservesDeepOutputAndDoesNotFlushDirtyTrees()
    {
        var child = new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10)));
        var view = CreateView(1101);
        view.Child = child;
        WithRegisteredViews([view], () =>
        {
            string expected = view.ToStringDeep();

            Assert.Equal(expected, Capture(RenderingDebug.DebugDumpRenderTree));
            Assert.Equal(Constants.KDebugMode, child.DebugNeedsLayout);
            Assert.Equal(Constants.KDebugMode, child.DebugNeedsPaint);
            if (Constants.KDebugMode)
            {
                Assert.StartsWith("RenderView#", expected, StringComparison.Ordinal);
                Assert.Contains("child: RenderConstrainedBox#", expected, StringComparison.Ordinal);
                Assert.EndsWith("\n", expected, StringComparison.Ordinal);
            }
            else
            {
                Assert.Equal(string.Empty, expected);
            }
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void RenderAndFragmentDumps_JoinEveryRootWithoutTrimming(int kind)
    {
        var first = CreateView(1102);
        var second = CreateView(1103);
        WithRegisteredViews([first, second], () =>
        {
            string firstDump = kind == 0 ? first.ToStringDeep() : first.Semantics.ToStringDeep();
            string secondDump = kind == 0 ? second.ToStringDeep() : second.Semantics.ToStringDeep();

            Assert.Equal(firstDump + "\n\n" + secondDump, Capture(() => Dump(kind)));
            if (!Constants.KDebugMode)
            {
                Assert.Equal("\n\n", Capture(() => Dump(kind)));
            }
        });
    }

    [Fact]
    public void LayerDump_ReportsEachUnpreparedView()
    {
        var first = CreateView(1104);
        var second = CreateView(1105);
        WithRegisteredViews([first, second], () =>
        {
            Assert.Equal(
                $"Layer tree unavailable for {first}.\n\nLayer tree unavailable for {second}.",
                Capture(RenderingDebug.DebugDumpLayerTree));
            Assert.Null(first.DebugLayer);
            Assert.Null(second.DebugLayer);
        });
    }

    [Fact]
    public void LayerDump_UsesTheRetainedTreeAlongsideUnavailableViews_InEveryBuildMode()
    {
        var prepared = CreateView(1106);
        var unavailable = CreateView(1107);
        var owner = new PipelineOwner();
        owner.RootNode = prepared;
        try
        {
            WithRegisteredViews([prepared, unavailable], () =>
            {
                prepared.PrepareInitialFrame();
                var layer = Assert.IsAssignableFrom<ContainerLayer>(prepared.DebugLayer);
                layer.Append(new OpacityLayer { Opacity = 0.5 });
                string preparedDump = Constants.KDebugMode
                    ? layer.ToStringDeep()
                    : $"Layer tree unavailable for {prepared}.";

                Assert.Equal(
                    preparedDump + $"\n\nLayer tree unavailable for {unavailable}.",
                    Capture(RenderingDebug.DebugDumpLayerTree));
                if (Constants.KDebugMode)
                {
                    Assert.StartsWith("OffsetLayer#", preparedDump, StringComparison.Ordinal);
                    Assert.Contains("child 1: OpacityLayer#", preparedDump, StringComparison.Ordinal);
                    Assert.EndsWith("\n", preparedDump, StringComparison.Ordinal);
                }
            });
        }
        finally
        {
            owner.RootNode = null;
            owner.Dispose();
        }
    }

    [Fact]
    public void FragmentDump_DoesNotRequireAnEnabledOrBuiltSemanticsTree()
    {
        var view = CreateView(1108);
        Assert.Null(view.Owner);
        Assert.Null(view.SemanticsNode);
        WithRegisteredViews([view], () =>
        {
            string expected = view.Semantics.ToStringDeep();

            Assert.Equal(expected, Capture(RenderingDebug.DebugDumpRenderObjectSemanticsTree));
            if (Constants.KDebugMode)
            {
                Assert.Contains("RenderObjectSemantics", expected, StringComparison.Ordinal);
                Assert.Contains("owner: \"RenderView#", expected, StringComparison.Ordinal);
            }
        });
    }

    [Fact]
    public void Dumps_ObserveRemovalAndReRegistrationInInsertionOrder()
    {
        var first = CreateView(1109);
        var second = CreateView(1110);
        WithRegisteredViews([first, second], () =>
        {
            RendererBinding binding = RendererBinding.Instance;
            binding.RemoveRenderView(first);
            Assert.Equal([second], binding.RenderViews);
            Assert.Equal(second.ToStringDeep(), Capture(RenderingDebug.DebugDumpRenderTree));

            binding.AddRenderView(first);

            Assert.Equal([second, first], binding.RenderViews);
            Assert.Equal(second.ToStringDeep() + "\n\n" + first.ToStringDeep(),
                Capture(RenderingDebug.DebugDumpRenderTree));
            Assert.Equal($"Layer tree unavailable for {second}.\n\nLayer tree unavailable for {first}.",
                Capture(RenderingDebug.DebugDumpLayerTree));
            Assert.Equal(second.Semantics.ToStringDeep() + "\n\n" + first.Semantics.ToStringDeep(),
                Capture(RenderingDebug.DebugDumpRenderObjectSemanticsTree));
        });
    }

    private static RenderView CreateView(int id) => new(new FlutterView(new Size(40, 20), viewId: id));

    private static void Dump(int kind)
    {
        switch (kind)
        {
            case 0:
                RenderingDebug.DebugDumpRenderTree();
                break;
            case 1:
                RenderingDebug.DebugDumpLayerTree();
                break;
            case 2:
                RenderingDebug.DebugDumpRenderObjectSemanticsTree();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static string Capture(Action action)
    {
        var messages = new List<(string? Message, int? WrapWidth)>();
        DebugPrintCallback previous = Print.DebugPrint;
        Print.DebugPrint = (message, wrapWidth) => messages.Add((message, wrapWidth));
        try
        {
            action();
            (string? message, int? wrapWidth) = Assert.Single(messages);
            Assert.Null(wrapWidth);
            return Assert.IsType<string>(message);
        }
        finally
        {
            Print.DebugPrint = previous;
        }
    }

    private static void WithRegisteredViews(RenderView[] views, Action action)
    {
        RendererBinding binding = RendererBinding.Instance;
        Assert.Empty(binding.RenderViews);
        try
        {
            foreach (RenderView view in views)
            {
                binding.AddRenderView(view);
            }

            action();
        }
        finally
        {
            foreach (RenderView view in views)
            {
                if (binding.RenderViews.Contains(view))
                {
                    binding.RemoveRenderView(view);
                }
            }
        }
    }
}
