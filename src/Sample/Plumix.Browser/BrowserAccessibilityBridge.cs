using System;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia.Threading;
using Plumix;
using Plumix.Accessibility;
using Plumix.UI;

// C#-only infrastructure: the browser host's accessibility bridge. It renders a PlumixHost's semantics
// tree as an ARIA DOM overlay through wwwroot/plumix-semantics.js, the way the web engine's
// EngineSemanticsOwner does (lib/web_ui/lib/src/engine/semantics/semantics.dart), with the element
// description decided by Plumix.Accessibility.WebSemantics.

internal static partial class BrowserAccessibilityBridge
{
    private const string ModuleName = "plumix-semantics";
    private static AccessibilityBridge? s_bridge;
    private static bool s_rendered;

    /// <summary>
    /// Shows the "Enable accessibility" placeholder and mirrors <paramref name="host"/> once enabled.
    /// </summary>
    public static async Task AttachAsync(PlumixHost host, string containerId)
    {
        await JSHost.ImportAsync(ModuleName, "./plumix-semantics.js");
        s_bridge = host.AccessibilityBridge;
        Initialize(containerId, HandleAction, HandleEnable);
        s_bridge.Committed += HandleCommitted;
        s_bridge.Cleared += HandleCleared;
        s_bridge.AnnouncementRequested += static announcement =>
            Announce(announcement.Message, announcement.Assertiveness == Assertiveness.Assertive);
    }

    [JSImport("initialize", ModuleName)]
    private static partial void Initialize(
        string containerId,
        [JSMarshalAs<JSType.Function<JSType.Number, JSType.String, JSType.String>>]
        Action<int, string, string> onAction,
        [JSMarshalAs<JSType.Function>] Action onEnable);

    [JSImport("update", ModuleName)]
    private static partial void Update(string json);

    [JSImport("clear", ModuleName)]
    private static partial void ClearTree();

    [JSImport("announce", ModuleName)]
    private static partial void Announce(string message, bool assertive);

    [JSImport("removePlaceholder", ModuleName)]
    private static partial void RemovePlaceholder();

    // The placeholder was activated: turn semantics on, then render the first tree that arrives.
    private static void HandleEnable() => Dispatcher.UIThread.Post(static () => s_bridge?.Activate());

    private static void HandleCommitted(AccessibilityTreeUpdate update)
    {
        AccessibilityBridge bridge = s_bridge!;
        if (!bridge.IsActive)
        {
            // The web engine's auto-enable: a semantics update while disabled means the application
            // asked for semantics itself.
            RemovePlaceholder();
            bridge.Activate();
        }

        if (!s_rendered)
        {
            s_rendered = true;
            Update(WebSemantics.WriteUpdate(bridge, WebSemantics.AllNodes(bridge), []));
            return;
        }

        Update(WebSemantics.WriteUpdate(
            bridge,
            update.UpdatedNodes,
            update.RemovedNodes.Select(static node => node.Id)));
    }

    private static void HandleCleared()
    {
        s_rendered = false;
        ClearTree();
    }

    private static void HandleAction(int nodeId, string name, string argument)
    {
        if (WebSemantics.ParseAction(name, argument) is not { } action)
        {
            return;
        }

        Dispatcher.UIThread.Post(() => s_bridge?.DispatchAction(nodeId, action.Action, action.Arguments));
    }
}
