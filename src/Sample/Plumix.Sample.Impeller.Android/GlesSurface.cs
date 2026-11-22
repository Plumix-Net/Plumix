using System.Runtime.InteropServices;
using NImpeller;
using Plumix.Impeller;
using Plumix.Sample.Impeller.Android.Interop;

namespace Plumix.Sample.Impeller.Android;

// C#-only host infrastructure; no Dart parity source. Adapted from AvaloniaUI/NImpeller
// samples/Sandbox.Android/Rendering/GlesBackend.cs (MIT License, Copyright (c) AvaloniaUI OÜ): OpenGL ES 3
// through EGL, with the EGL and Impeller contexts outliving window surfaces.

/// <summary>Presents Impeller display lists to an ANativeWindow on the UI thread.</summary>
internal sealed unsafe class GlesSurface : IDisposable
{
    private readonly IntPtr _display;
    private readonly IntPtr _config;
    private readonly IntPtr _context;
    private IntPtr _glesLibrary;
    private IntPtr _eglSurface;
    private ImpellerSurface? _surface;
    private (int Width, int Height) _surfaceSize;

    public GlesSurface()
    {
        _display = Egl.eglGetDisplay(IntPtr.Zero);
        if (_display == IntPtr.Zero || Egl.eglInitialize(_display, null, null) == 0)
        {
            throw Egl.Failure("eglInitialize");
        }

        int* configAttribs = stackalloc int[]
        {
            Egl.EGL_RENDERABLE_TYPE, Egl.EGL_OPENGL_ES3_BIT,
            Egl.EGL_SURFACE_TYPE, Egl.EGL_WINDOW_BIT,
            Egl.EGL_RED_SIZE, 8,
            Egl.EGL_GREEN_SIZE, 8,
            Egl.EGL_BLUE_SIZE, 8,
            Egl.EGL_ALPHA_SIZE, 8,
            Egl.EGL_DEPTH_SIZE, 0,
            Egl.EGL_STENCIL_SIZE, 8,
            Egl.EGL_NONE,
        };
        IntPtr config;
        int count;
        if (Egl.eglChooseConfig(_display, configAttribs, &config, 1, &count) == 0 || count == 0)
        {
            throw Egl.Failure("eglChooseConfig");
        }

        _config = config;
        int* contextAttribs = stackalloc int[] { Egl.EGL_CONTEXT_CLIENT_VERSION, 3, Egl.EGL_NONE };
        _context = Egl.eglCreateContext(_display, _config, IntPtr.Zero, contextAttribs);
        if (_context == IntPtr.Zero)
        {
            throw Egl.Failure("eglCreateContext");
        }
    }

    public bool IsAttached => _eglSurface != IntPtr.Zero;

    public void Attach(IntPtr nativeWindow)
    {
        _eglSurface = Egl.eglCreateWindowSurface(_display, _config, nativeWindow, null);
        if (_eglSurface == IntPtr.Zero)
        {
            throw Egl.Failure("eglCreateWindowSurface");
        }

        if (Egl.eglMakeCurrent(_display, _eglSurface, _eglSurface, _context) == 0)
        {
            throw Egl.Failure("eglMakeCurrent");
        }

        // Impeller resolves GL entry points while the context is current.
        ImpellerBackend.Context ??= ImpellerContext.CreateOpenGLESNew((Func<IntPtr, IntPtr>)GetProcAddress)
            ?? throw new InvalidOperationException("Impeller could not create an OpenGL ES context.");
    }

    public void Detach()
    {
        _surface?.Dispose();
        _surface = null;
        if (_eglSurface != IntPtr.Zero)
        {
            // Keep the context current without a surface (EGL_KHR_surfaceless_context, always on Android) so
            // Impeller can still release GL objects.
            Egl.eglMakeCurrent(_display, IntPtr.Zero, IntPtr.Zero, _context);
            Egl.eglDestroySurface(_display, _eglSurface);
            _eglSurface = IntPtr.Zero;
        }
    }

    public void Present(ImpellerDisplayList displayList, int width, int height)
    {
        ImpellerContext context = ImpellerBackend.Context
            ?? throw new InvalidOperationException("No Impeller context.");
        if (_surface is null || _surfaceSize != (width, height))
        {
            _surface?.Dispose();
            // FBO 0 is the window's default framebuffer.
            _surface = context.SurfaceCreateWrappedFBONew(0, ImpellerPixelFormat.kImpellerPixelFormatRGBA8888,
                    new ImpellerISize(width, height))
                ?? throw new InvalidOperationException("Impeller could not wrap the default framebuffer.");
            _surfaceSize = (width, height);
        }

        _surface.DrawDisplayList(displayList);
        Egl.eglSwapBuffers(_display, _eglSurface);
    }

    private IntPtr GetProcAddress(IntPtr name)
    {
        IntPtr proc = Egl.eglGetProcAddress(name);
        if (proc != IntPtr.Zero)
        {
            return proc;
        }

        // eglGetProcAddress may not return core entry points on every driver.
        if (_glesLibrary == IntPtr.Zero)
        {
            NativeLibrary.TryLoad("libGLESv3.so", out _glesLibrary);
        }

        return _glesLibrary != IntPtr.Zero
               && NativeLibrary.TryGetExport(_glesLibrary, Marshal.PtrToStringAnsi(name)!, out IntPtr export)
            ? export
            : IntPtr.Zero;
    }

    public void Dispose()
    {
        Detach();
        if (ImpellerBackend.Context is { } impeller)
        {
            Egl.eglMakeCurrent(_display, IntPtr.Zero, IntPtr.Zero, _context);
            impeller.Dispose();
            ImpellerBackend.Context = null;
        }

        Egl.eglMakeCurrent(_display, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        Egl.eglDestroyContext(_display, _context);
        Egl.eglTerminate(_display);
        if (_glesLibrary != IntPtr.Zero)
        {
            NativeLibrary.Free(_glesLibrary);
        }
    }
}
