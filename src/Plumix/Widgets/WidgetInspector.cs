using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Canvas = Plumix.UI.Canvas;
using Path = Plumix.UI.Path;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/widget_inspector.dart

namespace Plumix.Widgets;

/// <summary>Signature for the builder callback used by
/// <see cref="WidgetInspector.ExitWidgetSelectionButtonBuilder"/>.</summary>
/// <remarks>Flutter's <c>ExitWidgetSelectionButtonBuilder</c>.</remarks>
public delegate Widget ExitWidgetSelectionButtonBuilder(
    BuildContext context,
    Action onPressed,
    string semanticsLabel,
    GlobalKey key);

/// <summary>Signature for the builder callback used by
/// <see cref="WidgetInspector.MoveExitWidgetSelectionButtonBuilder"/>.</summary>
/// <remarks>Flutter's <c>MoveExitWidgetSelectionButtonBuilder</c>.</remarks>
public delegate Widget MoveExitWidgetSelectionButtonBuilder(
    BuildContext context,
    Action onPressed,
    string semanticsLabel,
    bool usesDefaultAlignment = true);

/// <summary>Signature for the builder callback used by
/// <see cref="WidgetInspector.TapBehaviorButtonBuilder"/>.</summary>
/// <remarks>Flutter's <c>TapBehaviorButtonBuilder</c>.</remarks>
public delegate Widget TapBehaviorButtonBuilder(
    BuildContext context,
    Action onPressed,
    string semanticsLabel,
    bool selectionOnTapEnabled);

/// <summary>
/// A widget that enables inspecting the child widget's structure.
/// </summary>
/// <remarks>
/// Flutter's <c>WidgetInspector</c>. Select a location on your device or emulator and view what
/// widgets and render objects that best match the location. An outline of the selected widget and
/// terse summary information is shown on device with detailed information is shown in the
/// observatory or in IntelliJ when using the Flutter Plugin.
/// </remarks>
public sealed class WidgetInspector : StatefulWidget
{
    /// <summary>Creates a widget that enables inspection for the child.</summary>
    public WidgetInspector(
        Widget child,
        TapBehaviorButtonBuilder? tapBehaviorButtonBuilder,
        ExitWidgetSelectionButtonBuilder? exitWidgetSelectionButtonBuilder,
        MoveExitWidgetSelectionButtonBuilder? moveExitWidgetSelectionButtonBuilder,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        TapBehaviorButtonBuilder = tapBehaviorButtonBuilder;
        ExitWidgetSelectionButtonBuilder = exitWidgetSelectionButtonBuilder;
        MoveExitWidgetSelectionButtonBuilder = moveExitWidgetSelectionButtonBuilder;
    }

    /// <summary>The widget that is being inspected.</summary>
    public Widget Child { get; }

    /// <summary>A builder that is called to create the exit select-mode button.</summary>
    /// <remarks>The <c>onPressed</c> callback passed as an argument to the builder should be hooked up
    /// to the returned widget.</remarks>
    public ExitWidgetSelectionButtonBuilder? ExitWidgetSelectionButtonBuilder { get; }

    /// <summary>A builder that is called to create the button that moves the exit select-mode button
    /// to the right or left.</summary>
    public MoveExitWidgetSelectionButtonBuilder? MoveExitWidgetSelectionButtonBuilder { get; }

    /// <summary>A builder that is called to create the button that changes the default tap behavior
    /// when Select Widget mode is enabled.</summary>
    public TapBehaviorButtonBuilder? TapBehaviorButtonBuilder { get; }

    /// <inheritdoc />
    public override State CreateState() => new WidgetInspectorState();
}

/// <summary>The state of a <see cref="WidgetInspector"/>.</summary>
/// <remarks>Flutter's private <c>_WidgetInspectorState</c>.</remarks>
internal sealed class WidgetInspectorState : State<WidgetInspector>
{
    /// <summary>Distance from the edge of the bounding box for an element to consider as selecting the
    /// edge of the bounding box.</summary>
    private const double EdgeHitMargin = 2.0;

    private readonly GlobalKey _ignorePointerKey = new LabeledGlobalKey<State>(null);
    private Point? _lastPointerLocation;
    private InspectorSelection _selection = null!;
    private bool _isSelectMode;

    /// <summary>Status of the widget selection mode toggle.</summary>
    public bool IsSelectMode => _isSelectMode;

    /// <summary>The selection the overlay highlights.</summary>
    public InspectorSelection Selection => _selection;

    private static ValueNotifier<bool> SelectionOnTapEnabled =>
        WidgetsBinding.Instance.DebugWidgetInspectorSelectionOnTapEnabled;

    private bool IsSelectModeWithSelectionOnTapEnabled => _isSelectMode && SelectionOnTapEnabled.Value;

    public override void InitState()
    {
        base.InitState();

        WidgetInspectorService.Instance.Selection.AddListener(SelectionInformationChanged);
        WidgetsBinding.Instance.DebugShowWidgetInspectorOverrideNotifier.AddListener(SelectionInformationChanged);
        SelectionOnTapEnabled.AddListener(SelectionInformationChanged);
        _selection = WidgetInspectorService.Instance.Selection;
        _isSelectMode = WidgetsBinding.Instance.DebugShowWidgetInspectorOverride;
    }

    public override void Dispose()
    {
        WidgetInspectorService.Instance.Selection.RemoveListener(SelectionInformationChanged);
        WidgetsBinding.Instance.DebugShowWidgetInspectorOverrideNotifier.RemoveListener(SelectionInformationChanged);
        SelectionOnTapEnabled.RemoveListener(SelectionInformationChanged);
        base.Dispose();
    }

    private void SelectionInformationChanged()
    {
        SetState(() =>
        {
            _selection = WidgetInspectorService.Instance.Selection;
            _isSelectMode = WidgetsBinding.Instance.DebugShowWidgetInspectorOverride;
        });
    }

    private static bool HitTestHelper(
        List<RenderObject> hits,
        List<RenderObject> edgeHits,
        Point position,
        RenderObject @object,
        Matrix4 transform)
    {
        bool hit = false;
        Matrix4? inverse = Matrix4.TryInvert(transform);
        if (inverse is null)
        {
            // We cannot invert the transform. That means the object doesn't appear on
            // screen and cannot be hit.
            return false;
        }

        Point localPosition = MatrixUtils.TransformPoint(inverse, position);

        List<DiagnosticsNode> children = @object.DebugDescribeChildren();
        for (int i = children.Count - 1; i >= 0; i -= 1)
        {
            DiagnosticsNode diagnostics = children[i];
            if (diagnostics.Style == DiagnosticsTreeStyle.Offstage || diagnostics.Value is not RenderObject child)
            {
                continue;
            }

            Rect? paintClip = @object.InvokeDescribeApproximatePaintClip(child);
            if (paintClip is { } clip && !clip.Contains(localPosition))
            {
                continue;
            }

            Matrix4 childTransform = transform.Clone();
            @object.ApplyPaintTransform(child, childTransform);
            if (HitTestHelper(hits, edgeHits, position, child, childTransform))
            {
                hit = true;
            }
        }

        Rect bounds = @object.SemanticBoundsForSemantics;
        if (bounds.Contains(localPosition))
        {
            hit = true;
            // Hits that occur on the edge of the bounding box of an object are
            // given priority to provide a way to select objects that would
            // otherwise be hard to select.
            if (!bounds.Deflate(EdgeHitMargin).Contains(localPosition))
            {
                edgeHits.Add(@object);
            }
        }

        if (hit)
        {
            hits.Add(@object);
        }

        return hit;
    }

    /// <summary>Returns the list of render objects located at the given position ordered by priority.</summary>
    /// <remarks>
    /// All render objects that are not offstage that match the location are included in the list of
    /// matches. Priority is given to matches that occur at the edge of a render object's bounding box
    /// and to matches found by smaller render objects.
    /// </remarks>
    public List<RenderObject> HitTest(Point position, RenderObject root)
    {
        var regularHits = new List<RenderObject>();
        var edgeHits = new List<RenderObject>();

        HitTestHelper(regularHits, edgeHits, position, root, root.GetTransformTo(null));
        // Order matches by the size of the hit area.
        double Area(RenderObject @object)
        {
            Size size = @object.SemanticBoundsForSemantics.Size;
            return size.Width * size.Height;
        }

        List<RenderObject> sortedHits = regularHits.OrderBy(Area).ToList();
        var hits = new List<RenderObject>();
        var seen = new HashSet<RenderObject>(ReferenceEqualityComparer.Instance);
        foreach (RenderObject hit in edgeHits.Concat(sortedHits))
        {
            if (seen.Add(hit))
            {
                hits.Add(hit);
            }
        }

        return hits;
    }

    private void InspectAt(Point position)
    {
        if (!IsSelectModeWithSelectionOnTapEnabled)
        {
            return;
        }

        var ignorePointer = (RenderIgnorePointer)_ignorePointerKey.CurrentContext!.FindRenderObject()!;
        RenderObject userRender = ignorePointer.Child!;
        List<RenderObject> selected = HitTest(position, userRender);

        _selection.Candidates = WidgetInspectorModalRoutes.FilterInspectorHitCandidatesToModalRouteScope(selected);
    }

    private void HandlePanDown(DragDownDetails @event)
    {
        _lastPointerLocation = @event.GlobalPosition;
        InspectAt(@event.GlobalPosition);
    }

    private void HandlePanUpdate(DragUpdateDetails @event)
    {
        _lastPointerLocation = @event.GlobalPosition;
        InspectAt(@event.GlobalPosition);
    }

    private void HandlePanEnd(DragEndDetails details)
    {
        // If the pan ends on the edge of the window assume that it indicates the
        // pointer is being dragged off the edge of the display not a regular touch
        // on the edge of the display. If the pointer is being dragged off the edge
        // of the display we do not want to select anything. A user can still select
        // a widget that is only at the exact screen margin by tapping.
        FlutterView view = View.Of(Context);
        var bounds = new Rect(
            0,
            0,
            view.PhysicalSize.Width / view.DevicePixelRatio,
            view.PhysicalSize.Height / view.DevicePixelRatio);
        bounds = Deflate(bounds, WidgetInspectorOverlay.OffScreenMargin);
        if (!bounds.Contains(_lastPointerLocation!.Value))
        {
            _selection.Clear();
        }
        else
        {
            // Otherwise notify DevTools of the current selection.
            WidgetInspectorService.Instance.NotifyToolsOfSelection(_selection.Current, restrictToProjectFiles: true);
        }
    }

    private static Rect Deflate(Rect rect, double delta)
    {
        double width = Math.Max(0.0, rect.Width - 2 * delta);
        double height = Math.Max(0.0, rect.Height - 2 * delta);
        return new Rect(rect.X + delta, rect.Y + delta, width, height);
    }

    private void HandleTap()
    {
        if (!IsSelectModeWithSelectionOnTapEnabled)
        {
            return;
        }

        if (_lastPointerLocation is { } location)
        {
            InspectAt(location);
            WidgetInspectorService.Instance.NotifyToolsOfSelection(_selection.Current, restrictToProjectFiles: true);
        }
    }

    public override Widget Build(BuildContext context)
    {
        // Be careful changing this build method. The _InspectorOverlayLayer
        // assumes the root RenderObject for the WidgetInspector will be
        // a RenderStack containing a _RenderInspectorOverlay as a child.
        var children = new List<Widget>
        {
            new GestureDetector(
                onTap: HandleTap,
                onPanDown: HandlePanDown,
                onPanEnd: HandlePanEnd,
                onPanUpdate: HandlePanUpdate,
                behavior: HitTestBehavior.Opaque,
                excludeFromSemantics: true,
                child: new IgnorePointer(
                    ignoring: IsSelectModeWithSelectionOnTapEnabled,
                    key: _ignorePointerKey,
                    child: Widget.Child)),
            Positioned.Fill(new InspectorOverlay(_selection)),
        };
        if (_isSelectMode && Widget.ExitWidgetSelectionButtonBuilder is { } exitWidgetSelectionButtonBuilder)
        {
            children.Add(new WidgetInspectorButtonGroup(
                tapBehaviorButtonBuilder: Widget.TapBehaviorButtonBuilder,
                exitWidgetSelectionButtonBuilder: exitWidgetSelectionButtonBuilder,
                moveExitWidgetSelectionButtonBuilder: Widget.MoveExitWidgetSelectionButtonBuilder));
        }

        return new Stack(children: children);
    }
}

/// <summary>
/// Defines the visual and behavioral variants for an <see cref="InspectorButton"/>.
/// </summary>
/// <remarks>Flutter's <c>InspectorButtonVariant</c>.</remarks>
public enum InspectorButtonVariant
{
    /// <summary>A button with a filled background, typically used for primary actions.</summary>
    Filled,

    /// <summary>A button that can be toggled on or off, visually representing its state.</summary>
    Toggle,

    /// <summary>A button that displays only an icon, typically used for less prominent actions.</summary>
    IconOnly,
}

/// <summary>
/// An abstract base class for creating Material or Cupertino-styled inspector buttons.
/// </summary>
/// <remarks>
/// Flutter's <c>InspectorButton</c>. Subclasses are responsible for implementing the design-specific
/// rendering logic in the <see cref="StatelessWidget.Build"/> method and providing the
/// variant-specific colors.
/// </remarks>
public abstract class InspectorButton : StatelessWidget
{
    /// <summary>The default size of the button.</summary>
    public const double ButtonSize = 32.0;

    /// <summary>The default size of the icon in the button.</summary>
    public const double ButtonIconSize = 18.0;

    /// <summary>Creates an inspector button.</summary>
    /// <remarks>This is the base constructor used by named constructors.</remarks>
    protected InspectorButton(
        Action onPressed,
        string semanticsLabel,
        IconData icon,
        InspectorButtonVariant variant,
        GlobalKey? buttonKey = null,
        bool? toggledOn = null,
        Key? key = null) : base(key)
    {
        OnPressed = onPressed ?? throw new ArgumentNullException(nameof(onPressed));
        SemanticsLabel = semanticsLabel;
        Icon = icon;
        Variant = variant;
        ButtonKey = buttonKey;
        ToggledOn = toggledOn;
    }

    /// <summary>The callback that is called when the button is tapped.</summary>
    public Action OnPressed { get; }

    /// <summary>The semantic label for the button, used for accessibility.</summary>
    public string SemanticsLabel { get; }

    /// <summary>The icon to display within the button.</summary>
    public IconData Icon { get; }

    /// <summary>An optional key to identify the button widget.</summary>
    public GlobalKey? ButtonKey { get; }

    /// <summary>The visual variant of the button.</summary>
    public InspectorButtonVariant Variant { get; }

    /// <summary>The toggle state of the button. Only meaningful for the toggle variant.</summary>
    public bool? ToggledOn { get; }

    /// <summary>The icon size for the button, based on its <see cref="Variant"/>.</summary>
    protected double IconSizeForVariant => Variant switch
    {
        InspectorButtonVariant.IconOnly => ButtonSize,
        _ => ButtonIconSize,
    };

    /// <summary>Provides the appropriate foreground color for the button's icon.</summary>
    protected abstract Color ForegroundColor(BuildContext context);

    /// <summary>Provides the appropriate background color for the button.</summary>
    protected abstract Color BackgroundColor(BuildContext context);
}

/// <summary>
/// Mutable selection state of the inspector.
/// </summary>
/// <remarks>Flutter's <c>InspectorSelection</c>.</remarks>
public sealed class InspectorSelection : ChangeNotifier
{
    private List<RenderObject> _candidates = [];
    private int _index;
    private RenderObject? _current;
    private Element? _currentElement;

    /// <summary>Creates an instance of <see cref="InspectorSelection"/>.</summary>
    public InspectorSelection()
    {
        if (FlutterMemoryAllocations.KFlutterMemoryAllocationsEnabled)
        {
            MaybeDispatchObjectCreation(this);
        }
    }

    /// <summary>Render objects that are candidates to be selected.</summary>
    /// <remarks>Tools may wish to iterate through the list of candidates.</remarks>
    public List<RenderObject> Candidates
    {
        get => _candidates;
        set
        {
            _candidates = value;
            _index = 0;
            ComputeCurrent();
        }
    }

    /// <summary>Index within the list of candidates that is currently selected.</summary>
    public int Index
    {
        get => _index;
        set
        {
            _index = value;
            ComputeCurrent();
        }
    }

    /// <summary>Set the selection to empty.</summary>
    public void Clear()
    {
        _candidates = [];
        _index = 0;
        ComputeCurrent();
    }

    /// <summary>Clears the candidate list without changing the current selection.</summary>
    /// <remarks>Does not notify the listeners.</remarks>
    public void ClearCandidates()
    {
        if (_candidates.Count == 0)
        {
            return;
        }

        _candidates = [];
        _index = 0;
    }

    /// <summary>Selected render object typically from the <see cref="Candidates"/> list.</summary>
    /// <remarks>Setting <see cref="Candidates"/> or calling <see cref="Clear"/> resets the selection.
    /// Returns null if the selection is invalid.</remarks>
    public RenderObject? Current
    {
        get => Active ? _current : null;
        set
        {
            if (!ReferenceEquals(_current, value))
            {
                _current = value;
                _currentElement = WidgetInspectorModalRoutes.ElementForRenderObject(value);
                NotifyListeners();
            }
        }
    }

    /// <summary>Selected <see cref="Element"/> consistent with the <see cref="Current"/> selected
    /// <see cref="RenderObject"/>.</summary>
    /// <remarks>Setting <see cref="Candidates"/> or calling <see cref="Clear"/> resets the selection.
    /// Returns null if the selection is invalid.</remarks>
    public Element? CurrentElement
    {
        get => _currentElement?.DebugIsDefunct ?? true ? null : _currentElement;
        set
        {
            if (value?.DebugIsDefunct ?? false)
            {
                _currentElement = null;
                _current = null;
                NotifyListeners();
                return;
            }

            if (!ReferenceEquals(CurrentElement, value))
            {
                _currentElement = value;
                _current = value?.FindRenderObject();
                NotifyListeners();
            }
        }
    }

    private void ComputeCurrent()
    {
        if (_index < Candidates.Count)
        {
            _current = Candidates[Index];
            _currentElement = (_current?.DebugCreator as DebugCreator)?.Element;
        }
        else
        {
            _current = null;
            _currentElement = null;
        }

        NotifyListeners();
    }

    /// <summary>Whether the selected render object is attached to the tree or has gone out of scope.</summary>
    public bool Active => _current is { Attached: true };
}

/// <summary>
/// A widget that tells the widget inspector to hide its subtree from the summary tree and from
/// on-device selection.
/// </summary>
/// <remarks>
/// Flutter's <c>DisableWidgetInspectorScope</c>. Use an <see cref="EnableWidgetInspectorScope"/>
/// inside it to show part of the subtree again.
/// </remarks>
public sealed class DisableWidgetInspectorScope : ProxyWidget
{
    /// <summary>Creates a scope that hides its subtree from the widget inspector.</summary>
    public DisableWidgetInspectorScope(Widget child, Key? key = null) : base(child, key)
    {
    }

    /// <inheritdoc />
    public override Element CreateElement() => new DisableWidgetInspectorScopeProxyElement(this);
}

/// <summary>Flutter's private <c>_DisableWidgetInspectorScopeProxyElement</c>.</summary>
internal sealed class DisableWidgetInspectorScopeProxyElement(DisableWidgetInspectorScope widget)
    : ProxyElement(widget)
{
    public override void NotifyClients(ProxyWidget oldWidget)
    {
        // Do nothing.
    }
}

/// <summary>
/// A widget that tells the widget inspector to show its subtree again inside a
/// <see cref="DisableWidgetInspectorScope"/>.
/// </summary>
/// <remarks>Flutter's <c>EnableWidgetInspectorScope</c>.</remarks>
public sealed class EnableWidgetInspectorScope : ProxyWidget
{
    /// <summary>Creates a scope that shows its subtree in the widget inspector again.</summary>
    public EnableWidgetInspectorScope(Widget child, Key? key = null) : base(child, key)
    {
    }

    /// <inheritdoc />
    public override Element CreateElement() => new EnableWidgetInspectorScopeProxyElement(this);
}

/// <summary>Flutter's private <c>_EnableWidgetInspectorScopeProxyElement</c>.</summary>
internal sealed class EnableWidgetInspectorScopeProxyElement(EnableWidgetInspectorScope widget)
    : ProxyElement(widget)
{
    public override void NotifyClients(ProxyWidget oldWidget)
    {
        // Do nothing.
    }
}

/// <summary>Flutter's private <c>_InspectorOverlay</c>.</summary>
internal sealed class InspectorOverlay(InspectorSelection selection) : LeafRenderObjectWidget
{
    public InspectorSelection Selection { get; } = selection;

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderInspectorOverlay(Selection);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderInspectorOverlay)renderObject).Selection = Selection;
    }
}

/// <summary>Flutter's private <c>_RenderInspectorOverlay</c>.</summary>
internal sealed class RenderInspectorOverlay : RenderBox
{
    private InspectorSelection _selection;

    public RenderInspectorOverlay(InspectorSelection selection)
    {
        _selection = selection;
    }

    public InspectorSelection Selection
    {
        get => _selection;
        set
        {
            if (!ReferenceEquals(value, _selection))
            {
                _selection = value;
            }

            MarkNeedsPaint();
        }
    }

    protected override bool SizedByParent => true;

    public override bool AlwaysNeedsCompositing => true;

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        return constraints.Constrain(new Size(double.PositiveInfinity, double.PositiveInfinity));
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        Debug.Assert(NeedsCompositing);
        context.AddLayer(new InspectorOverlayLayer(
            overlayRect: new Rect(offset.X, offset.Y, Size.Width, Size.Height),
            selection: Selection,
            rootRenderObject: Parent));
    }
}

/// <summary>A render object, its semantic bounds and its transform to the inspector's root.</summary>
/// <remarks>Flutter's private <c>_TransformedRect</c>.</remarks>
internal sealed class InspectorTransformedRect : IEquatable<InspectorTransformedRect>
{
    public InspectorTransformedRect(RenderObject @object, RenderObject? ancestor)
    {
        Rect = @object.SemanticBoundsForSemantics;
        Transform = @object.GetTransformTo(ancestor);
    }

    public Rect Rect { get; }

    public Matrix4 Transform { get; }

    public bool Equals(InspectorTransformedRect? other) =>
        other is not null && Rect == other.Rect && Transform == other.Transform;

    public override bool Equals(object? obj) => Equals(obj as InspectorTransformedRect);

    public override int GetHashCode() => HashCode.Combine(Rect, Transform);
}

/// <summary>State describing how the inspector overlay should be rendered.</summary>
/// <remarks>
/// Flutter's private <c>_InspectorOverlayRenderState</c>. The equality operator can be used to
/// determine whether the overlay needs to be rendered again.
/// </remarks>
internal sealed class InspectorOverlayRenderState : IEquatable<InspectorOverlayRenderState>
{
    public InspectorOverlayRenderState(
        Rect overlayRect,
        InspectorTransformedRect selected,
        List<InspectorTransformedRect> candidates,
        string tooltip,
        TextDirection textDirection)
    {
        OverlayRect = overlayRect;
        Selected = selected;
        Candidates = candidates;
        Tooltip = tooltip;
        TextDirection = textDirection;
    }

    public Rect OverlayRect { get; }

    public InspectorTransformedRect Selected { get; }

    public List<InspectorTransformedRect> Candidates { get; }

    public string Tooltip { get; }

    public TextDirection TextDirection { get; }

    public bool Equals(InspectorOverlayRenderState? other) =>
        other is not null
        && OverlayRect == other.OverlayRect
        && Selected.Equals(other.Selected)
        && Candidates.SequenceEqual(other.Candidates)
        && Tooltip == other.Tooltip;

    public override bool Equals(object? obj) => Equals(obj as InspectorOverlayRenderState);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(OverlayRect);
        hash.Add(Selected);
        foreach (InspectorTransformedRect candidate in Candidates)
        {
            hash.Add(candidate);
        }

        hash.Add(Tooltip);
        return hash.ToHashCode();
    }
}

/// <summary>The top-level route helpers the inspector's hit testing scopes candidates with.</summary>
internal static class WidgetInspectorModalRoutes
{
    /// <remarks>Flutter's private <c>_elementForRenderObject</c>.</remarks>
    public static Element? ElementForRenderObject(RenderObject? @object)
    {
        object? creator = @object?.DebugCreator;
        return creator is DebugCreator debugCreator ? debugCreator.Element : null;
    }

    /// <remarks>Flutter's private <c>_modalRouteForRenderObject</c>.</remarks>
    public static ModalRoute? ModalRouteForRenderObject(RenderObject? @object)
    {
        Element? element = ElementForRenderObject(@object);
        return element is null ? null : ModalRoute.MaybeOf(element);
    }

    /// <remarks>Flutter's private <c>_inspectorHitArea</c>.</remarks>
    private static double InspectorHitArea(RenderObject @object)
    {
        Size size = @object.SemanticBoundsForSemantics.Size;
        return size.Width * size.Height;
    }

    /// <summary>The route the inspector scopes a set of hits to.</summary>
    /// <remarks>
    /// Flutter's private <c>_inspectorScopeRouteForHits</c>: the current route of any hit, else the
    /// route of the smallest hit that has one.
    /// </remarks>
    private static ModalRoute? InspectorScopeRouteForHits(List<RenderObject> hits)
    {
        foreach (RenderObject hit in hits)
        {
            ModalRoute? route = ModalRouteForRenderObject(hit);
            if (route?.IsCurrent ?? false)
            {
                return route;
            }
        }

        RenderObject? smallestHit = null;
        double smallestArea = double.PositiveInfinity;
        foreach (RenderObject hit in hits)
        {
            ModalRoute? route = ModalRouteForRenderObject(hit);
            if (route is null)
            {
                continue;
            }

            double area = InspectorHitArea(hit);
            if (area < smallestArea)
            {
                smallestArea = area;
                smallestHit = hit;
            }
        }

        if (smallestHit is not null)
        {
            return ModalRouteForRenderObject(smallestHit);
        }

        return ModalRouteForRenderObject(hits[0]);
    }

    /// <summary>Keeps the onstage hits of the scope route, smallest first.</summary>
    /// <remarks>Flutter's private <c>_filterInspectorHitCandidatesToModalRouteScope</c>.</remarks>
    public static List<RenderObject> FilterInspectorHitCandidatesToModalRouteScope(List<RenderObject> hits)
    {
        if (hits.Count == 0)
        {
            return hits;
        }

        List<RenderObject> onstageHits = hits
            .Where(hit =>
            {
                ModalRoute? route = ModalRouteForRenderObject(hit);
                return route is null || !route.Offstage;
            })
            .ToList();
        if (onstageHits.Count == 0)
        {
            return onstageHits;
        }

        ModalRoute? scopeRoute = InspectorScopeRouteForHits(onstageHits);
        return onstageHits
            .Where(hit => ReferenceEquals(ModalRouteForRenderObject(hit), scopeRoute))
            .OrderBy(InspectorHitArea)
            .ToList();
    }
}

/// <summary>The constants of the inspector overlay.</summary>
internal static class WidgetInspectorOverlay
{
    /// <summary>Flutter's <c>_kMaxTooltipLines</c>.</summary>
    public const int MaxTooltipLines = 5;

    /// <summary>Flutter's <c>_kScreenEdgeMargin</c>.</summary>
    public const double ScreenEdgeMargin = 10.0;

    /// <summary>Flutter's <c>_kTooltipPadding</c>.</summary>
    public const double TooltipPadding = 5.0;

    /// <summary>Interpret pointer up events within with this margin as indicating the pointer is
    /// moving off the device.</summary>
    /// <remarks>Flutter's <c>_kOffScreenMargin</c>.</remarks>
    public const double OffScreenMargin = 1.0;

    /// <summary>Flutter's <c>_kTooltipBackgroundColor</c>.</summary>
    public static readonly Color TooltipBackgroundColor = Color.FromARGB(230, 60, 60, 60);

    /// <summary>Flutter's <c>_kHighlightedRenderObjectFillColor</c>.</summary>
    public static readonly Color HighlightedRenderObjectFillColor = Color.FromARGB(128, 128, 128, 255);

    /// <summary>Flutter's <c>_kHighlightedRenderObjectBorderColor</c>.</summary>
    public static readonly Color HighlightedRenderObjectBorderColor = Color.FromARGB(128, 64, 64, 128);

    /// <summary>Flutter's <c>_messageStyle</c>.</summary>
    public static readonly TextStyle MessageStyle = new(Color: new Color(0xFFFFFFFF), FontSize: 10.0, Height: 1.2);
}

/// <summary>A layer that outlines the selected <see cref="RenderObject"/> and candidate render
/// objects that also match the last pointer location.</summary>
/// <remarks>
/// Flutter's private <c>_InspectorOverlayLayer</c>. This approach is horrific for performance and is
/// only used here because this is limited to debug mode.
/// </remarks>
internal sealed class InspectorOverlayLayer : Layer
{
    private InspectorOverlayRenderState? _lastState;

    /// <summary>Picture generated from <see cref="_lastState"/>.</summary>
    private Picture? _picture;

    private TextPainter? _textPainter;
    private double? _textPainterMaxWidth;

    /// <summary>The picture the last <see cref="AddToScene"/> built, for tests.</summary>
    internal Picture? DebugPicture => _picture;

    public InspectorOverlayLayer(Rect overlayRect, InspectorSelection selection, RenderObject? rootRenderObject)
    {
        OverlayRect = overlayRect;
        Selection = selection;
        RootRenderObject = rootRenderObject;
        if (!Constants.KDebugMode)
        {
            throw new FlutterError(
            [
                new ErrorSummary(
                    "The inspector should never be used in production mode due to the "
                    + "negative performance impact."),
            ]);
        }
    }

    public InspectorSelection Selection { get; set; }

    /// <summary>The rectangle in this layer's coordinate system that the overlay should occupy.</summary>
    public Rect OverlayRect { get; }

    /// <summary>Widget inspector root render object. The selection overlay will be painted with
    /// transforms relative to this render object.</summary>
    public RenderObject? RootRenderObject { get; }

    protected internal override void Dispose()
    {
        _textPainter?.Dispose();
        _textPainter = null;
        _picture?.Dispose();
        base.Dispose();
    }

    protected internal override void AddToScene(SceneBuilder builder)
    {
        if (!Selection.Active)
        {
            return;
        }

        RenderObject selected = Selection.Current!;

        if (!IsInInspectorRenderObjectTree(selected))
        {
            return;
        }

        var candidates = new List<InspectorTransformedRect>();
        foreach (RenderObject candidate in Selection.Candidates)
        {
            if (ReferenceEquals(candidate, selected)
                || !candidate.Attached
                || !IsInInspectorRenderObjectTree(candidate)
                || !ReferenceEquals(
                    WidgetInspectorModalRoutes.ModalRouteForRenderObject(candidate),
                    WidgetInspectorModalRoutes.ModalRouteForRenderObject(selected)))
            {
                continue;
            }

            candidates.Add(new InspectorTransformedRect(candidate, RootRenderObject));
        }

        var selectedRect = new InspectorTransformedRect(selected, RootRenderObject);
        string widgetName = Selection.CurrentElement!.ToStringShort();
        string width = selectedRect.Rect.Width.ToString("F1", CultureInfo.InvariantCulture);
        string height = selectedRect.Rect.Height.ToString("F1", CultureInfo.InvariantCulture);

        var state = new InspectorOverlayRenderState(
            overlayRect: OverlayRect,
            selected: selectedRect,
            tooltip: $"{widgetName} ({width} x {height})",
            textDirection: TextDirection.Ltr,
            candidates: candidates);

        if (!state.Equals(_lastState))
        {
            _lastState = state;
            _picture?.Dispose();
            _picture = BuildPicture(state);
        }

        builder.AddPicture(new Point(0, 0), _picture!);
    }

    private Picture BuildPicture(InspectorOverlayRenderState state)
    {
        var recorder = new PictureRecorder();
        var canvas = new Canvas(recorder);
        Size size = state.OverlayRect.Size;
        // The overlay rect could have an offset if the widget inspector does
        // not take all the screen.
        canvas.Translate(state.OverlayRect.Left, state.OverlayRect.Top);

        var fillPaint = new Paint
        {
            Style = PaintingStyle.Fill,
            Color = WidgetInspectorOverlay.HighlightedRenderObjectFillColor,
        };

        var borderPaint = new Paint
        {
            Style = PaintingStyle.Stroke,
            StrokeWidth = 1.0,
            Color = WidgetInspectorOverlay.HighlightedRenderObjectBorderColor,
        };

        // Highlight the selected renderObject.
        Rect selectedPaintRect = state.Selected.Rect.Deflate(0.5);
        canvas.Save();
        canvas.Transform(state.Selected.Transform);
        canvas.DrawRect(selectedPaintRect, fillPaint);
        canvas.DrawRect(selectedPaintRect, borderPaint);
        canvas.Restore();

        // Show all other candidate possibly selected elements. This helps selecting
        // render objects by selecting the edge of the bounding box shows all
        // elements the user could toggle the selection between.
        foreach (InspectorTransformedRect transformedRect in state.Candidates)
        {
            canvas.Save();
            canvas.Transform(transformedRect.Transform);
            canvas.DrawRect(transformedRect.Rect.Deflate(0.5), borderPaint);
            canvas.Restore();
        }

        Rect targetRect = MatrixUtils.TransformRect(state.Selected.Transform, state.Selected.Rect);
        if (!HasNaN(targetRect))
        {
            var target = new Point(targetRect.Left, targetRect.Center.Y);
            const double offsetFromWidget = 9.0;
            double verticalOffset = targetRect.Height / 2 + offsetFromWidget;

            PaintDescription(canvas, state.Tooltip, state.TextDirection, target, verticalOffset, size, targetRect);
        }

        // TODO(jacobr): provide an option to perform a debug paint of just the
        // selected widget.
        return recorder.EndRecording();
    }

    private static bool HasNaN(Rect rect) =>
        double.IsNaN(rect.Left) || double.IsNaN(rect.Top) || double.IsNaN(rect.Right) || double.IsNaN(rect.Bottom);

    private void PaintDescription(
        Canvas canvas,
        string message,
        TextDirection textDirection,
        Point target,
        double verticalOffset,
        Size size,
        Rect targetRect)
    {
        canvas.Save();
        double maxWidth = Math.Max(
            size.Width - 2 * (WidgetInspectorOverlay.ScreenEdgeMargin + WidgetInspectorOverlay.TooltipPadding),
            0);
        var textSpan = _textPainter?.Text as TextSpan;
        if (_textPainter is null || textSpan!.Text != message || _textPainterMaxWidth != maxWidth)
        {
            _textPainterMaxWidth = maxWidth;
            _textPainter?.Dispose();
            _textPainter = new TextPainter(
                text: new TextSpan(style: WidgetInspectorOverlay.MessageStyle, text: message),
                textDirection: textDirection,
                maxLines: WidgetInspectorOverlay.MaxTooltipLines,
                ellipsis: "...");
            _textPainter.Layout(maxWidth: maxWidth);
        }

        const double tooltipPadding = WidgetInspectorOverlay.TooltipPadding;
        var tooltipSize = new Size(
            _textPainter.Size.Width + tooltipPadding * 2,
            _textPainter.Size.Height + tooltipPadding * 2);
        Point tipOffset = PaintingGeometry.PositionDependentBox(
            size: size,
            childSize: tooltipSize,
            target: target,
            verticalOffset: verticalOffset,
            preferBelow: false);

        var tooltipBackground = new Paint
        {
            Style = PaintingStyle.Fill,
            Color = WidgetInspectorOverlay.TooltipBackgroundColor,
        };
        canvas.DrawRect(
            new Rect(tipOffset, new Point(tipOffset.X + tooltipSize.Width, tipOffset.Y + tooltipSize.Height)),
            tooltipBackground);

        double wedgeY = tipOffset.Y;
        bool tooltipBelow = tipOffset.Y > target.Y;
        if (!tooltipBelow)
        {
            wedgeY += tooltipSize.Height;
        }

        const double wedgeSize = tooltipPadding * 2;
        double wedgeX = Math.Max(tipOffset.X, target.X) + wedgeSize * 2;
        wedgeX = Math.Min(wedgeX, tipOffset.X + tooltipSize.Width - wedgeSize * 2);
        var wedge = new List<Point>
        {
            new(wedgeX - wedgeSize, wedgeY),
            new(wedgeX + wedgeSize, wedgeY),
            new(wedgeX, wedgeY + (tooltipBelow ? -wedgeSize : wedgeSize)),
        };
        var wedgePath = new Path();
        wedgePath.AddPolygon(wedge, true);
        canvas.DrawPath(wedgePath, tooltipBackground);
        _textPainter.Paint(canvas, new Point(tipOffset.X + tooltipPadding, tipOffset.Y + tooltipPadding));
        canvas.Restore();
    }

    protected internal override bool FindAnnotations<S>(
        AnnotationResult<S> result,
        Point localPosition,
        bool onlyFirst)
    {
        return false;
    }

    /// <summary>Return whether or not a render object belongs to this inspector widget tree.</summary>
    /// <remarks>
    /// The assumption is that an inspector widget tree is a sub-tree of a <see cref="RenderStack"/>
    /// whose children contain a <see cref="RenderInspectorOverlay"/>, and that the nearest such stack
    /// is <see cref="RootRenderObject"/>.
    /// </remarks>
    private bool IsInInspectorRenderObjectTree(RenderObject child)
    {
        RenderObject? current = child.Parent;
        while (current is not null)
        {
            // We found the widget inspector render object.
            if (current is RenderStack stack && HasInspectorOverlay(stack))
            {
                return ReferenceEquals(RootRenderObject, current);
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool HasInspectorOverlay(RenderStack stack)
    {
        for (RenderBox? child = stack.FirstChild; child is not null; child = stack.ChildAfter(child))
        {
            if (child is RenderInspectorOverlay)
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>Flutter's private <c>_WidgetInspectorButtonGroup</c>.</summary>
internal sealed class WidgetInspectorButtonGroup : StatefulWidget
{
    public WidgetInspectorButtonGroup(
        ExitWidgetSelectionButtonBuilder exitWidgetSelectionButtonBuilder,
        MoveExitWidgetSelectionButtonBuilder? moveExitWidgetSelectionButtonBuilder,
        TapBehaviorButtonBuilder? tapBehaviorButtonBuilder)
    {
        ExitWidgetSelectionButtonBuilder = exitWidgetSelectionButtonBuilder;
        MoveExitWidgetSelectionButtonBuilder = moveExitWidgetSelectionButtonBuilder;
        TapBehaviorButtonBuilder = tapBehaviorButtonBuilder;
    }

    public ExitWidgetSelectionButtonBuilder ExitWidgetSelectionButtonBuilder { get; }

    public MoveExitWidgetSelectionButtonBuilder? MoveExitWidgetSelectionButtonBuilder { get; }

    public TapBehaviorButtonBuilder? TapBehaviorButtonBuilder { get; }

    public override State CreateState() => new WidgetInspectorButtonGroupState();
}

/// <summary>Flutter's private <c>_WidgetInspectorButtonGroupState</c>.</summary>
internal sealed class WidgetInspectorButtonGroupState : State<WidgetInspectorButtonGroup>
{
    private const double ExitWidgetSelectionButtonMargin = 10.0;
    private const bool DefaultSelectionOnTapEnabled = true;

    private readonly GlobalKey _exitWidgetSelectionButtonKey =
        new LabeledGlobalKey<State>("Exit Widget Selection button");

    private string? _tooltipMessage;

    /// <summary>Indicates whether the button is using the default alignment based on text
    /// direction.</summary>
    /// <remarks>For LTR, the default alignment is on the left. For RTL, the default alignment is on
    /// the right.</remarks>
    private bool _usesDefaultAlignment = true;

    private static ValueNotifier<bool> SelectionOnTapEnabled =>
        WidgetsBinding.Instance.DebugWidgetInspectorSelectionOnTapEnabled;

    private Widget? SelectionOnTapButton
    {
        get
        {
            if (Widget.TapBehaviorButtonBuilder is not { } tapBehaviorButtonBuilder)
            {
                return null;
            }

            return new WidgetInspectorButton(
                button: tapBehaviorButtonBuilder(
                    Context,
                    onPressed: () => ChangeSelectionOnTapMode(),
                    semanticsLabel: "Change widget selection mode for taps",
                    selectionOnTapEnabled: SelectionOnTapEnabled.Value),
                onTooltipVisible: ChangeSelectionOnTapTooltip,
                onTooltipHidden: OnTooltipHidden);
        }
    }

    private Widget ExitWidgetSelectionButton
    {
        get
        {
            const string buttonLabel = "Exit Select Widget mode";
            return new WidgetInspectorButton(
                button: Widget.ExitWidgetSelectionButtonBuilder(
                    Context,
                    onPressed: ExitWidgetSelectionMode,
                    semanticsLabel: buttonLabel,
                    key: _exitWidgetSelectionButtonKey),
                onTooltipVisible: () => ChangeTooltipMessage(buttonLabel),
                onTooltipHidden: OnTooltipHidden);
        }
    }

    private Widget? MoveExitWidgetSelectionButton
    {
        get
        {
            if (Widget.MoveExitWidgetSelectionButtonBuilder is not { } buttonBuilder)
            {
                return null;
            }

            TextDirection textDirection = Directionality.Of(Context);

            string buttonLabel = "Move to the "
                + (_usesDefaultAlignment == (textDirection == TextDirection.Ltr) ? "right" : "left");

            return new WidgetInspectorButton(
                button: buttonBuilder(
                    Context,
                    onPressed: () =>
                    {
                        ChangeButtonGroupAlignment();
                        OnTooltipHidden();
                    },
                    semanticsLabel: buttonLabel,
                    usesDefaultAlignment: _usesDefaultAlignment),
                onTooltipVisible: () => ChangeTooltipMessage(buttonLabel),
                onTooltipHidden: OnTooltipHidden);
        }
    }

    public override Widget Build(BuildContext context)
    {
        double bottomPadding = Math.Max(ExitWidgetSelectionButtonMargin, MediaQuery.ViewPaddingOf(context).Bottom);
        var selectionModeButtons = new Column(
            children: SelectionOnTapButton is { } tapButton
                ? [tapButton, ExitWidgetSelectionButton]
                : [ExitWidgetSelectionButton]);

        var rowChildren = new List<Widget>();
        if (_usesDefaultAlignment)
        {
            rowChildren.Add(selectionModeButtons);
        }

        if (MoveExitWidgetSelectionButton is { } moveButton)
        {
            rowChildren.Add(moveButton);
        }

        if (!_usesDefaultAlignment)
        {
            rowChildren.Add(selectionModeButtons);
        }

        var buttonGroup = new Stack(
            alignment: AlignmentDirectional.TopCenter,
            children:
            [
                new CustomPaint(
                    painter: new ExitWidgetSelectionTooltipPainter(
                        tooltipMessage: _tooltipMessage,
                        buttonKey: _exitWidgetSelectionButtonKey,
                        usesDefaultAlignment: _usesDefaultAlignment)),
                new Row(
                    crossAxisAlignment: CrossAxisAlignment.End,
                    mainAxisAlignment: MainAxisAlignment.Center,
                    children: rowChildren),
            ]);

        return Positioned.Directional(
            textDirection: Directionality.Of(context),
            start: _usesDefaultAlignment ? ExitWidgetSelectionButtonMargin : null,
            end: _usesDefaultAlignment ? null : ExitWidgetSelectionButtonMargin,
            bottom: bottomPadding,
            child: buttonGroup);
    }

    private void ExitWidgetSelectionMode()
    {
        WidgetInspectorService.Instance.ChangeWidgetSelectionMode(false);
        // Reset to default selection on tap behavior on exit.
        ChangeSelectionOnTapMode(selectionOnTapEnabled: DefaultSelectionOnTapEnabled);
    }

    private void ChangeSelectionOnTapMode(bool? selectionOnTapEnabled = null)
    {
        bool newValue = selectionOnTapEnabled ?? !SelectionOnTapEnabled.Value;
        SelectionOnTapEnabled.Value = newValue;
        WidgetInspectorService.Instance.Selection.Clear();
        if (_tooltipMessage is not null)
        {
            ChangeSelectionOnTapTooltip();
        }
    }

    private void ChangeSelectionOnTapTooltip()
    {
        ChangeTooltipMessage(SelectionOnTapEnabled.Value
            ? "Disable widget selection for taps"
            : "Enable widget selection for taps");
    }

    private void ChangeButtonGroupAlignment()
    {
        if (Mounted)
        {
            SetState(() => _usesDefaultAlignment = !_usesDefaultAlignment);
        }
    }

    private void OnTooltipHidden()
    {
        ChangeTooltipMessage(null);
    }

    private void ChangeTooltipMessage(string? message)
    {
        if (Mounted)
        {
            SetState(() => _tooltipMessage = message);
        }
    }
}

/// <summary>Flutter's private <c>_WidgetInspectorButton</c>.</summary>
internal sealed class WidgetInspectorButton : StatefulWidget
{
    /// <summary>Flutter's <c>_tooltipShownOnLongPressDuration</c>.</summary>
    public static readonly TimeSpan TooltipShownOnLongPressDuration = TimeSpan.FromMilliseconds(1500);

    /// <summary>Flutter's <c>_tooltipDelayDuration</c>.</summary>
    public static readonly TimeSpan TooltipDelayDuration = TimeSpan.FromMilliseconds(100);

    public WidgetInspectorButton(Widget button, Action onTooltipVisible, Action onTooltipHidden)
    {
        Button = button;
        OnTooltipVisible = onTooltipVisible;
        OnTooltipHidden = onTooltipHidden;
    }

    public Widget Button { get; }

    public Action OnTooltipVisible { get; }

    public Action OnTooltipHidden { get; }

    public override State CreateState() => new WidgetInspectorButtonState();
}

/// <summary>Flutter's private <c>_WidgetInspectorButtonState</c>.</summary>
/// <remarks>Dart's <c>Timer</c>s are <see cref="GestureTimer"/>s, Plumix's one-shot timer.</remarks>
internal sealed class WidgetInspectorButtonState : State<WidgetInspectorButton>
{
    private GestureTimer? _tooltipVisibleTimer;
    private GestureTimer? _tooltipHiddenTimer;

    public override void Dispose()
    {
        _tooltipVisibleTimer?.Cancel();
        _tooltipVisibleTimer = null;
        _tooltipHiddenTimer?.Cancel();
        _tooltipHiddenTimer = null;
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new Stack(
            alignment: AlignmentDirectional.TopCenter,
            children:
            [
                new GestureDetector(
                    onLongPress: () =>
                    {
                        TooltipVisibleAfter(WidgetInspectorButton.TooltipDelayDuration);
                        TooltipHiddenAfter(
                            WidgetInspectorButton.TooltipShownOnLongPressDuration
                            + WidgetInspectorButton.TooltipDelayDuration);
                    },
                    child: new MouseRegion(
                        onEnter: _ => TooltipVisibleAfter(WidgetInspectorButton.TooltipDelayDuration),
                        onExit: _ => TooltipHiddenAfter(WidgetInspectorButton.TooltipDelayDuration),
                        child: Widget.Button)),
            ]);
    }

    private void TooltipVisibleAfter(TimeSpan duration)
    {
        TooltipVisibilityChangedAfter(duration, isVisible: true);
    }

    private void TooltipHiddenAfter(TimeSpan duration)
    {
        TooltipVisibilityChangedAfter(duration, isVisible: false);
    }

    private void TooltipVisibilityChangedAfter(TimeSpan duration, bool isVisible)
    {
        GestureTimer? timer = isVisible ? _tooltipVisibleTimer : _tooltipHiddenTimer;
        if (timer?.IsActive ?? false)
        {
            timer.Cancel();
        }

        if (isVisible)
        {
            _tooltipVisibleTimer = GestureTimer.Start(duration, () => Widget.OnTooltipVisible());
        }
        else
        {
            _tooltipHiddenTimer = GestureTimer.Start(duration, () => Widget.OnTooltipHidden());
        }
    }
}

/// <summary>Flutter's private <c>_ExitWidgetSelectionTooltipPainter</c>.</summary>
internal sealed class ExitWidgetSelectionTooltipPainter : CustomPainter
{
    public ExitWidgetSelectionTooltipPainter(string? tooltipMessage, GlobalKey buttonKey, bool usesDefaultAlignment)
    {
        TooltipMessage = tooltipMessage;
        ButtonKey = buttonKey;
        UsesDefaultAlignment = usesDefaultAlignment;
    }

    public string? TooltipMessage { get; }

    public GlobalKey ButtonKey { get; }

    public bool UsesDefaultAlignment { get; }

    public override void Paint(PaintingContext context, Size size)
    {
        // Do not render the tooltip if there is no message.
        if (TooltipMessage is null)
        {
            return;
        }

        // Do not render the tooltip if the exit select mode button is not rendered.
        RenderObject? buttonRenderObject = ButtonKey.CurrentContext?.FindRenderObject();
        if (buttonRenderObject is null)
        {
            return;
        }

        Canvas canvas = context.Canvas;

        // Define tooltip appearance.
        const double tooltipPadding = 4.0;
        const double tooltipSpacing = 6.0;

        var tooltipTextPainter = new TextPainter(
            text: new TextSpan(text: TooltipMessage, style: WidgetInspectorOverlay.MessageStyle),
            textDirection: TextDirection.Ltr,
            maxLines: 1,
            ellipsis: "...");
        tooltipTextPainter.Layout();

        var tooltipPaint = new Paint
        {
            Style = PaintingStyle.Fill,
            Color = WidgetInspectorOverlay.TooltipBackgroundColor,
        };

        // Determine tooltip position.
        double buttonWidth = buttonRenderObject.PaintBounds.Width;
        Size textSize = tooltipTextPainter.Size;
        double textWidth = textSize.Width;
        double textHeight = textSize.Height;
        double tooltipWidth = textWidth + (tooltipPadding * 2);
        double tooltipHeight = textHeight + (tooltipPadding * 2);

        double tooltipXOffset = UsesDefaultAlignment ? 0 - buttonWidth : 0 - (tooltipWidth - buttonWidth);
        double tooltipYOffset = 0 - tooltipHeight - tooltipSpacing;

        // Draw tooltip background.
        canvas.DrawRect(new Rect(tooltipXOffset, tooltipYOffset, tooltipWidth, tooltipHeight), tooltipPaint);

        // Draw tooltip text.
        tooltipTextPainter.Paint(
            canvas,
            new Point(tooltipXOffset + tooltipPadding, tooltipYOffset + tooltipPadding));
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate)
    {
        return oldDelegate is not ExitWidgetSelectionTooltipPainter old || TooltipMessage != old.TooltipMessage;
    }
}
