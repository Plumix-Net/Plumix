using System.Runtime.CompilerServices;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/editable.dart

namespace Plumix.Rendering;

/// <summary>Represents the coordinates of the point in a selection, and the text direction at that
/// point, relative to top left of the <see cref="RenderEditable"/> that holds the selection.</summary>
/// <param name="Point">Coordinates of the lower left or lower right corner of the selection, relative to
/// the top left of the <see cref="RenderEditable"/> object.</param>
/// <param name="Direction">Direction of the text at this edge of the selection.</param>
public readonly record struct TextSelectionPoint(Point Point, TextDirection? Direction)
{
    /// <inheritdoc />
    public override string ToString()
    {
        return Direction switch
        {
            TextDirection.Ltr => $"{Point}-ltr",
            TextDirection.Rtl => $"{Point}-rtl",
            _ => $"{Point}",
        };
    }
}

/// <summary>The consecutive sequence of <see cref="TextPosition"/>s that the caret should move to when
/// the user navigates the paragraph using the upward arrow key or the downward arrow key.</summary>
/// <remarks>
/// Dart's <c>VerticalCaretMovementRun</c>. When the user presses the upward arrow key or the downward
/// arrow key on the keyboard, the caret moves to the previous line or the next line, while maintaining
/// its original horizontal location. When it encounters a shorter line, the caret moves to the closest
/// horizontal location within that line, and restores the original horizontal location when a long
/// enough line is encountered.
/// </remarks>
public sealed class VerticalCaretMovementRun : IEnumerator<TextPosition>
{
    private readonly RenderEditable _editable;
    private readonly IReadOnlyList<LineMetrics> _lineMetrics;
    private readonly Dictionary<int, KeyValuePair<Point, TextPosition>> _positionCache = [];
    private Point _currentOffset;
    private int _currentLine;
    private TextPosition _currentTextPosition;
    private bool _isValid = true;

    internal VerticalCaretMovementRun(
        RenderEditable editable,
        IReadOnlyList<LineMetrics> lineMetrics,
        TextPosition currentTextPosition,
        int currentLine,
        Point currentOffset)
    {
        _editable = editable;
        _lineMetrics = lineMetrics;
        _currentTextPosition = currentTextPosition;
        _currentLine = currentLine;
        _currentOffset = currentOffset;
    }

    /// <summary>Whether this <see cref="VerticalCaretMovementRun"/> can still continue.</summary>
    /// <remarks>
    /// A <see cref="VerticalCaretMovementRun"/> run is valid if the underlying text layout hasn't
    /// changed. It's still possible to access <see cref="Current"/> of an invalid run, but a subsequent
    /// <see cref="MoveNext"/> invocation always returns false.
    /// </remarks>
    public bool IsValid
    {
        get
        {
            if (!_isValid)
            {
                return false;
            }

            IReadOnlyList<LineMetrics> newLineMetrics = _editable.TextPainter.ComputeLineMetrics();
            // Use the implementation detail of the TextPainter.ComputeLineMetrics method to figure out
            // if the current text layout has been invalidated.
            if (!ReferenceEquals(newLineMetrics, _lineMetrics))
            {
                _isValid = false;
            }

            return _isValid;
        }
    }

    private KeyValuePair<Point, TextPosition> GetTextPositionForLine(int lineNumber)
    {
        RenderEditable.DebugAssert(IsValid);
        RenderEditable.DebugAssert(lineNumber >= 0);
        if (_positionCache.TryGetValue(lineNumber, out KeyValuePair<Point, TextPosition> cachedPosition))
        {
            return cachedPosition;
        }

        RenderEditable.DebugAssert(lineNumber != _currentLine);

        var newOffset = new Point(_currentOffset.X, _lineMetrics[lineNumber].Baseline);
        TextPosition closestPosition = _editable.TextPainter.GetPositionForOffset(newOffset);
        var position = new KeyValuePair<Point, TextPosition>(newOffset, closestPosition);
        _positionCache[lineNumber] = position;
        return position;
    }

    /// <inheritdoc />
    public TextPosition Current
    {
        get
        {
            RenderEditable.DebugAssert(IsValid);
            return _currentTextPosition;
        }
    }

    object System.Collections.IEnumerator.Current => Current;

    /// <inheritdoc />
    public bool MoveNext()
    {
        RenderEditable.DebugAssert(IsValid);
        if (_currentLine + 1 >= _lineMetrics.Count)
        {
            return false;
        }

        KeyValuePair<Point, TextPosition> position = GetTextPositionForLine(_currentLine + 1);
        _currentLine += 1;
        _currentOffset = position.Key;
        _currentTextPosition = position.Value;
        return true;
    }

    /// <summary>Move back to the previous element.</summary>
    /// <returns>true and updates <see cref="Current"/> if successful.</returns>
    public bool MovePrevious()
    {
        RenderEditable.DebugAssert(IsValid);
        if (_currentLine <= 0)
        {
            return false;
        }

        KeyValuePair<Point, TextPosition> position = GetTextPositionForLine(_currentLine - 1);
        _currentLine -= 1;
        _currentOffset = position.Key;
        _currentTextPosition = position.Value;
        return true;
    }

    /// <summary>Move forward or backward by a number of elements determined by the pixel
    /// <paramref name="offset"/>.</summary>
    /// <remarks>
    /// If <paramref name="offset"/> is negative, move backward; otherwise move forward. Returns true
    /// and updates <see cref="Current"/> if successful.
    /// </remarks>
    public bool MoveByOffset(double offset)
    {
        Point initialOffset = _currentOffset;
        if (offset >= 0.0)
        {
            while (_currentOffset.Y < initialOffset.Y + offset)
            {
                if (!MoveNext())
                {
                    break;
                }
            }
        }
        else
        {
            while (_currentOffset.Y > initialOffset.Y + offset)
            {
                if (!MovePrevious())
                {
                    break;
                }
            }
        }

        return initialOffset != _currentOffset;
    }

    void System.Collections.IEnumerator.Reset() => throw new NotSupportedException();

    void IDisposable.Dispose()
    {
    }
}

/// <summary>Displays some text in a scrollable container with a potentially blinking cursor and with
/// gesture recognizers.</summary>
/// <remarks>
/// <para>This is the renderer for an editable text field. It does not directly provide affordances for
/// editing the text, but it does handle text selection and manipulation of the text cursor.</para>
/// <para>The <see cref="Text"/> is displayed, scrolled by the given <see cref="Offset"/>, aligned
/// according to <see cref="TextAlign"/>. The <see cref="MaxLines"/> property controls whether the text
/// displays on one line or many. The <see cref="Selection"/>, if it is not collapsed, is painted in the
/// <see cref="SelectionColor"/>. If it <em>is</em> collapsed, then it represents the cursor position.
/// The cursor is shown while <see cref="ShowCursor"/> is true. It is painted in the
/// <see cref="CursorColor"/>.</para>
/// <para>Keyboard handling, IME handling, scrolling, toggling the <see cref="ShowCursor"/> value to
/// actually blink the cursor, and other features not mentioned above are the responsibility of higher
/// layers and not handled by this object.</para>
/// <para>C# has no mixins: Dart's <c>ContainerRenderObjectMixin</c> is composed as a
/// <see cref="RenderBoxContainerDefaultsMixin{TChild,TParentData}"/> field and
/// <c>RenderInlineChildrenContainerDefaults</c> is the static
/// <see cref="RenderInlineChildrenContainerDefaults"/> helper, the same way <c>RenderParagraph</c> does
/// it. <c>RelayoutWhenSystemFontsChangeMixin</c> is not ported (docs/ai/BACKLOG.md).</para>
/// </remarks>
public class RenderEditable : RenderBox,
    IRenderBoxContainerDefaultsMixin<RenderBox, TextParentData>,
    IRenderObjectContainer,
    ITextLayoutMetrics
{
    // Dart's top-level constants of editable.dart.
    private const double KCaretGap = 1.0; // pixels
    private const double KCaretHeightOffset = 2.0; // pixels

    // The additional size on the x and y axis with which to expand the prototype cursor to render the
    // floating cursor in pixels.
    private static readonly Thickness KFloatingCursorSizeIncrease = new(0.5, 1.0);

    // _kFloatingCursorRadius and the shortest caret distance live on CaretPainter, their only reader.

    private readonly RenderBoxContainerDefaultsMixin<RenderBox, TextParentData> _container;
    private readonly TextPainter _textPainter;
    private readonly TextHighlightPainter _selectionPainter = new();
    private readonly TextHighlightPainter _autocorrectHighlightPainter = new();
    private readonly ValueNotifier<bool> _selectionStartInViewport = new(true);
    private readonly ValueNotifier<bool> _selectionEndInViewport = new(true);
    private readonly LayerHandle<LeaderLayer> _leaderLayerHandler = new();
    private readonly LayerHandle<ClipRectLayer> _clipRectLayer = new();

    private CaretPainter? _caretPainterCache;
    private RenderEditableCustomPaint? _foregroundRenderObject;
    private RenderEditableCustomPaint? _backgroundRenderObject;
    private RenderEditablePainter? _foregroundPainter;
    private RenderEditablePainter? _painter;
    private CompositeRenderEditablePainter? _cachedBuiltInForegroundPainters;
    private CompositeRenderEditablePainter? _cachedBuiltInPainters;
    private double _devicePixelRatio;
    private string _obscuringCharacter;
    private bool _obscureText;
    private AttributedString? _cachedAttributedValue;
    private List<InlineSpanSemanticsInformation>? _cachedCombinedSemanticsInfos;
    private TextPainter? _textIntrinsicsCache;
    private bool _disposeShowCursor;
    private ValueNotifier<bool> _showCursor;
    private bool _hasFocus;
    private bool _forceLine;
    private bool _readOnly;
    private int? _maxLines;
    private int? _minLines;
    private bool _expands;
    private TextSelection? _selection;
    private ViewportOffset _offset;
    private double _cursorWidth;
    private double? _cursorHeight;
    private bool _paintCursorOnTop;
    private LayerLink _startHandleLayerLink;
    private LayerLink _endHandleLayerLink;
    private bool _floatingCursorOn;
    private TextPosition _floatingCursorTextPosition;
    private bool? _enableInteractiveSelection;
    private double _maxScrollExtent;
    private Clip _clipBehavior;
    private List<InlineSpanSemanticsInformation>? _semanticsInfo;
    private OrderedDictionary<UniqueKey, SemanticsNode>? _cachedChildNodes;
    private int? _cachedLineBreakCount;
    private TapGestureRecognizer? _tap;
    private LongPressGestureRecognizer? _longPress;
    private Point? _lastTapDownPosition;
    private Point? _lastSecondaryTapDownPosition;
    private List<PlaceholderDimensions>? _placeholderDimensions;
    private Rect _caretPrototype;
    private Point _relativeOrigin;
    private Point? _previousOffset;
    private bool _shouldResetOrigin = true;
    private bool _resetOriginOnLeft;
    private bool _resetOriginOnRight;
    private bool _resetOriginOnTop;
    private bool _resetOriginOnBottom;
    private double? _resetFloatingCursorAnimationValue;

    /// <summary>Creates a render object that implements the visual aspects of a text field.</summary>
    /// <remarks>
    /// <para>The <paramref name="textAlign"/> argument defaults to <see cref="TextAlign.Start"/>.</para>
    /// <para>If <paramref name="showCursor"/> is not specified, then it defaults to hiding the cursor.</para>
    /// <para>The <paramref name="maxLines"/> property can be set to null to remove the restriction on the
    /// number of lines. By default, it is 1, meaning this is a single-line text field. If it is not null,
    /// it must be greater than zero.</para>
    /// <para>Use <paramref name="ignorePointer"/> to ignore all pointer events in scenarios where the
    /// caller might want to handle them by themselves (for example, from an ancestor).</para>
    /// <para>A null <paramref name="textScaler"/> is Dart's default <c>TextScaler.noScaling</c>, and a
    /// null <paramref name="floatingCursorAddedMargin"/> is Dart's default
    /// <c>EdgeInsets.fromLTRB(4, 4, 4, 5)</c>. The <c>required</c> Dart parameters come first because a
    /// C# optional parameter cannot precede a required one.</para>
    /// </remarks>
    public RenderEditable(
        TextDirection textDirection,
        LayerLink startHandleLayerLink,
        LayerLink endHandleLayerLink,
        ViewportOffset offset,
        ITextSelectionDelegate textSelectionDelegate,
        InlineSpan? text = null,
        TextAlign textAlign = TextAlign.Start,
        Color? cursorColor = null,
        Color? backgroundCursorColor = null,
        ValueNotifier<bool>? showCursor = null,
        bool? hasFocus = null,
        int? maxLines = 1,
        int? minLines = null,
        bool expands = false,
        StrutStyle? strutStyle = null,
        Color? selectionColor = null,
        double textScaleFactor = 1.0,
        TextScaler? textScaler = null,
        TextSelection? selection = null,
        bool ignorePointer = false,
        bool readOnly = false,
        bool forceLine = true,
        TextHeightBehavior? textHeightBehavior = null,
        TextWidthBasis textWidthBasis = TextWidthBasis.Parent,
        string obscuringCharacter = "•",
        bool obscureText = false,
        string? locale = null,
        double cursorWidth = 1.0,
        double? cursorHeight = null,
        Radius? cursorRadius = null,
        bool paintCursorAboveText = false,
        Point cursorOffset = default,
        double devicePixelRatio = 1.0,
        BoxHeightStyle selectionHeightStyle = BoxHeightStyle.Max,
        BoxWidthStyle selectionWidthStyle = BoxWidthStyle.Max,
        bool? enableInteractiveSelection = null,
        Thickness? floatingCursorAddedMargin = null,
        TextRange? promptRectRange = null,
        Color? promptRectColor = null,
        Clip clipBehavior = Clip.HardEdge,
        RenderEditablePainter? painter = null,
        RenderEditablePainter? foregroundPainter = null,
        List<RenderBox>? children = null)
    {
        TextScaler effectiveTextScaler = textScaler ?? TextScaler.NoScaling;
        DebugAssert(maxLines is null || maxLines > 0);
        DebugAssert(minLines is null || minLines > 0);
        DebugAssert(
            maxLines is null || minLines is null || maxLines >= minLines,
            "minLines can't be greater than maxLines");
        DebugAssert(
            !expands || (maxLines is null && minLines is null),
            "minLines and maxLines must be null when expands is true.");
        DebugAssert(
            textScaler is null || textScaleFactor == 1.0,
            "textScaleFactor is deprecated and cannot be specified when textScaler is specified.");
        DebugAssert(new StringInfo(obscuringCharacter).LengthInTextElements == 1);
        DebugAssert(cursorWidth >= 0.0);
        DebugAssert(cursorHeight is null || cursorHeight >= 0.0);

        _container = new RenderBoxContainerDefaultsMixin<RenderBox, TextParentData>(this);
        _textPainter = new TextPainter(
            text: text,
            textAlign: textAlign,
            textDirection: textDirection,
            textScaler: Equals(effectiveTextScaler, TextScaler.NoScaling)
                ? TextScaler.Linear(textScaleFactor)
                : effectiveTextScaler,
            locale: locale,
            maxLines: maxLines == 1 ? 1 : null,
            strutStyle: strutStyle,
            textHeightBehavior: textHeightBehavior,
            textWidthBasis: textWidthBasis);
        _showCursor = showCursor ?? new ValueNotifier<bool>(false);
        _maxLines = maxLines;
        _minLines = minLines;
        _expands = expands;
        _selection = selection;
        _offset = offset;
        _cursorWidth = cursorWidth;
        _cursorHeight = cursorHeight;
        _paintCursorOnTop = paintCursorAboveText;
        _enableInteractiveSelection = enableInteractiveSelection;
        _devicePixelRatio = devicePixelRatio;
        _startHandleLayerLink = startHandleLayerLink;
        _endHandleLayerLink = endHandleLayerLink;
        _obscuringCharacter = obscuringCharacter;
        _obscureText = obscureText;
        _readOnly = readOnly;
        _forceLine = forceLine;
        _clipBehavior = clipBehavior;
        _hasFocus = hasFocus ?? false;
        _disposeShowCursor = showCursor is null;
        IgnorePointer = ignorePointer;
        TextSelectionDelegate = textSelectionDelegate;
        FloatingCursorAddedMargin = floatingCursorAddedMargin ?? new Thickness(4, 4, 4, 5);

        DebugAssert(!_showCursor.Value || cursorColor is not null);

        _selectionPainter.HighlightColor = selectionColor;
        _selectionPainter.HighlightedRange = selection?.AsTextRange();
        _selectionPainter.SelectionHeightStyle = selectionHeightStyle;
        _selectionPainter.SelectionWidthStyle = selectionWidthStyle;

        _autocorrectHighlightPainter.HighlightColor = promptRectColor;
        _autocorrectHighlightPainter.HighlightedRange = promptRectRange;

        CaretPainterInstance.CaretColor = cursorColor;
        CaretPainterInstance.CursorRadius = cursorRadius;
        CaretPainterInstance.CursorOffset = cursorOffset;
        CaretPainterInstance.BackgroundCursorColor = backgroundCursorColor;

        UpdateForegroundPainter(foregroundPainter);
        UpdatePainter(painter);
        AddAll(children);
    }

    /// <summary>Dart's <c>assert</c>: throws an <see cref="AssertionError"/> in debug builds, the way
    /// <c>TextPainter</c>'s asserts do, so a failed assert is observable instead of aborting the
    /// process.</summary>
    internal static void DebugAssert(
        bool condition,
        string? message = null,
        [CallerArgumentExpression(nameof(condition))] string? expression = null)
    {
        if (Constants.KDebugMode && !condition)
        {
            throw new AssertionError(message ?? expression ?? "Assertion failed.");
        }
    }

    /// <summary>Whether this render object ignores pointer events.</summary>
    /// <remarks>
    /// If true, <see cref="HandleEvent"/> does nothing and an ancestor is responsible for calling
    /// <see cref="HandleTapDown"/>, <see cref="HandleTap"/>, <see cref="HandleDoubleTap"/> and
    /// <see cref="HandleLongPress"/>.
    /// </remarks>
    public bool IgnorePointer { get; set; }

    /// <summary>The object that controls the text selection, used by this render object for
    /// implementing cut, copy, and paste keyboard shortcuts.</summary>
    /// <remarks>It will make cut, copy and paste functionality work with the most recently set
    /// <see cref="ITextSelectionDelegate"/>.</remarks>
    public ITextSelectionDelegate TextSelectionDelegate { get; set; }

    /// <summary>The margin, on top of the text's size, that the floating cursor may travel.</summary>
    /// <remarks>Dart's <c>floatingCursorAddedMargin</c>.</remarks>
    public Thickness FloatingCursorAddedMargin { get; set; }

    /// <summary>The text painter this editable lays out and paints with.</summary>
    internal TextPainter TextPainter => _textPainter;

    // -- Container (Dart's ContainerRenderObjectMixin) ----------------------------------------------

    public RenderBox? FirstChild => _container.FirstChild;

    public RenderBox? LastChild => _container.LastChild;

    public int ChildCount => _container.ChildCount;

    public void AddAll(List<RenderBox>? children) => _container.AddAll(children);

    public void RemoveAll() => _container.RemoveAll();

    public RenderBox? ChildBefore(RenderBox child) => _container.ChildBefore(child);

    public RenderBox? ChildAfter(RenderBox child) => _container.ChildAfter(child);

    public void Insert(RenderBox child, RenderBox? after = null) => _container.Insert(child, after);

    public void Move(RenderBox child, RenderBox? after = null) => _container.Move(child, after);

    public void Remove(RenderBox child) => _container.Remove(child);

    public void DefaultPaint(PaintingContext ctx, Point offset) => _container.DefaultPaint(ctx, offset);

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position)
    {
        return _container.DefaultHitTestChildren(result, position);
    }

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after)
    {
        Insert((RenderBox)child, after as RenderBox);
    }

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after)
    {
        Move((RenderBox)child, after as RenderBox);
    }

    void IRenderObjectContainer.Remove(RenderObject child) => Remove((RenderBox)child);

    /// <inheritdoc />
    /// <remarks>Dart's <c>RenderInlineChildrenContainerDefaults.setupParentData</c>.</remarks>
    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not TextParentData)
        {
            child.parentData = new TextParentData();
        }
    }

    private List<RenderBox> InlineChildren => _container.GetChildrenAsList();

    // -- Painters ------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override void Dispose()
    {
        _leaderLayerHandler.Layer = null;
        _foregroundRenderObject?.Dispose();
        _foregroundRenderObject = null;
        _backgroundRenderObject?.Dispose();
        _backgroundRenderObject = null;
        _clipRectLayer.Layer = null;
        _cachedBuiltInForegroundPainters?.Dispose();
        _cachedBuiltInPainters?.Dispose();
        _selectionStartInViewport.Dispose();
        _selectionEndInViewport.Dispose();
        _autocorrectHighlightPainter.Dispose();
        _selectionPainter.Dispose();
        CaretPainterInstance.Dispose();
        _textPainter.Dispose();
        _textIntrinsicsCache?.Dispose();
        if (_disposeShowCursor)
        {
            _showCursor.Dispose();
            _disposeShowCursor = false;
        }

        base.Dispose();
    }

    private void UpdateForegroundPainter(RenderEditablePainter? newPainter)
    {
        RenderEditablePainter effectivePainter = newPainter is null
            ? BuiltInForegroundPainters
            : new CompositeRenderEditablePainter([BuiltInForegroundPainters, newPainter]);

        if (_foregroundRenderObject is null)
        {
            var foregroundRenderObject = new RenderEditableCustomPaint(effectivePainter);
            AdoptChild(foregroundRenderObject);
            _foregroundRenderObject = foregroundRenderObject;
        }
        else
        {
            _foregroundRenderObject.Painter = effectivePainter;
        }

        _foregroundPainter = newPainter;
    }

    /// <summary>The <see cref="RenderEditablePainter"/> to use for painting above this
    /// <see cref="RenderEditable"/>'s text content.</summary>
    /// <remarks>The new <see cref="RenderEditablePainter"/> will replace the previously specified
    /// foreground painter, and schedule a repaint if the new painter's <c>ShouldRepaint</c> method
    /// returns true.</remarks>
    public RenderEditablePainter? ForegroundPainter
    {
        get => _foregroundPainter;
        set
        {
            if (ReferenceEquals(value, _foregroundPainter))
            {
                return;
            }

            UpdateForegroundPainter(value);
        }
    }

    private void UpdatePainter(RenderEditablePainter? newPainter)
    {
        RenderEditablePainter effectivePainter = newPainter is null
            ? BuiltInPainters
            : new CompositeRenderEditablePainter([BuiltInPainters, newPainter]);

        if (_backgroundRenderObject is null)
        {
            var backgroundRenderObject = new RenderEditableCustomPaint(effectivePainter);
            AdoptChild(backgroundRenderObject);
            _backgroundRenderObject = backgroundRenderObject;
        }
        else
        {
            _backgroundRenderObject.Painter = effectivePainter;
        }

        _painter = newPainter;
    }

    /// <summary>Sets the <see cref="RenderEditablePainter"/> to use for painting beneath this
    /// <see cref="RenderEditable"/>'s text content.</summary>
    /// <remarks>The new <see cref="RenderEditablePainter"/> will replace the previously specified
    /// painter, and schedule a repaint if the new painter's <c>ShouldRepaint</c> method returns
    /// true.</remarks>
    public RenderEditablePainter? Painter
    {
        get => _painter;
        set
        {
            if (ReferenceEquals(value, _painter))
            {
                return;
            }

            UpdatePainter(value);
        }
    }

    // Caret Painters:
    // A single painter for both the regular caret and the floating cursor.
    private CaretPainter CaretPainterInstance => _caretPainterCache ??= new CaretPainter();

    // Text Highlight painters:
    // * Autocorrect highlight painter (_autocorrectHighlightPainter)
    // * Text selection highlight painter (_selectionPainter)

    private CompositeRenderEditablePainter BuiltInForegroundPainters =>
        _cachedBuiltInForegroundPainters ??= CreateBuiltInForegroundPainters();

    private CompositeRenderEditablePainter CreateBuiltInForegroundPainters()
    {
        return new CompositeRenderEditablePainter(PaintCursorAboveText ? [CaretPainterInstance] : []);
    }

    private CompositeRenderEditablePainter BuiltInPainters => _cachedBuiltInPainters ??= CreateBuiltInPainters();

    private CompositeRenderEditablePainter CreateBuiltInPainters()
    {
        var painters = new List<RenderEditablePainter> { _autocorrectHighlightPainter, _selectionPainter };
        if (!PaintCursorAboveText)
        {
            painters.Add(CaretPainterInstance);
        }

        return new CompositeRenderEditablePainter(painters);
    }

    // -- Properties -------------------------------------------------------------------------------------

    /// <summary>Defines how to apply <see cref="TextStyle.Height"/> over and under text.</summary>
    public TextHeightBehavior? TextHeightBehavior
    {
        get => _textPainter.TextHeightBehavior;
        set
        {
            if (Equals(_textPainter.TextHeightBehavior, value))
            {
                return;
            }

            _textPainter.TextHeightBehavior = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>Defines how to measure the width of the rendered text.</summary>
    public TextWidthBasis TextWidthBasis
    {
        get => _textPainter.TextWidthBasis;
        set
        {
            if (_textPainter.TextWidthBasis == value)
            {
                return;
            }

            _textPainter.TextWidthBasis = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>The pixel ratio of the current device.</summary>
    /// <remarks>Should be obtained by querying <c>MediaQuery.of(context).devicePixelRatio</c>.</remarks>
    public double DevicePixelRatio
    {
        get => _devicePixelRatio;
        set
        {
            if (DevicePixelRatio == value)
            {
                return;
            }

            _devicePixelRatio = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>Character used for obscuring text if <see cref="ObscureText"/> is true.</summary>
    /// <remarks>Must have a length of exactly one.</remarks>
    public string ObscuringCharacter
    {
        get => _obscuringCharacter;
        set
        {
            if (_obscuringCharacter == value)
            {
                return;
            }

            DebugAssert(new StringInfo(value).LengthInTextElements == 1);
            _obscuringCharacter = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>Whether to hide the text being edited (e.g., for passwords).</summary>
    public bool ObscureText
    {
        get => _obscureText;
        set
        {
            if (_obscureText == value)
            {
                return;
            }

            _obscureText = value;
            _cachedAttributedValue = null;
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <summary>Controls how tall the selection highlight boxes are computed to be.</summary>
    public BoxHeightStyle SelectionHeightStyle
    {
        get => _selectionPainter.SelectionHeightStyle;
        set => _selectionPainter.SelectionHeightStyle = value;
    }

    /// <summary>Controls how wide the selection highlight boxes are computed to be.</summary>
    public BoxWidthStyle SelectionWidthStyle
    {
        get => _selectionPainter.SelectionWidthStyle;
        set => _selectionPainter.SelectionWidthStyle = value;
    }

    /// <summary>Track whether position of the start of the selected text is within the viewport.</summary>
    /// <remarks>For example, if the text contains "Hello World", and the user selects "Hello", then
    /// scrolls so only "World" is visible, this will become false. If the user scrolls back so that the
    /// "H" is visible again, this will become true. This bool indicates whether the text is scrolled so
    /// that the handle is inside the text field viewport, as opposed to whether it is actually visible
    /// on the screen.</remarks>
    public IValueListenable<bool> SelectionStartInViewport => _selectionStartInViewport;

    /// <summary>Track whether position of the end of the selected text is within the viewport.</summary>
    public IValueListenable<bool> SelectionEndInViewport => _selectionEndInViewport;

    /// <summary>Returns the TextPosition above or below the given offset.</summary>
    private TextPosition GetTextPositionVertical(TextPosition position, double verticalOffset)
    {
        Point caretOffset = _textPainter.GetOffsetForCaret(position, _caretPrototype);
        var caretOffsetTranslated = new Point(caretOffset.X, caretOffset.Y + verticalOffset);
        return _textPainter.GetPositionForOffset(caretOffsetTranslated);
    }

    // Start TextLayoutMetrics.

    /// <inheritdoc />
    public TextSelection GetLineAtOffset(TextPosition position)
    {
        TextRange line = _textPainter.GetLineBoundary(position);
        // If text is obscured, the entire string should be treated as one line.
        if (ObscureText)
        {
            return new TextSelection(0, PlainText.Length);
        }

        return new TextSelection(line.Start, line.End);
    }

    /// <inheritdoc />
    public TextRange GetWordBoundary(TextPosition position) => _textPainter.GetWordBoundary(position);

    /// <inheritdoc />
    public TextPosition GetTextPositionAbove(TextPosition position)
    {
        // The caret offset gives a location in the upper left hand corner of the caret so the middle of
        // the line above is a half line above that point and the line below is 1.5 lines below that point.
        double preferredLineHeight = _textPainter.PreferredLineHeight;
        double verticalOffset = -0.5 * preferredLineHeight;
        return GetTextPositionVertical(position, verticalOffset);
    }

    /// <inheritdoc />
    public TextPosition GetTextPositionBelow(TextPosition position)
    {
        // The caret offset gives a location in the upper left hand corner of the caret so the middle of
        // the line above is a half line above that point and the line below is 1.5 lines below that point.
        double preferredLineHeight = _textPainter.PreferredLineHeight;
        double verticalOffset = 1.5 * preferredLineHeight;
        return GetTextPositionVertical(position, verticalOffset);
    }

    // End TextLayoutMetrics.

    private void UpdateSelectionExtentsVisibility(Point effectiveOffset)
    {
        DebugAssert(Selection is not null);
        TextSelection selection = Selection!.Value;
        if (!selection.IsValid)
        {
            _selectionStartInViewport.Value = false;
            _selectionEndInViewport.Value = false;
            return;
        }

        var visibleRegion = new Rect(Size);

        Point startOffset = _textPainter.GetOffsetForCaret(
            new TextPosition(selection.Start, selection.Affinity),
            _caretPrototype);
        // Check if the selection is visible with an approximation because a difference between rounded
        // and unrounded values causes the caret to be reported as having a slightly (< 0.5) negative y
        // offset. This rounding happens in paragraph.cc's layout and TextPainter's
        // _applyFloatingPointHack. Ideally, the rounding mismatch will be fixed and this can be changed
        // to be a strict check instead of an approximation.
        const double visibleRegionSlop = 0.5;
        _selectionStartInViewport.Value = Contains(
            visibleRegion.Inflate(visibleRegionSlop),
            startOffset + effectiveOffset);

        Point endOffset = _textPainter.GetOffsetForCaret(
            new TextPosition(selection.End, selection.Affinity),
            _caretPrototype);
        _selectionEndInViewport.Value = Contains(
            visibleRegion.Inflate(visibleRegionSlop),
            endOffset + effectiveOffset);
    }

    // Dart's `Rect.contains`: the left and top edges are inside, the right and bottom edges are not.
    private static bool Contains(Rect rect, Point point)
    {
        return point.X >= rect.Left && point.X < rect.Right && point.Y >= rect.Top && point.Y < rect.Bottom;
    }

    private void SetTextEditingValue(TextEditingValue newValue, SelectionChangedCause cause)
    {
        TextSelectionDelegate.UserUpdateTextEditingValue(newValue, cause);
    }

    private void SetSelection(TextSelection nextSelection, SelectionChangedCause cause)
    {
        if (nextSelection.IsValid)
        {
            // The nextSelection is calculated based on plainText, which can be different from the text
            // in TextEditingValue delegate. This happens when the text is empty or contains inline
            // widgets.
            int textLength = TextSelectionDelegate.TextEditingValue.Text.Length;
            nextSelection = nextSelection with
            {
                BaseOffset = Math.Min(nextSelection.BaseOffset, textLength),
                ExtentOffset = Math.Min(nextSelection.ExtentOffset, textLength),
            };
        }

        SetTextEditingValue(TextSelectionDelegate.TextEditingValue.CopyWith(selection: nextSelection), cause);
    }

    /// <inheritdoc />
    /// <remarks>Dart's override also repaints the two custom painter children.</remarks>
    public override void MarkNeedsPaint()
    {
        base.MarkNeedsPaint();
        // Tell the painters to repaint since text layout may have changed.
        _foregroundRenderObject?.MarkNeedsPaint();
        _backgroundRenderObject?.MarkNeedsPaint();
    }

    /// <summary>Returns a plain text version of the text in <see cref="TextPainter"/>.</summary>
    /// <remarks>If <see cref="ObscureText"/> is true, returns the obscured text. See
    /// <see cref="InlineSpan.ToPlainText"/>.</remarks>
    public string PlainText => _textPainter.PlainText;

    /// <summary>The text to paint in the form of a tree of <see cref="InlineSpan"/>s.</summary>
    /// <remarks>In order to get the plain text representation, use <see cref="PlainText"/>.</remarks>
    public InlineSpan? Text
    {
        get => _textPainter.Text;
        set
        {
            if (Equals(_textPainter.Text, value))
            {
                return;
            }

            _cachedLineBreakCount = null;
            _textPainter.Text = value;
            _cachedAttributedValue = null;
            _cachedCombinedSemanticsInfos = null;
            MarkNeedsLayout();
            MarkNeedsSemanticsUpdate();
        }
    }

    private TextPainter TextIntrinsics
    {
        get
        {
            TextPainter painter = _textIntrinsicsCache ??= new TextPainter();
            painter.Text = _textPainter.Text;
            painter.TextAlign = _textPainter.TextAlign;
            painter.TextDirection = _textPainter.TextDirection;
            painter.TextScaler = _textPainter.TextScaler;
            painter.MaxLines = _textPainter.MaxLines;
            painter.Ellipsis = _textPainter.Ellipsis;
            painter.Locale = _textPainter.Locale;
            painter.StrutStyle = _textPainter.StrutStyle;
            painter.TextWidthBasis = _textPainter.TextWidthBasis;
            painter.TextHeightBehavior = _textPainter.TextHeightBehavior;
            return painter;
        }
    }

    /// <summary>How the text should be aligned horizontally.</summary>
    public TextAlign TextAlign
    {
        get => _textPainter.TextAlign;
        set
        {
            if (_textPainter.TextAlign == value)
            {
                return;
            }

            _textPainter.TextAlign = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>The directionality of the text.</summary>
    /// <remarks>
    /// This decides how the <see cref="TextAlign.Start"/>, <see cref="TextAlign.End"/>, and
    /// <see cref="TextAlign.Justify"/> values of <see cref="TextAlign"/> are interpreted. This is also
    /// used to disambiguate how to render bidirectional text.
    /// </remarks>
    public TextDirection TextDirection
    {
        get => _textPainter.TextDirection!.Value;
        set
        {
            if (_textPainter.TextDirection == value)
            {
                return;
            }

            _textPainter.TextDirection = value;
            MarkNeedsLayout();
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <summary>Used by this renderer's internal <see cref="TextPainter"/> to select a locale-specific
    /// font.</summary>
    /// <remarks>If this value is null, a system-dependent algorithm is used to select the font.</remarks>
    public string? Locale
    {
        get => _textPainter.Locale;
        set
        {
            if (_textPainter.Locale == value)
            {
                return;
            }

            _textPainter.Locale = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>The <see cref="StrutStyle"/> used by the renderer's internal <see cref="TextPainter"/> to
    /// determine the strut to use.</summary>
    public StrutStyle? StrutStyle
    {
        get => _textPainter.StrutStyle;
        set
        {
            if (Equals(_textPainter.StrutStyle, value))
            {
                return;
            }

            _textPainter.StrutStyle = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>The color to use when painting the cursor.</summary>
    public Color? CursorColor
    {
        get => CaretPainterInstance.CaretColor;
        set => CaretPainterInstance.CaretColor = value;
    }

    /// <summary>The color to use when painting the cursor aligned to the text while rendering the
    /// floating cursor.</summary>
    /// <remarks>Typically this would be set to <c>CupertinoColors.inactiveGray</c>. If this is null,
    /// the background cursor is not painted.</remarks>
    public Color? BackgroundCursorColor
    {
        get => CaretPainterInstance.BackgroundCursorColor;
        set => CaretPainterInstance.BackgroundCursorColor = value;
    }

    /// <summary>Whether to paint the cursor.</summary>
    public ValueNotifier<bool> ShowCursor
    {
        get => _showCursor;
        set
        {
            if (ReferenceEquals(_showCursor, value))
            {
                return;
            }

            if (Attached)
            {
                _showCursor.RemoveListener(ShowHideCursor);
            }

            if (_disposeShowCursor)
            {
                _showCursor.Dispose();
                _disposeShowCursor = false;
            }

            _showCursor = value;
            if (Attached)
            {
                ShowHideCursor();
                _showCursor.AddListener(ShowHideCursor);
            }
        }
    }

    private void ShowHideCursor()
    {
        CaretPainterInstance.ShouldPaint = ShowCursor.Value;
    }

    /// <summary>Whether the editable is currently focused.</summary>
    public bool HasFocus
    {
        get => _hasFocus;
        set
        {
            if (_hasFocus == value)
            {
                return;
            }

            _hasFocus = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <summary>Whether this rendering object will take a full line regardless the text width.</summary>
    public bool ForceLine
    {
        get => _forceLine;
        set
        {
            if (_forceLine == value)
            {
                return;
            }

            _forceLine = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>Whether this rendering object is read only.</summary>
    public bool ReadOnly
    {
        get => _readOnly;
        set
        {
            if (_readOnly == value)
            {
                return;
            }

            _readOnly = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <summary>The maximum number of lines for the text to span, wrapping if necessary.</summary>
    /// <remarks>
    /// If this is 1 (the default), the text will not wrap, but will extend indefinitely instead. If this
    /// is null, there is no limit to the number of lines. When this is not null, the intrinsic height of
    /// the render object is the height of one line of text multiplied by this value. In other words, this
    /// also controls the height of the actual editing widget.
    /// </remarks>
    public int? MaxLines
    {
        get => _maxLines;
        set
        {
            DebugAssert(value is null || value > 0);
            if (MaxLines == value)
            {
                return;
            }

            _maxLines = value;

            // Special case maxLines == 1 to keep only the first line so we can get the height of the
            // first line in case there are hard line breaks in the text. See the `_preferredHeight`
            // method.
            _textPainter.MaxLines = value == 1 ? 1 : null;
            MarkNeedsLayout();
        }
    }

    /// <summary>The minimum number of lines to occupy when the content spans fewer lines.</summary>
    public int? MinLines
    {
        get => _minLines;
        set
        {
            DebugAssert(value is null || value > 0);
            if (MinLines == value)
            {
                return;
            }

            _minLines = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>Whether this widget's height will be sized to fill its parent.</summary>
    public bool Expands
    {
        get => _expands;
        set
        {
            if (Expands == value)
            {
                return;
            }

            _expands = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>The color to use when painting the selection.</summary>
    public Color? SelectionColor
    {
        get => _selectionPainter.HighlightColor;
        set => _selectionPainter.HighlightColor = value;
    }

    /// <summary>Deprecated. Will be removed in a future version of Flutter. Use
    /// <see cref="TextScaler"/> instead.</summary>
    [Obsolete("Use TextScaler instead. Use of textScaleFactor was deprecated in preparation for the "
              + "upcoming nonlinear text scaling support. This feature was deprecated after v3.12.0-2.0.pre.")]
    public double TextScaleFactor
    {
        get => _textPainter.TextScaleFactor;
        set => TextScaler = TextScaler.Linear(value);
    }

    /// <summary>The font scaling strategy to use when laying out and rendering the text.</summary>
    public TextScaler TextScaler
    {
        get => _textPainter.TextScaler;
        set
        {
            if (Equals(_textPainter.TextScaler, value))
            {
                return;
            }

            _textPainter.TextScaler = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>The region of text that is selected, if any.</summary>
    /// <remarks>The caret position is represented by a collapsed selection. If <see cref="Selection"/>
    /// is null, there is no selection and attempts to manipulate the selection will throw.</remarks>
    public TextSelection? Selection
    {
        get => _selection;
        set
        {
            if (Nullable.Equals(_selection, value))
            {
                return;
            }

            _selection = value;
            _selectionPainter.HighlightedRange = value?.AsTextRange();
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <summary>The offset at which the text should be painted.</summary>
    /// <remarks>If the text content is larger than the editable line itself, the editable line clips
    /// the text. This property controls which part of the text is visible by shifting the text by the
    /// given offset before clipping.</remarks>
    public ViewportOffset Offset
    {
        get => _offset;
        set
        {
            if (ReferenceEquals(_offset, value))
            {
                return;
            }

            if (Attached)
            {
                _offset.RemoveListener(MarkNeedsPaint);
            }

            _offset = value;
            if (Attached)
            {
                _offset.AddListener(MarkNeedsPaint);
            }

            MarkNeedsLayout();
        }
    }

    /// <summary>How thick the cursor will be.</summary>
    public double CursorWidth
    {
        get => _cursorWidth;
        set
        {
            if (_cursorWidth == value)
            {
                return;
            }

            _cursorWidth = value;
            MarkNeedsLayout();
        }
    }

    /// <summary>How tall the cursor will be.</summary>
    /// <remarks>This can be null, in which case the getter will actually return
    /// <see cref="PreferredLineHeight"/>. Setting this to itself fixes the value to the current
    /// <see cref="PreferredLineHeight"/>. Setting this to null returns the behavior of deferring to
    /// <see cref="PreferredLineHeight"/>.</remarks>
    public double? CursorHeight
    {
        get => _cursorHeight ?? PreferredLineHeight;
        set
        {
            if (_cursorHeight == value)
            {
                return;
            }

            _cursorHeight = value;
            MarkNeedsLayout();
        }
    }

    private double EffectiveCursorHeight => _cursorHeight ?? PreferredLineHeight;

    /// <summary>Whether to paint the cursor above the text.</summary>
    /// <remarks>
    /// On iOS devices, the cursor is painted above the text. On other platforms, it is painted under
    /// the text.
    /// </remarks>
    public bool PaintCursorAboveText
    {
        get => _paintCursorOnTop;
        set
        {
            if (_paintCursorOnTop == value)
            {
                return;
            }

            _paintCursorOnTop = value;
            // Clear cached built-in painters and reconfigure painters.
            _cachedBuiltInForegroundPainters = null;
            _cachedBuiltInPainters = null;
            // Call update methods to rebuild and set the effective painters.
            UpdateForegroundPainter(_foregroundPainter);
            UpdatePainter(_painter);
        }
    }

    /// <summary>The offset that is used, in pixels, when painting the cursor on screen.</summary>
    /// <remarks>By default, the cursor position should be set to an offset of (-cursorWidth * 0.5, 0.0)
    /// on iOS platforms and (0, 0) on Android platforms. The origin from where the offset is applied to
    /// is the arbitrary location where the cursor ends up being rendered from by default.</remarks>
    public Point CursorOffset
    {
        get => CaretPainterInstance.CursorOffset;
        set => CaretPainterInstance.CursorOffset = value;
    }

    /// <summary>How rounded the corners of the cursor should be.</summary>
    /// <remarks>A null value is the same as <c>Radius.zero</c>.</remarks>
    public Radius? CursorRadius
    {
        get => CaretPainterInstance.CursorRadius;
        set => CaretPainterInstance.CursorRadius = value;
    }

    /// <summary>The <see cref="LayerLink"/> of start selection handle.</summary>
    /// <remarks><see cref="RenderEditable"/> is responsible for calculating the <see cref="Point"/> of
    /// this <see cref="LayerLink"/>, which will be used as <see cref="CompositedTransformTarget"/> of
    /// start handle.</remarks>
    public LayerLink StartHandleLayerLink
    {
        get => _startHandleLayerLink;
        set
        {
            if (ReferenceEquals(_startHandleLayerLink, value))
            {
                return;
            }

            _startHandleLayerLink = value;
            MarkNeedsPaint();
        }
    }

    /// <summary>The <see cref="LayerLink"/> of end selection handle.</summary>
    public LayerLink EndHandleLayerLink
    {
        get => _endHandleLayerLink;
        set
        {
            if (ReferenceEquals(_endHandleLayerLink, value))
            {
                return;
            }

            _endHandleLayerLink = value;
            MarkNeedsPaint();
        }
    }

    /// <summary>Whether the floating cursor is currently shown.</summary>
    public bool FloatingCursorOn => _floatingCursorOn;

    /// <summary>If true, <see cref="HandleEvent"/> does nothing and it's assumed that this renderer will
    /// be notified of input gestures via <see cref="HandleTapDown"/>, <see cref="HandleTap"/>,
    /// <see cref="HandleDoubleTap"/>, and <see cref="HandleLongPress"/>.</summary>
    /// <remarks>If there are any gesture recognizers in the text span, the <see cref="HandleEvent"/>
    /// method will still call the gesture recognizers. The default value is false.</remarks>
    public bool? EnableInteractiveSelection
    {
        get => _enableInteractiveSelection;
        set
        {
            if (_enableInteractiveSelection == value)
            {
                return;
            }

            _enableInteractiveSelection = value;
            MarkNeedsLayout();
            MarkNeedsSemanticsUpdate();
        }
    }

    /// <summary>Whether interactive selection are enabled based on the values of
    /// <see cref="EnableInteractiveSelection"/> and <see cref="ObscureText"/>.</summary>
    /// <remarks>Since <see cref="EnableInteractiveSelection"/> defaults to null, the default is to
    /// return the opposite of <see cref="ObscureText"/>, or true if both are unset.</remarks>
    public bool SelectionEnabled => EnableInteractiveSelection ?? !ObscureText;

    /// <summary>The color used to paint the prompt rectangle.</summary>
    /// <remarks>The prompt rectangle will only be requested on non-web iOS applications. When set to
    /// null, the prompt rectangle will not be painted.</remarks>
    public Color? PromptRectColor
    {
        get => _autocorrectHighlightPainter.HighlightColor;
        set => _autocorrectHighlightPainter.HighlightColor = value;
    }

    /// <summary>Dismisses the currently displayed prompt rectangle and displays a new prompt rectangle
    /// over <paramref name="newRange"/> in the given color <see cref="PromptRectColor"/>.</summary>
    /// <remarks>The prompt rectangle will only be requested on non-web iOS applications. When set to
    /// null, the currently displayed prompt rectangle (if any) will be dismissed.</remarks>
    public void SetPromptRectRange(TextRange? newRange)
    {
        _autocorrectHighlightPainter.HighlightedRange = newRange;
    }

    /// <summary>The maximum amount the text is allowed to scroll.</summary>
    /// <remarks>This value is only valid after layout and can change as additional text is entered or
    /// removed in order to accommodate expanding when <see cref="Expands"/> is set to true.</remarks>
    public double MaxScrollExtent => _maxScrollExtent;

    private double CaretMargin => KCaretGap + CursorWidth;

    /// <summary>The content will be clipped (or not) according to this option.</summary>
    /// <remarks>Defaults to <see cref="Clip.HardEdge"/>.</remarks>
    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (value != _clipBehavior)
            {
                _clipBehavior = value;
                MarkNeedsPaint();
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    /// <summary>Returns a list of rects that bound the given selection, and the text direction. The
    /// text direction is used by the engine to calculate the closest position to a given point.</summary>
    /// <remarks>See <see cref="TextPainter.GetBoxesForSelection"/> for more details.</remarks>
    public IReadOnlyList<TextBox> GetBoxesForSelection(TextSelection selection)
    {
        ComputeTextMetricsIfNeeded();
        Point paintOffset = PaintOffset;
        return _textPainter
            .GetBoxesForSelection(selection, SelectionHeightStyle, SelectionWidthStyle)
            .Select(textBox => TextBox.FromLTRBD(
                textBox.Left + paintOffset.X,
                textBox.Top + paintOffset.Y,
                textBox.Right + paintOffset.X,
                textBox.Bottom + paintOffset.Y,
                textBox.Direction))
            .ToList();
    }

    // -- Semantics ------------------------------------------------------------------------------------

    /// <inheritdoc />
    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration config)
    {
        base.DescribeSemanticsConfiguration(config);
        _semanticsInfo = _textPainter.Text!.GetSemanticsInformation();
        // TODO(chunhtai): the macOS does not provide a public API to support text selections across
        // multiple semantics nodes. Remove this platform check once we can support it.
        // https://github.com/flutter/flutter/issues/77957
        if (_semanticsInfo.Any(info => info.Recognizer is not null)
            && PlatformDefaults.TargetPlatform != TargetPlatform.MacOS)
        {
            DebugAssert(ReadOnly && !ObscureText);
            // For Selectable rich text with recognizer, we need to create a semantics node for each text
            // fragment.
            config.IsSemanticBoundary = true;
            config.ExplicitChildNodes = true;
            return;
        }

        if (_cachedAttributedValue is null)
        {
            if (ObscureText)
            {
                _cachedAttributedValue = new AttributedString(
                    string.Concat(Enumerable.Repeat(ObscuringCharacter, PlainText.Length)));
            }
            else
            {
                var buffer = new StringBuilder();
                int offset = 0;
                var attributes = new List<StringAttribute>();
                foreach (InlineSpanSemanticsInformation info in _semanticsInfo)
                {
                    string label = info.SemanticsLabel ?? info.Text;
                    foreach (StringAttribute infoAttribute in info.StringAttributes)
                    {
                        TextRange originalRange = infoAttribute.Range;
                        attributes.Add(infoAttribute.Copy(new TextRange(
                            offset + originalRange.Start,
                            offset + originalRange.End)));
                    }

                    buffer.Append(label);
                    offset += label.Length;
                }

                _cachedAttributedValue = new AttributedString(buffer.ToString(), attributes);
            }
        }

        config.AttributedValue = _cachedAttributedValue;
        config.IsObscured = ObscureText;
        config.IsMultiline = IsMultiline;
        config.TextDirection = TextDirection;
        config.IsFocused = HasFocus;
        config.IsFocusable = true;
        config.IsTextField = true;
        config.IsReadOnly = ReadOnly;
        config.InputType = SemanticsInputType.Text;

        if (HasFocus && SelectionEnabled)
        {
            config.OnSetSelection = HandleSetSelection;
        }

        if (HasFocus && !ReadOnly)
        {
            config.OnSetText = HandleSetText;
        }

        if (SelectionEnabled && Selection is { IsValid: true } selection)
        {
            config.TextSelection = selection;
            if (_textPainter.GetOffsetBefore(selection.ExtentOffset) is not null)
            {
                config.OnMoveCursorBackwardByWord = HandleMoveCursorBackwardByWord;
                config.OnMoveCursorBackwardByCharacter = HandleMoveCursorBackwardByCharacter;
            }

            if (_textPainter.GetOffsetAfter(selection.ExtentOffset) is not null)
            {
                config.OnMoveCursorForwardByWord = HandleMoveCursorForwardByWord;
                config.OnMoveCursorForwardByCharacter = HandleMoveCursorForwardByCharacter;
            }
        }
    }

    private void HandleSetText(string text)
    {
        TextSelectionDelegate.UserUpdateTextEditingValue(
            new TextEditingValue(text, TextSelection.Collapsed(text.Length)),
            SelectionChangedCause.Keyboard);
    }

    /// <inheritdoc />
    protected override void AssembleSemanticsNode(
        SemanticsNode node,
        SemanticsConfiguration config,
        IReadOnlyList<SemanticsNode> children)
    {
        DebugAssert(_semanticsInfo is { Count: > 0 });
        var newChildren = new List<SemanticsNode>();
        TextDirection currentDirection = TextDirection;
        Rect currentRect;
        double ordinal = 0.0;
        int start = 0;
        int placeholderIndex = 0;
        int childIndex = 0;
        RenderBox? child = FirstChild;
        var newChildCache = new OrderedDictionary<UniqueKey, SemanticsNode>();
        _cachedCombinedSemanticsInfos ??= InlineSpan.CombineSemanticsInfo(_semanticsInfo!);
        foreach (InlineSpanSemanticsInformation info in _cachedCombinedSemanticsInfos)
        {
            var selection = new TextSelection(start, start + info.Text.Length);
            start += info.Text.Length;

            if (info.IsPlaceholder)
            {
                // A placeholder span may have 0 to multiple semantics nodes, we need to annotate all of
                // the semantics nodes belong to this span.
                while (children.Count > childIndex
                       && children[childIndex].IsTagged(new PlaceholderSpanIndexSemanticsTag(placeholderIndex)))
                {
                    SemanticsNode childNode = children[childIndex];
                    var parentData = (TextParentData)child!.parentData!;
                    DebugAssert(parentData.InlineOffset is not null);
                    newChildren.Add(childNode);
                    childIndex += 1;
                }

                child = ChildAfter(child!);
                placeholderIndex += 1;
            }
            else
            {
                TextDirection initialDirection = currentDirection;
                IReadOnlyList<TextBox> rects = _textPainter.GetBoxesForSelection(selection);
                if (rects.Count == 0)
                {
                    continue;
                }

                Rect rect = rects[0].ToRect();
                currentDirection = rects[0].Direction;
                foreach (TextBox textBox in rects.Skip(1))
                {
                    rect = rect.Union(textBox.ToRect());
                    currentDirection = textBox.Direction;
                }

                // Any of the text boxes may have had infinite dimensions. We shouldn't pass infinite
                // dimensions up to the bridges.
                rect = new Rect(
                    Math.Max(0.0, rect.Left),
                    Math.Max(0.0, rect.Top),
                    Math.Min(rect.Width, Constraints.MaxWidth),
                    Math.Min(rect.Height, Constraints.MaxHeight));
                // Round the current rectangle to make this API testable and add some padding so that
                // the accessibility rects do not overlap with the text.
                currentRect = FromLTRB(
                    Math.Floor(rect.Left) - 4.0,
                    Math.Floor(rect.Top) - 4.0,
                    Math.Ceiling(rect.Right) + 4.0,
                    Math.Ceiling(rect.Bottom) + 4.0);
                var configuration = new SemanticsConfiguration
                {
                    SortKey = new OrdinalSortKey(ordinal++),
                    TextDirection = initialDirection,
                    AttributedLabel = new AttributedString(
                        info.SemanticsLabel ?? info.Text,
                        info.StringAttributes),
                };
                switch (info.Recognizer)
                {
                    case TapGestureRecognizer { OnTap: var handler }:
                        if (handler is not null)
                        {
                            configuration.OnTap = handler;
                            configuration.IsLink = true;
                        }

                        break;
                    case DoubleTapGestureRecognizer { OnDoubleTap: var handler }:
                        if (handler is not null)
                        {
                            configuration.OnTap = handler;
                            configuration.IsLink = true;
                        }

                        break;
                    case LongPressGestureRecognizer { OnLongPress: var onLongPress }:
                        if (onLongPress is not null)
                        {
                            configuration.OnLongPress = onLongPress;
                        }

                        break;
                    case null:
                        break;
                    default:
                        DebugAssert(false, $"{info.Recognizer.GetType()} is not supported.");
                        break;
                }

                if (node.ParentPaintClipRect is { } parentPaintClipRect)
                {
                    Rect paintRect = parentPaintClipRect.Intersect(currentRect);
                    configuration.IsHidden = IsEmpty(paintRect) && !IsEmpty(currentRect);
                }

                SemanticsNode newChild;
                if (_cachedChildNodes is { Count: > 0 })
                {
                    UniqueKey firstKey = _cachedChildNodes.GetAt(0).Key;
                    newChild = _cachedChildNodes[firstKey];
                    _cachedChildNodes.Remove(firstKey);
                    newChildCache[firstKey] = newChild;
                }
                else
                {
                    var key = new UniqueKey();
                    newChild = new SemanticsNode(GetType().Name, CreateShowOnScreenFor(key));
                    newChildCache[key] = newChild;
                }

                newChild.UpdateWith(configuration);
                newChild.Rect = currentRect;
                newChildren.Add(newChild);
            }
        }

        _cachedChildNodes = newChildCache;
        node.UpdateWith(config, newChildren);
    }

    private static Rect FromLTRB(double left, double top, double right, double bottom)
    {
        return new Rect(left, top, Math.Max(0.0, right - left), Math.Max(0.0, bottom - top));
    }

    // Dart's `Rect.isEmpty`.
    private static bool IsEmpty(Rect rect) => rect.Left >= rect.Right || rect.Top >= rect.Bottom;

    private Action CreateShowOnScreenFor(UniqueKey key)
    {
        return () =>
        {
            SemanticsNode node = _cachedChildNodes![key];
            ShowOnScreen(descendant: this, rect: node.Rect);
        };
    }

    // TODO(ianh): in theory, [selection] could become null between when we last called
    // describeSemanticsConfiguration and when the callbacks are invoked, in which case the callbacks
    // will crash...

    private void HandleSetSelection(TextSelection selection)
    {
        SetSelection(selection, SelectionChangedCause.Keyboard);
    }

    private void HandleMoveCursorForwardByCharacter(bool extendSelection)
    {
        DebugAssert(Selection is not null);
        TextSelection selection = Selection!.Value;
        int? extentOffset = _textPainter.GetOffsetAfter(selection.ExtentOffset);
        if (extentOffset is null)
        {
            return;
        }

        int baseOffset = !extendSelection ? extentOffset.Value : selection.BaseOffset;
        SetSelection(new TextSelection(baseOffset, extentOffset.Value), SelectionChangedCause.Keyboard);
    }

    private void HandleMoveCursorBackwardByCharacter(bool extendSelection)
    {
        DebugAssert(Selection is not null);
        TextSelection selection = Selection!.Value;
        int? extentOffset = _textPainter.GetOffsetBefore(selection.ExtentOffset);
        if (extentOffset is null)
        {
            return;
        }

        int baseOffset = !extendSelection ? extentOffset.Value : selection.BaseOffset;
        SetSelection(new TextSelection(baseOffset, extentOffset.Value), SelectionChangedCause.Keyboard);
    }

    private void HandleMoveCursorForwardByWord(bool extendSelection)
    {
        DebugAssert(Selection is not null);
        TextSelection selection = Selection!.Value;
        TextRange currentWord = _textPainter.GetWordBoundary(selection.Extent);
        TextRange? nextWord = GetNextWord(currentWord.End);
        if (nextWord is null)
        {
            return;
        }

        int baseOffset = extendSelection ? selection.BaseOffset : nextWord.Value.Start;
        SetSelection(new TextSelection(baseOffset, nextWord.Value.Start), SelectionChangedCause.Keyboard);
    }

    private void HandleMoveCursorBackwardByWord(bool extendSelection)
    {
        DebugAssert(Selection is not null);
        TextSelection selection = Selection!.Value;
        TextRange currentWord = _textPainter.GetWordBoundary(selection.Extent);
        TextRange? previousWord = GetPreviousWord(currentWord.Start - 1);
        if (previousWord is null)
        {
            return;
        }

        int baseOffset = extendSelection ? selection.BaseOffset : previousWord.Value.Start;
        SetSelection(
            new TextSelection(baseOffset, previousWord.Value.Start),
            SelectionChangedCause.Keyboard);
    }

    private TextRange? GetNextWord(int offset)
    {
        while (true)
        {
            TextRange range = _textPainter.GetWordBoundary(new TextPosition(offset));
            if (!range.IsValid || range.IsCollapsed)
            {
                return null;
            }

            if (!OnlyWhitespace(range))
            {
                return range;
            }

            offset = range.End;
        }
    }

    private TextRange? GetPreviousWord(int offset)
    {
        while (offset >= 0)
        {
            TextRange range = _textPainter.GetWordBoundary(new TextPosition(offset));
            if (!range.IsValid || range.IsCollapsed)
            {
                return null;
            }

            if (!OnlyWhitespace(range))
            {
                return range;
            }

            offset = range.Start - 1;
        }

        return null;
    }

    // Check if the given text range only contains white space or separator characters.
    //
    // Includes newline characters from ASCII and separators from the
    // [unicode separator category](https://www.compart.com/en/unicode/category/Zs)
    // TODO(zanderso): replace when we expose this ICU information.
    private bool OnlyWhitespace(TextRange range)
    {
        for (int i = range.Start; i < range.End; i++)
        {
            int codeUnit = Text!.CodeUnitAt(i)!.Value;
            if (!ITextLayoutMetrics.IsWhitespace(codeUnit))
            {
                return false;
            }
        }

        return true;
    }

    // -- Lifecycle ----------------------------------------------------------------------------------

    /// <inheritdoc />
    /// <remarks>Dart's <c>attach</c>: the base walk has already attached the painter children.</remarks>
    protected override void OnAttach()
    {
        base.OnAttach();
        _tap = new TapGestureRecognizer { DebugOwner = this };
        _tap.OnTapDown = HandleTapDownFromRecognizer;
        _tap.OnTap = HandleTapFromRecognizer;
        _longPress = new LongPressGestureRecognizer { DebugOwner = this };
        _longPress.OnLongPress = HandleLongPressFromRecognizer;
        _offset.AddListener(MarkNeedsPaint);
        ShowHideCursor();
        _showCursor.AddListener(ShowHideCursor);
    }

    /// <inheritdoc />
    protected override void OnDetach()
    {
        _tap!.Dispose();
        _longPress!.Dispose();
        _offset.RemoveListener(MarkNeedsPaint);
        _showCursor.RemoveListener(ShowHideCursor);
        base.OnDetach();
    }

    /// <inheritdoc />
    /// <remarks>Dart visits the foreground painter child, then the background painter child, then the
    /// inline children.</remarks>
    public override void VisitChildren(Action<RenderObject> visitor)
    {
        RenderObject? foregroundChild = _foregroundRenderObject;
        RenderObject? backgroundChild = _backgroundRenderObject;
        if (foregroundChild is not null)
        {
            visitor(foregroundChild);
        }

        if (backgroundChild is not null)
        {
            visitor(backgroundChild);
        }

        _container.VisitChildren(visitor);
    }

    private bool IsMultiline => MaxLines != 1;

    private Axis ViewportAxis => IsMultiline ? Axis.Vertical : Axis.Horizontal;

    /// <summary>The offset the text is painted at, which is the negated scroll offset along the
    /// viewport axis.</summary>
    internal Point PaintOffset => ViewportAxis switch
    {
        Axis.Horizontal => new Point(-Offset.Pixels, 0.0),
        _ => new Point(0.0, -Offset.Pixels),
    };

    private double ViewportExtent
    {
        get
        {
            DebugAssert(HasSize);
            return ViewportAxis switch
            {
                Axis.Horizontal => Size.Width,
                _ => Size.Height,
            };
        }
    }

    private double GetMaxScrollExtent(Size contentSize)
    {
        DebugAssert(HasSize);
        return ViewportAxis switch
        {
            Axis.Horizontal => Math.Max(0.0, contentSize.Width - Size.Width),
            _ => Math.Max(0.0, contentSize.Height - Size.Height),
        };
    }

    // We need to check the paint offset here because during animation, the start of the text may
    // position outside the visible region even when the text fits.
    private bool HasVisualOverflow => _maxScrollExtent > 0 || PaintOffset != default;

    /// <summary>Returns the local coordinates of the endpoints of the given selection.</summary>
    /// <remarks>
    /// If the selection is collapsed (and therefore occupies a single point), the returned list is of
    /// length one. Otherwise, the selection is not collapsed and the returned list is of length two. In
    /// this case, however, the two points might actually be co-located (e.g., because of a bidirectional
    /// selection that contains some text but whose ends meet in the middle).
    /// </remarks>
    public IReadOnlyList<TextSelectionPoint> GetEndpointsForSelection(TextSelection selection)
    {
        ComputeTextMetricsIfNeeded();

        Point paintOffset = PaintOffset;

        IReadOnlyList<TextBox> boxes = selection.IsCollapsed
            ? []
            : _textPainter.GetBoxesForSelection(selection, SelectionHeightStyle, SelectionWidthStyle);
        if (boxes.Count == 0)
        {
            // TODO(mpcomplete): This doesn't work well at an RTL/LTR boundary.
            Point caretOffset = _textPainter.GetOffsetForCaret(selection.Extent, _caretPrototype);
            var start = new Point(
                caretOffset.X + paintOffset.X,
                PreferredLineHeight + caretOffset.Y + paintOffset.Y);
            return [new TextSelectionPoint(start, null)];
        }
        else
        {
            var start = new Point(
                Math.Clamp(boxes[0].Start, 0, _textPainter.Size.Width) + paintOffset.X,
                boxes[0].Bottom + paintOffset.Y);
            var end = new Point(
                Math.Clamp(boxes[^1].End, 0, _textPainter.Size.Width) + paintOffset.X,
                boxes[^1].Bottom + paintOffset.Y);
            return
            [
                new TextSelectionPoint(start, boxes[0].Direction),
                new TextSelectionPoint(end, boxes[^1].Direction),
            ];
        }
    }

    /// <summary>Returns the smallest <see cref="Rect"/>, in the local coordinate system, that covers the
    /// text within the <see cref="TextRange"/> specified.</summary>
    /// <remarks>This method is used to calculate the approximate position of the IME bar on iOS.
    /// Returns null if <see cref="TextRange.IsValid"/> is false for the given range, or the given range
    /// is collapsed.</remarks>
    public Rect? GetRectForComposingRange(TextRange range)
    {
        if (!range.IsValid || range.IsCollapsed)
        {
            return null;
        }

        ComputeTextMetricsIfNeeded();

        IReadOnlyList<TextBox> boxes = _textPainter.GetBoxesForSelection(
            new TextSelection(range.Start, range.End),
            SelectionHeightStyle,
            SelectionWidthStyle);

        Rect? accumulated = null;
        foreach (TextBox incoming in boxes)
        {
            accumulated = accumulated?.Union(incoming.ToRect()) ?? incoming.ToRect();
        }

        return accumulated?.Translate(new Vector(PaintOffset.X, PaintOffset.Y));
    }

    /// <summary>Returns the position in the text for the given global coordinate.</summary>
    /// <remarks>See also <see cref="GetLocalRectForCaret"/>, which is the reverse operation, taking a
    /// <see cref="TextPosition"/> and returning a <see cref="Rect"/>, and
    /// <see cref="TextPainter.GetPositionForOffset"/>, which is the equivalent method for a
    /// <see cref="TextPainter"/> object.</remarks>
    public TextPosition GetPositionForPoint(Point globalPosition)
    {
        ComputeTextMetricsIfNeeded();
        return _textPainter.GetPositionForOffset(GlobalToLocal(globalPosition) - PaintOffset);
    }

    /// <summary>Returns the <see cref="Rect"/> in local coordinates for the caret at the given text
    /// position.</summary>
    /// <remarks>See also <see cref="GetPositionForPoint"/>, which is the reverse operation, taking an
    /// <see cref="Point"/> in global coordinates and returning a <see cref="TextPosition"/>.</remarks>
    public Rect GetLocalRectForCaret(TextPosition caretPosition)
    {
        ComputeTextMetricsIfNeeded();
        Rect caretPrototype = _caretPrototype;
        Point caretOffset = _textPainter.GetOffsetForCaret(caretPosition, caretPrototype);
        Rect caretRect = caretPrototype.Translate(new Vector(
            caretOffset.X + CursorOffset.X,
            caretOffset.Y + CursorOffset.Y));
        double scrollableWidth = Math.Max(_textPainter.Width + CaretMargin, Size.Width);

        double caretX = Math.Clamp(caretRect.Left, 0, Math.Max(scrollableWidth - CaretMargin, 0));
        caretRect = new Rect(new Point(caretX, caretRect.Top), caretRect.Size);

        double fullHeight = _textPainter.GetFullHeightForCaret(caretPosition, caretPrototype);
        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.IOS:
            case TargetPlatform.MacOS:
            {
                // Center the caret vertically along the text.
                double heightDiff = fullHeight - caretRect.Height;
                caretRect = new Rect(
                    caretRect.Left,
                    caretRect.Top + (heightDiff / 2),
                    caretRect.Width,
                    caretRect.Height);
                break;
            }
            case TargetPlatform.Android:
            case TargetPlatform.Fuchsia:
            case TargetPlatform.Linux:
            case TargetPlatform.Windows:
            {
                // Override the height to take the full height of the glyph at the TextPosition when not
                // on iOS. iOS has special handling that creates a taller caret.
                // TODO(garyq): See the TODO for _computeCaretPrototype().
                double caretHeight = EffectiveCursorHeight;
                double heightDiff = fullHeight - caretHeight;
                caretRect = new Rect(
                    caretRect.Left,
                    caretRect.Top - KCaretHeightOffset + (heightDiff / 2),
                    caretRect.Width,
                    caretHeight);
                break;
            }
        }

        caretRect = caretRect.Translate(new Vector(PaintOffset.X, PaintOffset.Y));
        Point snap = SnapToPhysicalPixel(caretRect.TopLeft);
        return caretRect.Translate(new Vector(snap.X, snap.Y));
    }

    // -- Intrinsics ----------------------------------------------------------------------------------

    /// <inheritdoc />
    protected override double ComputeMinIntrinsicWidth(double height)
    {
        List<PlaceholderDimensions> placeholderDimensions = RenderInlineChildrenContainerDefaults
            .LayoutInlineChildren(
                InlineChildren,
                double.PositiveInfinity,
                (child, _) => new Size(child.GetMinIntrinsicWidth(double.PositiveInfinity), 0.0),
                ChildLayoutHelper.GetDryBaseline);
        (double minWidth, double maxWidth) = AdjustConstraints();
        TextPainter intrinsics = TextIntrinsics;
        intrinsics.SetPlaceholderDimensions(placeholderDimensions);
        intrinsics.Layout(minWidth, maxWidth);
        return intrinsics.MinIntrinsicWidth;
    }

    /// <inheritdoc />
    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        List<PlaceholderDimensions> placeholderDimensions = RenderInlineChildrenContainerDefaults
            .LayoutInlineChildren(
                InlineChildren,
                double.PositiveInfinity,
                // Height and baseline is irrelevant as all text will be laid out in a single line.
                (child, _) => new Size(child.GetMaxIntrinsicWidth(double.PositiveInfinity), 0.0),
                ChildLayoutHelper.GetDryBaseline);
        (double minWidth, double maxWidth) = AdjustConstraints();
        TextPainter intrinsics = TextIntrinsics;
        intrinsics.SetPlaceholderDimensions(placeholderDimensions);
        intrinsics.Layout(minWidth, maxWidth);
        return intrinsics.MaxIntrinsicWidth + CaretMargin;
    }

    /// <summary>An estimate of the height of a line in the text. See
    /// <see cref="TextPainter.PreferredLineHeight"/>. This does not require the layout to be
    /// updated.</summary>
    public double PreferredLineHeight => _textPainter.PreferredLineHeight;

    private int CountHardLineBreaks(string text)
    {
        if (_cachedLineBreakCount is { } cachedValue)
        {
            return cachedValue;
        }

        int count = 0;
        for (int index = 0; index < text.Length; index += 1)
        {
            switch (text[index])
            {
                case '\u000A': // LF
                case '\u0085': // NEL
                case '\u000B': // VT
                case '\u000C': // FF, treating it as a regular line separator
                case '\u2028': // LS
                case '\u2029': // PS
                    count += 1;
                    break;
            }
        }

        _cachedLineBreakCount = count;
        return count;
    }

    private double PreferredHeight(double width)
    {
        int? maxLines = MaxLines;
        int? minLines = MinLines ?? maxLines;
        double minHeight = PreferredLineHeight * (minLines ?? 0);
        DebugAssert(maxLines != 1 || TextIntrinsics.MaxLines == 1);

        if (maxLines is null)
        {
            double estimatedHeight;
            if (double.IsPositiveInfinity(width))
            {
                estimatedHeight = PreferredLineHeight * (CountHardLineBreaks(PlainText) + 1);
            }
            else
            {
                (double minWidth, double maxWidth) = AdjustConstraints(maxWidth: width);
                TextPainter painter = TextIntrinsics;
                painter.Layout(minWidth, maxWidth);
                estimatedHeight = painter.Height;
            }

            return Math.Max(estimatedHeight, minHeight);
        }

        // Special case maxLines == 1 since it forces the scrollable direction to be horizontal. Report
        // the real height to prevent the text from being clipped.
        if (maxLines == 1)
        {
            // The _layoutText call lays out the paragraph using infinite width when maxLines == 1. Also
            // _textPainter.maxLines will be set to 1 so should there be any line breaks only the first
            // line is shown.
            (double minWidth, double maxWidth) = AdjustConstraints(maxWidth: width);
            TextPainter painter = TextIntrinsics;
            painter.Layout(minWidth, maxWidth);
            return painter.Height;
        }

        if (minLines == maxLines)
        {
            return minHeight;
        }

        double maxHeight = PreferredLineHeight * maxLines.Value;
        (double adjustedMinWidth, double adjustedMaxWidth) = AdjustConstraints(maxWidth: width);
        TextPainter textIntrinsics = TextIntrinsics;
        textIntrinsics.Layout(adjustedMinWidth, adjustedMaxWidth);
        return Math.Clamp(textIntrinsics.Height, minHeight, maxHeight);
    }

    /// <inheritdoc />
    protected override double ComputeMinIntrinsicHeight(double width) => GetMaxIntrinsicHeight(width);

    /// <inheritdoc />
    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        TextIntrinsics.SetPlaceholderDimensions(RenderInlineChildrenContainerDefaults.LayoutInlineChildren(
            InlineChildren,
            width,
            ChildLayoutHelper.DryLayoutChild,
            ChildLayoutHelper.GetDryBaseline));
        return PreferredHeight(width);
    }

    /// <inheritdoc />
    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        ComputeTextMetricsIfNeeded();
        return _textPainter.ComputeDistanceToActualBaseline(baseline);
    }

    // -- Hit testing and gestures ---------------------------------------------------------------------

    /// <inheritdoc />
    protected override bool HitTestSelf(Point position) => true;

    /// <inheritdoc />
    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        Point effectivePosition = position - PaintOffset;
        GlyphInfo? glyph = _textPainter.GetClosestGlyphForOffset(effectivePosition);
        // The hit-test can't fall through the horizontal gaps between visually adjacent characters on
        // the same line, even with a large letter-spacing or text justification, as
        // graphemeClusterLayoutBounds.width is the advance width to the next character, so there's no
        // gap between their graphemeClusterLayoutBounds rects.
        InlineSpan? spanHit = glyph is not null && Contains(glyph.GraphemeClusterLayoutBounds, effectivePosition)
            ? _textPainter.Text!.GetSpanForPosition(new TextPosition(glyph.GraphemeClusterCodeUnitRange.Start))
            : null;
        switch (spanHit)
        {
            case IHitTestTarget span:
                result.Add(new HitTestEntry(span));
                return true;
            default:
                return RenderInlineChildrenContainerDefaults.HitTestInlineChildren(
                    InlineChildren,
                    result,
                    effectivePosition);
        }
    }

    /// <inheritdoc />
    public override void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        DebugAssert(DebugHandleEvent(@event, entry));
        if (@event is PointerDownEvent downEvent)
        {
            DebugAssert(!DebugNeedsLayout);

            if (!IgnorePointer)
            {
                // Propagates the pointer event to selection handlers.
                _tap!.AddPointer(downEvent);
                _longPress!.AddPointer(downEvent);
            }
        }
    }

    /// <summary>The position of the most recent secondary tap down event on this text input.</summary>
    public Point? LastSecondaryTapDownPosition => _lastSecondaryTapDownPosition;

    /// <summary>Tracks the position of a secondary tap event.</summary>
    /// <remarks>Should be called before attempting to change the selection based on the position of a
    /// secondary tap.</remarks>
    public void HandleSecondaryTapDown(TapDownDetails details)
    {
        _lastTapDownPosition = details.GlobalPosition;
        _lastSecondaryTapDownPosition = details.GlobalPosition;
    }

    /// <summary>If <see cref="IgnorePointer"/> is false (the default) then this method is called by the
    /// internal gesture recognizer's <c>TapGestureRecognizer.OnTapDown</c> callback.</summary>
    /// <remarks>When <see cref="IgnorePointer"/> is true, an ancestor widget must respond to tap down
    /// events by calling this method.</remarks>
    public void HandleTapDown(TapDownDetails details)
    {
        _lastTapDownPosition = details.GlobalPosition;
    }

    private void HandleTapDownFromRecognizer(TapDownDetails details)
    {
        DebugAssert(!IgnorePointer);
        HandleTapDown(details);
    }

    /// <summary>If <see cref="IgnorePointer"/> is false (the default) then this method is called by the
    /// internal gesture recognizer's <c>TapGestureRecognizer.OnTap</c> callback.</summary>
    /// <remarks>When <see cref="IgnorePointer"/> is true, an ancestor widget must respond to tap events
    /// by calling this method.</remarks>
    public void HandleTap()
    {
        SelectPosition(cause: SelectionChangedCause.Tap);
    }

    private void HandleTapFromRecognizer()
    {
        DebugAssert(!IgnorePointer);
        HandleTap();
    }

    /// <summary>If <see cref="IgnorePointer"/> is false (the default) then this method is called by the
    /// internal gesture recognizer's <c>DoubleTapGestureRecognizer.OnDoubleTap</c> callback.</summary>
    /// <remarks>When <see cref="IgnorePointer"/> is true, an ancestor widget must respond to double tap
    /// events by calling this method.</remarks>
    public void HandleDoubleTap()
    {
        SelectWord(cause: SelectionChangedCause.DoubleTap);
    }

    /// <summary>If <see cref="IgnorePointer"/> is false (the default) then this method is called by the
    /// internal gesture recognizer's <c>LongPressGestureRecognizer.OnLongPress</c> callback.</summary>
    /// <remarks>When <see cref="IgnorePointer"/> is true, an ancestor widget must respond to long press
    /// events by calling this method.</remarks>
    public void HandleLongPress()
    {
        SelectWord(cause: SelectionChangedCause.LongPress);
    }

    private void HandleLongPressFromRecognizer()
    {
        DebugAssert(!IgnorePointer);
        HandleLongPress();
    }

    /// <summary>Move selection to the location of the last tap down.</summary>
    /// <remarks>This method is mainly used to translate user inputs in global positions into a
    /// <see cref="TextSelection"/>. When used in conjunction with an <see cref="EditableText"/>, the
    /// selection change is fed back into <c>TextEditingController.Selection</c>. If you have a
    /// <see cref="TextEditingController"/>, it's generally easier to programmatically manipulate its
    /// <c>Value</c> or <c>Selection</c> directly.</remarks>
    public void SelectPosition(SelectionChangedCause cause)
    {
        SelectPositionAt(from: _lastTapDownPosition!.Value, cause: cause);
    }

    /// <summary>Select text between the global positions <paramref name="from"/> and
    /// <paramref name="to"/>.</summary>
    /// <remarks><paramref name="from"/> corresponds to the <see cref="TextSelection.BaseOffset"/>, and
    /// <paramref name="to"/> corresponds to the <see cref="TextSelection.ExtentOffset"/>.</remarks>
    public void SelectPositionAt(Point from, SelectionChangedCause cause, Point? to = null)
    {
        ComputeTextMetricsIfNeeded();
        TextPosition fromPosition = _textPainter.GetPositionForOffset(GlobalToLocal(from) - PaintOffset);
        TextPosition? toPosition = to is null
            ? null
            : _textPainter.GetPositionForOffset(GlobalToLocal(to.Value) - PaintOffset);

        int baseOffset = fromPosition.Offset;
        int extentOffset = toPosition?.Offset ?? fromPosition.Offset;

        var newSelection = new TextSelection(baseOffset, extentOffset, fromPosition.Affinity);

        SetSelection(newSelection, cause);
    }

    /// <summary>A list of text boundaries of the text laid out.</summary>
    /// <remarks>Dart's <c>RenderEditable.wordBoundaries</c>.</remarks>
    public WordBoundary WordBoundaries => _textPainter.WordBoundaries;

    /// <summary>Select a word around the location of the last tap down.</summary>
    public void SelectWord(SelectionChangedCause cause)
    {
        SelectWordsInRange(from: _lastTapDownPosition!.Value, cause: cause);
    }

    /// <summary>Selects the set words of a paragraph that intersect a given range of global
    /// positions.</summary>
    /// <remarks>The set of words selected are not strictly bounded by the range of global positions.
    /// The first and last endpoints of the selection will always be at the beginning and end of a word
    /// respectively.</remarks>
    public void SelectWordsInRange(Point from, SelectionChangedCause cause, Point? to = null)
    {
        ComputeTextMetricsIfNeeded();
        TextPosition fromPosition = _textPainter.GetPositionForOffset(GlobalToLocal(from) - PaintOffset);
        TextSelection fromWord = GetWordAtOffset(fromPosition);
        TextPosition toPosition = to is null
            ? fromPosition
            : _textPainter.GetPositionForOffset(GlobalToLocal(to.Value) - PaintOffset);
        TextSelection toWord = toPosition == fromPosition ? fromWord : GetWordAtOffset(toPosition);
        bool isFromWordBeforeToWord = fromWord.Start < toWord.End;

        SetSelection(
            new TextSelection(
                isFromWordBeforeToWord ? fromWord.Base.Offset : fromWord.Extent.Offset,
                isFromWordBeforeToWord ? toWord.Extent.Offset : toWord.Base.Offset,
                fromWord.Affinity),
            cause);
    }

    /// <summary>Move the selection to the beginning or end of a word.</summary>
    public void SelectWordEdge(SelectionChangedCause cause)
    {
        ComputeTextMetricsIfNeeded();
        DebugAssert(_lastTapDownPosition is not null);
        TextPosition position = _textPainter.GetPositionForOffset(
            GlobalToLocal(_lastTapDownPosition!.Value) - PaintOffset);
        TextRange word = _textPainter.GetWordBoundary(position);
        TextSelection newSelection;
        if (position.Offset <= word.Start)
        {
            newSelection = TextSelection.Collapsed(word.Start);
        }
        else
        {
            newSelection = TextSelection.Collapsed(word.End, TextAffinity.Upstream);
        }

        SetSelection(newSelection, cause);
    }

    /// <summary>Returns a <see cref="TextSelection"/> that encompasses the word at the given
    /// <see cref="TextPosition"/>.</summary>
    /// <remarks>Dart marks this <c>@visibleForTesting</c>.</remarks>
    public TextSelection GetWordAtOffset(TextPosition position)
    {
        // When long-pressing past the end of the text, we want a collapsed cursor.
        if (position.Offset >= PlainText.Length)
        {
            return TextSelection.FromPosition(new TextPosition(PlainText.Length, TextAffinity.Upstream));
        }

        // If text is obscured, the entire sentence should be treated as one word.
        if (ObscureText)
        {
            return new TextSelection(0, PlainText.Length);
        }

        TextRange word = _textPainter.GetWordBoundary(position);
        int effectiveOffset = position.Affinity switch
        {
            TextAffinity.Upstream => position.Offset - 1,
            // upstream affinity is effectively -1 in text position.
            _ => position.Offset,
        };
        DebugAssert(effectiveOffset >= 0);

        // On iOS, select the previous word if there is a previous word, or select to the end of the next
        // word if there is a next word. Select nothing if there is neither a previous word nor a next
        // word.
        //
        // If the platform is Android and the text is read only, try to select the previous word if there
        // is one; otherwise, select the single whitespace at the position.
        if (effectiveOffset > 0 && ITextLayoutMetrics.IsWhitespace(PlainText[effectiveOffset]))
        {
            TextRange? previousWord = GetPreviousWord(word.Start);
            switch (PlatformDefaults.TargetPlatform)
            {
                case TargetPlatform.IOS:
                    if (previousWord is null)
                    {
                        TextRange? nextWord = GetNextWord(word.Start);
                        if (nextWord is null)
                        {
                            return TextSelection.Collapsed(position.Offset);
                        }

                        return new TextSelection(position.Offset, nextWord.Value.End);
                    }

                    return new TextSelection(previousWord.Value.Start, position.Offset);
                case TargetPlatform.Android:
                    if (ReadOnly)
                    {
                        if (previousWord is null)
                        {
                            return new TextSelection(position.Offset, position.Offset + 1);
                        }

                        return new TextSelection(previousWord.Value.Start, position.Offset);
                    }

                    break;
                case TargetPlatform.Fuchsia:
                case TargetPlatform.MacOS:
                case TargetPlatform.Linux:
                case TargetPlatform.Windows:
                    break;
            }
        }

        return new TextSelection(word.Start, word.End);
    }

    // -- Layout ---------------------------------------------------------------------------------------

    // Dart's `_adjustConstraints`: the text painter's `minWidth` and `maxWidth` for the given box
    // constraint extents.
    private (double MinWidth, double MaxWidth) AdjustConstraints(
        double minWidth = 0.0,
        double maxWidth = double.PositiveInfinity)
    {
        double availableMaxWidth = Math.Max(0.0, maxWidth - CaretMargin);
        double availableMinWidth = Math.Min(minWidth, availableMaxWidth);
        return (
            ForceLine ? availableMaxWidth : availableMinWidth,
            IsMultiline ? availableMaxWidth : double.PositiveInfinity);
    }

    // Computes the text metrics if `_textPainter`'s layout information was marked as dirty.
    //
    // This method must be called in `RenderEditable`'s public methods that expose `_textPainter`'s
    // metrics. For instance, `systemFontsDidChange` sets `_textPainter._paragraph` to null, so accessing
    // _textPainter's metrics immediately after `systemFontsDidChange` without first calling this method
    // may crash.
    //
    // This method is also called in various paint methods (`RenderEditable.paint` as well as its
    // foreground/background painters' `paint`). It's needed because invisible render objects kept in
    // the tree by `KeepAlive` may not get laid out even though the text layout could be invalidated
    // after the last layout.
    internal void ComputeTextMetricsIfNeeded()
    {
        (double minWidth, double maxWidth) = AdjustConstraints(Constraints.MinWidth, Constraints.MaxWidth);
        _textPainter.Layout(minWidth, maxWidth);
    }

    // TODO(garyq): This is no longer producing the highest-fidelity caret heights for Android,
    // especially when non-alphabetic languages are involved. The current implementation overrides the
    // height set here with the full measured height of the text on Android which looks superior (subjective
    // and in full fidelity) but it is not how it is done in the native platform.
    private void ComputeCaretPrototype()
    {
        switch (PlatformDefaults.TargetPlatform)
        {
            case TargetPlatform.IOS:
            case TargetPlatform.MacOS:
                _caretPrototype = new Rect(0.0, 0.0, CursorWidth, EffectiveCursorHeight + 2);
                break;
            case TargetPlatform.Android:
            case TargetPlatform.Fuchsia:
            case TargetPlatform.Linux:
            case TargetPlatform.Windows:
                _caretPrototype = new Rect(
                    0.0,
                    KCaretHeightOffset,
                    CursorWidth,
                    Math.Max(0.0, EffectiveCursorHeight - (2.0 * KCaretHeightOffset)));
                break;
        }
    }

    // Computes the offset to apply to the given [sourceOffset] so it perfectly snaps to physical
    // pixels.
    private Point SnapToPhysicalPixel(Point sourceOffset)
    {
        Point globalOffset = LocalToGlobal(sourceOffset);
        double pixelMultiple = 1.0 / _devicePixelRatio;
        return new Point(
            double.IsFinite(globalOffset.X)
                ? (DartRound(globalOffset.X / pixelMultiple) * pixelMultiple) - globalOffset.X
                : 0,
            double.IsFinite(globalOffset.Y)
                ? (DartRound(globalOffset.Y / pixelMultiple) * pixelMultiple) - globalOffset.Y
                : 0);
    }

    // Dart's `double.round()`: rounds half away from zero.
    private static double DartRound(double value) => Math.Round(value, MidpointRounding.AwayFromZero);

    /// <inheritdoc />
    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        (double minWidth, double maxWidth) = AdjustConstraints(constraints.MinWidth, constraints.MaxWidth);
        TextPainter intrinsics = TextIntrinsics;
        intrinsics.SetPlaceholderDimensions(RenderInlineChildrenContainerDefaults.LayoutInlineChildren(
            InlineChildren,
            constraints.MaxWidth,
            ChildLayoutHelper.DryLayoutChild,
            ChildLayoutHelper.GetDryBaseline));
        intrinsics.Layout(minWidth, maxWidth);
        double width = ForceLine
            ? constraints.MaxWidth
            : constraints.ConstrainWidth(intrinsics.Size.Width + CaretMargin);
        return new Size(width, constraints.ConstrainHeight(PreferredHeight(constraints.MaxWidth)));
    }

    /// <inheritdoc />
    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        (double minWidth, double maxWidth) = AdjustConstraints(constraints.MinWidth, constraints.MaxWidth);
        TextPainter intrinsics = TextIntrinsics;
        intrinsics.SetPlaceholderDimensions(RenderInlineChildrenContainerDefaults.LayoutInlineChildren(
            InlineChildren,
            constraints.MaxWidth,
            ChildLayoutHelper.DryLayoutChild,
            ChildLayoutHelper.GetDryBaseline));
        intrinsics.Layout(minWidth, maxWidth);
        return intrinsics.ComputeDistanceToActualBaseline(baseline);
    }

    /// <inheritdoc />
    protected override void PerformLayout()
    {
        BoxConstraints constraints = Constraints;
        List<RenderBox> inlineChildren = InlineChildren;
        _placeholderDimensions = RenderInlineChildrenContainerDefaults.LayoutInlineChildren(
            inlineChildren,
            constraints.MaxWidth,
            ChildLayoutHelper.LayoutChild,
            ChildLayoutHelper.GetBaseline);
        (double minWidth, double maxWidth) = AdjustConstraints(constraints.MinWidth, constraints.MaxWidth);
        _textPainter.SetPlaceholderDimensions(_placeholderDimensions);
        _textPainter.Layout(minWidth, maxWidth);
        RenderInlineChildrenContainerDefaults.PositionInlineChildren(
            inlineChildren,
            _textPainter.InlinePlaceholderBoxes!.Select(box => box.ToRect()).ToList());
        ComputeCaretPrototype();

        double width = ForceLine
            ? constraints.MaxWidth
            : constraints.ConstrainWidth(_textPainter.Width + CaretMargin);
        DebugAssert(MaxLines != 1 || _textPainter.MaxLines == 1);
        double preferredHeight = MaxLines switch
        {
            null => Math.Max(_textPainter.Height, PreferredLineHeight * (MinLines ?? 0)),
            1 => _textPainter.Height,
            int maxLines => Math.Clamp(
                _textPainter.Height,
                PreferredLineHeight * (MinLines ?? maxLines),
                PreferredLineHeight * maxLines),
        };

        Size = new Size(width, constraints.ConstrainHeight(preferredHeight));
        var contentSize = new Size(_textPainter.Width + CaretMargin, _textPainter.Height);

        var painterConstraints = BoxConstraints.Tight(contentSize);

        _foregroundRenderObject?.Layout(painterConstraints);
        _backgroundRenderObject?.Layout(painterConstraints);

        _maxScrollExtent = GetMaxScrollExtent(contentSize);
        Offset.ApplyViewportDimension(ViewportExtent);
        Offset.ApplyContentDimensions(0.0, _maxScrollExtent);
    }

    // -- Floating cursor --------------------------------------------------------------------------------

    private static Point CalculateAdjustedCursorOffset(Point offset, Rect boundingRects)
    {
        double adjustedX = Math.Clamp(offset.X, boundingRects.Left, boundingRects.Right);
        double adjustedY = Math.Clamp(offset.Y, boundingRects.Top, boundingRects.Bottom);
        return new Point(adjustedX, adjustedY);
    }

    /// <summary>Returns the position within the text field closest to the raw cursor offset.</summary>
    /// <remarks>See also <see cref="FloatingCursorDragState"/>, which explains the floating cursor
    /// feature in detail.</remarks>
    public Point CalculateBoundedFloatingCursorOffset(Point rawCursorOffset, bool? shouldResetOrigin = null)
    {
        Point deltaPosition = default;
        double topBound = -FloatingCursorAddedMargin.Top;
        double bottomBound = Math.Min(Size.Height, _textPainter.Height)
                             - PreferredLineHeight
                             + FloatingCursorAddedMargin.Bottom;
        double leftBound = -FloatingCursorAddedMargin.Left;
        double rightBound = Math.Min(Size.Width, _textPainter.Width) + FloatingCursorAddedMargin.Right;
        Rect boundingRects = FromLTRBUnclamped(leftBound, topBound, rightBound, bottomBound);

        if (shouldResetOrigin is not null)
        {
            _shouldResetOrigin = shouldResetOrigin.Value;
        }

        if (!_shouldResetOrigin)
        {
            return CalculateAdjustedCursorOffset(rawCursorOffset, boundingRects);
        }

        if (_previousOffset is { } previousOffset)
        {
            deltaPosition = new Point(rawCursorOffset.X - previousOffset.X, rawCursorOffset.Y - previousOffset.Y);
        }

        // If the cursor was previously out of bounds, reset the relative origin to be the current
        // position, so the cursor begins moving from the correct point.
        if (_resetOriginOnLeft && deltaPosition.X > 0)
        {
            _relativeOrigin = new Point(rawCursorOffset.X - boundingRects.Left, _relativeOrigin.Y);
            _resetOriginOnLeft = false;
        }
        else if (_resetOriginOnRight && deltaPosition.X < 0)
        {
            _relativeOrigin = new Point(rawCursorOffset.X - boundingRects.Right, _relativeOrigin.Y);
            _resetOriginOnRight = false;
        }

        if (_resetOriginOnTop && deltaPosition.Y > 0)
        {
            _relativeOrigin = new Point(_relativeOrigin.X, rawCursorOffset.Y - boundingRects.Top);
            _resetOriginOnTop = false;
        }
        else if (_resetOriginOnBottom && deltaPosition.Y < 0)
        {
            _relativeOrigin = new Point(_relativeOrigin.X, rawCursorOffset.Y - boundingRects.Bottom);
            _resetOriginOnBottom = false;
        }

        double currentX = rawCursorOffset.X - _relativeOrigin.X;
        double currentY = rawCursorOffset.Y - _relativeOrigin.Y;
        Point adjustedOffset = CalculateAdjustedCursorOffset(new Point(currentX, currentY), boundingRects);

        if (currentX < boundingRects.Left && deltaPosition.X < 0)
        {
            _resetOriginOnLeft = true;
        }
        else if (currentX > boundingRects.Right && deltaPosition.X > 0)
        {
            _resetOriginOnRight = true;
        }

        if (currentY < boundingRects.Top && deltaPosition.Y < 0)
        {
            _resetOriginOnTop = true;
        }
        else if (currentY > boundingRects.Bottom && deltaPosition.Y > 0)
        {
            _resetOriginOnBottom = true;
        }

        _previousOffset = rawCursorOffset;

        return adjustedOffset;
    }

    // Dart's `Rect.fromLTRB`, which keeps a negative extent; `Math.Clamp` in
    // `CalculateAdjustedCursorOffset` reads its edges only.
    private static Rect FromLTRBUnclamped(double left, double top, double right, double bottom)
    {
        return new Rect(left, top, Math.Max(0.0, right - left), Math.Max(0.0, bottom - top));
    }

    /// <summary>Sets the screen position of the floating cursor and the text position closest to the
    /// cursor.</summary>
    /// <remarks><paramref name="resetLerpValue"/> drives the size of the floating cursor. See
    /// <c>EditableTextState.OnFloatingCursorResetTick</c>.</remarks>
    public void SetFloatingCursor(
        FloatingCursorDragState state,
        Point boundedOffset,
        TextPosition lastTextPosition,
        double? resetLerpValue = null)
    {
        if (state == FloatingCursorDragState.End)
        {
            _relativeOrigin = default;
            _previousOffset = null;
            _shouldResetOrigin = true;
            _resetOriginOnBottom = false;
            _resetOriginOnTop = false;
            _resetOriginOnRight = false;
            _resetOriginOnBottom = false;
        }

        _floatingCursorOn = state != FloatingCursorDragState.End;
        _resetFloatingCursorAnimationValue = resetLerpValue;
        if (_floatingCursorOn)
        {
            _floatingCursorTextPosition = lastTextPosition;
            double? animationValue = _resetFloatingCursorAnimationValue;
            Thickness sizeAdjustment = animationValue is { } value
                ? LerpThickness(KFloatingCursorSizeIncrease, default, value)
                : KFloatingCursorSizeIncrease;
            Rect inflated = sizeAdjustment.InflateRect(_caretPrototype);
            CaretPainterInstance.FloatingCursorRect = inflated.Translate(new Vector(boundedOffset.X, boundedOffset.Y));
        }
        else
        {
            CaretPainterInstance.FloatingCursorRect = null;
        }

        CaretPainterInstance.ShowRegularCaret = _resetFloatingCursorAnimationValue is null;
    }

    // Dart's `EdgeInsets.lerp` of two non-null insets.
    private static Thickness LerpThickness(Thickness a, Thickness b, double t)
    {
        return new Thickness(
            a.Left + ((b.Left - a.Left) * t),
            a.Top + ((b.Top - a.Top) * t),
            a.Right + ((b.Right - a.Right) * t),
            a.Bottom + ((b.Bottom - a.Bottom) * t));
    }

    /// <summary>The text position the floating cursor sits on, read by the caret painter.</summary>
    internal TextPosition FloatingCursorTextPosition => _floatingCursorTextPosition;

    private KeyValuePair<int, Point> LineNumberFor(TextPosition startPosition, IReadOnlyList<LineMetrics> metrics)
    {
        // TODO(LongCatIsLooong): include line boundaries information in ui.LineMetrics, then we can get
        // rid of this.
        Point offset = _textPainter.GetOffsetForCaret(startPosition, default);
        foreach (LineMetrics lineMetrics in metrics)
        {
            if (lineMetrics.Baseline > offset.Y)
            {
                return new KeyValuePair<int, Point>(
                    lineMetrics.LineNumber,
                    new Point(offset.X, lineMetrics.Baseline));
            }
        }

        DebugAssert(startPosition.Offset == 0, $"unable to find the line for {startPosition}");
        return new KeyValuePair<int, Point>(
            Math.Max(0, metrics.Count - 1),
            new Point(offset.X, metrics.Count > 0 ? metrics[^1].Baseline + metrics[^1].Descent : 0.0));
    }

    /// <summary>Starts a <see cref="VerticalCaretMovementRun"/> at the given location in the text, for
    /// handling consecutive vertical caret movements.</summary>
    /// <remarks>This can be used to handle consecutive upward/downward arrow key movements in an input
    /// field. The <see cref="VerticalCaretMovementRun"/> is not a lazy iterator: it is invalidated once
    /// the text layout changes.</remarks>
    public VerticalCaretMovementRun StartVerticalCaretMovement(TextPosition startPosition)
    {
        IReadOnlyList<LineMetrics> metrics = _textPainter.ComputeLineMetrics();
        KeyValuePair<int, Point> currentLine = LineNumberFor(startPosition, metrics);
        return new VerticalCaretMovementRun(this, metrics, startPosition, currentLine.Key, currentLine.Value);
    }

    // -- Paint ----------------------------------------------------------------------------------------

    private void PaintContents(PaintingContext context, Point offset)
    {
        Point paintOffset = PaintOffset;
        var effectiveOffset = new Point(offset.X + paintOffset.X, offset.Y + paintOffset.Y);

        if (Selection is not null && !_floatingCursorOn)
        {
            UpdateSelectionExtentsVisibility(effectiveOffset);
        }

        RenderBox? foregroundChild = _foregroundRenderObject;
        RenderBox? backgroundChild = _backgroundRenderObject;

        // The painters paint in the viewport's coordinate space, since the textPainter's coordinate
        // space is not known to high level widgets.
        if (backgroundChild is not null)
        {
            context.PaintChild(backgroundChild, offset);
        }

        _textPainter.Paint(context.Canvas, effectiveOffset);
        RenderInlineChildrenContainerDefaults.PaintInlineChildren(InlineChildren, context, effectiveOffset);

        if (foregroundChild is not null)
        {
            context.PaintChild(foregroundChild, offset);
        }
    }

    private void PaintHandleLayers(PaintingContext context, IReadOnlyList<TextSelectionPoint> endpoints, Point offset)
    {
        Point startPoint = endpoints[0].Point;
        startPoint = new Point(
            Math.Clamp(startPoint.X, 0.0, Size.Width),
            Math.Clamp(startPoint.Y, 0.0, Size.Height));
        _leaderLayerHandler.Layer = new LeaderLayer(StartHandleLayerLink, startPoint + offset);
        context.PushLayer(_leaderLayerHandler.Layer, PaintNothing, default);
        if (endpoints.Count == 2)
        {
            Point endPoint = endpoints[1].Point;
            endPoint = new Point(
                Math.Clamp(endPoint.X, 0.0, Size.Width),
                Math.Clamp(endPoint.Y, 0.0, Size.Height));
            context.PushLayer(new LeaderLayer(EndHandleLayerLink, endPoint + offset), PaintNothing, default);
        }
        else if (Selection!.Value.IsCollapsed)
        {
            context.PushLayer(new LeaderLayer(EndHandleLayerLink, startPoint + offset), PaintNothing, default);
        }
    }

    // Dart passes `super.paint` (the empty `RenderBox.paint`) as the leader layers' painter; C#'s
    // `RenderObject.Paint` is abstract, so the no-op is spelled out.
    private static void PaintNothing(PaintingContext context, Point offset)
    {
    }

    /// <inheritdoc />
    /// <remarks>The foreground and background painter children paint in this box's own coordinate
    /// space; the inline children translate by their <see cref="TextParentData.InlineOffset"/>.</remarks>
    public override void ApplyPaintTransform(RenderObject child, Matrix4 transform)
    {
        if (ReferenceEquals(child, _foregroundRenderObject) || ReferenceEquals(child, _backgroundRenderObject))
        {
            return;
        }

        RenderInlineChildrenContainerDefaults.DefaultApplyPaintTransform((RenderBox)child, transform);
    }

    /// <inheritdoc />
    public override void Paint(PaintingContext context, Point offset)
    {
        ComputeTextMetricsIfNeeded();
        if (HasVisualOverflow && ClipBehavior != Clip.None)
        {
            _clipRectLayer.Layer = context.PushClipRect(
                NeedsCompositing,
                offset,
                new Rect(Size),
                PaintContents,
                clipBehavior: ClipBehavior,
                oldLayer: _clipRectLayer.Layer);
        }
        else
        {
            _clipRectLayer.Layer = null;
            PaintContents(context, offset);
        }

        if (Selection is { IsValid: true } selection)
        {
            PaintHandleLayers(context, GetEndpointsForSelection(selection), offset);
        }
    }

    /// <inheritdoc />
    protected override Rect? DescribeApproximatePaintClip(RenderObject? child)
    {
        switch (ClipBehavior)
        {
            case Clip.None:
                return null;
            default:
                return HasVisualOverflow ? new Rect(Size) : null;
        }
    }

    // -- Diagnostics ------------------------------------------------------------------------------------

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new ColorProperty("cursorColor", CursorColor));
        properties.Add(new DiagnosticsProperty<ValueNotifier<bool>>("showCursor", ShowCursor));
        properties.Add(new IntProperty("maxLines", MaxLines));
        properties.Add(new IntProperty("minLines", MinLines));
        properties.Add(new DiagnosticsProperty<bool>("expands", Expands, defaultValue: false));
        properties.Add(new ColorProperty("selectionColor", SelectionColor));
        properties.Add(new DiagnosticsProperty<TextScaler>(
            "textScaler",
            TextScaler,
            defaultValue: TextScaler.NoScaling));
        // Dart's `DiagnosticsProperty<Locale>`, which prints the locale unquoted.
        properties.Add(new DiagnosticsProperty<string>(
            "locale",
            Locale,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DiagnosticsProperty<TextSelection?>("selection", Selection));
        properties.Add(new DiagnosticsProperty<ViewportOffset>("offset", Offset));
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren()
    {
        return Text is null
            ? []
            : [Text.ToDiagnosticsNode(name: "text", style: DiagnosticsTreeStyle.Transition)];
    }
}
