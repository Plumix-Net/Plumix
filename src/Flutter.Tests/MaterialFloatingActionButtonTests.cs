using System.Linq;
using Avalonia;
using Avalonia.Media;
using Flutter.Material;
using Flutter.Rendering;
using Flutter.UI;
using Flutter.Widgets;
using Xunit;

namespace Flutter.Tests;

public sealed class MaterialFloatingActionButtonTests
{
    [Fact]
    public void FloatingActionButton_DefaultM3_UsesThemePrimaryContainerAndOnPrimaryContainerColors()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedIconTheme = null;
        var theme = ThemeData.Light with
        {
            PrimaryContainerColor = Colors.Moccasin,
            OnPrimaryContainerColor = Colors.MediumBlue,
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new FloatingActionButton(
                    child: new CaptureIconThemeWidget(iconTheme => capturedIconTheme = iconTheme),
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);

        Assert.NotNull(decorated);
        Assert.Equal(Colors.Moccasin, decorated!.Decoration.Color);
        Assert.NotNull(capturedIconTheme);
        Assert.Equal(Colors.MediumBlue, capturedIconTheme!.Color);
        Assert.Equal(24, capturedIconTheme.Size);
    }

    [Fact]
    public void FloatingActionButton_DefaultM3_UsesRegular56ConstraintAndRounded16Shape()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: new FloatingActionButton(
                    child: new Icon(Icons.Add),
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var constrainedBox = FindDescendant<RenderConstrainedBox>(renderRoot);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);

        Assert.NotNull(constrainedBox);
        Assert.Equal(56, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(56, constrainedBox.AdditionalConstraints.MaxWidth);
        Assert.Equal(56, constrainedBox.AdditionalConstraints.MinHeight);
        Assert.Equal(56, constrainedBox.AdditionalConstraints.MaxHeight);

        Assert.NotNull(decorated);
        Assert.Equal(BorderRadius.Circular(16), decorated!.Decoration.BorderRadius);
    }

    [Fact]
    public void FloatingActionButton_Small_Uses40Constraint()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: FloatingActionButton.Small(
                    child: new Icon(Icons.Add),
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(40, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(40, constrainedBox.AdditionalConstraints.MaxWidth);
        Assert.Equal(40, constrainedBox.AdditionalConstraints.MinHeight);
        Assert.Equal(40, constrainedBox.AdditionalConstraints.MaxHeight);
    }

    [Fact]
    public void FloatingActionButton_Large_Uses96ConstraintAndIconSize36()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedIconTheme = null;
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: FloatingActionButton.Large(
                    child: new CaptureIconThemeWidget(iconTheme => capturedIconTheme = iconTheme),
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var constrainedBox = FindDescendant<RenderConstrainedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(constrainedBox);
        Assert.Equal(96, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(96, constrainedBox.AdditionalConstraints.MaxWidth);
        Assert.Equal(96, constrainedBox.AdditionalConstraints.MinHeight);
        Assert.Equal(96, constrainedBox.AdditionalConstraints.MaxHeight);
        Assert.NotNull(capturedIconTheme);
        Assert.Equal(36, capturedIconTheme!.Size);
    }

    [Fact]
    public void FloatingActionButton_Extended_WithIcon_UsesHeight56AndDirectionalPadding()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: FloatingActionButton.Extended(
                    label: new Text("Create"),
                    icon: new Icon(Icons.Add),
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var constrainedBox = FindDescendant<RenderConstrainedBox>(renderRoot);
        var paddings = FindDescendants<RenderPadding>(renderRoot);
        var hasExpectedPadding = paddings.Any(p => p.Padding == new Thickness(16, 0, 20, 0));
        var label = FindDescendants<RenderParagraph>(renderRoot).FirstOrDefault(p => p.Text == "Create");

        Assert.NotNull(constrainedBox);
        Assert.Equal(56, constrainedBox!.AdditionalConstraints.MinHeight);
        Assert.Equal(56, constrainedBox.AdditionalConstraints.MaxHeight);
        Assert.True(hasExpectedPadding);
        Assert.NotNull(label);
    }

    [Fact]
    public void FloatingActionButton_Extended_WithoutIcon_UsesStart20End20Padding()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light,
                child: FloatingActionButton.Extended(
                    label: new Text("Create"),
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var paddings = FindDescendants<RenderPadding>(RequireRenderObject<RenderObject>(root.ChildElement));
        var hasExpectedPadding = paddings.Any(p => p.Padding == new Thickness(20, 0, 20, 0));
        Assert.True(hasExpectedPadding);
    }

    [Fact]
    public void FloatingActionButton_ThemeDefaults_ApplyWhenWidgetOverridesMissing()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedIconTheme = null;
        var theme = ThemeData.Light with
        {
            FloatingActionButtonTheme = new FloatingActionButtonThemeData(
                ForegroundColor: Colors.White,
                BackgroundColor: Colors.Orange,
                SizeConstraints: TightConstraints(60, 60)),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new FloatingActionButton(
                    child: new CaptureIconThemeWidget(iconTheme => capturedIconTheme = iconTheme),
                    onPressed: () => { })));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var constrainedBox = FindDescendant<RenderConstrainedBox>(renderRoot);
        var decorated = FindDescendant<RenderDecoratedBox>(renderRoot);

        Assert.NotNull(constrainedBox);
        Assert.Equal(60, constrainedBox!.AdditionalConstraints.MinWidth);
        Assert.Equal(60, constrainedBox.AdditionalConstraints.MaxWidth);
        Assert.NotNull(decorated);
        Assert.Equal(Colors.Orange, decorated!.Decoration.Color);
        Assert.NotNull(capturedIconTheme);
        Assert.Equal(Colors.White, capturedIconTheme!.Color);
    }

    [Fact]
    public void FloatingActionButton_WidgetColors_OverrideThemeDefaults()
    {
        var owner = new BuildOwner();
        IconThemeData? capturedIconTheme = null;
        var theme = ThemeData.Light with
        {
            FloatingActionButtonTheme = new FloatingActionButtonThemeData(
                ForegroundColor: Colors.White,
                BackgroundColor: Colors.Green),
        };

        var root = new TestRootElement(
            new Theme(
                data: theme,
                child: new FloatingActionButton(
                    child: new CaptureIconThemeWidget(iconTheme => capturedIconTheme = iconTheme),
                    onPressed: () => { },
                    foregroundColor: Colors.Yellow,
                    backgroundColor: Colors.Purple)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(Colors.Purple, decorated!.Decoration.Color);
        Assert.NotNull(capturedIconTheme);
        Assert.Equal(Colors.Yellow, capturedIconTheme!.Color);
    }

    [Fact]
    public void FloatingActionButton_HoverAndPressed_UseConfiguredElevations()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { ShadowColor = Colors.Black },
                child: new FloatingActionButton(
                    child: new Icon(Icons.Add),
                    onPressed: () => { },
                    elevation: 2,
                    hoverElevation: 4,
                    highlightElevation: 7)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
        var defaultDecorated = FindDescendant<RenderDecoratedBox>(renderRoot);
        Assert.NotNull(defaultDecorated);
        Assert.Equal(2, RequirePrimaryShadow(defaultDecorated!).OffsetY);

        var hoverListener = FindHoverPointerListener(renderRoot);
        Assert.NotNull(hoverListener);
        hoverListener!.HandleEvent(
            new PointerEnterEvent(
                pointer: 700,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.None,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(hoverListener, new Point(10, 8)));
        owner.FlushBuild();

        var hoveredDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(hoveredDecorated);
        Assert.Equal(4, RequirePrimaryShadow(hoveredDecorated!).OffsetY);

        var interactiveListener = FindInteractivePointerListener(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(interactiveListener);
        interactiveListener!.HandleEvent(
            new PointerDownEvent(
                pointer: 700,
                kind: PointerDeviceKind.Mouse,
                position: new Point(10, 8),
                buttons: PointerButtons.Primary,
                timestampUtc: DateTime.UtcNow),
            new BoxHitTestEntry(interactiveListener, new Point(10, 8)));
        owner.FlushBuild();

        var pressedDecorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(pressedDecorated);
        Assert.Equal(7, RequirePrimaryShadow(pressedDecorated!).OffsetY);
    }

    [Fact]
    public void FloatingActionButton_Disabled_FallsBackToBaseElevationWhenDisabledElevationMissing()
    {
        var owner = new BuildOwner();
        var root = new TestRootElement(
            new Theme(
                data: ThemeData.Light with { ShadowColor = Colors.Black },
                child: new FloatingActionButton(
                    child: new Icon(Icons.Add),
                    onPressed: null,
                    elevation: 5)));

        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        var decorated = FindDescendant<RenderDecoratedBox>(RequireRenderObject<RenderObject>(root.ChildElement));
        Assert.NotNull(decorated);
        Assert.Equal(5, RequirePrimaryShadow(decorated!).OffsetY);
    }

    [Fact]
    public void FloatingActionButton_Tooltip_ShowsOnPointerEnter_AndHidesOnPointerExit()
    {
        Scheduler.ResetForTests();
        try
        {
            var owner = new BuildOwner();
            var root = new TestRootElement(
                new Theme(
                    data: ThemeData.Light,
                    child: new FloatingActionButton(
                        child: new Icon(Icons.Add),
                        tooltip: "Create item",
                        onPressed: () => { })));

            root.Attach(owner);
            root.Mount(parent: null, newSlot: null);
            owner.FlushBuild();

            var renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
            Assert.Null(FindParagraphByText(renderRoot, "Create item"));

            var tooltipListener = FindTooltipHoverPointerListener(renderRoot);
            Assert.NotNull(tooltipListener);
            tooltipListener!.HandleEvent(
                new PointerEnterEvent(
                    pointer: 701,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(10, 8),
                    buttons: PointerButtons.None,
                    timestampUtc: DateTime.UtcNow),
                new BoxHitTestEntry(tooltipListener, new Point(10, 8)));
            owner.FlushBuild();

            renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
            Assert.NotNull(FindParagraphByText(renderRoot, "Create item"));

            tooltipListener = FindTooltipHoverPointerListener(renderRoot);
            Assert.NotNull(tooltipListener);
            tooltipListener!.HandleEvent(
                new PointerExitEvent(
                    pointer: 701,
                    kind: PointerDeviceKind.Mouse,
                    position: new Point(180, 8),
                    buttons: PointerButtons.None,
                    timestampUtc: DateTime.UtcNow),
                new BoxHitTestEntry(tooltipListener, new Point(180, 8)));
            Scheduler.PumpFrameForTests(TimeSpan.FromSeconds(Scheduler.CurrentSeconds + 0.50));
            owner.FlushBuild();

            renderRoot = RequireRenderObject<RenderObject>(root.ChildElement);
            Assert.Null(FindParagraphByText(renderRoot, "Create item"));
        }
        finally
        {
            Scheduler.ResetForTests();
        }
    }

    private static BoxConstraints TightConstraints(double width, double height)
    {
        return new BoxConstraints(
            MinWidth: width,
            MaxWidth: width,
            MinHeight: height,
            MaxHeight: height);
    }

    private static T RequireRenderObject<T>(Element? element) where T : RenderObject
    {
        Assert.NotNull(element);
        var renderObject = element!.RenderObject;
        return Assert.IsAssignableFrom<T>(renderObject);
    }

    private static T? FindDescendant<T>(RenderObject? root) where T : RenderObject
    {
        if (root is null)
        {
            return null;
        }

        if (root is T target)
        {
            return target;
        }

        T? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindDescendant<T>(child);
        });
        return result;
    }

    private static List<T> FindDescendants<T>(RenderObject? root) where T : RenderObject
    {
        var results = new List<T>();
        CollectDescendants(root, results);
        return results;
    }

    private static void CollectDescendants<T>(RenderObject? root, List<T> results) where T : RenderObject
    {
        if (root is null)
        {
            return;
        }

        if (root is T target)
        {
            results.Add(target);
        }

        root.VisitChildren(child => CollectDescendants(child, results));
    }

    private static RenderPointerListener? FindInteractivePointerListener(RenderObject? root)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderPointerListener listener
            && listener.OnPointerDown != null
            && listener.OnPointerUp != null
            && listener.OnPointerCancel != null)
        {
            return listener;
        }

        RenderPointerListener? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindInteractivePointerListener(child);
        });
        return result;
    }

    private static RenderPointerListener? FindHoverPointerListener(RenderObject? root)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderPointerListener listener
            && listener.OnPointerEnter != null
            && listener.OnPointerExit != null)
        {
            return listener;
        }

        RenderPointerListener? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindHoverPointerListener(child);
        });
        return result;
    }

    private static RenderPointerListener? FindTooltipHoverPointerListener(RenderObject? root)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderPointerListener listener
            && listener.OnPointerEnter != null
            && listener.OnPointerExit != null
            && listener.OnPointerDown != null)
        {
            return listener;
        }

        RenderPointerListener? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindTooltipHoverPointerListener(child);
        });
        return result;
    }

    private static RenderParagraph? FindParagraphByText(RenderObject? root, string text)
    {
        if (root is null)
        {
            return null;
        }

        if (root is RenderParagraph paragraph && paragraph.Text == text)
        {
            return paragraph;
        }

        RenderParagraph? result = null;
        root.VisitChildren(child =>
        {
            if (result is not null)
            {
                return;
            }

            result = FindParagraphByText(child, text);
        });
        return result;
    }

    private static BoxShadow RequirePrimaryShadow(RenderDecoratedBox decorated)
    {
        Assert.True(decorated.Decoration.BoxShadows.HasValue);
        var shadows = decorated.Decoration.BoxShadows!.Value;
        Assert.True(shadows.Count > 0);
        return shadows[0];
    }

    private sealed class CaptureIconThemeWidget : StatelessWidget
    {
        private readonly Action<IconThemeData> _capture;

        public CaptureIconThemeWidget(Action<IconThemeData> capture)
        {
            _capture = capture;
        }

        public override Widget Build(BuildContext context)
        {
            _capture(IconTheme.Of(context));
            return new SizedBox(width: 12, height: 12);
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        public Element? ChildElement => _child;

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

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        internal override void Unmount()
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
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
            if (!Equals(oldSlot, newSlot))
            {
                throw new InvalidOperationException("TestRootElement does not support slot moves.");
            }
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (slot != null)
            {
                throw new InvalidOperationException("TestRootElement expects null slot.");
            }
        }
    }
}
