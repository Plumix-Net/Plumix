using System.Globalization;
using Avalonia;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/editable_text.dart

namespace Plumix.Widgets;

public sealed partial class EditableText
{
    public sealed partial class EditableTextState
    {
        private ScribbleCacheKey? _scribbleCacheKey;

        // Distance from the end of the text to the scribble placeholder, or -1 when there is none.
        private int _placeholderLocation = -1;

        /// Dart's `_stylusHandwritingEnabled`: the deprecated `scribbleEnabled` still switches it off.
        private bool StylusHandwritingEnabled =>
            Widget.ScribbleEnabled ? Widget.StylusHandwritingEnabled : Widget.ScribbleEnabled;

        /// Dart's `_updateSelectionRects`: sends the bounds of every visible grapheme to the iOS
        /// scribble engine, skipping the work when nothing that affects layout changed.
        private void UpdateSelectionRects(bool force = false)
        {
            if (!StylusHandwritingEnabled || PlatformDefaults.TargetPlatform != TargetPlatform.IOS)
            {
                return;
            }

            if (EffectiveScrollController.Position.UserScrollDirection != ScrollDirection.Idle)
            {
                return;
            }

            RenderEditable renderEditable = RenderEditableObject;
            InlineSpan inlineSpan = renderEditable.Text!;
            TextScaler effectiveTextScaler = MediaQuery.TextScalerOf(Context);

            var newCacheKey = new ScribbleCacheKey(
                inlineSpan: inlineSpan,
                textAlign: Widget.TextAlign,
                textDirection: Widget.TextDirection ?? Directionality.Of(Context),
                textScaler: effectiveTextScaler,
                textHeightBehavior: null,
                locale: null,
                structStyle: Widget.StrutStyle ?? new StrutStyle(),
                placeholder: _placeholderLocation,
                size: renderEditable.Size);

            RenderComparison comparison = force
                ? RenderComparison.Layout
                : _scribbleCacheKey?.Compare(newCacheKey) ?? RenderComparison.Layout;
            if (comparison < RenderComparison.Layout)
            {
                return;
            }

            _scribbleCacheKey = newCacheKey;

            var rects = new List<SelectionRect>();
            int graphemeStart = 0;
            // Can't use _value.text here: the controller value could change between frames.
            string plainText = inlineSpan.ToPlainText(includeSemanticsLabels: false);
            TextElementEnumerator characterRange = StringInfo.GetTextElementEnumerator(plainText);
            while (characterRange.MoveNext())
            {
                int graphemeEnd = graphemeStart + characterRange.GetTextElement().Length;
                IReadOnlyList<TextBox> boxes = renderEditable.GetBoxesForSelection(
                    new TextSelection(graphemeStart, graphemeEnd));

                TextBox? box = boxes.Count == 0 ? null : boxes[0];
                if (box is { } textBox)
                {
                    Rect paintBounds = renderEditable.PaintBounds;
                    if (paintBounds.Bottom <= textBox.Top)
                    {
                        break;
                    }

                    // Include any TextBox which intersects with the RenderEditable.
                    if (paintBounds.Left <= textBox.Right
                        && textBox.Left <= paintBounds.Right
                        && paintBounds.Top <= textBox.Bottom)
                    {
                        // At least some part of the letter is visible within the text field.
                        rects.Add(new SelectionRect(
                            position: graphemeStart,
                            bounds: textBox.ToRect(),
                            direction: textBox.Direction));
                    }
                }

                graphemeStart = graphemeEnd;
            }

            _textInputConnection!.SetSelectionRects(rects);
        }

        /// <inheritdoc/>
        public void InsertTextPlaceholder(Size size)
        {
            if (!StylusHandwritingEnabled)
            {
                return;
            }

            if (!Widget.Controller.Selection.IsValid)
            {
                return;
            }

            SetState(() => _placeholderLocation = EditingValue.Text.Length - Widget.Controller.Selection.End);
        }

        /// <inheritdoc/>
        public void RemoveTextPlaceholder()
        {
            if (!StylusHandwritingEnabled || _placeholderLocation == -1)
            {
                return;
            }

            SetState(() => _placeholderLocation = -1);
        }

        /// The text span with the scribble placeholders, or null when none is showing. The branch of
        /// Dart's `buildTextSpan` between the obscured and the spell-check branches.
        private TextSpan? BuildTextSpanWithScribblePlaceholder(TextStyle style)
        {
            string text = EditingValue.Text;
            if (_placeholderLocation < 0 || _placeholderLocation > text.Length)
            {
                return null;
            }

            var placeholders = new List<InlineSpan>();
            int placeholderLocation = text.Length - _placeholderLocation;
            if (Widget.MaxLines != 1)
            {
                // The zero size placeholder here allows the line to break and keep the caret on the
                // first line.
                placeholders.Add(new ScribblePlaceholder(child: new SizedBox(width: 0, height: 0), size: default));
                placeholders.Add(new ScribblePlaceholder(
                    child: new SizedBox(width: 0, height: 0),
                    size: new Size(RenderEditableObject.Size.Width, 0.0)));
            }
            else
            {
                placeholders.Add(new ScribblePlaceholder(
                    child: new SizedBox(width: 0, height: 0),
                    size: new Size(100.0, 0.0)));
            }

            return new TextSpan(
                style: style,
                children:
                [
                    new TextSpan(text: text[..placeholderLocation]),
                    .. placeholders,
                    new TextSpan(text: text[placeholderLocation..]),
                ]);
        }
    }
}

/// Dart's `_ScribbleCacheKey`: what the iOS scribble selection rects depend on.
internal sealed class ScribbleCacheKey(
    InlineSpan inlineSpan,
    TextAlign textAlign,
    TextDirection textDirection,
    TextScaler textScaler,
    TextHeightBehavior? textHeightBehavior,
    string? locale,
    StrutStyle structStyle,
    int placeholder,
    Size size)
{
    public TextAlign TextAlign { get; } = textAlign;

    public TextDirection TextDirection { get; } = textDirection;

    public TextScaler TextScaler { get; } = textScaler;

    public TextHeightBehavior? TextHeightBehavior { get; } = textHeightBehavior;

    public string? Locale { get; } = locale;

    public StrutStyle StructStyle { get; } = structStyle;

    public int Placeholder { get; } = placeholder;

    public Size Size { get; } = size;

    public InlineSpan InlineSpan { get; } = inlineSpan;

    public RenderComparison Compare(ScribbleCacheKey other)
    {
        if (ReferenceEquals(other, this))
        {
            return RenderComparison.Identical;
        }

        bool needsLayout = TextAlign != other.TextAlign
                           || TextDirection != other.TextDirection
                           || !Equals(TextScaler, other.TextScaler)
                           || !Equals(TextHeightBehavior ?? new TextHeightBehavior(),
                               other.TextHeightBehavior ?? new TextHeightBehavior())
                           || !string.Equals(Locale, other.Locale, StringComparison.Ordinal)
                           || !Equals(StructStyle, other.StructStyle)
                           || Placeholder != other.Placeholder
                           || Size != other.Size;
        return needsLayout ? RenderComparison.Layout : InlineSpan.CompareTo(other.InlineSpan);
    }
}

/// Dart's `_ScribbleFocusable`: registers the field with the iOS scribble engine and focuses it when
/// a stylus starts writing over it.
internal sealed class ScribbleFocusable : StatefulWidget
{
    public ScribbleFocusable(
        Widget child,
        FocusNode focusNode,
        GlobalKey editableKey,
        Action updateSelectionRects,
        bool enabled,
        Key? key = null) : base(key)
    {
        Child = child;
        FocusNode = focusNode;
        EditableKey = editableKey;
        UpdateSelectionRects = updateSelectionRects;
        Enabled = enabled;
    }

    public Widget Child { get; }

    public FocusNode FocusNode { get; }

    public GlobalKey EditableKey { get; }

    public Action UpdateSelectionRects { get; }

    public bool Enabled { get; }

    public override State CreateState() => new ScribbleFocusableState();
}

internal sealed class ScribbleFocusableState : State<ScribbleFocusable>, IScribbleClient
{
    private static int _nextElementIdentifier = 1;

    private readonly string _elementIdentifier;

    public ScribbleFocusableState()
    {
        _elementIdentifier = (_nextElementIdentifier++).ToString(CultureInfo.InvariantCulture);
    }

    public override void InitState()
    {
        base.InitState();
        if (Widget.Enabled)
        {
            UI.TextInput.RegisterScribbleElement(ElementIdentifier, this);
        }
    }

    public override void DidUpdateWidget(ScribbleFocusable oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        if (!oldWidget.Enabled && Widget.Enabled)
        {
            UI.TextInput.RegisterScribbleElement(ElementIdentifier, this);
        }

        if (oldWidget.Enabled && !Widget.Enabled)
        {
            UI.TextInput.UnregisterScribbleElement(ElementIdentifier);
        }
    }

    public override void Dispose()
    {
        UI.TextInput.UnregisterScribbleElement(ElementIdentifier);
        base.Dispose();
    }

    private RenderEditable? RenderEditable =>
        Widget.EditableKey.CurrentContext?.FindRenderObject() as RenderEditable;

    public string ElementIdentifier => _elementIdentifier;

    public void OnScribbleFocus(Point offset)
    {
        Widget.FocusNode.RequestFocus();
        RenderEditable?.SelectPositionAt(from: offset, cause: SelectionChangedCause.StylusHandwriting);
        Widget.UpdateSelectionRects();
    }

    public bool IsInScribbleRect(Rect rect)
    {
        Rect calculatedBounds = Bounds;
        if (RenderEditable?.ReadOnly ?? false)
        {
            return false;
        }

        if (calculatedBounds == default)
        {
            return false;
        }

        if (!calculatedBounds.Intersects(rect))
        {
            return false;
        }

        Rect intersection = calculatedBounds.Intersect(rect);
        var result = new HitTestResult();
        RendererBinding.Instance.HitTestInView(result, intersection.Center, View.Of(Context).ViewId);
        return result.Path.Any(entry => ReferenceEquals(entry.Target, RenderEditable));
    }

    public Rect Bounds
    {
        get
        {
            if (Context.FindRenderObject() is not RenderBox box || !Mounted || !box.Attached)
            {
                return default;
            }

            Matrix4 transform = box.GetTransformTo(null);
            return MatrixUtils.TransformRect(transform, new Rect(0, 0, box.Size.Width, box.Size.Height));
        }
    }

    public override Widget Build(BuildContext context) => Widget.Child;
}

/// Dart's `_ScribblePlaceholder`: a <see cref="WidgetSpan"/> that reserves
/// <see cref="Size"/> instead of the laid-out size of its child.
internal sealed class ScribblePlaceholder(Widget child, Size size) : WidgetSpan(child)
{
    /// The size of the span, used in place of adding a placeholder size to the [TextPainter].
    public Size Size { get; } = size;

    public override void Build(
        ParagraphBuilder builder,
        TextScaler? textScaler = null,
        IReadOnlyList<PlaceholderDimensions>? dimensions = null)
    {
        DebugAssertIsValid();
        bool hasStyle = Style is not null;
        if (hasStyle)
        {
            builder.PushStyle(Style!.GetTextStyle(textScaler: textScaler ?? TextScaler.NoScaling));
        }

        builder.AddPlaceholder(Size.Width, Size.Height, Alignment);
        if (hasStyle)
        {
            builder.Pop();
        }
    }
}
