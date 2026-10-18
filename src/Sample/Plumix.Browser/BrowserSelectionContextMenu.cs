using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Plumix;
using Plumix.UI;
using Plumix.Widgets;

// C#-only infrastructure: DOM adapter for widgets/_platform_selectable_region_context_menu_web.dart.
// Avalonia has no HtmlElementView embedder; the framework hit test identifies the native view's subtree.
internal static partial class BrowserSelectionContextMenu
{
    private const string Module = "plumix-selection-context-menu";

    private static int s_viewId;

    public static async Task InitializeAsync(PlumixHost host, string containerId)
    {
        await JSHost.ImportAsync(Module, "./plumix-selection-context-menu.js");
        s_viewId = host.RootFlutterView.ViewId;
        Initialize(containerId, HandleMouseDown);
        SystemChannels.ContextMenu.SetPlatformMethodCallHandler(call =>
        {
            switch (call.Method)
            {
                case "enableContextMenu":
                    SetEnabled(true);
                    break;
                case "disableContextMenu":
                    SetEnabled(false);
                    break;
                default:
                    throw new MissingPluginException();
            }

            return Task.FromResult<object?>(null);
        });
    }

    [JSImport("initialize", Module)]
    private static partial void Initialize(
        string containerId,
        [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number, JSType.Number, JSType.String>>]
        Func<double, double, int, string?> callback);

    [JSImport("setEnabled", Module)]
    private static partial void SetEnabled(bool enabled);

    private static string? HandleMouseDown(double x, double y, int button)
    {
        RenderView? view = RendererBinding.Instance.RenderViews
            .FirstOrDefault(view => view.FlutterView.ViewId == s_viewId);
        if (view is null || !BrowserContextMenu.Enabled)
        {
            return null;
        }

        PlatformSelectionContextMenuResult? result =
            PlatformSelectableRegionContextMenu.HandleMouseDown(view, new Point(x, y), button);
        if (result is null)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartObject();
        writer.WriteBoolean("selectText", result.SelectText);
        writer.WriteString("text", result.Text);
        writer.WriteEndObject();
        writer.Flush();
        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
