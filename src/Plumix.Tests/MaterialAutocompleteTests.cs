using Avalonia;
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
    }

    public void Dispose()
    {
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
        Assert.Contains(FindDescendants<RenderDecoratedBox>(harness.RenderView), box =>
            box.Decoration.Color == ThemeData.Light.CanvasColor && box.Decoration.BoxShadows is not null);
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

        var position = Assert.Single(FindDescendants<RenderAutocompleteOptionsPosition>(harness.RenderView));
        Assert.True(position.OpensUp);
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

    private static Widget Wrap(Widget child)
    {
        return new Directionality(
            TextDirection.Ltr,
            new MediaQuery(
                new MediaQueryData(Size: new Size(480, 320)),
                new Theme(ThemeData.Light, child)));
    }

    private static RenderParagraph? FindParagraph(RenderObject? root, string text)
    {
        return FindDescendants<RenderParagraph>(root).FirstOrDefault(paragraph => paragraph.Text == text);
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
