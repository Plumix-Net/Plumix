using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity source: cupertino_ui/test/text_selection_test.dart

[Collection(SchedulerTestCollection.Name)]
public sealed class CupertinoTextSelectionControlsTests : IDisposable
{
    public CupertinoTextSelectionControlsTests()
    {
        Scheduler.ResetForTests();
    }

    public void Dispose()
    {
        Scheduler.ResetForTests();
    }

    [Fact]
    public void MobileControls_HandleSizesAndAnchorsMatchFlutterGeometry()
    {
        TextSelectionControls controls = CupertinoTextSelectionControls.Instance;

        Assert.Equal(new Size(12.0, 20.5), controls.GetHandleSize(10.0));
        Assert.Equal(new Size(12.0, 60.5), controls.GetHandleSize(50.0));
        Assert.Equal(new Point(6.0, 20.5), controls.GetHandleAnchor(TextSelectionHandleType.Left, 10.0));
        Assert.Equal(new Point(6.0, 10.0), controls.GetHandleAnchor(TextSelectionHandleType.Right, 10.0));
        Assert.Equal(new Point(6.0, 15.25), controls.GetHandleAnchor(TextSelectionHandleType.Collapsed, 10.0));
    }

    [Fact]
    public void MobileControls_BuildLeftRightAndInvisibleCollapsedHandles()
    {
        Widget? left = null;
        Widget? right = null;
        Widget? collapsed = null;
        using var harness = new WidgetRenderHarness(Wrap(new Builder(context =>
        {
            left = CupertinoTextSelectionControls.Instance.BuildHandle(
                context,
                TextSelectionHandleType.Left,
                10.0);
            right = CupertinoTextSelectionControls.Instance.BuildHandle(
                context,
                TextSelectionHandleType.Right,
                10.0,
                () => throw new InvalidOperationException("Cupertino handles ignore taps."));
            collapsed = CupertinoTextSelectionControls.Instance.BuildHandle(
                context,
                TextSelectionHandleType.Collapsed,
                10.0);
            return new Row(children: [left, right, collapsed]);
        })));

        harness.Pump(new Size(120.0, 40.0));

        var leftBox = Assert.IsType<SizedBox>(left);
        Assert.Equal(12.0, leftBox.Width);
        Assert.Equal(20.5, leftBox.Height);
        Assert.IsType<CustomPaint>(leftBox.Child);

        var rightTransform = Assert.IsType<Plumix.Widgets.Transform>(right);
        Matrix4 expected = Matrix4.Identity();
        expected.TranslateByDouble(6.0, 10.25, 0.0, 1.0);
        expected.RotateZ(Math.PI);
        expected.TranslateByDouble(-6.0, -10.25, 0.0, 1.0);
        Assert.Equal(expected, rightTransform.Matrix);

        var collapsedBox = Assert.IsType<SizedBox>(collapsed);
        Assert.Equal(12.0, collapsedBox.Width);
        Assert.Equal(20.5, collapsedBox.Height);
        Assert.Null(collapsedBox.Child);
    }

    [Fact]
    public void HandlePainter_UsesThemeColorSingleUnionPathAndColorBasedRepaint()
    {
        Color color = Color.FromArgb(0x55, 0x00, 0x00, 0xAA);
        var painter = new CupertinoTextSelectionHandlePainter(color);
        var geometry = Assert.IsType<CombinedGeometry>(
            CupertinoTextSelectionHandlePainter.BuildPath(new Size(12.0, 20.5)));

        Assert.Equal(GeometryCombineMode.Union, geometry.GeometryCombineMode);
        Assert.Equal(new Rect(0.0, 0.0, 12.0, 12.0), Assert.IsType<EllipseGeometry>(geometry.Geometry1).Rect);
        Assert.Equal(new Rect(5.0, 10.5, 2.0, 10.0), Assert.IsType<RectangleGeometry>(geometry.Geometry2).Rect);

        var root = new ContainerLayer();
        var context = new PaintingContext(root);
        painter.Paint(context, new Size(12.0, 20.5));
        context.DebugStopRecordingIfNeeded();
        Assert.False(Assert.IsType<PictureLayer>(Assert.Single(root.Children)).IsEmpty);
        Assert.False(painter.ShouldRepaint(new CupertinoTextSelectionHandlePainter(color)));
        Assert.True(painter.ShouldRepaint(new CupertinoTextSelectionHandlePainter(Colors.Red)));

        Color? resolved = null;
        using var harness = new WidgetRenderHarness(Wrap(
            new Builder(context => CupertinoTextSelectionControls.Instance.BuildHandle(
                context,
                TextSelectionHandleType.Left,
                10.0)),
            selectionHandleColor: color));
        harness.Pump(new Size(20.0, 30.0));
        resolved = Assert.IsType<CupertinoTextSelectionHandlePainter>(
            Assert.Single(FindDescendants<RenderCustomPaint>(harness.RenderView)).Painter).Color;
        Assert.Equal(color, resolved);
    }

    [Theory]
    [InlineData("", 0, 0, false)]
    [InlineData("123", 1, 1, true)]
    [InlineData("123", 1, 2, false)]
    [InlineData("123", 0, 3, false)]
    public void MobileControls_CanSelectAllFollowsIosRules(
        string text,
        int baseOffset,
        int extentOffset,
        bool expected)
    {
        var @delegate = new FakeSelectionDelegate(
            new TextEditingValue(text, new TextSelection(baseOffset, extentOffset)));

#pragma warning disable CS0618 // Exercises Flutter's deprecated legacy selection-controls surface.
        Assert.Equal(expected, CupertinoTextSelectionControls.Instance.CanSelectAll(@delegate));
#pragma warning restore CS0618
    }

    [Fact]
    public void MobileLegacyToolbar_ClampsAnchorsOrdersLocalizedItemsAndUsesPhysicalPixelDividers()
    {
        var clipboard = new ClipboardStatusNotifier(ClipboardStatus.Pasteable);
        var @delegate = new FakeSelectionDelegate(new TextEditingValue("hello", new TextSelection(1, 3)));
        using var harness = BuildMobileToolbarHarness(@delegate, clipboard);

        harness.Pump(new Size(320.0, 240.0));

        CupertinoTextSelectionToolbar toolbar = Assert.Single(harness.FindWidgets<CupertinoTextSelectionToolbar>());
        Assert.Equal(new Point(287.0, 56.0), toolbar.AnchorAbove);
        Assert.Equal(new Point(287.0, 82.0), toolbar.AnchorBelow);
        Assert.Equal(["Cut", "Copy", "Paste"], MobileToolbarLabels(harness));
        Assert.Equal(2, toolbar.Children.OfType<SizedBox>().Count(divider => divider.Width == 0.5));
    }

    [Fact]
    public void MobileLegacyToolbar_ListensForClipboardAvailabilityAndBuildsNothingWithoutActions()
    {
        var clipboard = new ClipboardStatusNotifier();
        var @delegate = new FakeSelectionDelegate(new TextEditingValue("hello", new TextSelection(1, 3)));
        using var harness = BuildMobileToolbarHarness(@delegate, clipboard);

        harness.Pump(new Size(320.0, 240.0));
        Assert.Empty(harness.FindWidgets<CupertinoTextSelectionToolbar>());

        clipboard.Value = ClipboardStatus.NotPasteable;
        harness.Pump(new Size(320.0, 240.0));
        Assert.Equal(["Cut", "Copy"], MobileToolbarLabels(harness));

        var disabledDelegate = new FakeSelectionDelegate(
            new TextEditingValue("hello", new TextSelection(0, 5)),
            enabled: false);
        using var disabled = BuildMobileToolbarHarness(
            disabledDelegate,
            new ClipboardStatusNotifier(ClipboardStatus.Pasteable));
        disabled.Pump(new Size(320.0, 240.0));
        Assert.Empty(disabled.FindWidgets<CupertinoTextSelectionToolbar>());
    }

    [Fact]
    public void DesktopControls_HaveNoHandlesAndSelectAllHidesToolbar()
    {
        TextSelectionControls controls = CupertinoDesktopTextSelectionControls.Instance;
        var @delegate = new FakeSelectionDelegate(new TextEditingValue("hello", new TextSelection(2, 2)));

        Assert.Equal(default, controls.GetHandleSize(20.0));
        Assert.Equal(default, controls.GetHandleAnchor(TextSelectionHandleType.Left, 20.0));
        Assert.IsType<SizedBox>(controls.BuildHandle(default, TextSelectionHandleType.Left, 20.0));

#pragma warning disable CS0618 // Exercises Flutter's deprecated legacy selection-controls surface.
        controls.HandleSelectAll(@delegate);
#pragma warning restore CS0618

        Assert.Equal(["selectAll", "hideToolbar"], @delegate.Calls);
    }

    [Fact]
    public void DesktopLegacyToolbar_PrefersSecondaryTapAndFallsBackToClampedMidpoint()
    {
        var clipboard = new ClipboardStatusNotifier(ClipboardStatus.Pasteable);
        var @delegate = new FakeSelectionDelegate(new TextEditingValue("hello", new TextSelection(1, 3)));
        using var secondary = BuildDesktopToolbarHarness(@delegate, clipboard, new Point(240.0, 180.0));
        secondary.Pump(new Size(320.0, 240.0));
        Assert.Equal(
            new Point(240.0, 180.0),
            Assert.Single(secondary.FindWidgets<CupertinoDesktopTextSelectionToolbar>()).Anchor);
        Assert.Equal(["Cut", "Copy", "Paste"], DesktopToolbarLabels(secondary));

        using var midpoint = BuildDesktopToolbarHarness(@delegate, clipboard, lastSecondaryTap: null);
        midpoint.Pump(new Size(320.0, 240.0));
        Assert.Equal(
            new Point(308.0, 40.0),
            Assert.Single(midpoint.FindWidgets<CupertinoDesktopTextSelectionToolbar>()).Anchor);
    }

    [Fact]
    public void HandleOnlyInstancesSuppressToolbarAndLegacyActions()
    {
#pragma warning disable CS0618 // Exercises Flutter's deprecated handle-only controls.
        TextSelectionControls[] controls =
        [
            CupertinoTextSelectionHandleControls.Instance,
            CupertinoDesktopTextSelectionControls.HandleControls,
        ];
#pragma warning restore CS0618

        foreach (TextSelectionControls control in controls)
        {
            var @delegate = new FakeSelectionDelegate(new TextEditingValue("hello", new TextSelection(1, 3)));
            Assert.IsAssignableFrom<ITextSelectionHandleControls>(control);
#pragma warning disable CS0618
            Assert.False(control.CanCut(@delegate));
            Assert.False(control.CanCopy(@delegate));
            Assert.False(control.CanPaste(@delegate));
            Assert.False(control.CanSelectAll(@delegate));
            control.HandleCut(@delegate);
            control.HandleCopy(@delegate);
            control.HandlePaste(@delegate);
            control.HandleSelectAll(@delegate);
#pragma warning restore CS0618
            Assert.Empty(@delegate.Calls);
        }
    }

    [Theory]
    [InlineData(TargetPlatform.IOS)]
    [InlineData(TargetPlatform.MacOS)]
    [InlineData(TargetPlatform.Android)]
    public void MaterialTextControls_DefaultToFlutterPlatformHandleControls(TargetPlatform platform)
    {
        ThemeData theme = ThemeData.Light with { Platform = platform };
        using var textFieldHarness = new WidgetRenderHarness(new MediaQuery(
            new MediaQueryData(),
            new Theme(theme, new Directionality(TextDirection.Ltr, new TextField()))));
        textFieldHarness.Pump(new Size(320.0, 120.0));
        TextSelectionControls? textFieldControls =
            Assert.Single(textFieldHarness.FindWidgets<EditableText>()).SelectionControls;

        using var selectableHarness = new WidgetRenderHarness(new MediaQuery(
            new MediaQueryData(),
            new Theme(theme, new Directionality(TextDirection.Ltr, new SelectableText("Select me")))));
        selectableHarness.Pump(new Size(320.0, 120.0));
        TextSelectionControls? selectableControls =
            Assert.Single(selectableHarness.FindWidgets<EditableText>()).SelectionControls;

#pragma warning disable CS0618 // Flutter's platform defaults intentionally use deprecated instances.
        TextSelectionControls expected = platform switch
        {
            TargetPlatform.IOS => CupertinoTextSelectionHandleControls.Instance,
            TargetPlatform.MacOS => CupertinoDesktopTextSelectionControls.HandleControls,
            _ => MaterialTextSelectionHandleControls.Instance,
        };
#pragma warning restore CS0618
        Assert.Same(expected, textFieldControls);
        Assert.Same(expected, selectableControls);
    }

    private static string[] MobileToolbarLabels(WidgetRenderHarness harness)
    {
        return harness.FindWidgets<CupertinoTextSelectionToolbarButton>()
            .Where(button => button.Text is not null)
            .Select(button => button.Text!)
            .ToArray();
    }

    private static string[] DesktopToolbarLabels(WidgetRenderHarness harness)
    {
        return harness.FindWidgets<CupertinoDesktopTextSelectionToolbarButton>()
            .Where(button => button.Text is not null)
            .Select(button => button.Text!)
            .ToArray();
    }

    private static WidgetRenderHarness BuildMobileToolbarHarness(
        ITextSelectionDelegate @delegate,
        ClipboardStatusNotifier clipboardStatus)
    {
#pragma warning disable CS0618 // Exercises Flutter's deprecated legacy selection-controls surface.
        return new WidgetRenderHarness(Wrap(new Builder(context =>
            CupertinoTextSelectionControls.Instance.BuildToolbar(
                context,
                globalEditableRegion: new Rect(10.0, 40.0, 200.0, 60.0),
                textLineHeight: 14.0,
                selectionMidpoint: new Point(400.0, 0.0),
                endpoints:
                [
                    new TextSelectionPoint(new Point(4.0, 30.0), null),
                    new TextSelectionPoint(new Point(60.0, 42.0), null),
                ],
                @delegate: @delegate,
                clipboardStatus: clipboardStatus,
                lastSecondaryTapDownPosition: null)),
            mediaQuery: new MediaQueryData(
                Size: new Size(320.0, 240.0),
                DevicePixelRatio: 2.0,
                Padding: new Thickness(5.0, 0.0, 7.0, 0.0))));
#pragma warning restore CS0618
    }

    private static WidgetRenderHarness BuildDesktopToolbarHarness(
        ITextSelectionDelegate @delegate,
        ClipboardStatusNotifier clipboardStatus,
        Point? lastSecondaryTap)
    {
#pragma warning disable CS0618 // Exercises Flutter's deprecated legacy selection-controls surface.
        return new WidgetRenderHarness(Wrap(new Builder(context =>
            CupertinoDesktopTextSelectionControls.Instance.BuildToolbar(
                context,
                globalEditableRegion: new Rect(10.0, 40.0, 200.0, 60.0),
                textLineHeight: 14.0,
                selectionMidpoint: new Point(400.0, 80.0),
                endpoints:
                [
                    new TextSelectionPoint(new Point(4.0, 30.0), null),
                    new TextSelectionPoint(new Point(60.0, 42.0), null),
                ],
                @delegate: @delegate,
                clipboardStatus: clipboardStatus,
                lastSecondaryTapDownPosition: lastSecondaryTap)),
            mediaQuery: new MediaQueryData(
                Size: new Size(320.0, 240.0),
                DevicePixelRatio: 2.0,
                Padding: new Thickness(5.0, 0.0, 12.0, 0.0))));
#pragma warning restore CS0618
    }

    private static Widget Wrap(
        Widget child,
        MediaQueryData? mediaQuery = null,
        Color? selectionHandleColor = null)
    {
        return new MediaQuery(
            mediaQuery ?? new MediaQueryData(Size: new Size(320.0, 240.0)),
            new CupertinoTheme(
                new CupertinoThemeData(selectionHandleColor: selectionHandleColor),
                new Localizations(
                    locale: new Locale("en", "US"),
                    delegates:
                    [
                        DefaultCupertinoLocalizations.Delegate,
                        DefaultWidgetsLocalizations.Delegate,
                    ],
                    child: new Directionality(TextDirection.Ltr, child))));
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

    private sealed class FakeSelectionDelegate : ITextSelectionDelegate
    {
        private readonly bool _enabled;

        public FakeSelectionDelegate(TextEditingValue value, bool enabled = true)
        {
            TextEditingValue = value;
            _enabled = enabled;
        }

        public TextEditingValue TextEditingValue { get; private set; }

        public List<string> Calls { get; } = [];

        public bool CutEnabled => _enabled;

        public bool CopyEnabled => _enabled;

        public bool PasteEnabled => _enabled;

        public bool SelectAllEnabled => _enabled;

        public void UserUpdateTextEditingValue(TextEditingValue value, SelectionChangedCause? cause)
        {
            TextEditingValue = value;
        }

        public void CutSelection(SelectionChangedCause cause) => Calls.Add("cut");

        public void CopySelection(SelectionChangedCause cause) => Calls.Add("copy");

        public void PasteText(SelectionChangedCause cause) => Calls.Add("paste");

        public void SelectAll(SelectionChangedCause cause) => Calls.Add("selectAll");

        public void HideToolbar(bool hideHandles = true) => Calls.Add("hideToolbar");
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
        }

        public IReadOnlyList<T> FindWidgets<T>() where T : Widget
        {
            var widgets = new List<T>();
            Visit(_rootElement);
            return widgets;

            void Visit(Element element)
            {
                if (element.Widget is T widget)
                {
                    widgets.Add(widget);
                }

                element.VisitChildren(Visit);
            }
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
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            internal override void VisitChildren(Action<Element> visitor)
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
