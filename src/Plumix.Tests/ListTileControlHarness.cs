using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Tests;

/// Hosts one `CheckboxListTile`/`RadioListTile`/`SwitchListTile` inside a themed `Material`, the way
/// `material_ui/test/*_list_tile_test.dart` wraps them, and exposes the render/semantics probes those
/// tests rely on.
internal sealed class ListTileControlHarness : IDisposable
{
    private readonly BuildOwner _owner = new();
    private readonly RootElement _root;
    private readonly PipelineOwner _pipeline;
    private readonly Size _viewSize;

    public ListTileControlHarness(
        Widget tile,
        ThemeData? theme = null,
        double width = 300.0,
        double height = 800.0,
        double viewWidth = 400.0,
        double viewHeight = 300.0)
    {
        _viewSize = new Size(viewWidth, viewHeight);
        RenderView = new RenderView();
        _pipeline = new PipelineOwner(RenderView);
        _pipeline.Attach(RenderView);
        _root = new RootElement(RenderView, Wrap(tile, theme, width, height));
        _root.Attach(_owner);
        _root.Mount(parent: null, newSlot: null);
        _owner.FlushBuild();
    }

    public RenderView RenderView { get; }

    public RenderListTile Tile => Find<RenderListTile>(RenderView)
                                  ?? throw new InvalidOperationException("No RenderListTile mounted.");

    public void Pump()
    {
        _owner.FlushBuild();
        _pipeline.RequestLayout();
        _pipeline.FlushLayout(_viewSize);
        _pipeline.FlushCompositingBits();
        _pipeline.FlushPaint();
    }

    public SemanticsNode PumpSemantics()
    {
        Pump();
        _pipeline.RequestSemanticsUpdate();
        _pipeline.FlushSemantics();
        return _pipeline.SemanticsOwner!.RootNode
               ?? throw new InvalidOperationException("The semantics tree is empty.");
    }

    public void Update(Widget tile, ThemeData? theme = null, double width = 300.0, double height = 800.0)
    {
        _root.Update(Wrap(tile, theme, width, height));
        Pump();
    }

    /// Taps the tile body well clear of the embedded control, the way Flutter's `tap(find.byType(...))`
    /// on the list tile does.
    public void TapTile(int pointer, Point? position = null)
    {
        Point target = position ?? new Point(150, 28);
        GestureBinding binding = GestureBinding.Instance;
        binding.ResetForTests();
        try
        {
            DateTime timestamp = DateTime.UtcNow;
            binding.HandlePointerEvent(
                RenderView,
                new PointerDownEvent(
                    pointer: pointer,
                    kind: PointerDeviceKind.Mouse,
                    position: target,
                    buttons: PointerButtons.Primary,
                    timestampUtc: timestamp));
            binding.HandlePointerEvent(
                RenderView,
                new PointerUpEvent(
                    pointer: pointer,
                    kind: PointerDeviceKind.Mouse,
                    position: target,
                    buttons: PointerButtons.None,
                    timestampUtc: timestamp.AddMilliseconds(20)));
        }
        finally
        {
            binding.ResetForTests();
        }
    }

    public void Dispose() => _root.Unmount();

    public static RenderParagraph? FindText(RenderObject? root, string text) =>
        FindAll<RenderParagraph>(root)
            .FirstOrDefault(paragraph => string.Equals(paragraph.PlainText, text, StringComparison.Ordinal));

    public static Color? ForegroundOf(RenderParagraph? paragraph) =>
        paragraph?.Foreground is SolidColorBrush brush ? brush.Color : null;

    public static Point GlobalOffsetOf(RenderObject renderObject)
    {
        var result = new Point();
        RenderObject? current = renderObject;
        while (current is not null)
        {
            if (current.parentData is BoxParentData parentData)
            {
                result = new Point(result.X + parentData.offset.X, result.Y + parentData.offset.Y);
            }

            current = current.Parent;
        }

        return result;
    }

    public static SemanticsNode? FindSemantics(SemanticsNode node, Func<SemanticsNode, bool> predicate)
    {
        if (predicate(node))
        {
            return node;
        }

        foreach (SemanticsNode child in node.Children)
        {
            SemanticsNode? found = FindSemantics(child, predicate);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    /// Returns the first mounted widget of type <typeparamref name="T"/>, the way Flutter's
    /// `tester.widget<T>(find.byType(T))` reads a composed child's arguments back.
    public T? FindWidget<T>() where T : Widget => FindWidgets<T>().FirstOrDefault();

    /// Returns every mounted widget of type <typeparamref name="T"/>, in depth-first order.
    public List<T> FindWidgets<T>() where T : Widget
    {
        var result = new List<T>();
        Collect(_root, result);
        return result;

        static void Collect(Element element, List<T> sink)
        {
            if (element.Widget is T typed)
            {
                sink.Add(typed);
            }

            element.VisitChildren(child => Collect(child, sink));
        }
    }

    public static T? Find<T>(RenderObject? root) where T : RenderObject => FindAll<T>(root).FirstOrDefault();

    public static List<T> FindAll<T>(RenderObject? root) where T : RenderObject
    {
        var result = new List<T>();
        Collect(root, result);
        return result;

        static void Collect(RenderObject? node, List<T> sink)
        {
            if (node is null)
            {
                return;
            }

            if (node is T typed)
            {
                sink.Add(typed);
            }

            node.VisitChildren(child => Collect(child, sink));
        }
    }

    private static Widget Wrap(Widget tile, ThemeData? theme, double width, double height)
    {
        return new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Theme(
                data: theme ?? ThemeData.Light,
                child: new MediaQuery(
                    data: new MediaQueryData(Size: new Size(width, height)),
                    child: new Align(
                        alignment: Alignment.TopLeft,
                        child: new SizedBox(
                            width: width,
                            child: new Plumix.Material.Material(child: tile))))));
    }

    private sealed class RootElement : Element, IRenderObjectHost
    {
        private readonly RenderView _view;
        private Element? _child;

        public RootElement(RenderView view, Widget widget) : base(widget) => _view = view;

        public override RenderObject? RenderObject => _child?.RenderObject;

        public override Element? RenderObjectAttachingChild => _child;

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            _child = UpdateChild(_child, Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild(force: true);
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

        public void InsertRenderObjectChild(RenderObject child, object? slot) => _view.Child = (RenderBox)child;

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
            if (ReferenceEquals(_view.Child, child))
            {
                _view.Child = null;
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
