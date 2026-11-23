// C#-only host infrastructure; no Dart parity source.
using System.Runtime.InteropServices;

namespace Plumix.Android.Interop;

/// <summary>The slice of OpenGL ES 3 the raster thread's pixel readback needs, resolved through EGL.</summary>
internal sealed unsafe class Gles
{
    public const int GL_FRAMEBUFFER = 0x8D40;
    public const int GL_RENDERBUFFER = 0x8D41;
    public const int GL_RGBA = 0x1908;
    public const int GL_RGBA8 = 0x8058;
    public const int GL_UNSIGNED_BYTE = 0x1401;
    public const int GL_COLOR_ATTACHMENT0 = 0x8CE0;
    public const int GL_DEPTH_STENCIL_ATTACHMENT = 0x821A;
    public const int GL_DEPTH24_STENCIL8 = 0x88F0;
    public const int GL_FRAMEBUFFER_COMPLETE = 0x8CD5;
    public const int GL_PACK_ALIGNMENT = 0x0D05;

    public Gles(Func<string, IntPtr> getProcAddress)
    {
        GenFramebuffers = (delegate* unmanaged<int, uint*, void>)getProcAddress("glGenFramebuffers");
        DeleteFramebuffers = (delegate* unmanaged<int, uint*, void>)getProcAddress("glDeleteFramebuffers");
        BindFramebuffer = (delegate* unmanaged<int, uint, void>)getProcAddress("glBindFramebuffer");
        GenRenderbuffers = (delegate* unmanaged<int, uint*, void>)getProcAddress("glGenRenderbuffers");
        DeleteRenderbuffers = (delegate* unmanaged<int, uint*, void>)getProcAddress("glDeleteRenderbuffers");
        BindRenderbuffer = (delegate* unmanaged<int, uint, void>)getProcAddress("glBindRenderbuffer");
        RenderbufferStorage = (delegate* unmanaged<int, int, int, int, void>)getProcAddress("glRenderbufferStorage");
        FramebufferRenderbuffer =
            (delegate* unmanaged<int, int, int, uint, void>)getProcAddress("glFramebufferRenderbuffer");
        CheckFramebufferStatus = (delegate* unmanaged<int, int>)getProcAddress("glCheckFramebufferStatus");
        PixelStorei = (delegate* unmanaged<int, int, void>)getProcAddress("glPixelStorei");
        ReadPixels = (delegate* unmanaged<int, int, int, int, int, int, void*, void>)getProcAddress("glReadPixels");
    }

    public delegate* unmanaged<int, uint*, void> GenFramebuffers { get; }

    public delegate* unmanaged<int, uint*, void> DeleteFramebuffers { get; }

    public delegate* unmanaged<int, uint, void> BindFramebuffer { get; }

    public delegate* unmanaged<int, uint*, void> GenRenderbuffers { get; }

    public delegate* unmanaged<int, uint*, void> DeleteRenderbuffers { get; }

    public delegate* unmanaged<int, uint, void> BindRenderbuffer { get; }

    public delegate* unmanaged<int, int, int, int, void> RenderbufferStorage { get; }

    public delegate* unmanaged<int, int, int, uint, void> FramebufferRenderbuffer { get; }

    public delegate* unmanaged<int, int> CheckFramebufferStatus { get; }

    public delegate* unmanaged<int, int, void> PixelStorei { get; }

    public delegate* unmanaged<int, int, int, int, int, int, void*, void> ReadPixels { get; }
}
