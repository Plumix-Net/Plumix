using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/editable.dart

namespace Plumix.Rendering;

/// <summary>The render object that paints a <see cref="RenderEditablePainter"/> for a
/// <see cref="RenderEditable"/>.</summary>
/// <remarks>Dart's private <c>_RenderEditableCustomPaint</c>.</remarks>
internal sealed class RenderEditableCustomPaint : RenderBox
{
    private RenderEditablePainter? _painter;

    public RenderEditableCustomPaint(RenderEditablePainter? painter = null)
    {
        _painter = painter;
    }

    private RenderEditable? EditableParent => Parent as RenderEditable;

    /// <inheritdoc />
    public override bool IsRepaintBoundary => true;

    /// <inheritdoc />
    protected override bool SizedByParent => true;

    public RenderEditablePainter? Painter
    {
        get => _painter;
        set
        {
            if (ReferenceEquals(value, _painter))
            {
                return;
            }

            RenderEditablePainter? oldPainter = _painter;
            _painter = value;

            if (value?.ShouldRepaint(oldPainter) ?? true)
            {
                MarkNeedsPaint();
            }

            if (Attached)
            {
                oldPainter?.RemoveListener(MarkNeedsPaint);
                value?.AddListener(MarkNeedsPaint);
            }
        }
    }

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        RenderEditable? parent = EditableParent;
        RenderEditable.DebugAssert(parent is not null);
        RenderEditablePainter? painter = Painter;
        if (painter is not null && parent is not null)
        {
            parent.ComputeTextMetricsIfNeeded();
            painter.Paint(context.Canvas, Size, parent);
        }
    }

    /// <inheritdoc />
    protected override void OnAttach()
    {
        base.OnAttach();
        _painter?.AddListener(MarkNeedsPaint);
    }

    /// <inheritdoc />
    protected override void OnDetach()
    {
        _painter?.RemoveListener(MarkNeedsPaint);
        base.OnDetach();
    }

    /// <inheritdoc />
    protected override Size ComputeDryLayout(BoxConstraints constraints) => constraints.Biggest;
}

/// <summary>An interface that paints within a <see cref="RenderEditable"/>'s bounds, above or beneath
/// its text content.</summary>
/// <remarks>
/// <para>This painter is typically used for painting auxiliary content that depends on text layout
/// metrics (for instance, for painting carets and text highlight blocks). It can paint independently
/// from its <see cref="RenderEditable"/>, allowing it to repaint without triggering a repaint on the
/// entire <see cref="RenderEditable"/> stack when only auxiliary content changes (e.g. a blinking
/// cursor) are present. It will be scheduled to repaint when:</para>
/// <list type="bullet">
/// <item>It's assigned to a new <see cref="RenderEditable"/> (replacing a prior
/// <see cref="RenderEditablePainter"/>) and the <see cref="ShouldRepaint"/> method returns true.</item>
/// <item>Any of the <see cref="RenderEditable"/>s it is attached to repaints.</item>
/// <item>The <see cref="ChangeNotifier.NotifyListeners"/> method is called, which typically happens
/// when the painter's attributes change.</item>
/// </list>
/// </remarks>
public abstract class RenderEditablePainter : ChangeNotifier
{
    /// <summary>Determines whether repaint is needed when a new <see cref="RenderEditablePainter"/> is
    /// provided to a <see cref="RenderEditable"/>.</summary>
    /// <remarks>If the new instance represents different information than the old instance, then the
    /// method should return true, otherwise it should return false. When
    /// <paramref name="oldDelegate"/> is null, this method should always return true unless the new
    /// painter initially does not paint anything.</remarks>
    public abstract bool ShouldRepaint(RenderEditablePainter? oldDelegate);

    /// <summary>Paints within the bounds of a <see cref="RenderEditable"/>.</summary>
    /// <remarks>The given <see cref="Canvas"/> has the same coordinate space as the
    /// <see cref="RenderEditable"/>, which may be different from the coordinate space the
    /// <see cref="RenderEditable"/>'s <see cref="TextPainter"/> uses, when the text moves inside the
    /// <see cref="RenderEditable"/>.</remarks>
    public abstract void Paint(Canvas canvas, Size size, RenderEditable renderEditable);
}

/// <summary>Paints the highlight of a text range (the selection, or the autocorrect prompt).</summary>
/// <remarks>Dart's private <c>_TextHighlightPainter</c>.</remarks>
internal sealed class TextHighlightPainter : RenderEditablePainter
{
    private Color? _highlightColor;
    private TextRange? _highlightedRange;
    private BoxHeightStyle _selectionHeightStyle = BoxHeightStyle.Tight;
    private BoxWidthStyle _selectionWidthStyle = BoxWidthStyle.Tight;

    public TextHighlightPainter(TextRange? highlightedRange = null, Color? highlightColor = null)
    {
        _highlightedRange = highlightedRange;
        _highlightColor = highlightColor;
    }

    public Color? HighlightColor
    {
        get => _highlightColor;
        set
        {
            if (Nullable.Equals(value, _highlightColor))
            {
                return;
            }

            _highlightColor = value;
            NotifyListeners();
        }
    }

    public TextRange? HighlightedRange
    {
        get => _highlightedRange;
        set
        {
            if (Nullable.Equals(value, _highlightedRange))
            {
                return;
            }

            _highlightedRange = value;
            NotifyListeners();
        }
    }

    /// <summary>Controls how tall the selection highlight boxes are computed to be.</summary>
    public BoxHeightStyle SelectionHeightStyle
    {
        get => _selectionHeightStyle;
        set
        {
            if (_selectionHeightStyle == value)
            {
                return;
            }

            _selectionHeightStyle = value;
            NotifyListeners();
        }
    }

    /// <summary>Controls how wide the selection highlight boxes are computed to be.</summary>
    public BoxWidthStyle SelectionWidthStyle
    {
        get => _selectionWidthStyle;
        set
        {
            if (_selectionWidthStyle == value)
            {
                return;
            }

            _selectionWidthStyle = value;
            NotifyListeners();
        }
    }

    public override void Paint(Canvas canvas, Size size, RenderEditable renderEditable)
    {
        TextRange? range = HighlightedRange;
        Color? color = HighlightColor;
        if (range is not { } highlightedRange || color is not { } highlightColor || highlightedRange.IsCollapsed)
        {
            return;
        }

        var highlightBrush = new SolidColorBrush(highlightColor);
        TextPainter textPainter = renderEditable.TextPainter;
        IEnumerable<TextBox> boxes = textPainter.GetBoxesForSelection(
                new TextSelection(highlightedRange.Start, highlightedRange.End),
                SelectionHeightStyle,
                SelectionWidthStyle)
            .Distinct();

        Point paintOffset = renderEditable.PaintOffset;
        var textRect = new Rect(0, 0, textPainter.Width, textPainter.Height);
        foreach (TextBox box in boxes)
        {
            Rect shifted = box.ToRect().Translate(new Vector(paintOffset.X, paintOffset.Y));
            canvas.DrawRectangle(highlightBrush, null, Intersect(shifted, textRect));
        }
    }

    // Dart's `Rect.intersect`, which keeps the (possibly negative) extent of disjoint rects instead of
    // collapsing them to Avalonia's empty rect at the origin.
    private static Rect Intersect(Rect a, Rect b)
    {
        double left = Math.Max(a.Left, b.Left);
        double top = Math.Max(a.Top, b.Top);
        double right = Math.Min(a.Right, b.Right);
        double bottom = Math.Min(a.Bottom, b.Bottom);
        return new Rect(left, top, Math.Max(0.0, right - left), Math.Max(0.0, bottom - top));
    }

    public override bool ShouldRepaint(RenderEditablePainter? oldDelegate)
    {
        if (ReferenceEquals(oldDelegate, this))
        {
            return false;
        }

        if (oldDelegate is null)
        {
            return HighlightColor is not null && HighlightedRange is not null;
        }

        return oldDelegate is not TextHighlightPainter oldPainter
               || !Nullable.Equals(oldPainter.HighlightColor, HighlightColor)
               || !Nullable.Equals(oldPainter.HighlightedRange, HighlightedRange)
               || oldPainter.SelectionHeightStyle != SelectionHeightStyle
               || oldPainter.SelectionWidthStyle != SelectionWidthStyle;
    }
}

/// <summary>Paints the regular caret and the floating cursor.</summary>
/// <remarks>Dart's private <c>_CaretPainter</c>.</remarks>
internal sealed class CaretPainter : RenderEditablePainter
{
    // The corner radius of the floating cursor in pixels.
    private static readonly Radius KFloatingCursorRadius = Radius.Circular(1.0);

    // The shortest squared distance between the floating cursor and the regular cursor at which both
    // are painted. Mirrors RenderEditable's constant of the same name.
    private const double KShortestDistanceSquaredWithFloatingAndRegularCursors = 15.0 * 15.0;

    private bool _shouldPaint = true;
    private Color? _caretColor;
    private Radius? _cursorRadius;
    private Point _cursorOffset;
    private Color? _backgroundCursorColor;
    private Rect? _floatingCursorRect;

    public bool ShouldPaint
    {
        get => _shouldPaint;
        set
        {
            if (ShouldPaint == value)
            {
                return;
            }

            _shouldPaint = value;
            NotifyListeners();
        }
    }

    // This is directly manipulated by the RenderEditable during setFloatingCursor.
    //
    // When changing this value, the caller is responsible for ensuring that listeners are notified.
    public bool ShowRegularCaret { get; set; }

    public Color? CaretColor
    {
        get => _caretColor;
        set
        {
            if (Nullable.Equals(CaretColor, value))
            {
                return;
            }

            _caretColor = value;
            NotifyListeners();
        }
    }

    public Radius? CursorRadius
    {
        get => _cursorRadius;
        set
        {
            if (Nullable.Equals(_cursorRadius, value))
            {
                return;
            }

            _cursorRadius = value;
            NotifyListeners();
        }
    }

    public Point CursorOffset
    {
        get => _cursorOffset;
        set
        {
            if (_cursorOffset == value)
            {
                return;
            }

            _cursorOffset = value;
            NotifyListeners();
        }
    }

    public Color? BackgroundCursorColor
    {
        get => _backgroundCursorColor;
        set
        {
            if (Nullable.Equals(BackgroundCursorColor, value))
            {
                return;
            }

            _backgroundCursorColor = value;
            if (ShowRegularCaret)
            {
                NotifyListeners();
            }
        }
    }

    public Rect? FloatingCursorRect
    {
        get => _floatingCursorRect;
        set
        {
            if (Nullable.Equals(_floatingCursorRect, value))
            {
                return;
            }

            _floatingCursorRect = value;
            NotifyListeners();
        }
    }

    public void PaintRegularCursor(
        Canvas canvas,
        RenderEditable renderEditable,
        Color caretColor,
        TextPosition textPosition)
    {
        Rect integralRect = renderEditable.GetLocalRectForCaret(textPosition);
        if (ShouldPaint)
        {
            if (FloatingCursorRect is { } floatingCursorRect)
            {
                double dx = floatingCursorRect.Center.X - integralRect.Center.X;
                double dy = floatingCursorRect.Center.Y - integralRect.Center.Y;
                double distanceSquared = (dx * dx) + (dy * dy);
                if (distanceSquared < KShortestDistanceSquaredWithFloatingAndRegularCursors)
                {
                    return;
                }
            }

            Radius? radius = CursorRadius;
            var caretBrush = new SolidColorBrush(caretColor);
            if (radius is null)
            {
                canvas.DrawRectangle(caretBrush, null, integralRect);
            }
            else
            {
                RRect caretRRect = RRect.FromRectAndRadius(integralRect, radius.Value);
                canvas.DrawRRect(caretRRect, caretBrush, null);
            }
        }
    }

    public override void Paint(Canvas canvas, Size size, RenderEditable renderEditable)
    {
        // Compute the caret location even when `shouldPaint` is false.

        TextSelection? selection = renderEditable.Selection;

        if (selection is not { IsCollapsed: true, IsValid: true } collapsedSelection)
        {
            return;
        }

        Rect? floatingCursorRect = FloatingCursorRect;

        Color? caretColor = floatingCursorRect is null
            ? CaretColor
            : ShowRegularCaret
                ? BackgroundCursorColor
                : null;
        TextPosition caretTextPosition = floatingCursorRect is null
            ? collapsedSelection.Extent
            : renderEditable.FloatingCursorTextPosition;

        if (caretColor is { } color)
        {
            PaintRegularCursor(canvas, renderEditable, color, caretTextPosition);
        }

        Color? floatingCursorColor = CaretColor?.WithOpacity(0.75);
        // Floating Cursor.
        if (floatingCursorRect is not { } floatingRect || floatingCursorColor is not { } floatingColor || !ShouldPaint)
        {
            return;
        }

        canvas.DrawRRect(
            RRect.FromRectAndRadius(floatingRect, KFloatingCursorRadius),
            new SolidColorBrush(floatingColor),
            null);
    }

    public override bool ShouldRepaint(RenderEditablePainter? oldDelegate)
    {
        if (ReferenceEquals(this, oldDelegate))
        {
            return false;
        }

        if (oldDelegate is null)
        {
            return ShouldPaint;
        }

        return oldDelegate is not CaretPainter oldPainter
               || oldPainter.ShouldPaint != ShouldPaint
               || oldPainter.ShowRegularCaret != ShowRegularCaret
               || !Nullable.Equals(oldPainter.CaretColor, CaretColor)
               || !Nullable.Equals(oldPainter.CursorRadius, CursorRadius)
               || oldPainter.CursorOffset != CursorOffset
               || !Nullable.Equals(oldPainter.BackgroundCursorColor, BackgroundCursorColor)
               || !Nullable.Equals(oldPainter.FloatingCursorRect, FloatingCursorRect);
    }
}

/// <summary>Paints a list of <see cref="RenderEditablePainter"/>s in order.</summary>
/// <remarks>Dart's private <c>_CompositeRenderEditablePainter</c>. Its own listener list is never used:
/// listeners are forwarded to the child painters.</remarks>
internal sealed class CompositeRenderEditablePainter : RenderEditablePainter
{
    public CompositeRenderEditablePainter(List<RenderEditablePainter> painters)
    {
        Painters = painters;
    }

    public List<RenderEditablePainter> Painters { get; }

    public override void AddListener(Action listener)
    {
        foreach (RenderEditablePainter painter in Painters)
        {
            painter.AddListener(listener);
        }
    }

    public override void RemoveListener(Action listener)
    {
        foreach (RenderEditablePainter painter in Painters)
        {
            painter.RemoveListener(listener);
        }
    }

    public override void Paint(Canvas canvas, Size size, RenderEditable renderEditable)
    {
        foreach (RenderEditablePainter painter in Painters)
        {
            painter.Paint(canvas, size, renderEditable);
        }
    }

    public override bool ShouldRepaint(RenderEditablePainter? oldDelegate)
    {
        if (ReferenceEquals(oldDelegate, this))
        {
            return false;
        }

        if (oldDelegate is not CompositeRenderEditablePainter oldComposite
            || oldComposite.Painters.Count != Painters.Count)
        {
            return true;
        }

        using IEnumerator<RenderEditablePainter> oldPainters = oldComposite.Painters.GetEnumerator();
        using IEnumerator<RenderEditablePainter> newPainters = Painters.GetEnumerator();
        while (oldPainters.MoveNext() && newPainters.MoveNext())
        {
            if (newPainters.Current.ShouldRepaint(oldPainters.Current))
            {
                return true;
            }
        }

        return false;
    }
}
