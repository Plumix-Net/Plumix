using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Material;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialMenuAcceleratorTests : IDisposable
{
    public MaterialMenuAcceleratorTests()
    {
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
    }

    public void Dispose()
    {
        FocusManager.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Theory]
    [InlineData("Plain String", "Plain String", -1)]
    [InlineData("&Simple Accelerator", "Simple Accelerator", 0)]
    [InlineData("&Multiple &Accelerators", "Multiple Accelerators", 0)]
    [InlineData("Whitespace & Accelerators", "Whitespace  Accelerators", -1)]
    [InlineData("&Quoted && Ampersand", "Quoted & Ampersand", 0)]
    [InlineData("Ampersand at End &", "Ampersand at End ", -1)]
    [InlineData("&&Multiple Ampersands &&& &&&A &&&&B &&&&", "&Multiple Ampersands & &A &&B &&", 24)]
    [InlineData("Bohrium 𨨏 Code point U+28A0F", "Bohrium 𨨏 Code point U+28A0F", -1)]
    public void StripAcceleratorMarkers_MatchesFlutterRules(
        string source,
        string expectedLabel,
        int expectedIndex)
    {
        int acceleratorIndex = int.MinValue;

        string displayLabel = MenuAcceleratorLabel.StripAcceleratorMarkers(
            source,
            index => acceleratorIndex = index);

        Assert.Equal(expectedLabel, displayLabel);
        Assert.Equal(expectedIndex, acceleratorIndex);
        var label = new MenuAcceleratorLabel(source);
        Assert.Equal(expectedLabel, label.DisplayLabel);
        Assert.Equal(expectedIndex >= 0, label.HasAccelerator);
    }

    [Fact]
    public void StripAcceleratorMarkers_UsesGraphemeClusterIndexes()
    {
        int acceleratorIndex = -1;

        string displayLabel = MenuAcceleratorLabel.StripAcceleratorMarkers(
            "A&👨‍👩‍👧‍👦B",
            index => acceleratorIndex = index);

        Assert.Equal("A👨‍👩‍👧‍👦B", displayLabel);
        Assert.Equal(1, acceleratorIndex);
    }

    [Fact]
    public void CallbackBinding_ProvidesDependentLookupAndValidatesRequiredAncestor()
    {
        MenuAcceleratorCallbackBinding? captured = null;
        Action callback = () => { };
        using var harness = new WidgetRenderHarness(Wrap(
            new MenuAcceleratorCallbackBinding(
                child: new Builder(context =>
                {
                    captured = MenuAcceleratorCallbackBinding.Of(context);
                    return new SizedBox();
                }),
                onInvoke: callback,
                hasSubmenu: true)));

        harness.Pump(new Size(300, 100));

        Assert.NotNull(captured);
        Assert.Same(callback, captured!.OnInvoke);
        Assert.True(captured.HasSubmenu);

        using var missing = new WidgetRenderHarness(Wrap(
            new Builder(context =>
            {
                Assert.Null(MenuAcceleratorCallbackBinding.MaybeOf(context));
                Assert.Throws<InvalidOperationException>(() => MenuAcceleratorCallbackBinding.Of(context));
                return new SizedBox();
            })));
        missing.Pump(new Size(300, 100));
    }

    [Fact]
    public void MenuItemButton_RegistersAltAcceleratorAndUsesUnderlinedDefaultBuilder()
    {
        int invoked = 0;
        using var harness = new WidgetRenderHarness(Wrap(
            new MenuItemButton(
                child: new MenuAcceleratorLabel("&Open"),
                onPressed: () => invoked++)));
        harness.Pump(new Size(300, 100));
        Assert.NotNull(FindParagraph(harness.RenderView, "Open"));

        Assert.False(FocusManager.Instance.HandleKeyEvent(
            KeySim.Down(LogicalKeyboardKey.AltLeft, alt: true)));
        harness.Pump(new Size(300, 100));

        // The default builder composes one RichText paragraph whose accelerator run
        // carries the underline, rather than baseline-aligned sibling paragraphs.
        RenderParagraph accelerator = Assert.Single(
            FindDescendants<RenderParagraph>(harness.RenderView),
            paragraph => paragraph.PlainText == "Open");
        var underlined = new List<TextSpan>();
        accelerator.Text.VisitChildren(span =>
        {
            if (span is TextSpan textSpan && textSpan.Style?.Decoration == Plumix.UI.TextDecoration.Underline)
            {
                underlined.Add(textSpan);
            }

            return true;
        });
        Assert.Equal("O", Assert.Single(underlined).Text);
        Assert.True(FocusManager.Instance.HandleKeyEvent(
            KeySim.Down(LogicalKeyboardKey.KeyO, alt: true, character: "o")));
        Scheduler.PumpFrameForTests();
        Assert.Equal(1, invoked);

        Assert.False(FocusManager.Instance.HandleKeyEvent(KeySim.Up(LogicalKeyboardKey.AltLeft)));
        harness.Pump(new Size(300, 100));
        Assert.NotNull(FindParagraph(harness.RenderView, "Open"));
    }

    [Fact]
    public void CustomBuilder_ReceivesStrippedLabelAndVisibleAcceleratorIndex()
    {
        var builds = new List<(string Label, int Index)>();
        using var harness = new WidgetRenderHarness(Wrap(
            new MenuAcceleratorCallbackBinding(
                child: new MenuAcceleratorLabel(
                    "E&xit",
                    (_, label, index) =>
                    {
                        builds.Add((label, index));
                        return new Text(label);
                    }),
                onInvoke: () => { })));
        harness.Pump(new Size(300, 100));
        Assert.Equal(("Exit", -1), builds[^1]);

        FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.AltRight, alt: true));
        harness.Pump(new Size(300, 100));
        Assert.Equal(("Exit", 1), builds[^1]);

        FocusManager.Instance.HandleKeyEvent(KeySim.Up(LogicalKeyboardKey.AltRight));
        harness.Pump(new Size(300, 100));
        Assert.Equal(("Exit", -1), builds[^1]);
    }

    [Fact]
    public void DefaultBuilder_AppliesAmbientStyleToEveryRichTextRun()
    {
        TextStyle style = new(FontSize: 23, Color: Colors.DarkSlateBlue);
        using var harness = new WidgetRenderHarness(Wrap(
            new DefaultTextStyle(
                style,
                new MenuAcceleratorCallbackBinding(
                    child: new MenuAcceleratorLabel("E&xit"),
                    onInvoke: () => { }))));
        harness.Pump(new Size(300, 100));

        FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.AltLeft, alt: true));
        harness.Pump(new Size(300, 100));

        RenderParagraph paragraph = Assert.Single(
            FindDescendants<RenderParagraph>(harness.RenderView),
            candidate => candidate.PlainText == "Exit");
        var runs = new List<TextSpan>();
        paragraph.Text.VisitChildren(span =>
        {
            if (span is TextSpan { Text: not null } run)
            {
                runs.Add(run);
            }

            return true;
        });
        Assert.Equal(3, runs.Count);
        Assert.All(runs, run => Assert.Equal(23.0, run.Style?.FontSize));
        Assert.All(runs, run => Assert.Equal(Colors.DarkSlateBlue, run.Style?.Color));
        Assert.Equal(
            Plumix.UI.TextDecoration.Underline,
            Assert.Single(runs, run => run.Text == "x").Style?.Decoration);
    }

    [Fact]
    public void LabelMountedWhileAltIsHeld_ShowsItsAcceleratorImmediately()
    {
        int lastIndex = int.MinValue;
        FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.AltLeft, alt: true));

        using var harness = new WidgetRenderHarness(Wrap(
            new MenuAcceleratorCallbackBinding(
                child: new MenuAcceleratorLabel(
                    "&Open",
                    (_, label, index) =>
                    {
                        lastIndex = index;
                        return new Text(label);
                    }),
                onInvoke: () => { })));
        harness.Pump(new Size(300, 100));

        Assert.Equal(0, lastIndex);
    }

    [Fact]
    public void FocusLocalShortcut_TakesPrecedenceOverMenuAccelerator()
    {
        int menuInvocations = 0;
        int localInvocations = 0;
        using var harness = new WidgetRenderHarness(Wrap(
            new CallbackShortcuts(
                bindings: new Dictionary<ShortcutActivator, Action>
                {
                    [new CharacterActivator("o", alt: true)] = () => localInvocations++,
                },
                child: new Focus(
                    autofocus: true,
                    child: new MenuAcceleratorCallbackBinding(
                        new MenuAcceleratorLabel("&Open"),
                        () => menuInvocations++))),
            autofocus: false));
        harness.Pump(new Size(300, 100));

        FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.AltLeft, alt: true));
        harness.Pump(new Size(300, 100));
        Assert.True(FocusManager.Instance.HandleKeyEvent(
            KeySim.Down(LogicalKeyboardKey.KeyO, alt: true, character: "o")));

        Assert.Equal(1, localInvocations);
        Assert.Equal(0, menuInvocations);
    }

    [Fact]
    public void IdenticalChildAccelerator_ReplacesOpenSubmenuRegistration()
    {
        var controller = new MenuController();
        int selected = 0;
        bool childHasRegistry = false;
        int childAcceleratorIndex = -1;
        ShortcutRegistry? childRegistry = null;
        using var harness = new WidgetRenderHarness(Wrap(
            new MenuBar(
                children:
                [
                    new SubmenuButton(
                        menuChildren:
                        [
                            new MenuItemButton(
                                child: new MenuAcceleratorLabel(
                                    "&File child",
                                    (context, label, index) =>
                                    {
                                        childRegistry = ShortcutRegistry.MaybeOf(context);
                                        childHasRegistry = childRegistry is not null;
                                        childAcceleratorIndex = index;
                                        return new Text(label);
                                    }),
                                onPressed: () => selected++),
                        ],
                        child: new MenuAcceleratorLabel("&File"),
                        controller: controller),
                ])));
        harness.Pump(new Size(500, 240));

        FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.AltLeft, alt: true));
        harness.Pump(new Size(500, 240));
        Assert.True(FocusManager.Instance.HandleKeyEvent(
            KeySim.Down(LogicalKeyboardKey.KeyF, alt: true, character: "f")));
        Assert.False(FocusManager.Instance.HandleKeyEvent(
            KeySim.Up(LogicalKeyboardKey.KeyF, alt: true)));
        harness.Pump(new Size(500, 240));
        Assert.True(controller.IsOpen);
        harness.Pump(new Size(500, 240));
        harness.Pump(new Size(500, 240));
        Assert.True(childHasRegistry);
        Assert.Equal(0, childAcceleratorIndex);
        Intent registeredIntent = Assert.Single(childRegistry!.Shortcuts).Value;
        var callbackIntent = Assert.IsType<VoidCallbackIntent>(registeredIntent);
        Assert.Contains("HandleSelect", callbackIntent.Callback.Method.Name, StringComparison.OrdinalIgnoreCase);

        callbackIntent.Callback();
        harness.Pump(new Size(500, 240));
        Assert.Equal(1, selected);
        Assert.False(controller.IsOpen);
    }

    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    public void CupertinoPlatforms_StripMarkersWithoutShowingOrInvokingAccelerators(
        TargetPlatform platform)
    {
        int lastIndex = int.MinValue;
        int invoked = 0;
        using var harness = new WidgetRenderHarness(Wrap(
            new MenuAcceleratorCallbackBinding(
                child: new MenuAcceleratorLabel(
                    "&Open",
                    (_, label, index) =>
                    {
                        lastIndex = index;
                        return new Text(label);
                    }),
                onInvoke: () => invoked++),
            platform));
        harness.Pump(new Size(300, 100));

        FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.AltLeft, alt: true));
        harness.Pump(new Size(300, 100));
        Assert.False(FocusManager.Instance.HandleKeyEvent(
            KeySim.Down(LogicalKeyboardKey.KeyO, alt: true, character: "o")));

        Assert.Equal(-1, lastIndex);
        Assert.Equal(0, invoked);
        Assert.NotNull(FindParagraph(harness.RenderView, "Open"));
    }

    [Fact]
    public void MissingShortcutRegistry_ShowsUnderlineWithoutInvoking()
    {
        int invoked = 0;
        using var harness = new WidgetRenderHarness(WrapWithoutShortcutRegistrar(
            new MenuAcceleratorCallbackBinding(
                child: new MenuAcceleratorLabel("&Open"),
                onInvoke: () => invoked++)));
        harness.Pump(new Size(300, 100));

        FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.AltLeft, alt: true));
        harness.Pump(new Size(300, 100));

        RenderParagraph paragraph = Assert.Single(
            FindDescendants<RenderParagraph>(harness.RenderView),
            candidate => candidate.PlainText == "Open");
        bool hasUnderline = false;
        paragraph.Text.VisitChildren(span =>
        {
            hasUnderline |= span is TextSpan textSpan
                            && textSpan.Style?.Decoration == Plumix.UI.TextDecoration.Underline;
            return true;
        });
        Assert.True(hasUnderline);
        Assert.False(FocusManager.Instance.HandleKeyEvent(
            KeySim.Down(LogicalKeyboardKey.KeyO, alt: true, character: "o")));
        Assert.Equal(0, invoked);
    }

    [Fact]
    public void ZeroArea_DoesNotCrashOrExpandLabel()
    {
        using var harness = new WidgetRenderHarness(Wrap(
            new Center(
                child: new SizedBox(
                    width: 0,
                    height: 0,
                    child: new MenuAcceleratorLabel("X")))));

        harness.Pump(new Size(300, 100));

        RenderParagraph paragraph = Assert.Single(
            FindDescendants<RenderParagraph>(harness.RenderView),
            candidate => candidate.PlainText == "X");
        Assert.Equal(new Size(0, 0), paragraph.Size);
    }

    private static Widget Wrap(
        Widget child,
        TargetPlatform platform = TargetPlatform.Windows,
        bool autofocus = true)
    {
        Widget themed = WrapWithoutShortcutRegistrar(child, platform, autofocus);
        return new Actions(
            actions: new Dictionary<Type, FlutterAction>
            {
                [typeof(VoidCallbackIntent)] = new VoidCallbackAction(),
            },
            child: new ShortcutRegistrar(themed));
    }

    private static Widget WrapWithoutShortcutRegistrar(
        Widget child,
        TargetPlatform platform = TargetPlatform.Windows,
        bool autofocus = true)
    {
        Widget content = autofocus
            ? new Focus(autofocus: true, child: child)
            : child;
        return new Directionality(
            TextDirection.Ltr,
            new Theme(
                ThemeData.Light with { Platform = platform },
                new Overlay(initialEntries: [new OverlayEntry(_ => content)])));
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text)
    {
        return FindDescendants<RenderParagraph>(root)
            .FirstOrDefault(paragraph => paragraph.PlainText == text);
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null)
        {
            return result;
        }

        if (root is T target)
        {
            result.Add(target);
        }

        root.VisitChildren(child => result.AddRange(FindDescendants<T>(child)));
        return result;
    }

    private sealed class WidgetRenderHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;

        public WidgetRenderHarness(Widget rootWidget)
        {
            RenderView = new RenderView();
            _pipeline = new PipelineOwner(RenderView);
            _pipeline.Attach(RenderView);
            _rootElement = new HarnessRootElement(RenderView, rootWidget);
            _rootElement.Attach(_owner);
            _rootElement.Mount(parent: null, newSlot: null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
            Scheduler.PumpFrameForTests();
        }

        public void Dispose() => _rootElement.Unmount();

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
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

            public override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            public override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot)
            {
                _renderView.Child = (RenderBox)child;
            }

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child))
                {
                    _renderView.Child = null;
                }
            }

            public override void Unmount()
            {
                if (_child is not null)
                {
                    UnmountChild(_child);
                    _child = null;
                }

                base.Unmount();
            }
        }
    }
}
