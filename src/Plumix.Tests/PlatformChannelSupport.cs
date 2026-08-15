using Plumix.UI;

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
