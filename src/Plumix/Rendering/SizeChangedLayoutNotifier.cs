using Avalonia;

namespace Plumix.Rendering;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/size_changed_layout_notifier.dart

internal sealed class RenderSizeChangedWithCallback : RenderProxyBox
{
    private Size? _oldSize;

    public RenderSizeChangedWithCallback(Action onLayoutChangedCallback, RenderBox? child = null)
    {
        OnLayoutChangedCallback = onLayoutChangedCallback
            ?? throw new ArgumentNullException(nameof(onLayoutChangedCallback));
        Child = child;
    }

    public Action OnLayoutChangedCallback { get; }

    protected override void PerformLayout()
    {
        base.PerformLayout();
        if (_oldSize.HasValue && _oldSize.Value != Size)
        {
            OnLayoutChangedCallback();
        }

        _oldSize = Size;
    }
}
