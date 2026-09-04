using System.Reflection;
using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart
// (RenderObjectWidget.createRenderObject/updateRenderObject/didUnmountRenderObject are public in
// Dart, so a render-object widget can be authored from outside the framework library.)

namespace Plumix.Tests;

public sealed class RenderObjectWidgetApiTests
{
    private static readonly Size Surface = new(800, 600);

    [Fact]
    public void RenderObjectWidget_ExposesItsRenderObjectHooksPublicly()
    {
        Type widget = typeof(RenderObjectWidget);

        MethodInfo? create = widget.GetMethod(nameof(RenderObjectWidget.CreateRenderObject));
        MethodInfo? update = widget.GetMethod(nameof(RenderObjectWidget.UpdateRenderObject));
        MethodInfo? didUnmount = widget.GetMethod(nameof(RenderObjectWidget.DidUnmountRenderObject));

        Assert.NotNull(create);
        Assert.NotNull(update);
        Assert.NotNull(didUnmount);
        Assert.True(create.IsPublic);
        Assert.True(update.IsPublic);
        Assert.True(didUnmount.IsPublic);
    }

    /// <summary>
    /// The three hooks are what an outside assembly overrides, so no shipped override may narrow
    /// them back to <c>internal</c>: a single narrowed override compiles inside Plumix but breaks
    /// the type for anyone subclassing it from another assembly.
    /// </summary>
    [Fact]
    public void EveryShippedRenderObjectWidget_KeepsThoseHooksPublic()
    {
        Assembly[] assemblies =
        [
            typeof(RenderObjectWidget).Assembly,
            typeof(Plumix.Material.MaterialApp).Assembly,
            typeof(Plumix.Cupertino.CupertinoApp).Assembly,
        ];

        var narrowed = new List<string>();
        foreach (Assembly assembly in assemblies)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (!type.IsSubclassOf(typeof(RenderObjectWidget)))
                {
                    continue;
                }

                foreach (string name in (string[])
                    [
                        nameof(RenderObjectWidget.CreateRenderObject),
                        nameof(RenderObjectWidget.UpdateRenderObject),
                        nameof(RenderObjectWidget.DidUnmountRenderObject),
                    ])
                {
                    MethodInfo? declared = type.GetMethod(
                        name,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                            | BindingFlags.DeclaredOnly);
                    if (declared is not null && !declared.IsPublic)
                    {
                        narrowed.Add($"{type.FullName}.{name}");
                    }
                }
            }
        }

        Assert.Empty(narrowed);
    }

    [Fact]
    public void AnExternallyShapedRenderObjectWidget_RunsTheFullRenderObjectLifecycle()
    {
        var log = new List<string>();
        using var harness = new TwoDimensionalRenderHarness(new PublicApiPaddingWidget(log, 4.0));

        harness.Pump(Surface);

        Assert.Equal(["create"], log);
        RenderPublicApiPadding renderObject = FindRenderObject(harness);
        Assert.Equal(4.0, renderObject.Inset);
        Assert.Equal(Surface, renderObject.Size);

        harness.Replace(new PublicApiPaddingWidget(log, 12.0));
        harness.Pump(Surface);

        Assert.Equal(["create", "update"], log);
        Assert.Same(renderObject, FindRenderObject(harness));
        Assert.Equal(12.0, renderObject.Inset);

        harness.Dispose();

        Assert.Equal(["create", "update", "didUnmount"], log);
        Assert.True(renderObject.DebugDisposed);
    }

    private static RenderPublicApiPadding FindRenderObject(TwoDimensionalRenderHarness harness)
    {
        RenderObject? child = harness.RenderView.Child;
        return Assert.IsType<RenderPublicApiPadding>(child);
    }
}

/// <summary>
/// Written the way an assembly outside Plumix has to write it: every render-object hook is a
/// <c>public override</c>, and nothing internal to the framework is referenced.
/// </summary>
internal sealed class PublicApiPaddingWidget : SingleChildRenderObjectWidget
{
    private readonly List<string> _log;

    public PublicApiPaddingWidget(List<string> log, double inset, Widget? child = null)
        : base(child)
    {
        _log = log;
        Inset = inset;
    }

    public double Inset { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        _log.Add("create");
        return new RenderPublicApiPadding { Inset = Inset };
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        _log.Add("update");
        ((RenderPublicApiPadding)renderObject).Inset = Inset;
    }

    public override void DidUnmountRenderObject(RenderObject renderObject)
    {
        _log.Add("didUnmount");
    }
}

internal sealed class RenderPublicApiPadding : RenderBox, IRenderObjectSingleChildContainer
{
    private double _inset;

    public double Inset
    {
        get => _inset;
        set
        {
            if (_inset == value)
            {
                return;
            }

            _inset = value;
            MarkNeedsLayout();
        }
    }

    public RenderObject? Child { get; set; }

    protected override void PerformLayout()
    {
        Size = Constraints.Biggest;
    }

    public override void Paint(PaintingContext context, Point offset)
    {
    }
}
