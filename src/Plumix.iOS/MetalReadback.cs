using System.Runtime.InteropServices;
using Foundation;
using Metal;
using NImpeller;
using ObjCRuntime;

namespace Plumix.iOS;

// C#-only host infrastructure; no Dart parity source. Scene.toImage on iOS: Impeller's C API wraps only Metal drawables
// and has no pixel readback, so the scene is drawn into a stand-in drawable over a shared-storage texture. The stand-in
// follows the engine's own FlutterDrawable (shell/platform/darwin/ios/framework/Source/FlutterMetalLayer.mm), which also
// hands Impeller an NSObject instead of a CAMetalDrawable: Impeller's SurfaceMTL::Present
// (impeller/renderer/backend/metal/surface_mtl.mm) passes drawables that conform to FlutterMetalDrawable its "present
// waiter" command buffer, committed on Impeller's queue after the frame's rendering, before it commits it. Its
// completion means the texture holds the scene.

/// <summary>Draws a display list into a texture and reads it back as premultiplied RGBA8888 rows.</summary>
internal static class MetalReadback
{
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Draws <paramref name="displayList"/> into a <paramref name="width"/> x <paramref name="height"/> texture and
    /// returns its pixels top to bottom, or null when Metal or Impeller cannot. Runs on the raster thread, which owns
    /// the Impeller context's drawing.
    /// </summary>
    public static byte[]? ReadPixels(ImpellerContext context, ImpellerDisplayList displayList, int width, int height)
    {
        if (width <= 0 || height <= 0 || MTLDevice.SystemDefault is not { } device)
        {
            return null;
        }

        // The device Impeller's ContextMTL uses (MTLCreateSystemDefaultDevice). The layer's drawables are BGRA8.
        using MTLTextureDescriptor descriptor = MTLTextureDescriptor.CreateTexture2DDescriptor(
            MTLPixelFormat.BGRA8Unorm, (nuint)width, (nuint)height, mipmapped: false);
        descriptor.Usage = MTLTextureUsage.RenderTarget | MTLTextureUsage.ShaderRead;
        descriptor.StorageMode = MTLStorageMode.Shared;
        using IMTLTexture? texture = device.CreateTexture(descriptor);
        if (texture is null)
        {
            return null;
        }

        using var drawable = new ReadbackDrawable(texture);
        using (ImpellerSurface? surface = context.SurfaceCreateWrappedMetalDrawableNew(drawable.Handle))
        {
            // Both return the C API's bool.
            if (surface is null || surface.DrawDisplayList(displayList) == 0 || surface.Present() == 0)
            {
                return null;
            }
        }

        if (!drawable.WaitForCompletion(CompletionTimeout))
        {
            return null;
        }

        byte[] pixels = new byte[width * height * 4];
        unsafe
        {
            fixed (byte* destination = pixels)
            {
                texture.GetBytes((IntPtr)destination, (nuint)(width * 4),
                    MTLRegion.Create2D(0, 0, (nuint)width, (nuint)height), 0);
            }
        }

        SwapRedAndBlue(pixels);
        return pixels;
    }

    private static void SwapRedAndBlue(byte[] pixels)
    {
        for (int i = 0; i < pixels.Length; i += 4)
        {
            (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
        }
    }

    /// <summary>
    /// FlutterDrawable's shape: the texture to render into, a present that shows nothing, and flutterPrepareForPresent:
    /// to learn when Impeller's work on the texture is complete.
    /// </summary>
    [Register("PlumixReadbackDrawable")]
    private sealed class ReadbackDrawable : NSObject
    {
        // SurfaceMTL::Present checks conformance by name; the protocol lives in the Impeller framework.
        private const string FlutterMetalDrawableProtocol = "FlutterMetalDrawable";

        private readonly IMTLTexture _texture;
        private readonly ManualResetEventSlim _completed = new();
        private bool _prepared;

        public ReadbackDrawable(IMTLTexture texture)
        {
            _texture = texture;
        }

        [Export("texture")]
        public IMTLTexture Texture => _texture;

        [Export("layer")]
        public NSObject? Layer => null;

        [Export("drawableID")]
        public nuint DrawableId => 0;

        [Export("presentedTime")]
        public double PresentedTime => 0;

        [Export("present")]
        public void Present()
        {
        }

        [Export("presentAtTime:")]
        public void PresentAtTime(double presentationTime)
        {
        }

        [Export("presentAfterMinimumDuration:")]
        public void PresentAfterMinimumDuration(double duration)
        {
        }

        [Export("flutterPrepareForPresent:")]
        public void PrepareForPresent(IntPtr commandBuffer)
        {
            IMTLCommandBuffer? buffer = Runtime.GetINativeObject<IMTLCommandBuffer>(commandBuffer, false);
            if (buffer is null)
            {
                return;
            }

            _prepared = true;
            buffer.AddCompletedHandler(_ => _completed.Set());
        }

        public override bool ConformsToProtocol(NativeHandle aProtocol) =>
            Marshal.PtrToStringUTF8(protocol_getName(aProtocol)) == FlutterMetalDrawableProtocol
            || base.ConformsToProtocol(aProtocol);

        // The event is never disposed: after a timeout the completed handler can still run.
        /// <summary>Waits for Impeller's present waiter command buffer; false if Impeller never handed it over.</summary>
        public bool WaitForCompletion(TimeSpan timeout) => _prepared && _completed.Wait(timeout);

        [DllImport("/usr/lib/libobjc.dylib")]
        private static extern IntPtr protocol_getName(IntPtr protocol);
    }
}
