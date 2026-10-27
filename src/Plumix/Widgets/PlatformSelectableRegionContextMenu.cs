using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity sources:
// - flutter/packages/flutter/lib/src/widgets/_platform_selectable_region_context_menu_io.dart
// - flutter/packages/flutter/lib/src/widgets/_platform_selectable_region_context_menu_web.dart

namespace Plumix.Widgets;

/// <summary>Provides native browser selection menus for a selectable subtree.</summary>
/// <remarks>The Browser host owns the invisible DOM view. A transparent render proxy identifies
/// its subtree in the framework hit test, including clipping, transforms and overlay ordering.</remarks>
public sealed class PlatformSelectableRegionContextMenu : SingleChildRenderObjectWidget
{
    private static SelectionContainerDelegate? _activeClient;

    public PlatformSelectableRegionContextMenu(Widget child, Key? key = null) : base(child, key)
    {
    }

    public static void Attach(SelectionContainerDelegate client)
    {
        CheckWeb();
        _activeClient = client;
    }

    public static void Detach(SelectionContainerDelegate client)
    {
        CheckWeb();
        // Deliberately retain the pinned source's asymmetric condition (same-client detach is a no-op).
        if (!ReferenceEquals(_activeClient, client))
        {
            _activeClient = null;
        }
    }

    /// <summary>The host's synchronous DOM mousedown entry point, in view logical coordinates.
    /// A null result leaves the browser event alone; otherwise prevent its default action.</summary>
    public static PlatformSelectionContextMenuResult? HandleMouseDown(RenderView view, Point position, int button)
    {
        CheckWeb();
        using var scope = EventLoop.EnterFrameworkThread();
        var result = new HitTestResult();
        view.HitTest(result, position);
        RenderPlatformSelectionContextMenu? surface = result.Path
            .Select(entry => entry.Target).OfType<RenderPlatformSelectionContextMenu>().FirstOrDefault();
        if (surface is null)
        {
            return null;
        }

        if (button != 2 || _activeClient is not { SelectionContainerContext: not null } client)
        {
            return new(false, string.Empty);
        }

        Point local = surface.GlobalToLocal(position);
        Point global = MatrixUtils.TransformPoint(client.GetTransformTo(null), local);
        client.DispatchSelectionEvent(new SelectWordSelectionEvent(global));
        return new(true, client.GetSelectedContent()?.PlainText ?? string.Empty);
    }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        CheckWeb();
        return new RenderPlatformSelectionContextMenu();
    }

    private static void CheckWeb()
    {
        if (!PlatformDefaults.IsWeb)
        {
            throw new NotImplementedException("Native selection context menus are only supported on the web.");
        }
    }

    internal static void ResetForTests() => _activeClient = null;
}

/// <summary>The invisible native selection view's response to a DOM mousedown.</summary>
public sealed record PlatformSelectionContextMenuResult(bool SelectText, string Text);

internal sealed class RenderPlatformSelectionContextMenu()
    : RenderProxyBoxWithHitTestBehavior(HitTestBehavior.Translucent);
