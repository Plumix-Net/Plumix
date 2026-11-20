using Avalonia.Media;
using Avalonia.Media.Imaging;
using Plumix.Foundation;

namespace Plumix.UI;

// Dart parity source: flutter/engine/src/flutter/lib/ui/painting.dart (Image)
//
// dart:ui's `Image` is `RasterImage` here: `Plumix.Widgets.Image` is the widget every application
// uses, and both namespaces are imported together, so the Dart name would be ambiguous in user code
// (the same reason dart:ui's `TextStyle` is `ParagraphTextStyle`).

/// <summary>
/// Opaque handle to raw decoded image data (pixels). Obtain one from a codec, a picture or a scene;
/// each handle must be disposed. <see cref="Clone"/> creates another handle to the same pixels, which
/// are released when the last handle is disposed.
/// </summary>
public sealed class RasterImage : IDisposable
{
    private readonly SharedPixels _pixels;
    private int _disposed;

    internal RasterImage(IImage native)
        : this(new SharedPixels(native ?? throw new ArgumentNullException(nameof(native))))
    {
    }

    private RasterImage(SharedPixels pixels)
    {
        _pixels = pixels;
    }

    /// <summary>The backend image the pixels live in.</summary>
    internal IImage Native => _pixels.Native;

    /// <summary>The backend image as a bitmap; images Plumix decodes or rasterizes always are.</summary>
    internal Bitmap NativeBitmap => (Bitmap)_pixels.Native;

    /// <summary>The number of image pixels along the image's horizontal axis.</summary>
    public int Width => Native is Bitmap bitmap ? bitmap.PixelSize.Width : (int)Math.Ceiling(Native.Size.Width);

    /// <summary>The number of image pixels along the image's vertical axis.</summary>
    public int Height => Native is Bitmap bitmap ? bitmap.PixelSize.Height : (int)Math.Ceiling(Native.Size.Height);

    /// <summary>Whether this handle has been disposed.</summary>
    public bool DebugDisposed => Volatile.Read(ref _disposed) != 0;

    /// <summary>
    /// Releases this handle's resources; the pixels go away when every clone has been disposed too.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _pixels.Release();
        }
    }

    /// <summary>Creates a disposable handle to the same pixels.</summary>
    public RasterImage Clone()
    {
        if (DebugDisposed)
        {
            throw new InvalidOperationException(
                "Cannot clone a disposed image.\nThe clone() method of a previously-disposed Image was called. "
                + "Once an Image object has been disposed, it can no longer be used to create handles, "
                + "as the underlying data may have been released.");
        }

        _pixels.AddReference();
        return new RasterImage(_pixels);
    }

    /// <summary>Whether <paramref name="other"/> is a handle to the same pixels as this one.</summary>
    public bool IsCloneOf(RasterImage other) => ReferenceEquals(other._pixels, _pixels);

    /// <summary>Converts the image to a byte array in the given format.</summary>
    public Task<ByteData?> ToByteData(ImageByteFormat format = ImageByteFormat.RawRgba) =>
        ImageByteData.ToByteData(this, format);

    public override string ToString() => $"[{Width}×{Height}]";

    private sealed class SharedPixels(IImage native)
    {
        private int _references = 1;

        public IImage Native { get; } = native;

        public void AddReference()
        {
            if (Interlocked.Increment(ref _references) <= 1)
            {
                throw new ObjectDisposedException(nameof(RasterImage));
            }
        }

        public void Release()
        {
            if (Interlocked.Decrement(ref _references) == 0 && Native is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
