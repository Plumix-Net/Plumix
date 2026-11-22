using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using NImpeller;
using Plumix.Impeller;
using Plumix.Impeller.Android.Interop;

namespace Plumix.Impeller.Android;

// C#-only host infrastructure; no Dart parity source. Adapted from AvaloniaUI/NImpeller
// samples/Sandbox.Android/Rendering/GlesBackend.cs (MIT License, Copyright (c) AvaloniaUI OÜ): OpenGL ES 3
// through EGL, with the EGL and Impeller contexts outliving window surfaces.
//
// The engine's raster thread: Impeller's GLES reactor runs GL work only on the thread that created its
// context, so the EGL context, the Impeller context, the window surface, DrawDisplayList and the buffer swap
// all live on one dedicated thread. The context is created when the host starts, while the runtime and the
// framework are still starting (Impeller compiles its pipelines then, hundreds of milliseconds on a
// mid-range GPU). The UI thread only records display lists (CPU work) and hands the latest one over;
// textures it creates are queued by the reactor and uploaded here.

/// <summary>Presents Impeller display lists to an ANativeWindow from a dedicated raster thread.</summary>
internal sealed unsafe class GlesSurface : IDisposable
{
    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _contextReady = new();
    private readonly object _frameGate = new();
    private IntPtr _display;
    private IntPtr _config;
    private IntPtr _context;
    private IntPtr _glesLibrary;
    private IntPtr _eglSurface;
    private IntPtr _pbuffer;
    private ImpellerSurface? _surface;
    private (int Width, int Height) _surfaceSize;
    private PendingFrame? _pendingFrame;
    private Exception? _initFailure;

    public GlesSurface()
    {
        _thread = new Thread(Run) { Name = "Plumix raster", IsBackground = true };
        _thread.Start();
        _queue.Add(CreateContext);
    }

    /// <summary>Raised on the raster thread once the Impeller context exists.</summary>
    public event Action? ContextCreated;

    /// <summary>Whether the Impeller context exists; display lists may need it for textures.</summary>
    public bool IsContextReady => _contextReady.IsSet;

    /// <summary>Whether a window surface is attached (set on the UI thread).</summary>
    public bool IsAttached { get; private set; }

    /// <summary>
    /// Attaches the window without waiting: the raster thread makes it current after creating its context,
    /// so the UI thread can start the app meanwhile.
    /// </summary>
    public void Attach(IntPtr nativeWindow)
    {
        _queue.Add(() =>
        {
            if (_initFailure is not null)
            {
                return;
            }

            _eglSurface = Egl.eglCreateWindowSurface(_display, _config, nativeWindow, null);
            if (_eglSurface == IntPtr.Zero)
            {
                throw Egl.Failure("eglCreateWindowSurface");
            }

            if (Egl.eglMakeCurrent(_display, _eglSurface, _eglSurface, _context) == 0)
            {
                throw Egl.Failure("eglMakeCurrent");
            }
        });
        IsAttached = true;
    }

    /// <summary>
    /// Detaches the window; returns once the raster thread no longer uses it (Android requires that before
    /// surfaceDestroyed returns).
    /// </summary>
    public void Detach()
    {
        if (!IsAttached)
        {
            return;
        }

        IsAttached = false;
        lock (_frameGate)
        {
            _pendingFrame?.DisplayList.Dispose();
            _pendingFrame = null;
        }

        RunAndWait(() =>
        {
            _surface?.Dispose();
            _surface = null;
            // Keep the context current without a surface (EGL_KHR_surfaceless_context, always on Android) so
            // Impeller can still release GL objects.
            MakeCurrentWithoutWindow();
            Egl.eglDestroySurface(_display, _eglSurface);
            _eglSurface = IntPtr.Zero;
        });
    }

    /// <summary>
    /// Hands <paramref name="displayList"/> to the raster thread, which takes ownership. A frame the raster
    /// thread has not started yet is replaced; <paramref name="presented"/> runs on the raster thread after
    /// the swap.
    /// </summary>
    public void Present(ImpellerDisplayList displayList, int width, int height, Action? presented)
    {
        bool schedule;
        lock (_frameGate)
        {
            schedule = _pendingFrame is null;
            _pendingFrame?.DisplayList.Dispose();
            _pendingFrame = new PendingFrame(displayList, width, height, presented);
        }

        if (schedule)
        {
            _queue.Add(DrawPendingFrame);
        }
    }

    /// <summary>Runs <paramref name="work"/> on the raster thread and returns its result.</summary>
    public T Invoke<T>(Func<T> work)
    {
        if (Thread.CurrentThread == _thread)
        {
            return work();
        }

        T result = default!;
        RunAndWait(() => result = work());
        return result;
    }

    private void Run()
    {
        foreach (Action work in _queue.GetConsumingEnumerable())
        {
            try
            {
                work();
            }
            catch (Exception exception)
            {
                // The prototype keeps the raster thread alive; the failure shows in logcat.
                global::Android.Util.Log.Error("PlumixImpeller", $"raster thread: {exception}");
            }
        }
    }

    private void RunAndWait(Action work)
    {
        _contextReady.Wait();
        if (_initFailure is not null)
        {
            throw new InvalidOperationException("The raster thread could not create its context.", _initFailure);
        }

        using var done = new ManualResetEventSlim();
        Exception? failure = null;
        _queue.Add(() =>
        {
            try
            {
                work();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                done.Set();
            }
        });
        done.Wait();
        if (failure is not null)
        {
            throw new InvalidOperationException("Raster thread work failed.", failure);
        }
    }

    private void CreateContext()
    {
        try
        {
            _display = Egl.eglGetDisplay(IntPtr.Zero);
            if (_display == IntPtr.Zero || Egl.eglInitialize(_display, null, null) == 0)
            {
                throw Egl.Failure("eglInitialize");
            }

            // Flutter's Android embedder for Impeller (android_context_gl_impeller.cc): RGBA8888 with a 24-bit depth
            // and 8-bit stencil buffer, which Impeller clips with on the onscreen pass (without depth no clip
            // applies), and 4x MSAA when the device has it, for antialiased paths and clips.
            _config = ChooseConfig(samples: 4) ?? ChooseConfig(samples: 1)
                ?? throw Egl.Failure("eglChooseConfig");
            int* contextAttribs = stackalloc int[] { Egl.EGL_CONTEXT_CLIENT_VERSION, 3, Egl.EGL_NONE };
            _context = Egl.eglCreateContext(_display, _config, IntPtr.Zero, contextAttribs);
            if (_context == IntPtr.Zero)
            {
                throw Egl.Failure("eglCreateContext");
            }

            // Current without a window until one arrives; Impeller resolves GL entry points while it is current.
            if (!MakeCurrentWithoutWindow())
            {
                throw Egl.Failure("eglMakeCurrent");
            }

            ImpellerBackend.Context = ImpellerContext.CreateOpenGLESNew((Func<IntPtr, IntPtr>)GetProcAddress)
                ?? throw new InvalidOperationException("Impeller could not create an OpenGL ES context.");
        }
        catch (Exception exception)
        {
            _initFailure = exception;
            global::Android.Util.Log.Error("PlumixImpeller", $"raster context: {exception}");
        }
        finally
        {
            _contextReady.Set();
        }

        if (_initFailure is null)
        {
            ContextCreated?.Invoke();
        }
    }

    // EGL_KHR_surfaceless_context where the driver has it (devices); otherwise a 1x1 pbuffer, as engines fall back
    // to (the emulator's EGL answers a surfaceless eglMakeCurrent with EGL_BAD_MATCH).
    private bool MakeCurrentWithoutWindow()
    {
        if (Egl.eglMakeCurrent(_display, IntPtr.Zero, IntPtr.Zero, _context) != 0)
        {
            return true;
        }

        if (_pbuffer == IntPtr.Zero)
        {
            int* attribs = stackalloc int[] { Egl.EGL_WIDTH, 1, Egl.EGL_HEIGHT, 1, Egl.EGL_NONE };
            _pbuffer = Egl.eglCreatePbufferSurface(_display, _config, attribs);
        }

        return _pbuffer != IntPtr.Zero && Egl.eglMakeCurrent(_display, _pbuffer, _pbuffer, _context) != 0;
    }

    private IntPtr? ChooseConfig(int samples)
    {
        int* attribs = stackalloc int[]
        {
            Egl.EGL_RENDERABLE_TYPE, Egl.EGL_OPENGL_ES3_BIT,
            Egl.EGL_SURFACE_TYPE, Egl.EGL_WINDOW_BIT | Egl.EGL_PBUFFER_BIT,
            Egl.EGL_RED_SIZE, 8,
            Egl.EGL_GREEN_SIZE, 8,
            Egl.EGL_BLUE_SIZE, 8,
            Egl.EGL_ALPHA_SIZE, 8,
            Egl.EGL_DEPTH_SIZE, 24,
            Egl.EGL_STENCIL_SIZE, 8,
            Egl.EGL_SAMPLE_BUFFERS, samples > 1 ? 1 : 0,
            Egl.EGL_SAMPLES, samples > 1 ? samples : 0,
            Egl.EGL_NONE,
        };
        IntPtr config;
        int count;
        return Egl.eglChooseConfig(_display, attribs, &config, 1, &count) != 0 && count > 0 ? config : null;
    }

    private void DrawPendingFrame()
    {
        PendingFrame? frame;
        lock (_frameGate)
        {
            frame = _pendingFrame;
            _pendingFrame = null;
        }

        if (frame is null)
        {
            return;
        }

        using ImpellerDisplayList displayList = frame.DisplayList;
        if (_eglSurface == IntPtr.Zero || ImpellerBackend.Context is not { } context)
        {
            return;
        }

        if (_surface is null || _surfaceSize != (frame.Width, frame.Height))
        {
            _surface?.Dispose();
            // FBO 0 is the window's default framebuffer.
            _surface = context.SurfaceCreateWrappedFBONew(0, ImpellerPixelFormat.kImpellerPixelFormatRGBA8888,
                    new ImpellerISize(frame.Width, frame.Height))
                ?? throw new InvalidOperationException("Impeller could not wrap the default framebuffer.");
            _surfaceSize = (frame.Width, frame.Height);
        }

        _surface.DrawDisplayList(displayList);
        Egl.eglSwapBuffers(_display, _eglSurface);
        frame.Presented?.Invoke();
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
        _contextReady.Wait();
        _queue.Add(() =>
        {
            if (ImpellerBackend.Context is { } impeller)
            {
                impeller.Dispose();
                ImpellerBackend.Context = null;
            }

            if (_display != IntPtr.Zero)
            {
                Egl.eglMakeCurrent(_display, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                if (_context != IntPtr.Zero)
                {
                    Egl.eglDestroyContext(_display, _context);
                }

                if (_pbuffer != IntPtr.Zero)
                {
                    Egl.eglDestroySurface(_display, _pbuffer);
                }

                Egl.eglTerminate(_display);
            }

            if (_glesLibrary != IntPtr.Zero)
            {
                NativeLibrary.Free(_glesLibrary);
            }
        });
        _queue.CompleteAdding();
        _thread.Join();
        _queue.Dispose();
        _contextReady.Dispose();
    }

    private sealed record PendingFrame(ImpellerDisplayList DisplayList, int Width, int Height, Action? Presented);
}
