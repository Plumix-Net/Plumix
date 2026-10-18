using Plumix.Accessibility;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// C#-only test infrastructure: an AccessibilityBridge fed by a FrameworkDartTester view, so bridge and
// platform-policy tests see the semantics updates the framework really sends.

namespace Plumix.Tests;

internal sealed class AccessibilityBridgeHarness : IDisposable
{
    private SemanticsHandle? _handle;

    public AccessibilityBridgeHarness(double devicePixelRatio = 2.0)
    {
        Tester = new FrameworkDartTester(devicePixelRatio: devicePixelRatio);
        FlutterView view = Tester.RenderView.FlutterView;
        Bridge = new AccessibilityBridge(view.ViewId, () => view.DevicePixelRatio);
        view.SemanticsUpdated += Bridge.AddSemanticsUpdate;
        Bridge.Committed += Updates.Add;
    }

    public FrameworkDartTester Tester { get; }

    public AccessibilityBridge Bridge { get; }

    public List<AccessibilityTreeUpdate> Updates { get; } = [];

    public void Pump(Widget widget)
    {
        Tester.PumpWidget(new Directionality(TextDirection.Ltr, widget));
        _handle ??= Tester.EnsureSemantics();
        Tester.Pump();
    }

    public AccessibilityNode Find(string label)
    {
        AccessibilityNode? found = FindOrNull(label);
        if (found is null)
        {
            throw new InvalidOperationException($"No accessibility node is labelled \"{label}\".");
        }

        return found;
    }

    public AccessibilityNode? FindOrNull(string label)
    {
        return Bridge.Root is { } root ? Visit(root) : null;

        AccessibilityNode? Visit(AccessibilityNode node)
        {
            if (node.Label == label)
            {
                return node;
            }

            foreach (AccessibilityNode child in node.Children)
            {
                if (Visit(child) is { } match)
                {
                    return match;
                }
            }

            return null;
        }
    }

    public void Dispose()
    {
        Tester.RenderView.FlutterView.SemanticsUpdated -= Bridge.AddSemanticsUpdate;
        _handle?.Dispose();
        Tester.Dispose();
        AccessibilityBridge.ResetForTests();
        PlatformDispatcher.Instance.UpdateSemanticsEnabled(false);
        ServicesBinding.Instance.AccessibilityFocus.Value = null;
    }
}
