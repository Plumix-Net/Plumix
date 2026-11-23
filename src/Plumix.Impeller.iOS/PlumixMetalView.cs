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

    public PlumixMetalView(IosImpellerHost host)
    {
        _host = host;
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

    // The engine also observes traitCollectionDidChange; the iOS 17 trait-change registration replaces it later.
#pragma warning disable CA1422
    public override void TraitCollectionDidChange(UITraitCollection? previousTraitCollection)
    {
        base.TraitCollectionDidChange(previousTraitCollection);
        _host.BrightnessChanged(
            TraitCollection.UserInterfaceStyle == UIUserInterfaceStyle.Dark ? Brightness.Dark : Brightness.Light);
    }
#pragma warning restore CA1422

    public override void TouchesBegan(NSSet touches, UIEvent? evt) => Dispatch(touches, evt);

    public override void TouchesMoved(NSSet touches, UIEvent? evt) => Dispatch(touches, evt);

    public override void TouchesEnded(NSSet touches, UIEvent? evt) => Dispatch(touches, evt);

    public override void TouchesCancelled(NSSet touches, UIEvent? evt) => Dispatch(touches, evt);

    private void Dispatch(NSSet touches, UIEvent? evt) =>
        _host.TouchProcessor.DispatchTouches(touches.Cast<UITouch>(), evt, this, overridden: null);
}
