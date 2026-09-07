using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/basic.dart (MouseRegion)

namespace Plumix.Widgets;

/// <summary>
/// A widget that tracks the movement of mice, and changes the cursor while a mouse is inside it.
/// Dart's `MouseRegion`.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="OnEnter"/> fires when the pointer moves into the region, a mouse is connected inside
/// it, or the region itself appears or moves under a motionless pointer. <see cref="OnExit"/> fires
/// when the pointer leaves, the mouse is disconnected, or the region moves away — but **not** when
/// the region is unmounted, because a detached render object is no longer valid for the tracker. An
/// <see cref="OnEnter"/> is therefore not always matched by an <see cref="OnExit"/>.
/// </para>
/// <para>
/// Changing only the callbacks does not repaint; changing <see cref="Cursor"/>,
/// <see cref="Opaque"/> or <see cref="HitTestBehavior"/> does, which is what makes the next
/// post-frame device update recompute the annotations.
/// </para>
/// </remarks>
public class MouseRegion : SingleChildRenderObjectWidget
{
    public MouseRegion(
        Widget? child = null,
        PointerEnterEventListener? onEnter = null,
        PointerExitEventListener? onExit = null,
        PointerHoverEventListener? onHover = null,
        MouseCursor? cursor = null,
        bool opaque = true,
        HitTestBehavior? hitTestBehavior = null,
        Key? key = null) : base(child, key)
    {
        OnEnter = onEnter;
        OnExit = onExit;
        OnHover = onHover;
        Cursor = cursor ?? MouseCursor.Defer;
        Opaque = opaque;
        HitTestBehavior = hitTestBehavior;
    }

    /// <summary>Triggered when a mouse pointer has entered this widget.</summary>
    public PointerEnterEventListener? OnEnter { get; }

    /// <summary>Triggered when a mouse pointer has exited this widget.</summary>
    public PointerExitEventListener? OnExit { get; }

    /// <summary>Triggered when a pointer moves into a position within this widget.</summary>
    public PointerHoverEventListener? OnHover { get; }

    /// <summary>
    /// The mouse cursor for mouse pointers that are hovering over the region. Defaults to
    /// <see cref="MouseCursor.Defer"/>, which passes the choice to the region behind it.
    /// </summary>
    public MouseCursor Cursor { get; }

    /// <summary>
    /// Whether this widget should prevent other <see cref="MouseRegion"/>s visually behind it from
    /// detecting the pointer. Dart's `MouseRegion.opaque`, default true.
    /// </summary>
    public bool Opaque { get; }

    /// <summary>
    /// How to behave during hit testing. Dart's `MouseRegion.hitTestBehavior`; null means
    /// <see cref="Plumix.Rendering.HitTestBehavior.Opaque"/> in the render object.
    /// </summary>
    public HitTestBehavior? HitTestBehavior { get; }

    /// <inheritdoc />
    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderMouseRegion(
            onEnter: OnEnter,
            onHover: OnHover,
            onExit: OnExit,
            cursor: Cursor,
            opaque: Opaque,
            hitTestBehavior: HitTestBehavior);
    }

    /// <inheritdoc />
    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var region = (RenderMouseRegion)renderObject;
        region.OnEnter = OnEnter;
        region.OnHover = OnHover;
        region.OnExit = OnExit;
        region.Cursor = Cursor;
        region.Opaque = Opaque;
        region.HitTestBehavior = HitTestBehavior;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        var listeners = new List<string>();
        if (OnEnter is not null)
        {
            listeners.Add("enter");
        }

        if (OnExit is not null)
        {
            listeners.Add("exit");
        }

        if (OnHover is not null)
        {
            listeners.Add("hover");
        }

        properties.Add(new IterableProperty<string>("listeners", listeners, ifEmpty: "<none>"));
        properties.Add(new DiagnosticsProperty<MouseCursor>(
            "cursor",
            Cursor,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<bool>("opaque", Opaque, defaultValue: true));
    }
}
