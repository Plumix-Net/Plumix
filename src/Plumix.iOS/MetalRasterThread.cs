using System.Collections.Concurrent;
using CoreAnimation;
using NImpeller;
using Plumix.Impeller;

namespace Plumix.iOS;

// C#-only host infrastructure; no Dart parity source. The engine's raster thread on iOS: the UI thread records each
// frame's display list (CPU work) and hands the latest one over; this thread takes the layer's next drawable, draws the
// list through Impeller's Metal backend and has Impeller present it, so waiting for a drawable or encoding GPU work never blocks
// the UI thread. Metal and CAMetalLayer.nextDrawable are thread-safe, so unlike the Android GLES surface no context is
// bound to the thread. As on Android, a frame the raster thread has not started yet is replaced by the newer one.

/// <summary>Presents Impeller display lists to a CAMetalLayer from a dedicated raster thread.</summary>
internal sealed class MetalRasterThread : IDisposable
{
    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _thread;
    private readonly object _frameGate = new();
    private PendingFrame? _pendingFrame;

    public MetalRasterThread()
    {
        _thread = new Thread(Run) { Name = "Plumix raster", IsBackground = true };
        _thread.Start();
    }

    /// <summary>
    /// Hands <paramref name="displayList"/> to the raster thread, which takes ownership and draws it into the next
    /// drawable of <paramref name="layer"/>; <paramref name="presented"/> runs on the raster thread after the present.
    /// </summary>
    public void Present(CAMetalLayer layer, ImpellerDisplayList displayList, Action? presented)
    {
        bool schedule;
        lock (_frameGate)
        {
            schedule = _pendingFrame is null;
            _pendingFrame?.DisplayList.Dispose();
            _pendingFrame = new PendingFrame(layer, displayList, presented);
        }

        if (schedule)
        {
            _queue.Add(DrawPendingFrame);
        }
    }

    /// <summary>
    /// Runs <paramref name="work"/> on the raster thread and waits for its result: drawing through the Impeller context
    /// stays on one thread (Scene.toImage's readback).
    /// </summary>
    public T Invoke<T>(Func<T> work)
    {
        if (Thread.CurrentThread == _thread)
        {
            return work();
        }

        using var done = new ManualResetEventSlim();
        T result = default!;
        Exception? failure = null;
        _queue.Add(() =>
        {
            try
            {
                result = work();
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
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(failure);
        }

        return result;
    }

    /// <summary>Drops a frame that has not been drawn yet (the layer is going away).</summary>
    public void DropPendingFrame()
    {
        lock (_frameGate)
        {
            _pendingFrame?.DisplayList.Dispose();
            _pendingFrame = null;
        }
    }

    public void Dispose()
    {
        DropPendingFrame();
        _queue.CompleteAdding();
    }

    private void Run()
    {
        foreach (Action work in _queue.GetConsumingEnumerable())
        {
            work();
        }
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
        if (ImpellerBackend.Context is not { } context)
        {
            return;
        }

        // nextDrawable waits (up to a second) when all of the layer's drawables are in flight.
        using ICAMetalDrawable? drawable = frame.Layer.NextDrawable();
        if (drawable is null)
        {
            return;
        }

        using (ImpellerSurface surface = context.SurfaceCreateWrappedMetalDrawableNew(drawable.Handle)
                   ?? throw new InvalidOperationException("Impeller could not wrap the drawable."))
        {
            surface.DrawDisplayList(displayList);
            // SurfaceMTL::Present: the drawable is presented once Impeller's command buffer is scheduled on its own
            // queue, after the frame's rendering (presenting it directly could show it before the GPU finished).
            surface.Present();
        }

        frame.Presented?.Invoke();
    }

    private sealed record PendingFrame(CAMetalLayer Layer, ImpellerDisplayList DisplayList, Action? Presented);
}
