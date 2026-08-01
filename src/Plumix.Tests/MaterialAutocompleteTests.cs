using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialAutocompleteTests : IDisposable
{
    private static readonly IReadOnlyList<string> Options =
    [
        "aardvark",
        "bobcat",
        "chameleon",
        "dingo",
        "elephant",
        "flamingo",
    ];

    public MaterialAutocompleteTests()
    {
        Scheduler.ResetForTests();
        FocusManager.Instance.ResetForTests();
        GestureBinding.Instance.ResetForTests();
        SemanticsService.ResetForTests();
    }

    public void Dispose()
    {
        SemanticsService.ResetForTests();
        GestureBinding.Instance.ResetForTests();
        FocusManager.Instance.ResetForTests();
        Scheduler.ResetForTests();
    }

    [Fact]
    public void Constructors_ExposeFlutterDefaultsAndValidateSplitFieldContract()
    {
        Func<TextEditingValue, IEnumerable<string>> optionsBuilder = value => Options;
        var autocomplete = new Autocomplete<string>(optionsBuilder);
        Assert.Equal(200, autocomplete.OptionsMaxHeight);
        Assert.Equal(OptionsViewOpenDirection.Down, autocomplete.OptionsViewOpenDirection);
        Assert.NotNull(autocomplete.FieldViewBuilder);
        Assert.Equal("value", autocomplete.DisplayStringForOption("value"));

        Assert.Throws<ArgumentException>(() => new RawAutocomplete<string>(
            optionsViewBuilder: (_, _, _) => new SizedBox(),
            optionsBuilder: optionsBuilder));
        Assert.Throws<ArgumentException>(() => new RawAutocomplete<string>(
            optionsViewBuilder: (_, _, _) => new SizedBox(),
            optionsBuilder: optionsBuilder,
            fieldViewBuilder: (_, _, _, _) => new SizedBox(),
            focusNode: new FocusNode()));
        Assert.Throws<ArgumentException>(() => new RawAutocomplete<string>(
            optionsViewBuilder: (_, _, _) => new SizedBox(),
            optionsBuilder: optionsBuilder,
            fieldViewBuilder: (_, _, _, _) => new SizedBox(),
            focusNode: new FocusNode(),
            textEditingController: new TextEditingController(),
            initialValue: new TextEditingValue("seed")));
    }

    [Fact]
    public void RawAutocomplete_FiltersHighlightsAndSelectsThroughKeyboard()
    {
        var controller = new TextEditingController();
        var focusNode = new FocusNode();
        string? selected = null;
        using var harness = new WidgetRenderHarness(Wrap(new Navigator(new BuilderPageRoute(_ =>
            new RawAutocomplete<string>(
                textEditingController: controller,
                focusNode: focusNode,
                optionsBuilder: value => Options.Where(option => option.Contains(value.Text, StringComparison.OrdinalIgnoreCase)),
                optionsViewBuilder: (context, onSelected, options) => new Column(
                    mainAxisSize: MainAxisSize.Min,
                    children: options.Select((option, index) => new GestureDetector(
                        onTap: () => onSelected(option),
                        child: new Text($"{AutocompleteHighlightedOption.Of(context) == index}:{option}"))).ToArray()),
                fieldViewBuilder: (_, textController, node, onSubmitted) => new TextField(
                    controller: textController,
                    focusNode: node,
                    onSubmitted: value => onSubmitted()),
                onSelected: value => selected = value)))));
        harness.Pump(new Size(480, 320));

        focusNode.RequestFocus();
        harness.Pump(new Size(480, 320));
        Assert.NotNull(FindParagraph(harness.RenderView, "True:aardvark"));

        controller.Value = new TextEditingValue("e", TextSelection.Collapsed(1));
        harness.Pump(new Size(480, 320));
        Assert.NotNull(FindParagraph(harness.RenderView, "True:chameleon"));
        Assert.NotNull(FindParagraph(harness.RenderView, "False:elephant"));

        focusNode.Unfocus();
        harness.Pump(new Size(480, 320));
        Assert.Null(FindParagraph(harness.RenderView, "True:chameleon"));
        focusNode.RequestFocus();
        harness.Pump(new Size(480, 320));
        Assert.NotNull(FindParagraph(harness.RenderView, "True:chameleon"));

        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("ArrowDown", isDown: true)));
        harness.Pump(new Size(480, 320));
        Assert.NotNull(FindParagraph(harness.RenderView, "True:elephant"));
        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("Enter", isDown: true)));
        harness.Pump(new Size(480, 320));

        Assert.Equal("elephant", selected);
        Assert.Equal("elephant", controller.Text);
        Assert.Null(FindParagraph(harness.RenderView, "True:elephant"));
    }

    [Fact]
    public void MaterialAutocomplete_UsesDefaultFieldSurfaceAndMaxHeight()
    {
        var controller = new TextEditingController();
        var focusNode = new FocusNode();
        using var harness = new WidgetRenderHarness(Wrap(new Navigator(new BuilderPageRoute(_ =>
            new Autocomplete<string>(
                optionsBuilder: value => Options,
                textEditingController: controller,
                focusNode: focusNode,
                optionsMaxHeight: 96)))));
        harness.Pump(new Size(480, 320));
        Assert.Contains(FindDescendants<RenderSemanticsAnnotations>(harness.RenderView), semantics =>
            semantics.Flags.HasFlag(SemanticsFlags.IsTextField));

        focusNode.RequestFocus();
        harness.Pump(new Size(480, 320));

        Assert.NotNull(FindParagraph(harness.RenderView, "aardvark"));
        Assert.Contains(FindDescendants<RenderConstrainedBox>(harness.RenderView), box =>
            Math.Abs(box.AdditionalConstraints.MaxHeight - 96) < 0.01);
        Plumix.Material.Material surface = Assert.Single(
            harness.FindWidgets<Plumix.Material.Material>(),
            material => material.Elevation == 4.0);
        Assert.Equal(MaterialType.Canvas, surface.Type);
    }

    [Fact]
    public void MostSpace_OpensAboveFieldNearBottomViewportEdge()
    {
        var controller = new TextEditingController();
        var focusNode = new FocusNode();
        using var harness = new WidgetRenderHarness(Wrap(new Navigator(new BuilderPageRoute(_ =>
            new Align(
                alignment: Alignment.BottomLeft,
                child: new SizedBox(
                    width: 260,
                    child: new Autocomplete<string>(
                        optionsBuilder: value => Options,
                        textEditingController: controller,
                        focusNode: focusNode,
                        optionsViewOpenDirection: OptionsViewOpenDirection.MostSpace)))))));
        harness.Pump(new Size(480, 240));
        focusNode.RequestFocus();
        harness.Pump(new Size(480, 240));

        RenderEditable field = Assert.Single(FindDescendants<RenderEditable>(harness.RenderView));
        RenderParagraph option = Assert.IsType<RenderParagraph>(FindParagraph(harness.RenderView, "aardvark"));
        Assert.True(GlobalRect(option).Bottom <= GlobalRect(field).Top + 0.01);
    }

    [Fact]
    public async Task AsyncOptions_IgnoreResultsFromAnOlderRequest()
    {
        var controller = new TextEditingController();
        var focusNode = new FocusNode();
        var first = new TaskCompletionSource<IEnumerable<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<IEnumerable<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = new WidgetRenderHarness(Wrap(new Navigator(new BuilderPageRoute(_ =>
            new Autocomplete<string>(
                optionsBuilder: async value => await (value.Text.Length == 0 ? first.Task : second.Task),
                textEditingController: controller,
                focusNode: focusNode)))));
        harness.Pump(new Size(480, 320));
        focusNode.RequestFocus();
        controller.Text = "new";

        second.SetResult(["new-result"]);
        await PumpUntilAsync(
            harness,
            new Size(480, 320),
            () => FindParagraph(harness.RenderView, "new-result") is not null);
        Assert.NotNull(FindParagraph(harness.RenderView, "new-result"));

        first.SetResult(["stale-result"]);
        await Task.Delay(10);
        harness.Pump(new Size(480, 320));
        Assert.NotNull(FindParagraph(harness.RenderView, "new-result"));
        Assert.Null(FindParagraph(harness.RenderView, "stale-result"));
    }

    [Fact]
    public void RawAutocomplete_UsesPortalWithoutPushingRouteAndKeepsLocalTheme()
    {
        var controller = new TextEditingController();
        var focusNode = new FocusNode();
        var navigatorKey = new LabeledGlobalKey<NavigatorState>("autocomplete navigator");
        var route = new BuilderPageRoute(_ =>
            new Theme(
                ThemeData.Light with { CanvasColor = Colors.Crimson },
                new RawAutocomplete<string>(
                    textEditingController: controller,
                    focusNode: focusNode,
                    optionsBuilder: value => Options,
                    optionsViewBuilder: (context, onSelected, options) => new ColoredBox(
                        Theme.Of(context).CanvasColor,
                        new Text(options.First())),
                    fieldViewBuilder: (_, textController, node, onSubmitted) => new TextField(
                        controller: textController,
                        focusNode: node))));
        using var harness = new WidgetRenderHarness(Wrap(new Navigator(route, key: navigatorKey)));
        harness.Pump(new Size(480, 320));

        focusNode.RequestFocus();
        harness.Pump(new Size(480, 320));

        Assert.Same(route, navigatorKey.CurrentState?.CurrentRoute);
        Assert.False(navigatorKey.CurrentState?.CanPop);
        Assert.Contains(
            FindDescendants<RenderColoredBox>(harness.RenderView),
            box => box.Color == Colors.Crimson);
    }

    [Fact]
    public async Task RawAutocomplete_AnnouncesOnlyEmptyStateTransitionsForOwningView()
    {
        var controller = new TextEditingController();
        var focusNode = new FocusNode();
        var announcements = new List<SemanticsAnnouncement>();
        SemanticsService.AnnouncementRequested += announcements.Add;
        using var harness = new WidgetRenderHarness(Wrap(
            new Navigator(new BuilderPageRoute(_ => new Autocomplete<string>(
                optionsBuilder: value => Options.Where(option => option.Contains(value.Text)),
                textEditingController: controller,
                focusNode: focusNode))),
            new MediaQueryData(Size: new Size(480, 320), SupportsAnnounce: true, ViewId: 7)));
        harness.Pump(new Size(480, 320));

        focusNode.RequestFocus();
        await PumpUntilAsync(harness, new Size(480, 320), () => announcements.Count == 1);
        controller.Text = "e";
        harness.Pump(new Size(480, 320));
        controller.Text = "no matches";
        await PumpUntilAsync(harness, new Size(480, 320), () => announcements.Count == 2);

        Assert.Equal(
            ["Search results found", "No results found"],
            announcements.Select(announcement => announcement.Message));
        Assert.All(announcements, announcement => Assert.Equal(7, announcement.ViewId));
        Assert.All(announcements, announcement => Assert.Equal(TextDirection.Ltr, announcement.TextDirection));
    }

    [Fact]
    public async Task RawAutocomplete_ReportsAnnouncementFailures()
    {
        var focusNode = new FocusNode();
        Exception? reported = null;
        SemanticsService.PlatformHandler = announcement =>
            Task.FromException(new FormatException("invalid announcement response"));
        SemanticsService.AnnouncementFailed += exception => reported = exception;
        using var harness = new WidgetRenderHarness(Wrap(
            new Navigator(new BuilderPageRoute(_ => new Autocomplete<string>(
                optionsBuilder: value => Options,
                focusNode: focusNode,
                textEditingController: new TextEditingController()))),
            new MediaQueryData(Size: new Size(480, 320), SupportsAnnounce: true)));
        harness.Pump(new Size(480, 320));

        focusNode.RequestFocus();
        await PumpUntilAsync(harness, new Size(480, 320), () => reported is not null);

        Assert.IsType<FormatException>(reported);
    }

    [Fact]
    public void RawAutocomplete_EscapeDelegatesAfterClosingPortal()
    {
        var controller = new TextEditingController();
        var focusNode = new FocusNode();
        int dismissals = 0;
        Widget navigator = new Navigator(new BuilderPageRoute(_ => new Autocomplete<string>(
            optionsBuilder: value => Options,
            textEditingController: controller,
            focusNode: focusNode)));
        navigator = new Actions(
            new Dictionary<Type, FlutterAction>
            {
                [typeof(DismissIntent)] = new CallbackAction<DismissIntent>(_ =>
                {
                    dismissals += 1;
                    return null;
                }),
            },
            navigator);
        using var harness = new WidgetRenderHarness(Wrap(navigator));
        harness.Pump(new Size(480, 320));
        focusNode.RequestFocus();
        harness.Pump(new Size(480, 320));
        Assert.NotNull(FindParagraph(harness.RenderView, "aardvark"));

        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("Escape", isDown: true)));
        harness.Pump(new Size(480, 320));
        Assert.Null(FindParagraph(harness.RenderView, "aardvark"));
        Assert.Equal(0, dismissals);

        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent("Escape", isDown: true)));
        Assert.Equal(1, dismissals);
    }

    [Fact]
    public void MaterialAutocomplete_EndShortcutBuildsAndHighlightsLastLazyOption()
    {
        string[] options = Enumerable.Range(0, 30).Select(index => $"option-{index}").ToArray();
        var controller = new TextEditingController();
        var focusNode = new FocusNode();
        using var harness = new WidgetRenderHarness(Wrap(new Navigator(new BuilderPageRoute(_ =>
            new Autocomplete<string>(
                optionsBuilder: value => options,
                textEditingController: controller,
                focusNode: focusNode,
                optionsMaxHeight: 96)))));
        harness.Pump(new Size(480, 320));
        focusNode.RequestFocus();
        harness.Pump(new Size(480, 320));

        Assert.True(FocusManager.Instance.HandleKeyEvent(new KeyEvent(
            "ArrowDown",
            isDown: true,
            isControlPressed: true)));
        for (int frame = 0; frame < 4; frame++)
        {
            Scheduler.PumpFrameForTests();
            harness.Pump(new Size(480, 320));
        }

        RenderViewport viewport = Assert.Single(FindDescendants<RenderViewport>(harness.RenderView));
        Assert.True(
            FindParagraph(harness.RenderView, "option-29") is not null,
            $"Expected last option at scroll offset {viewport.OffsetPixels}.");
        Assert.Null(FindParagraph(harness.RenderView, "option-0"));
    }

    private static Widget Wrap(Widget child, MediaQueryData? mediaQueryData = null)
    {
        return new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                mediaQueryData ?? new MediaQueryData(Size: new Size(480, 320)),
                new Localizations(
                    locale: new Locale("en"),
                    delegates: [DefaultWidgetsLocalizations.Delegate],
                    child: new Theme(
                        ThemeData.Light,
                        Overlay.Wrap(child)))));
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text)
    {
        return FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.Text == text);
    }

    private static Rect GlobalRect(RenderBox renderBox)
    {
        Assert.True(renderBox.TryGetTransformFromRoot(out Matrix transform));
        return RenderObject.TransformRect(transform, new Rect(renderBox.Size));
    }

    private static async Task PumpUntilAsync(
        WidgetRenderHarness harness,
        Size size,
        Func<bool> predicate)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            await Task.Delay(1);
            harness.Pump(size);
            if (predicate())
            {
                return;
            }
        }

        throw new TimeoutException("The expected asynchronous widget state was not reached.");
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        if (root is null) return result;
        if (root is T target) result.Add(target);
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
            _rootElement.Mount(null, null);
            _owner.FlushBuild();
        }

        public RenderView RenderView { get; }

        public IReadOnlyList<T> FindWidgets<T>() where T : Widget
        {
            var result = new List<T>();
            Visit(_rootElement);
            return result;

            void Visit(Element element)
            {
                if (element.Widget is T widget)
                {
                    result.Add(widget);
                }

                element.VisitChildren(Visit);
            }
        }

        public void Pump(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
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

            internal override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            internal override void Rebuild()
            {
                Dirty = false;
                _child = UpdateChild(_child, Widget, Slot);
            }

            internal override void Update(Widget newWidget)
            {
                base.Update(newWidget);
                Rebuild();
            }

            internal override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child)) _child = null;
            }

            internal override void VisitChildren(Action<Element> visitor)
            {
                if (_child is not null) visitor(_child);
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
                if (ReferenceEquals(_renderView.Child, child)) _renderView.Child = null;
            }

            internal override void Unmount()
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
