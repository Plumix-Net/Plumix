using System.Text;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/painting/text_span.dart

namespace Plumix.Painting;

/// An immutable span of text.
///
/// A [TextSpan] object can be styled using its [Style] property. The style will
/// be applied to the [Text] and the [Children].
///
/// A [TextSpan] object can just have plain text, or it can have children
/// [TextSpan] objects with their own styles that (possibly only partially)
/// override the [Style] of this object. If a [TextSpan] has both [Text] and
/// [Children], then the [Text] is treated as if it was an un-styled [TextSpan]
/// at the start of the [Children] list.
public class TextSpan : InlineSpan, IHitTestTarget, IMouseTrackerAnnotation
{
    /// Creates a [TextSpan] with the given values.
    ///
    /// For the object to be useful, at least one of [Text] or [Children] should be set.
    public TextSpan(
        string? text = null,
        IReadOnlyList<InlineSpan>? children = null,
        TextStyle? style = null,
        GestureRecognizer? recognizer = null,
        MouseCursor? mouseCursor = null,
        PointerEnterEventListener? onEnter = null,
        PointerExitEventListener? onExit = null,
        string? semanticsLabel = null,
        string? semanticsIdentifier = null,
        string? locale = null,
        bool? spellOut = null)
        : base(style)
    {
        if (text is null && semanticsLabel is not null)
        {
            throw new ArgumentException("A semantics label requires text.", nameof(semanticsLabel));
        }

        Text = text;
        Children = children;
        Recognizer = recognizer;
        MouseCursor = mouseCursor
                      ?? (recognizer is null ? Widgets.MouseCursor.Defer : SystemMouseCursors.Click);
        OnEnter = onEnter;
        OnExit = onExit;
        SemanticsLabel = semanticsLabel;
        SemanticsIdentifier = semanticsIdentifier;
        Locale = locale;
        SpellOut = spellOut;
    }

    /// The text contained in this span.
    ///
    /// If both [Text] and [Children] are non-null, the text will precede the children.
    ///
    /// This getter does not include the contents of its children.
    public string? Text { get; }

    /// Additional spans to include as children.
    ///
    /// If both [Text] and [Children] are non-null, the text will precede the children.
    public IReadOnlyList<InlineSpan>? Children { get; }

    /// A gesture recognizer that will receive events that hit this span.
    ///
    /// [InlineSpan] itself does not implement hit testing or event dispatch. The
    /// object that manages the [InlineSpan] painting is also responsible for
    /// dispatching events; in the rendering library that is [RenderParagraph].
    ///
    /// [InlineSpan] also does not manage the lifetime of the gesture recognizer.
    public GestureRecognizer? Recognizer { get; }

    /// Mouse cursor when the mouse hovers over this span.
    ///
    /// The default value is [SystemMouseCursors.Click] if [Recognizer] is not
    /// null, or [MouseCursor.Defer] otherwise.
    public MouseCursor MouseCursor { get; }

    public PointerEnterEventListener? OnEnter { get; }

    public PointerExitEventListener? OnExit { get; }

    /// Returns the value of [MouseCursor].
    ///
    /// This member, required by [IMouseTrackerAnnotation], is named apart from
    /// [MouseCursor] to avoid the confusion as a text cursor.
    MouseCursor IMouseTrackerAnnotation.Cursor => MouseCursor;

    /// An alternative semantics label for this [TextSpan].
    ///
    /// If present, the semantics of this span will contain this value instead
    /// of the actual text.
    public string? SemanticsLabel { get; }

    /// A unique identifier for the semantics node for this [TextSpan].
    public string? SemanticsIdentifier { get; }

    /// The language of the text in this span and its span children.
    ///
    /// If this span contains other text span children, they also inherit the
    /// locale from this span unless explicitly set to different locales.
    public string? Locale { get; }

    /// Whether the assistive technologies should spell out this text character
    /// by character.
    ///
    /// If the property is not set, this text span inherits the spell out setting
    /// from its parent.
    public bool? SpellOut { get; }

    public bool ValidForMouseTracker => true;

    public void HandleEvent(PointerEvent @event, HitTestEntry entry)
    {
        if (@event is PointerDownEvent downEvent)
        {
            Recognizer?.AddPointer(downEvent);
        }
    }

    /// Walks this [TextSpan] and its descendants in pre-order and calls `visitor`
    /// for each span that has text.
    public override bool VisitChildren(InlineSpanVisitor visitor)
    {
        if (Text is not null && !visitor(this))
        {
            return false;
        }

        IReadOnlyList<InlineSpan>? children = Children;
        if (children is not null)
        {
            foreach (InlineSpan child in children)
            {
                if (!child.VisitChildren(visitor))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public override bool VisitDirectChildren(InlineSpanVisitor visitor)
    {
        IReadOnlyList<InlineSpan>? children = Children;
        if (children is not null)
        {
            foreach (InlineSpan child in children)
            {
                if (!visitor(child))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// Returns the text span that contains the given position in the text.
    protected internal override InlineSpan? GetSpanForPositionVisitor(TextPosition position, Accumulator offset)
    {
        string? text = Text;
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        TextAffinity affinity = position.Affinity;
        int targetOffset = position.Offset;
        int endOffset = offset.Value + text.Length;

        if ((offset.Value == targetOffset && affinity == TextAffinity.Downstream)
            || (offset.Value < targetOffset && targetOffset < endOffset)
            || (endOffset == targetOffset && affinity == TextAffinity.Upstream))
        {
            return this;
        }

        offset.Increment(text.Length);
        return null;
    }

    protected internal override void ComputeToPlainText(
        StringBuilder buffer,
        bool includeSemanticsLabels = true,
        bool includePlaceholders = true)
    {
        DebugAssertIsValid();
        if (SemanticsLabel is not null && includeSemanticsLabels)
        {
            buffer.Append(SemanticsLabel);
        }
        else if (Text is not null)
        {
            buffer.Append(Text);
        }

        if (Children is not null)
        {
            foreach (InlineSpan child in Children)
            {
                child.ComputeToPlainText(buffer, includeSemanticsLabels, includePlaceholders);
            }
        }
    }

    protected internal override void ComputeSemanticsInformation(List<InlineSpanSemanticsInformation> collector)
    {
        ComputeSemanticsInformation(collector, inheritedLocale: null, inheritedSpellOut: false);
    }

    protected internal void ComputeSemanticsInformation(
        List<InlineSpanSemanticsInformation> collector,
        string? inheritedLocale,
        bool inheritedSpellOut)
    {
        DebugAssertIsValid();
        string? effectiveLocale = Locale ?? inheritedLocale;
        bool effectiveSpellOut = SpellOut ?? inheritedSpellOut;

        if (Text is not null)
        {
            int textLength = SemanticsLabel?.Length ?? Text.Length;
            var attributes = new List<StringAttribute>();
            if (effectiveSpellOut && textLength > 0)
            {
                attributes.Add(new SpellOutStringAttribute(new TextRange(0, textLength)));
            }

            if (effectiveLocale is not null && textLength > 0)
            {
                attributes.Add(new LocaleStringAttribute(new TextRange(0, textLength), effectiveLocale));
            }

            collector.Add(new InlineSpanSemanticsInformation(
                Text,
                semanticsLabel: SemanticsLabel,
                semanticsIdentifier: SemanticsIdentifier,
                stringAttributes: attributes,
                recognizer: Recognizer));
        }

        IReadOnlyList<InlineSpan>? children = Children;
        if (children is not null)
        {
            foreach (InlineSpan child in children)
            {
                if (child is TextSpan textSpan)
                {
                    textSpan.ComputeSemanticsInformation(collector, effectiveLocale, effectiveSpellOut);
                }
                else
                {
                    child.ComputeSemanticsInformation(collector);
                }
            }
        }
    }

    protected internal override int? CodeUnitAtVisitor(int index, Accumulator offset)
    {
        string? text = Text;
        if (text is null)
        {
            return null;
        }

        int localOffset = index - offset.Value;
        offset.Increment(text.Length);
        return localOffset >= 0 && localOffset < text.Length ? text[localOffset] : null;
    }

    /// Throws an exception if the object is not in a valid configuration.
    /// Otherwise, returns true.
    public override bool DebugAssertIsValid()
    {
        if (Children is not null)
        {
            foreach (InlineSpan child in Children)
            {
                child.DebugAssertIsValid();
            }
        }

        return base.DebugAssertIsValid();
    }

    public override RenderComparison CompareTo(InlineSpan other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (ReferenceEquals(this, other))
        {
            return RenderComparison.Identical;
        }

        if (other.GetType() != GetType())
        {
            return RenderComparison.Layout;
        }

        var textSpan = (TextSpan)other;
        if (!string.Equals(textSpan.Text, Text, StringComparison.Ordinal)
            || Children?.Count != textSpan.Children?.Count
            || (Style is null) != (textSpan.Style is null))
        {
            return RenderComparison.Layout;
        }

        RenderComparison result = Equals(Recognizer, textSpan.Recognizer)
            ? RenderComparison.Identical
            : RenderComparison.Metadata;
        if (Style is not null)
        {
            RenderComparison candidate = Style.CompareTo(textSpan.Style!);
            if (candidate > result)
            {
                result = candidate;
            }

            if (result == RenderComparison.Layout)
            {
                return result;
            }
        }

        if (Children is not null)
        {
            for (int index = 0; index < Children.Count; index += 1)
            {
                RenderComparison candidate = Children[index].CompareTo(textSpan.Children![index]);
                if (candidate > result)
                {
                    result = candidate;
                }

                if (result == RenderComparison.Layout)
                {
                    return result;
                }
            }
        }

        return result;
    }

    public override bool Equals(InlineSpan? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (!base.Equals(other))
        {
            return false;
        }

        return other is TextSpan textSpan
               && string.Equals(textSpan.Text, Text, StringComparison.Ordinal)
               && Equals(textSpan.Recognizer, Recognizer)
               && string.Equals(textSpan.SemanticsLabel, SemanticsLabel, StringComparison.Ordinal)
               && string.Equals(textSpan.SemanticsIdentifier, SemanticsIdentifier, StringComparison.Ordinal)
               && Equals(OnEnter, textSpan.OnEnter)
               && Equals(OnExit, textSpan.OnExit)
               && Equals(MouseCursor, textSpan.MouseCursor)
               && ChildrenEqual(textSpan.Children, Children);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(base.GetHashCode());
        hash.Add(Text);
        hash.Add(Recognizer);
        hash.Add(SemanticsLabel);
        hash.Add(SemanticsIdentifier);
        hash.Add(OnEnter);
        hash.Add(OnExit);
        hash.Add(MouseCursor);
        if (Children is not null)
        {
            foreach (InlineSpan child in Children)
            {
                hash.Add(child);
            }
        }

        return hash.ToHashCode();
    }

    public override string ToString() => "TextSpan";

    private static bool ChildrenEqual(IReadOnlyList<InlineSpan>? a, IReadOnlyList<InlineSpan>? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null || a.Count != b.Count)
        {
            return false;
        }

        for (int index = 0; index < a.Count; index += 1)
        {
            if (!Equals(a[index], b[index]))
            {
                return false;
            }
        }

        return true;
    }
}
