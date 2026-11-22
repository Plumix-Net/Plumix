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
/// <remarks>The pixels are the drawing backend's own image object, which the framework never looks into.</remarks>
public sealed class RasterImage : IDisposable
{
    private readonly SharedPixels _pixels;
    private int _disposed;

    /// <summary>
    /// Wraps a backend image of <paramref name="width"/> by <paramref name="height"/> pixels that covers
    /// <paramref name="logicalSize"/> canvas units when drawn whole.
    /// </summary>
    internal RasterImage(object native, int width, int height, Size logicalSize)
        : this(new SharedPixels(native ?? throw new ArgumentNullException(nameof(native)), width, height,
            logicalSize))
    {
    }

    private RasterImage(SharedPixels pixels)
    {
        _pixels = pixels;
    }

    /// <summary>The backend image the pixels live in.</summary>
    internal object Native => _pixels.Native;

    /// <summary>The image's size in canvas units, as <c>drawImage</c> draws it.</summary>
    internal Size LogicalSize => _pixels.LogicalSize;

    /// <summary>The number of image pixels along the image's horizontal axis.</summary>
    public int Width => _pixels.Width;

    /// <summary>The number of image pixels along the image's vertical axis.</summary>
    public int Height => _pixels.Height;

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

    private sealed class SharedPixels(object native, int width, int height, Size logicalSize)
    {
        private int _references = 1;

        public object Native { get; } = native;

        public int Width { get; } = width;

        public int Height { get; } = height;

        public Size LogicalSize { get; } = logicalSize;

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
