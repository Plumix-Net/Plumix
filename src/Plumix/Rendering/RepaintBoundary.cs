// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart

namespace Plumix.Rendering;

/// <summary>Creates a separate composited display list for its child.</summary>
public sealed class RenderRepaintBoundary : RenderProxyBox
{
    public RenderRepaintBoundary(RenderBox? child = null)
    {
        Child = child;
    }

    public override bool IsRepaintBoundary => true;
}
