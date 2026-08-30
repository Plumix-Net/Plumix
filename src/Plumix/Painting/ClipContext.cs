using Avalonia;
using Plumix.UI;
using Path = Plumix.UI.Path;

namespace Plumix.Painting;

// Dart parity source: flutter/packages/flutter/lib/src/painting/clip.dart

/// <summary>Clip utilities used by <c>PaintingContext</c>.</summary>
public abstract class ClipContext
{
    /// <summary>The canvas on which to paint.</summary>
    public abstract Canvas Canvas { get; }

    /// <remarks>Flutter's <c>ClipContext._clipAndPaint</c>.</remarks>
    private void ClipAndPaint(Action<bool> canvasClipCall, Clip clipBehavior, Rect bounds, Action painter)
    {
        Canvas.Save();
        switch (clipBehavior)
        {
            case Clip.None:
                break;
            case Clip.HardEdge:
                canvasClipCall(false);
                break;
            case Clip.AntiAlias:
                canvasClipCall(true);
                break;
            case Clip.AntiAliasWithSaveLayer:
                canvasClipCall(true);
                Canvas.SaveLayer(bounds);
                break;
        }

        painter();
        if (clipBehavior == Clip.AntiAliasWithSaveLayer)
        {
            Canvas.Restore();
        }

        Canvas.Restore();
    }

    /// <summary>
    /// Clips <see cref="Canvas"/> with <paramref name="path"/> according to <paramref name="clipBehavior"/>
    /// and then paints. The canvas is restored to the pre-clip status afterwards.
    /// </summary>
    public void ClipPathAndPaint(Path path, Clip clipBehavior, Rect bounds, Action painter)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(painter);
        ClipAndPaint(doAntiAlias => Canvas.ClipPath(path, doAntiAlias), clipBehavior, bounds, painter);
    }

    /// <summary>Clips <see cref="Canvas"/> with a rounded rectangle and then paints.</summary>
    public void ClipRRectAndPaint(RRect rrect, Clip clipBehavior, Rect bounds, Action painter)
    {
        ArgumentNullException.ThrowIfNull(painter);
        ClipAndPaint(doAntiAlias => Canvas.ClipRRect(rrect, doAntiAlias), clipBehavior, bounds, painter);
    }

    /// <summary>Clips <see cref="Canvas"/> with a rounded superellipse and then paints.</summary>
    public void ClipRSuperellipseAndPaint(
        RSuperellipse rse,
        Clip clipBehavior,
        Rect bounds,
        Action painter)
    {
        ArgumentNullException.ThrowIfNull(painter);
        ClipAndPaint(doAntiAlias => Canvas.ClipRSuperellipse(rse, doAntiAlias), clipBehavior, bounds, painter);
    }

    /// <summary>Clips <see cref="Canvas"/> with a rectangle and then paints.</summary>
    public void ClipRectAndPaint(Rect rect, Clip clipBehavior, Rect bounds, Action painter)
    {
        ArgumentNullException.ThrowIfNull(painter);
        ClipAndPaint(doAntiAlias => Canvas.ClipRect(rect, doAntiAlias), clipBehavior, bounds, painter);
    }

    /// <summary>
    /// Plumix-only: clips <see cref="Canvas"/> with an Avalonia geometry the caller already built.
    /// </summary>
    /// <remarks>
    /// Flutter has no counterpart because every Dart clip goes through <c>Path</c>; Plumix keeps this
    /// door open for render objects that receive a backend geometry (ovals, decoration outlines).
    /// </remarks>
    public void ClipGeometryAndPaint(
        Avalonia.Media.Geometry geometry,
        Point geometryOffset,
        Clip clipBehavior,
        Rect bounds,
        Action painter)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(painter);
        ClipAndPaint(
            doAntiAlias => Canvas.ClipGeometry(geometry, doAntiAlias, geometryOffset),
            clipBehavior,
            bounds,
            painter);
    }
}
