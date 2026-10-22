// Dart parity source: flutter/packages/flutter/lib/src/widgets/automatic_keep_alive.dart

namespace Plumix.Widgets;

// Shared implementation for states that combine AutomaticKeepAliveClientMixin with another state base.
internal sealed class AutomaticKeepAliveClient
{
    private KeepAliveHandle? _handle;

    public void Ensure(BuildContext context)
    {
        if (_handle is not null)
        {
            return;
        }
        _handle = new KeepAliveHandle();
        new KeepAliveNotification(_handle).Dispatch(context);
    }

    public void Release()
    {
        KeepAliveHandle? handle = _handle;
        _handle = null;
        handle?.Release();
    }
}
