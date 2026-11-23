using CoreAnimation;
using CoreGraphics;
using Foundation;
using Metal;
using ObjCRuntime;
using Plumix.UI;
using UIKit;

namespace Plumix.Impeller.iOS;

// C#-only host infrastructure; no Dart parity source. The engine's FlutterView on iOS: a UIView backed by a
// CAMetalLayer (FlutterMetalLayer) that Impeller draws into, the source of touches and of the metrics the view
// controller reports (drawable size, safe area, the interface style).

/// <summary>The view Plumix renders into.</summary>
internal sealed class PlumixMetalView : UIView
{
    private readonly IosImpellerHost _host;
    private readonly WeakReference<PlumixViewController> _viewController;

    public PlumixMetalView(IosImpellerHost host, PlumixViewController viewController)
    {
        _host = host;
        _viewController = new WeakReference<PlumixViewController>(viewController);
        MultipleTouchEnabled = true;
        ContentScaleFactor = UIScreen.MainScreen.Scale;
        var layer = (CAMetalLayer)Layer;
        layer.Device = MTLDevice.SystemDefault;
        layer.PixelFormat = MTLPixelFormat.BGRA8Unorm;
        // Impeller renders into the drawable's texture with its own passes.
        layer.FramebufferOnly = false;
        layer.Opaque = true;
        host.Attach(layer);
    }

    [Export("layerClass")]
    public static Class GetLayerClass() => new(typeof(CAMetalLayer));

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        nfloat scale = ContentScaleFactor;
        var size = new CGSize(Bounds.Width * scale, Bounds.Height * scale);
        ((CAMetalLayer)Layer).DrawableSize = size;
        _host.SizeChanged((int)size.Width, (int)size.Height);
    }

    public override void SafeAreaInsetsDidChange()
    {
        base.SafeAreaInsetsDidChange();
        nfloat scale = ContentScaleFactor;
        UIEdgeInsets insets = SafeAreaInsets;
        _host.SafeAreaChanged(
            (int)(insets.Left * scale), (int)(insets.Top * scale), (int)(insets.Right * scale),
            (int)(insets.Bottom * scale));
    }

    // FlutterView.isAccessibilityElement: never an element itself; the call tells the engine an assistive technology
    // is asking.
    public override bool IsAccessibilityElement
    {
        get
        {
            if (_viewController.TryGetTarget(out PlumixViewController? viewController))
            {
                viewController.FlutterViewAccessibilityDidCall();
            }

            return false;
        }

        set { }
    }

    public override void TouchesBegan(NSSet touches, UIEvent? evt) => Dispatch(touches, evt);

    public override void TouchesMoved(NSSet touches, UIEvent? evt) => Dispatch(touches, evt);

    public override void TouchesEnded(NSSet touches, UIEvent? evt) => Dispatch(touches, evt);

    public override void TouchesCancelled(NSSet touches, UIEvent? evt) => Dispatch(touches, evt);

    private void Dispatch(NSSet touches, UIEvent? evt) =>
        _host.TouchProcessor.DispatchTouches(touches.Cast<UITouch>(), evt, this, overridden: null);
}
