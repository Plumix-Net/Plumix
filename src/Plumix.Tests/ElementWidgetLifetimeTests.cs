using Plumix.Foundation;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/framework.dart

namespace Plumix.Tests;

public sealed class ElementWidgetLifetimeTests
{
    [Fact]
    public void Widget_IsAvailableFromConstructionThroughDeactivation_AndThrowsAfterUnmount()
    {
        var widget = new EmptyWidget();
        var element = new EmptyElement(widget);
        var owner = new BuildOwner();
        BuildContext context = element;

        Assert.True(context.Mounted);
        Assert.Same(widget, context.Widget);

        element.Attach(owner);
        owner.BuildScope(element, () => element.Mount(null, null));
        Assert.True(context.Mounted);
        Assert.Same(widget, context.Widget);

        var replacement = new EmptyWidget();
        element.Update(replacement);
        Assert.Same(replacement, context.Widget);

        element.Deactivate();
        Assert.False(element.IsActive);
        Assert.True(context.Mounted);
        Assert.Same(replacement, context.Widget);

        element.Unmount();
        Assert.False(context.Mounted);
        Assert.Throws<InvalidOperationException>(() => context.Widget);
        Assert.EndsWith("(DEFUNCT)", element.ToStringShort(), StringComparison.Ordinal);

        // Flutter's property diagnostics use nullable storage rather than the throwing getter.
        element.DebugFillProperties(new DiagnosticPropertiesBuilder());
    }

    [Fact]
    public void FailedElement_RetainsItsWidgetUntilUnmount()
    {
        var widget = new EmptyWidget();
        var element = new EmptyElement(widget);
        var owner = new BuildOwner();
        element.Attach(owner);
        owner.BuildScope(element, () => element.Mount(null, null));

        Element.DeactivateFailedSubtreeRecursively(element);

        Assert.Equal(ElementLifecycleState.Failed, element.LifecycleState);
        Assert.True(element.Mounted);
        Assert.Same(widget, element.Widget);

        element.Unmount();
        Assert.False(element.Mounted);
        Assert.Throws<InvalidOperationException>(() => element.Widget);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StatefulElement_ClearsItsWidgetBeforeDispose_WhileStateKeepsTheLatestConfiguration(bool throws)
    {
        var key = new GlobalObjectKey<DisposeState>(new object());
        var original = new DisposeWidget(key);
        var owner = new BuildOwner();
        RootElement root = new RootWidget(original).Attach(owner);
        var element = Assert.IsType<StatefulElement>(root.ChildElement);
        var state = Assert.IsType<DisposeState>(element.State);
        var replacement = new DisposeWidget(key, throws);
        new RootWidget(replacement).Attach(owner, root);
        owner.FlushBuild();
        Assert.Same(element, root.ChildElement);
        Assert.Same(replacement, element.Widget);
        Assert.Equal(1, owner.GlobalKeyCount);

        if (throws)
        {
            Assert.Throws<InvalidOperationException>(root.UnmountRoot);
        }
        else
        {
            root.UnmountRoot();
        }

        Assert.True(state.DisposeCalled);
        Assert.Same(replacement, state.ConfigurationDuringDispose);
        Assert.True(state.WasMountedDuringDispose);
        Assert.False(state.ContextWasMountedDuringDispose);
        Assert.True(state.WidgetReadThrewDuringDispose);
        Assert.False(state.Mounted);
        Assert.False(element.Mounted);
        Assert.Throws<InvalidOperationException>(() => element.Widget);
        Assert.Equal(0, owner.GlobalKeyCount);
    }

    [Fact]
    public void GlobalKey_ReactivationRetainsConfiguration_AndFinalizationReleasesIt()
    {
        var key = new GlobalObjectKey<DisposeState>(new object());
        var widget = new EmptyWidget(key);
        var owner = new BuildOwner();
        RootElement root = new RootWidget(widget).Attach(owner);
        Element element = root.ChildElement!;

        // DeactivateChild parks the element until the end of this build frame.
        root.Update(new RootWidget(new EmptyWidget()));
        Assert.True(element.Mounted);
        Assert.Same(widget, element.Widget);
        var replacement = new EmptyWidget(key);
        root.Update(new RootWidget(replacement));
        Assert.Same(element, root.ChildElement);
        Assert.Same(replacement, element.Widget);
        owner.FlushBuild();
        Assert.True(element.Mounted);

        root.Update(new RootWidget(new EmptyWidget()));
        owner.FlushBuild();
        Assert.False(element.Mounted);
        Assert.Throws<InvalidOperationException>(() => element.Widget);
        Assert.Equal(0, owner.GlobalKeyCount);
        root.UnmountRoot();
    }

    private sealed class EmptyWidget(Key? key = null) : Widget(key)
    {
        public override Element CreateElement() => new EmptyElement(this);
    }

    private sealed class EmptyElement(Widget widget) : Element(widget);

    private sealed class DisposeWidget(GlobalKey key, bool throws = false) : StatefulWidget(key)
    {
        public bool Throws { get; } = throws;

        public override State CreateState() => new DisposeState();
    }

    private sealed class DisposeState : State
    {
        public StatefulWidget? ConfigurationDuringDispose { get; private set; }
        public bool DisposeCalled { get; private set; }
        public bool WasMountedDuringDispose { get; private set; }
        public bool ContextWasMountedDuringDispose { get; private set; }
        public bool WidgetReadThrewDuringDispose { get; private set; }

        public override Widget Build(BuildContext context) => new EmptyWidget();

        public override void Dispose()
        {
            DisposeCalled = true;
            ConfigurationDuringDispose = StateWidget;
            WasMountedDuringDispose = Mounted;
            ContextWasMountedDuringDispose = Context.Mounted;
            try
            {
                _ = Context.Widget;
            }
            catch (InvalidOperationException)
            {
                WidgetReadThrewDuringDispose = true;
            }

            base.Dispose();
            if (((DisposeWidget)StateWidget).Throws)
            {
                throw new InvalidOperationException("dispose failure");
            }
        }
    }
}
