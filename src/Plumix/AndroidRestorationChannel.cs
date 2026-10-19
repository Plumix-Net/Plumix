using Plumix.Foundation;
using Plumix.UI;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/shell/platform/android/io/flutter/embedding/engine/systemchannels/RestorationChannel.java

namespace Plumix;

internal sealed class AndroidRestorationChannel
{
    private readonly MethodChannel _channel;
    private TaskCompletionSource<object?>? _pendingFrameworkRestorationChannelRequest;
    private bool _engineHasProvidedData;
    private bool _frameworkHasRequestedData;

    public AndroidRestorationChannel(bool waitForRestorationData, BinaryMessenger? binaryMessenger = null)
    {
        WaitForRestorationData = waitForRestorationData;
        _channel = new MethodChannel(
            SystemChannels.Restoration.Name,
            SystemChannels.Restoration.Codec,
            binaryMessenger ?? ServicesBinding.Instance.DefaultBinaryMessenger);
        _channel.SetPlatformMethodCallHandler(HandleMethodCall);
    }

    public bool WaitForRestorationData { get; }

    public byte[]? RestorationData { get; private set; }

    public void SetRestorationData(byte[]? data)
    {
        _engineHasProvidedData = true;
        if (_pendingFrameworkRestorationChannelRequest is not null)
        {
            _pendingFrameworkRestorationChannelRequest.SetResult(PackageData(data));
            _pendingFrameworkRestorationChannelRequest = null;
            RestorationData = data;
        }
        else if (_frameworkHasRequestedData)
        {
            EventLoop.RunAsync(() => PushRestorationData(data));
        }
        else
        {
            RestorationData = data;
        }
    }

    public void ClearData() => RestorationData = null;

    private async Task PushRestorationData(byte[]? data)
    {
        ByteData? reply = null;
        await _channel.BinaryMessenger.HandlePlatformMessage(
            _channel.Name,
            _channel.Codec.EncodeMethodCall(new MethodCall("push", PackageData(data))),
            value => reply = value);
        if (reply is null)
        {
            return;
        }

        try
        {
            _channel.Codec.DecodeEnvelope(reply);
            RestorationData = data;
        }
        catch (PlatformException exception)
        {
            Print.DebugPrint(
                $"Error {exception.Code} while sending restoration data to framework: {exception.ErrorMessage}");
        }
    }

    private Task<object?> HandleMethodCall(MethodCall call)
    {
        switch (call.Method)
        {
            case "put":
                RestorationData = (byte[]?)call.Arguments;
                return Task.FromResult<object?>(null);
            case "get":
                _frameworkHasRequestedData = true;
                if (_engineHasProvidedData || !WaitForRestorationData)
                {
                    return Task.FromResult<object?>(PackageData(RestorationData));
                }

                _pendingFrameworkRestorationChannelRequest = new TaskCompletionSource<object?>();
                return _pendingFrameworkRestorationChannelRequest.Task;
            default:
                throw new MissingPluginException();
        }
    }

    private static Dictionary<string, object?> PackageData(byte[]? data) => new()
    {
        ["enabled"] = true,
        ["data"] = data,
    };
}
