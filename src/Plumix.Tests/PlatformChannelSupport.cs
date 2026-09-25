using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Mirrors Flutter's <c>TestDefaultBinaryMessenger.setMockMethodCallHandler</c>: registers the platform
/// side of a channel, records every call the framework makes, and answers with a canned result.
/// </summary>
internal sealed class MockMethodCallHandler : IDisposable
{
    private readonly MethodChannel _channel;

    public MockMethodCallHandler(MethodChannel channel, Func<MethodCall, object?>? respond = null)
    {
        ArgumentNullException.ThrowIfNull(channel);
        _channel = channel;
        Respond = respond;
        _channel.SetPlatformMethodCallHandler(call =>
        {
            Log.Add(call);
            return Task.FromResult(Respond?.Invoke(call));
        });
    }

    /// <summary>Every method call the framework has sent on the channel, in order.</summary>
    public List<MethodCall> Log { get; } = [];

    /// <summary>Produces the result for a call; the default answers <c>null</c>.</summary>
    public Func<MethodCall, object?>? Respond { get; set; }

    /// <summary>The methods recorded so far, in order.</summary>
    public List<string> Methods => Log.ConvertAll(call => call.Method);

    public void Dispose() => _channel.SetPlatformMethodCallHandler(null);
}

internal sealed class MockClipboardPlatform : IDisposable
{
    private readonly MockMethodCallHandler _handler;

    public MockClipboardPlatform(string? text = null)
    {
        Text = text;
        _handler = new MockMethodCallHandler(SystemChannels.Platform, HandleCall);
    }

    public string? Text { get; set; }

    public IReadOnlyList<MethodCall> Calls => _handler.Log;

    private object? HandleCall(MethodCall call)
    {
        switch (call.Method)
        {
            case "Clipboard.setData":
                Text = ((System.Collections.IDictionary)call.Arguments!)["text"] as string;
                return null;
            case "Clipboard.getData":
                return Text is null ? null : new Dictionary<string, object?> { ["text"] = Text };
            case "Clipboard.hasStrings":
                return new Dictionary<string, object?> { ["value"] = !string.IsNullOrEmpty(Text) };
            default:
                throw new MissingPluginException(call.Method);
        }
    }

    public void Dispose() => _handler.Dispose();
}

/// <summary>
/// Focuses the first <c>Focus</c> widget below <paramref name="root"/> through its node, the way
/// Flutter's tests call <c>focusNode.requestFocus()</c>; Dart's <c>Focus</c> never takes focus from a
/// pointer.
/// </summary>
internal static class FocusTestSupport
{
    public static FocusNode RequestFocusOnFirstFocus(Element root)
    {
        FocusNode? node = null;

        void Visit(Element element)
        {
            if (node is not null)
            {
                return;
            }

            if (element.Widget is Focus)
            {
                element.VisitChildren(child => node ??= Focus.Of(child, scopeOk: true, createDependency: false));
                return;
            }

            element.VisitChildren(Visit);
        }

        Visit(root);
        Assert.NotNull(node);
        node!.RequestFocus();
        return node;
    }
}
