using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialSelectionTests
{
    [Fact]
    public void SelectableText_DefaultsAndGuardsMatchFlutterContract()
    {
        var widget = new SelectableText("Selectable");

        Assert.False(widget.ShowCursor);
        Assert.False(widget.Autofocus);
        Assert.True(widget.EnableInteractiveSelection);
        Assert.True(widget.SelectionEnabled);
        Assert.Equal(2, widget.CursorWidth);
        Assert.Null(widget.MinLines);
        Assert.Null(widget.MaxLines);
        Assert.NotNull(widget.ContextMenuBuilder);
        Assert.Same(TextMagnifier.AdaptiveMagnifierConfiguration, widget.MagnifierConfiguration);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SelectableText("x", minLines: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SelectableText("x", maxLines: 0));
        Assert.Throws<ArgumentException>(() => new SelectableText("x", minLines: 3, maxLines: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SelectableText("x", cursorWidth: 0));
    }

    [Fact]
    public void SelectionArea_ContextMenuItemsTrackSelectionAndInvokeCopy()
    {
        TextClipboard.ResetForTests();
        var key = new LabeledGlobalKey<SelectionAreaState>("context-menu-area");
        using var harness = new WidgetRenderHarness(Root(
            new SelectionArea(
                key: key,
                child: new Text("copy me")),
            ThemeData.Light));
        harness.Pump(new Size(320, 120));

        SelectableRegionState state = key.CurrentState!.SelectableRegion;
        Assert.Single(state.ContextMenuButtonItems);
        Assert.Equal(ContextMenuButtonType.SelectAll, state.ContextMenuButtonItems[0].Type);

        state.SelectAll();
        Assert.Equal(2, state.ContextMenuButtonItems.Count);
        ContextMenuButtonItem copy = state.ContextMenuButtonItems[0];
        Assert.Equal(ContextMenuButtonType.Copy, copy.Type);
        Assert.Equal(ContextMenuButtonType.SelectAll, state.ContextMenuButtonItems[1].Type);
        Assert.True(double.IsFinite(state.ContextMenuAnchors.PrimaryAnchor.X));
        copy.OnPressed!.Invoke();

        Assert.Equal("copy me", TextClipboard.GetText());
        Assert.False(state.ContextMenuIsVisible);
    }

    [Fact]
    public void EditableText_ContextMenuItemsFollowReadOnlyAndClipboardPolicies()
    {
        TextClipboard.ResetForTests();
        TextClipboard.SetText("paste");
        var key = new LabeledGlobalKey<EditableText.EditableTextState>("editable-menu");
        var controller = new TextEditingController("alpha beta", new TextSelection(0, 5));
        using var harness = new WidgetRenderHarness(Root(
            new EditableText(
                key: key,
                controller: controller,
                contextMenuBuilder: (_, state) => new SizedBox(
                    child: new Text(state.ContextMenuButtonItems.Count.ToString()))),
            ThemeData.Light));
        harness.Pump(new Size(320, 120));

        IReadOnlyList<ContextMenuButtonItem> items = key.CurrentState!.ContextMenuButtonItems;
        Assert.Equal(
            [
                ContextMenuButtonType.Cut,
                ContextMenuButtonType.Copy,
                ContextMenuButtonType.Paste,
                ContextMenuButtonType.SelectAll,
            ],
            items.Select(item => item.Type));

        ContextMenuButtonItem cut = items[0];
        cut.OnPressed!.Invoke();
        Assert.Equal("alpha", TextClipboard.GetText());
        Assert.Equal(" beta", controller.Text);
    }

    [Fact]
    public void SelectableText_ResolvesThemeStyleSelectionAndCursor()
    {
        var selection = Color.Parse("#55336699");
        var cursor = Colors.Crimson;
        var theme = ThemeData.Light with
        {
            TextSelectionTheme = new TextSelectionThemeData(
                CursorColor: cursor,
                SelectionColor: selection),
        };
        using var harness = new WidgetRenderHarness(Root(
            new SelectableText(
                "Styled",
                style: new TextStyle(FontSize: 21, Color: Colors.DarkGreen),
                showCursor: true,
                autofocus: true),
            theme));

        harness.Pump(new Size(320, 120));
        RenderEditable editable = FindEditables(harness.RenderView).Single();
        Assert.Equal(21, editable.FontSize);
        Assert.Equal(Colors.DarkGreen, ((SolidColorBrush)editable.Foreground).Color);
        Assert.Equal(selection, editable.SelectionColor);
        Assert.Equal(cursor, editable.CursorColor);
        Assert.True(editable.ShowCursor);
    }

    [Fact]
    public void SelectableText_SelectAllAndCopyUseReadOnlyKeyboardFlow()
    {
        TextSelection? changedSelection = null;
        SelectionChangedCause? changedCause = null;
        using var harness = new WidgetRenderHarness(Root(
            new SelectableText(
                "alpha beta",
                autofocus: true,
                onSelectionChanged: (selection, cause) =>
                {
                    changedSelection = selection;
                    changedCause = cause;
                }),
            ThemeData.Light));

        harness.Pump(new Size(320, 120));

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyA, control: true)));
        RenderEditable editable = FindEditables(harness.RenderView).Single();
        Assert.Equal(0, editable.Selection.BaseOffset);
        Assert.Equal(10, editable.Selection.ExtentOffset);
        Assert.Equal(new TextSelection(0, 10), changedSelection);
        Assert.Equal(SelectionChangedCause.Keyboard, changedCause);

        Assert.True(FocusManager.Instance.HandleKeyEvent(KeySim.Down(LogicalKeyboardKey.KeyC, control: true)));
        Assert.Equal("alpha beta", TextClipboard.GetText());
    }

    [Fact]
    public void SelectableText_PointerDragPaintSelectionAndReportsDragCause()
    {
        SelectionChangedCause? cause = null;
        using var harness = new WidgetRenderHarness(Root(
            new SelectableText(
                "drag selection",
                onSelectionChanged: (_, nextCause) => cause = nextCause),
            ThemeData.Light));
        harness.Pump(new Size(320, 120));
        Assert.True(FindEditables(harness.RenderView).Single().GetPositionForPoint(new Point(90, 8)).Offset > 0);

        var binding = GestureBinding.Instance;
        DateTime now = DateTime.UtcNow;
        binding.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            91,
            PointerDeviceKind.Mouse,
            new Point(1, 8),
            PointerButtons.Primary,
            now));
        Assert.Equal(SelectionChangedCause.Tap, cause);
        binding.HandlePointerEvent(harness.RenderView, new PointerMoveEvent(
            91,
            PointerDeviceKind.Mouse,
            new Point(90, 8),
            PointerButtons.Primary,
            down: true,
            now.AddMilliseconds(16)));
        binding.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            91,
            PointerDeviceKind.Mouse,
            new Point(90, 8),
            PointerButtons.None,
            now.AddMilliseconds(32)));

        RenderEditable editable = FindEditables(harness.RenderView).Single();
        Assert.NotEqual(editable.Selection.BaseOffset, editable.Selection.ExtentOffset);
        Assert.Equal(SelectionChangedCause.Drag, cause);
    }

    [Fact]
    public void SelectionArea_SelectAllAggregatesTextSubtreeAndExposesState()
    {
        var key = new LabeledGlobalKey<SelectionAreaState>("area");
        SelectedContent? selected = null;
        using var harness = new WidgetRenderHarness(Root(
            new SelectionArea(
                key: key,
                onSelectionChanged: content => selected = content,
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    children:
                    [
                        new Text("first "),
                        new Text("second"),
                    ])),
            ThemeData.Light));
        harness.Pump(new Size(320, 160));

        key.CurrentState!.SelectableRegion.SelectAll();

        Assert.Equal("first second", selected?.PlainText);
        Assert.Equal("first second", key.CurrentState.SelectableRegion.SelectedContent?.PlainText);
        List<RenderParagraph> paragraphs = FindParagraphs(harness.RenderView);
        Assert.Equal(paragraphs[0].PlainText.Length, paragraphs[0].Selections.Single().ExtentOffset);
        Assert.Equal(paragraphs[1].PlainText.Length, paragraphs[1].Selections.Single().ExtentOffset);

        key.CurrentState.SelectableRegion.CopySelection();
        Assert.Equal("first second", TextClipboard.GetText());
        key.CurrentState.SelectableRegion.ClearSelection();
        Assert.Null(key.CurrentState.SelectableRegion.SelectedContent);
    }

    [Fact]
    public void SelectionArea_PointerDragCanCrossParagraphBoundaries()
    {
        SelectedContent? selected = null;
        using var harness = new WidgetRenderHarness(Root(
            new SelectionArea(
                onSelectionChanged: content => selected = content,
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Start,
                    children:
                    [
                        new Text("first "),
                        new Text("second"),
                    ])),
            ThemeData.Light));
        harness.Pump(new Size(320, 160));

        var binding = GestureBinding.Instance;
        DateTime now = DateTime.UtcNow;
        binding.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            92,
            PointerDeviceKind.Mouse,
            new Point(1, 8),
            PointerButtons.Primary,
            now));
        binding.HandlePointerEvent(harness.RenderView, new PointerMoveEvent(
            92,
            PointerDeviceKind.Mouse,
            new Point(80, 28),
            PointerButtons.Primary,
            down: true,
            now.AddMilliseconds(16)));
        binding.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            92,
            PointerDeviceKind.Mouse,
            new Point(80, 28),
            PointerButtons.None,
            now.AddMilliseconds(32)));

        Assert.Equal("first second", selected?.PlainText);
        List<RenderParagraph> paragraphs = FindParagraphs(harness.RenderView);
        Assert.Equal(paragraphs[0].PlainText.Length, paragraphs[0].Selections.Single().ExtentOffset);
        Assert.Equal(paragraphs[1].PlainText.Length, paragraphs[1].Selections.Single().ExtentOffset);
    }

    [Fact]
    public void SelectionArea_LocalThemeOverridesGlobalThemeData()
    {
        var global = Colors.Crimson;
        var local = Colors.CornflowerBlue;
        var theme = ThemeData.Light with
        {
            TextSelectionTheme = new TextSelectionThemeData(SelectionColor: global),
        };
        using var harness = new WidgetRenderHarness(Root(
            new TextSelectionTheme(
                data: new TextSelectionThemeData(SelectionColor: local),
                child: new SelectionArea(child: new Text("themed area"))),
            theme));

        harness.Pump(new Size(320, 120));

        Assert.Equal(local, FindParagraphs(harness.RenderView).Single().SelectionColor);
    }

    [Fact]
    public void DefaultSelectionStyle_OverridesMaterialThemeForSelectableControls()
    {
        var global = Colors.Crimson;
        var local = Colors.CornflowerBlue;
        var cursor = Colors.DarkGreen;
        var theme = ThemeData.Light with
        {
            TextSelectionTheme = new TextSelectionThemeData(
                CursorColor: global,
                SelectionColor: global),
        };
        using var harness = new WidgetRenderHarness(Root(
            new DefaultSelectionStyle(
                cursorColor: cursor,
                selectionColor: local,
                mouseCursor: SystemMouseCursors.Click,
                child: new Column(
                    children:
                    [
                        new SelectableText("styled text", showCursor: true, autofocus: true),
                        new SelectionArea(child: new Text("styled area")),
                    ])),
            theme));

        harness.Pump(new Size(320, 160));

        List<RenderParagraph> paragraphs = FindParagraphs(harness.RenderView);
        RenderEditable editable = FindEditables(harness.RenderView).Single();
        Assert.Equal(local, editable.SelectionColor);
        Assert.Equal(cursor, editable.CursorColor);
        Assert.Equal(local, paragraphs[0].SelectionColor);
    }

    [Fact]
    public void SelectionArea_ShowToolbarSurvivesTheFocusHandoverToTheMenuRoute()
    {
        var key = new LabeledGlobalKey<SelectionAreaState>("toolbar-area");
        var focusNode = new FocusNode();
        using var harness = new WidgetRenderHarness(Root(
            new Navigator(new BuilderPageRoute(_ => new SelectionArea(
                key: key,
                focusNode: focusNode,
                child: new Text("long pressed")))),
            ThemeData.Light));
        harness.Pump(new Size(320, 160));

        focusNode.RequestFocus();
        harness.Pump(new Size(320, 160));

        SelectableRegionState state = key.CurrentState!.SelectableRegion;
        state.SelectAll();

        // Pushing the route-backed menu hands focus to the modal scope, which used to
        // re-enter HideToolbar and tear the route down mid-push.
        Assert.True(state.ShowToolbar());
        harness.Pump(new Size(320, 160));

        Assert.True(state.ContextMenuIsVisible);
        Assert.Equal("long pressed", state.SelectedContent?.PlainText);

        state.HideToolbar();
        Assert.False(state.ContextMenuIsVisible);
    }

    [Fact]
    public void SelectionArea_DoubleTapSelectsTheWordUnderThePointer()
    {
        using var timers = new FakeGestureTimers();
        SelectedContent? selected = null;
        using var harness = new WidgetRenderHarness(Root(
            new SelectionArea(
                onSelectionChanged: content => selected = content,
                child: new Text("alpha beta gamma")),
            ThemeData.Light));
        harness.Pump(new Size(320, 160));

        var position = new Point(2, 8);
        TapAt(harness, 71, position);
        TapAt(harness, 71, position);

        Assert.Equal("alpha", selected?.PlainText);
    }

    [Fact]
    public void SelectionArea_TripleTapSelectsTheParagraphOnDesktop()
    {
        using var timers = new FakeGestureTimers();
        var previous = PlatformDefaults.DebugTargetPlatformOverride;
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Linux;
        try
        {
            SelectedContent? selected = null;
            using var harness = new WidgetRenderHarness(Root(
                new SelectionArea(
                    onSelectionChanged: content => selected = content,
                    child: new Text("alpha beta gamma")),
                ThemeData.Light));
            harness.Pump(new Size(320, 160));

            var position = new Point(2, 8);
            TapAt(harness, 72, position);
            TapAt(harness, 72, position);
            TapAt(harness, 72, position);

            Assert.Equal("alpha beta gamma", selected?.PlainText);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    [Fact]
    public void SelectionArea_SingleTapAfterTheDoubleTapTimeoutStartsANewSeries()
    {
        using var timers = new FakeGestureTimers();
        SelectedContent? selected = null;
        using var harness = new WidgetRenderHarness(Root(
            new SelectionArea(
                onSelectionChanged: content => selected = content,
                child: new Text("alpha beta gamma")),
            ThemeData.Light));
        harness.Pump(new Size(320, 160));

        var position = new Point(2, 8);
        TapAt(harness, 73, position);
        TapAt(harness, 73, position);
        Assert.Equal("alpha", selected?.PlainText);

        // Past kDoubleTapTimeout the counter restarts, so the next tap collapses the selection
        // instead of selecting a word again.
        timers.Elapse(TimeSpan.FromMilliseconds(400));
        TapAt(harness, 73, position);
        Assert.True(string.IsNullOrEmpty(selected?.PlainText));
    }

    [Fact]
    public void SelectionArea_RightClickShowsTheContextMenuAndKeepsTheSelectionOnLinux()
    {
        using var timers = new FakeGestureTimers();
        var previous = PlatformDefaults.DebugTargetPlatformOverride;
        PlatformDefaults.DebugTargetPlatformOverride = TargetPlatform.Linux;
        try
        {
            var key = new LabeledGlobalKey<SelectionAreaState>("linux-area");
            using var harness = new WidgetRenderHarness(Root(
                new Navigator(new BuilderPageRoute(_ => new SelectionArea(
                    key: key,
                    child: new Text("alpha beta gamma")))),
                ThemeData.Light));
            harness.Pump(new Size(320, 160));

            SelectableRegionState state = key.CurrentState!.SelectableRegion;
            state.SelectAll();
            harness.Pump(new Size(320, 160));

            SecondaryTapAt(harness, 74, new Point(2, 8));
            harness.Pump(new Size(320, 160));

            // A right click inside the active selection shows the menu without collapsing it.
            Assert.True(state.ContextMenuIsVisible);
            Assert.Equal("alpha beta gamma", state.SelectedContent?.PlainText);

            // Flutter toggles the menu off on the next right click; Plumix's menu is a route whose
            // modal barrier consumes that press instead (see `DIVERGENCES.md`), so the toggle is
            // asserted through the API the barrier ultimately calls.
            state.HideToolbar();
            Assert.False(state.ContextMenuIsVisible);
        }
        finally
        {
            PlatformDefaults.DebugTargetPlatformOverride = previous;
        }
    }

    private static void TapAt(WidgetRenderHarness harness, int pointer, Point position)
    {
        var binding = GestureBinding.Instance;
        DateTime now = DateTime.UtcNow;
        binding.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            pointer, PointerDeviceKind.Mouse, position, PointerButtons.Primary, now));
        binding.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            pointer, PointerDeviceKind.Mouse, position, PointerButtons.None, now.AddMilliseconds(16)));
    }

    private static void SecondaryTapAt(WidgetRenderHarness harness, int pointer, Point position)
    {
        var binding = GestureBinding.Instance;
        DateTime now = DateTime.UtcNow;
        binding.HandlePointerEvent(harness.RenderView, new PointerDownEvent(
            pointer, PointerDeviceKind.Mouse, position, PointerButtons.Secondary, now));
        binding.HandlePointerEvent(harness.RenderView, new PointerUpEvent(
            pointer, PointerDeviceKind.Mouse, position, PointerButtons.None, now.AddMilliseconds(16)));
    }

    private static Widget Root(Widget child, ThemeData theme)
    {
        return new MediaQuery(
            data: new MediaQueryData(Size: new Size(320, 180)),
            child: new Directionality(
                TextDirection.Ltr,
                new Localizations(
                    locale: new Locale("en", "US"),
                    delegates:
                    [
                        DefaultWidgetsLocalizations.Delegate,
                        DefaultMaterialLocalizations.Delegate,
                        DefaultCupertinoLocalizations.Delegate,
                    ],
                    child: new Theme(theme, child))));
    }

    private static List<RenderParagraph> FindParagraphs(RenderObject? root)
    {
        var result = new List<RenderParagraph>();
        if (root is null)
        {
            return result;
        }
        if (root is RenderParagraph paragraph)
        {
            result.Add(paragraph);
        }
        root.VisitChildren(child => result.AddRange(FindParagraphs(child)));
        return result;
    }

    private static List<RenderEditable> FindEditables(RenderObject? root)
    {
        var result = new List<RenderEditable>();
        if (root is null) return result;
        if (root is RenderEditable editable) result.Add(editable);
        root.VisitChildren(child => result.AddRange(FindEditables(child)));
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
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushPaint();
        }

        public void Dispose()
        {
            _rootElement.Unmount();
            FocusManager.Instance.ResetForTests();
            GestureBinding.Instance.ResetForTests();
        }

        private sealed class HarnessRootElement : Element, IRenderObjectHost
        {
            private readonly RenderView _renderView;
            private Element? _child;

            public HarnessRootElement(RenderView renderView, Widget widget) : base(widget)
            {
                _renderView = renderView;
            }

            public override RenderObject? RenderObject => _child?.RenderObject;
            internal override Element? RenderObjectAttachingChild => _child;
            protected override void OnMount() { base.OnMount(); Rebuild(); }
            internal override void Rebuild() { Dirty = false; _child = UpdateChild(_child, Widget, Slot); }
            internal override void Update(Widget newWidget) { base.Update(newWidget); Rebuild(); }
            internal override void ForgetChild(Element child) { if (ReferenceEquals(_child, child)) _child = null; }
            internal override void VisitChildren(Action<Element> visitor) { if (_child is not null) visitor(_child); }
            public void InsertRenderObjectChild(RenderObject child, object? slot) =>
                _renderView.Child = (RenderBox)child;
            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot) { }
            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null;
            }
            internal override void Unmount()
            {
                if (_child is not null) { UnmountChild(_child); _child = null; }
                base.Unmount();
            }
        }
    }
}
