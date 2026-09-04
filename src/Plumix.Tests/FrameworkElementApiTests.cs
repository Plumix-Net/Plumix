using System.Reflection;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/framework.dart (public API surface)

namespace Plumix.Tests;

/// <summary>
/// Guards the accessibility of the widget/element lifecycle. Dart declares all of it public, so an
/// outside assembly must be able to pair a <see cref="Widget"/> with its own <see cref="Element"/>.
/// <c>Plumix.Tests</c> sees Plumix internals, so these assertions read IL-level accessibility through
/// reflection rather than relying on the compiler.
/// </summary>
public sealed class FrameworkElementApiTests
{
    private const BindingFlags AnyInstance =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private const BindingFlags AnyStatic =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    [Theory]
    [InlineData(nameof(Element.Mount))]
    [InlineData(nameof(Element.Update))]
    [InlineData(nameof(Element.Rebuild))]
    [InlineData(nameof(Element.Unmount))]
    [InlineData(nameof(Element.MarkNeedsBuild))]
    [InlineData(nameof(Element.Reassemble))]
    [InlineData(nameof(Element.DidChangeDependencies))]
    [InlineData(nameof(Element.VisitChildren))]
    [InlineData(nameof(Element.DebugVisitOnstageChildren))]
    [InlineData(nameof(Element.ForgetChild))]
    [InlineData(nameof(Element.UpdateSlot))]
    [InlineData(nameof(Element.UpdateSlotForChild))]
    [InlineData(nameof(Element.AttachRenderObject))]
    [InlineData(nameof(Element.DetachRenderObject))]
    [InlineData(nameof(Element.DeactivateChild))]
    [InlineData(nameof(Element.UnmountChild))]
    [InlineData(nameof(Element.InflateWidget))]
    [InlineData(nameof(Element.UpdateChild))]
    [InlineData(nameof(Element.UpdateChildren))]
    [InlineData(nameof(Element.DependOnInherited))]
    [InlineData(nameof(Element.DependOnInheritedElement))]
    public void ElementLifecycleMethod_IsPublic(string name)
    {
        AssertPublicMethod(typeof(Element), name);
    }

    [Theory]
    [InlineData(nameof(Element.Widget))]
    [InlineData(nameof(Element.Parent))]
    [InlineData(nameof(Element.Depth))]
    [InlineData(nameof(Element.Slot))]
    [InlineData(nameof(Element.Owner))]
    [InlineData(nameof(Element.Dirty))]
    [InlineData(nameof(Element.IsActive))]
    [InlineData(nameof(Element.IsMounted))]
    [InlineData(nameof(Element.RenderObject))]
    [InlineData(nameof(Element.RenderObjectAttachingChild))]
    public void ElementLifecycleProperty_HasPublicGetter(string name)
    {
        PropertyInfo? property = typeof(Element).GetProperty(name, AnyInstance);
        Assert.NotNull(property);
        Assert.True(
            property!.GetMethod?.IsPublic == true,
            $"Element.{name} must expose a public getter for out-of-assembly element authors.");
    }

    [Fact]
    public void ElementDirty_HasProtectedSetter()
    {
        // Dart backs `Element.dirty` with a library-private field that only `performRebuild` clears.
        // C# has no library privacy, so the setter is protected: subclasses clear it, nobody else.
        PropertyInfo property = typeof(Element).GetProperty(nameof(Element.Dirty), AnyInstance)!;
        Assert.NotNull(property.SetMethod);
        Assert.True(property.SetMethod!.IsFamily, "Element.Dirty must have a protected setter.");
    }

    [Theory]
    [InlineData("OnMount")]
    [InlineData("OnActivate")]
    [InlineData("OnDeactivate")]
    [InlineData("OnUnmount")]
    public void ElementLifecycleHook_IsProtectedAndVirtual(string name)
    {
        MethodInfo? method = typeof(Element).GetMethod(name, AnyInstance);
        Assert.NotNull(method);
        Assert.True(method!.IsFamily, $"Element.{name} must be protected so subclasses can extend it.");
        Assert.True(method.IsVirtual, $"Element.{name} must be virtual.");
    }

    [Fact]
    public void WidgetCreateElement_IsPublic()
    {
        AssertPublicMethod(typeof(Widget), nameof(Widget.CreateElement));

        MethodInfo? canUpdate = typeof(Widget).GetMethod(nameof(Widget.CanUpdate), AnyStatic);
        Assert.NotNull(canUpdate);
        Assert.True(canUpdate!.IsPublic, "Widget.CanUpdate must be public.");
    }

    [Fact]
    public void BuildContextConstructor_IsPublic()
    {
        ConstructorInfo? constructor = typeof(BuildContext).GetConstructor(
            AnyInstance,
            binder: null,
            [typeof(Element)],
            modifiers: null);

        Assert.NotNull(constructor);
        Assert.True(
            constructor!.IsPublic,
            "A hand-written element must be able to build a BuildContext over itself.");
    }

    // `BuildScope` is covered by BuildScopeAcceptsAnExternalContext: only the Dart-shaped
    // `BuildScope(Element, Action?)` overload is public, the parameterless Plumix one stays internal.
    [Theory]
    [InlineData(nameof(BuildOwner.FinalizeTree))]
    [InlineData(nameof(BuildOwner.ScheduleBuild))]
    [InlineData(nameof(BuildOwner.Reassemble))]
    [InlineData(nameof(BuildOwner.RegisterElement))]
    [InlineData(nameof(BuildOwner.UnregisterElement))]
    public void BuildOwnerMethod_IsPublic(string name)
    {
        AssertPublicMethod(typeof(BuildOwner), name);
    }

    [Theory]
    [InlineData(nameof(BuildOwner.GlobalKeyCount))]
    [InlineData(nameof(BuildOwner.IsBuilding))]
    [InlineData(nameof(BuildOwner.OnBuildScheduled))]
    public void BuildOwnerProperty_IsPublic(string name)
    {
        PropertyInfo? property = typeof(BuildOwner).GetProperty(name, AnyInstance);
        Assert.NotNull(property);
        Assert.True(property!.GetMethod?.IsPublic == true, $"BuildOwner.{name} must be public.");
    }

    [Theory]
    [InlineData(nameof(InheritedElement.UpdateDependencies))]
    [InlineData(nameof(InheritedElement.RemoveDependent))]
    [InlineData(nameof(InheritedElement.NotifyDependent))]
    public void InheritedElementDependencyHook_IsPublic(string name)
    {
        AssertPublicMethod(typeof(InheritedElement), name);
    }

    [Fact]
    public void BuildScopeAcceptsAnExternalContext()
    {
        // `BuildOwner.BuildScope(Element, Action)` is the entry point a lazily-building element needs;
        // Dart's `buildScope` is public for the same reason.
        MethodInfo method = typeof(BuildOwner).GetMethod(
            nameof(BuildOwner.BuildScope),
            AnyInstance,
            binder: null,
            [typeof(Element), typeof(Action)],
            modifiers: null)!;

        Assert.True(method.IsPublic);
    }

    [Fact]
    public void HandWrittenElement_RunsTheFullLifecycle()
    {
        var log = new ProbeLog();
        var owner = new BuildOwner();
        var root = new ProbeRootElement(new LifecycleProbe(log, new SizedBox(width: 10, height: 10)));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal(["mount", "rebuild"], log.Events);
        Assert.Equal(1, log.ChildBuilds);

        log.Events.Clear();
        root.Update(new LifecycleProbe(log, new SizedBox(width: 20, height: 20)));
        owner.FlushBuild();

        Assert.Equal(["update", "rebuild"], log.Events);
        Assert.Equal(2, log.ChildBuilds);

        log.Events.Clear();
        root.Unmount();

        Assert.Contains("deactivate", log.Events);
        Assert.Contains("unmount", log.Events);
        Assert.True(
            log.Events.IndexOf("deactivate") < log.Events.IndexOf("unmount"),
            "A hand-written element must be deactivated before it is unmounted.");
    }

    [Fact]
    public void HandWrittenElement_IsReinflatedWhenItsKeyChanges()
    {
        var log = new ProbeLog();
        var owner = new BuildOwner();
        var root = new ProbeRootElement(
            new LifecycleProbe(log, new SizedBox(width: 10, height: 10), new ValueKey<int>(1)));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        log.Events.Clear();
        root.Update(new LifecycleProbe(log, new SizedBox(width: 10, height: 10), new ValueKey<int>(2)));
        owner.FlushBuild();

        // A different key cannot update the old element, so the old one is deactivated and a new one mounts.
        Assert.Contains("deactivate", log.Events);
        Assert.Contains("mount", log.Events);
        Assert.DoesNotContain("update", log.Events);
    }

    private static void AssertPublicMethod(Type type, string name)
    {
        MethodInfo[] methods = type
            .GetMethods(AnyInstance | BindingFlags.Static)
            .Where(candidate => candidate.Name == name)
            .ToArray();

        Assert.NotEmpty(methods);
        foreach (MethodInfo method in methods)
        {
            Assert.True(method.IsPublic, $"{type.Name}.{name} must be public.");
        }
    }

    private sealed class ProbeLog
    {
        public List<string> Events { get; } = [];

        public int ChildBuilds { get; set; }
    }

    private sealed class LifecycleProbe : Widget
    {
        public LifecycleProbe(ProbeLog log, Widget child, Key? key = null) : base(key)
        {
            Log = log;
            Child = child;
        }

        public ProbeLog Log { get; }

        public Widget Child { get; }

        public override Element CreateElement() => new LifecycleProbeElement(this);
    }

    private sealed class LifecycleProbeElement : Element
    {
        private Element? _child;

        public LifecycleProbeElement(LifecycleProbe widget) : base(widget)
        {
        }

        public override RenderObject? RenderObject => _child?.RenderObject;

        public override Element? RenderObjectAttachingChild => _child;

        private LifecycleProbe Probe => (LifecycleProbe)Widget;

        protected override void OnMount()
        {
            base.OnMount();
            Probe.Log.Events.Add("mount");
            Rebuild();
        }

        public override void Rebuild()
        {
            Dirty = false;
            Probe.Log.Events.Add("rebuild");
            Probe.Log.ChildBuilds += 1;
            _child = UpdateChild(_child, Probe.Child, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Probe.Log.Events.Add("update");
            Rebuild();
        }

        protected override void OnDeactivate()
        {
            Probe.Log.Events.Add("deactivate");
            base.OnDeactivate();
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(child, _child))
            {
                _child = null;
            }
        }

        public override void Unmount()
        {
            if (_child != null)
            {
                UnmountChild(_child);
                _child = null;
            }

            Probe.Log.Events.Add("unmount");
            base.Unmount();
        }
    }

    private sealed class ProbeRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public ProbeRootElement(Widget widget) : base(widget)
        {
        }

        public override RenderObject? RenderObject => _child?.RenderObject;

        public override Element? RenderObjectAttachingChild => _child;

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        public override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
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

        public override void Unmount()
        {
            if (_child != null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
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
