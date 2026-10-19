using Plumix.Foundation;
using Plumix.UI;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/shell/platform/darwin/ios/framework/Source/FlutterRestorationPlugin.mm

namespace Plumix;

internal sealed class IOSRestorationPlugin
{
    private readonly bool _restorationEnabled;
    private bool _waitForData;
    private byte[]? _restorationData;
    private TaskCompletionSource<object?>? _pendingRequest;

    public IOSRestorationPlugin(bool restorationEnabled, BinaryMessenger? binaryMessenger = null)
    {
        _restorationEnabled = restorationEnabled;
        _waitForData = restorationEnabled;
        new MethodChannel(
                SystemChannels.Restoration.Name,
                SystemChannels.Restoration.Codec,
                binaryMessenger ?? ServicesBinding.Instance.DefaultBinaryMessenger)
            .SetPlatformMethodCallHandler(HandleMethodCall);
    }

    public byte[]? RestorationData
    {
        get => _restorationData;
        set
        {
            _restorationData = value?.ToArray();
            _waitForData = false;
            CompletePendingRequest();
        }
    }

    public void MarkRestorationComplete()
    {
        _waitForData = false;
        CompletePendingRequest();
    }

    public void Reset()
    {
        _pendingRequest = null;
        RestorationData = null;
    }

    private void CompletePendingRequest()
    {
        _pendingRequest?.SetResult(DataForFramework());
        _pendingRequest = null;
    }

    private Task<object?> HandleMethodCall(MethodCall call)
    {
        switch (call.Method)
        {
            case "put":
                DebugAssert(_pendingRequest is null, "Cannot put data while a get request is pending.");
                RestorationData = (byte[]?)call.Arguments;
                return Task.FromResult<object?>(null);
            case "get":
                if (!_restorationEnabled || !_waitForData)
                {
                    return Task.FromResult<object?>(DataForFramework());
                }

                DebugAssert(_pendingRequest is null, "There can only be one pending request.");
                _pendingRequest = new TaskCompletionSource<object?>();
                return _pendingRequest.Task;
            default:
                throw new MissingPluginException();
        }
    }

    private Dictionary<string, object?> DataForFramework()
    {
        var result = new Dictionary<string, object?> { ["enabled"] = _restorationEnabled };
        if (_restorationEnabled && _restorationData is not null)
        {
            result["data"] = _restorationData;
        }

        return result;
    }

    private static void DebugAssert(bool condition, string message)
    {
        if (Constants.KDebugMode && !condition)
        {
            throw new AssertionError(message);
        }
    }
}
