using System.Text;
using Plumix.Gestures;
using Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/painting/inline_span.dart

namespace Plumix.Painting;

/// Mutable wrapper of an integer that can be passed by reference to track a
/// value across a recursive stack.
public sealed class Accumulator
{
    private int _value;

    /// [Accumulator] may be initialized with a specified value, otherwise, it will
    /// initialize to zero.
    public Accumulator(int value = 0)
    {
        _value = value;
    }

    /// The integer stored in this [Accumulator].
    public int Value => _value;

    /// Increases the [Value] by the `addend`.
    public void Increment(int addend)
    {
        if (addend < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(addend));
        }

        _value += addend;
    }
}

/// Called on each span as [InlineSpan.VisitChildren] walks the [InlineSpan] tree.
///
/// Returns true when the walk should continue, and false to stop visiting further
/// [InlineSpan]s.
public delegate bool InlineSpanVisitor(InlineSpan span);

/// An attribute attached to a range of a semantics string.
public abstract record StringAttribute(TextRange Range)
{
    /// Returns a copy of this attribute with the given `range`.
    public abstract StringAttribute Copy(TextRange range);
}

/// A string attribute that causes assistive technologies to spell out the
/// covered range character by character.
public sealed record SpellOutStringAttribute(TextRange Range) : StringAttribute(Range)
{
    public override StringAttribute Copy(TextRange range) => new SpellOutStringAttribute(range);
}

/// A string attribute that tells assistive technologies which locale the covered
/// range is written in.
public sealed record LocaleStringAttribute(TextRange Range, string Locale) : StringAttribute(Range)
{
    public override StringAttribute Copy(TextRange range) => new LocaleStringAttribute(range, Locale);
}

/// The textual and semantic label information for an [InlineSpan].
///
/// For [PlaceholderSpan]s, [InlineSpanSemanticsInformation.Placeholder] is used by default.
public sealed class InlineSpanSemanticsInformation
{
    /// Constructs an object that holds the text and semantics label values of an
    /// [InlineSpan].
    ///
    /// Use [InlineSpanSemanticsInformation.Placeholder] instead of directly setting
    /// [IsPlaceholder].
    public InlineSpanSemanticsInformation(
        string text,
        bool isPlaceholder = false,
        string? semanticsLabel = null,
        string? semanticsIdentifier = null,
        IReadOnlyList<StringAttribute>? stringAttributes = null,
        GestureRecognizer? recognizer = null)
    {
        if (isPlaceholder && (text != PlaceholderText || semanticsLabel is not null || recognizer is not null))
        {
            throw new ArgumentException("A placeholder cannot carry text, a label or a recognizer.");
        }

        Text = text;
        IsPlaceholder = isPlaceholder;
        SemanticsLabel = semanticsLabel;
        SemanticsIdentifier = semanticsIdentifier;
        StringAttributes = stringAttributes ?? [];
        Recognizer = recognizer;
        RequiresOwnNode = isPlaceholder || recognizer is not null || semanticsIdentifier is not null;
    }

    /// The 'object replacement character' string a [PlaceholderSpan] flattens to.
    private const string PlaceholderText = "￼";

    /// The text info for a [PlaceholderSpan].
    public static InlineSpanSemanticsInformation Placeholder { get; } =
        new(PlaceholderText, isPlaceholder: true);

    /// The text value, if any. For [PlaceholderSpan]s, this will be the unicode
    /// placeholder value.
    public string Text { get; }

    /// The semantics label, if any.
    public string? SemanticsLabel { get; }

    /// The semantics identifier, if any.
    public string? SemanticsIdentifier { get; }

    /// The gesture recognizer, if any, for this span.
    public GestureRecognizer? Recognizer { get; }

    /// Whether this is for a placeholder span.
    public bool IsPlaceholder { get; }

    /// True if this configuration should get its own semantics node.
    ///
    /// This will be the case if the [Recognizer] is not null, or if
    /// [IsPlaceholder] is true, or if [SemanticsIdentifier] has a value.
    public bool RequiresOwnNode { get; }

    /// The string attributes attached to this semantics information.
    public IReadOnlyList<StringAttribute> StringAttributes { get; }

    public override bool Equals(object? obj)
    {
        return obj is InlineSpanSemanticsInformation other
               && string.Equals(other.Text, Text, StringComparison.Ordinal)
               && string.Equals(other.SemanticsLabel, SemanticsLabel, StringComparison.Ordinal)
               && string.Equals(other.SemanticsIdentifier, SemanticsIdentifier, StringComparison.Ordinal)
               && Equals(other.Recognizer, Recognizer)
               && other.IsPlaceholder == IsPlaceholder
               && other.StringAttributes.SequenceEqual(StringAttributes);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Text, SemanticsLabel, SemanticsIdentifier, Recognizer, IsPlaceholder);
    }

    public override string ToString()
    {
        return $"InlineSpanSemanticsInformation{{text: {Text}, semanticsLabel: {SemanticsLabel}, "
               + $"semanticsIdentifier: {SemanticsIdentifier}, recognizer: {Recognizer}}}";
    }
}

/// An immutable span of inline content which forms part of a paragraph.
///
///  * The subclass [TextSpan] specifies text and may contain child [InlineSpan]s.
///  * The subclass [PlaceholderSpan] represents a placeholder that may be
///    filled with non-text content.
///  * The subclass [WidgetSpan] specifies embedded inline widgets.
public abstract class InlineSpan : IEquatable<InlineSpan>
{
    /// Creates an [InlineSpan] with the given values.
    protected InlineSpan(TextStyle? style = null)
    {
        Style = style;
    }

    /// The [TextStyle] to apply to this span.
    ///
    /// The [Style] is also applied to any child spans when this is an instance
    /// of [TextSpan].
    public TextStyle? Style { get; }

    /// Combines `infoList` entries where permissible.
    ///
    /// Consecutive inline spans can be combined if their
    /// [InlineSpanSemanticsInformation.RequiresOwnNode] returns false.
    public static List<InlineSpanSemanticsInformation> CombineSemanticsInfo(
        IReadOnlyList<InlineSpanSemanticsInformation> infoList)
    {
        ArgumentNullException.ThrowIfNull(infoList);
        var combined = new List<InlineSpanSemanticsInformation>();
        string workingText = string.Empty;
        string workingLabel = string.Empty;
        var workingAttributes = new List<StringAttribute>();
        foreach (InlineSpanSemanticsInformation info in infoList)
        {
            if (info.RequiresOwnNode)
            {
                combined.Add(new InlineSpanSemanticsInformation(
                    workingText,
                    semanticsLabel: workingLabel,
                    stringAttributes: workingAttributes));
                workingText = string.Empty;
                workingLabel = string.Empty;
                workingAttributes = [];
                combined.Add(info);
            }
            else
            {
                workingText += info.Text;
                string effectiveLabel = info.SemanticsLabel ?? info.Text;
                foreach (StringAttribute infoAttribute in info.StringAttributes)
                {
                    workingAttributes.Add(infoAttribute.Copy(new TextRange(
                        infoAttribute.Range.Start + workingLabel.Length,
                        infoAttribute.Range.End + workingLabel.Length)));
                }

                workingLabel += effectiveLabel;
            }
        }

        combined.Add(new InlineSpanSemanticsInformation(
            workingText,
            semanticsLabel: workingLabel,
            stringAttributes: workingAttributes));
        return combined;
    }

    /// Walks this [InlineSpan] and any descendants in pre-order and calls `visitor`
    /// for each span that has content.
    ///
    /// When `visitor` returns true, the walk will continue. When `visitor` returns
    /// false, then the walk will end.
    public abstract bool VisitChildren(InlineSpanVisitor visitor);

    /// Calls `visitor` for each immediate child of this [InlineSpan].
    ///
    /// The immediate children are visited in the logical order of the child
    /// [InlineSpan]s in the text.
    public abstract bool VisitDirectChildren(InlineSpanVisitor visitor);

    /// Returns the [InlineSpan] that contains the given position in the text.
    public virtual InlineSpan? GetSpanForPosition(TextPosition position)
    {
        DebugAssertIsValid();
        var offset = new Accumulator();
        InlineSpan? result = null;
        VisitChildren(span =>
        {
            result = span.GetSpanForPositionVisitor(position, offset);
            return result is null;
        });
        return result;
    }

    /// Performs the check at each [InlineSpan] for if the `position` falls within
    /// the range of the span and returns the span if it does.
    ///
    /// This method should not be directly called. Use [GetSpanForPosition] instead.
    protected internal abstract InlineSpan? GetSpanForPositionVisitor(TextPosition position, Accumulator offset);

    /// Flattens the [InlineSpan] tree into a single string.
    ///
    /// Styles are not honored in this process. If `includeSemanticsLabels` is
    /// true, then the text returned will include the [TextSpan.SemanticsLabel]s
    /// instead of the text contents for [TextSpan]s.
    ///
    /// When `includePlaceholders` is true, [PlaceholderSpan]s in the tree will be
    /// represented as a 0xFFFC 'object replacement character'.
    public string ToPlainText(bool includeSemanticsLabels = true, bool includePlaceholders = true)
    {
        var buffer = new StringBuilder();
        ComputeToPlainText(buffer, includeSemanticsLabels, includePlaceholders);
        return buffer.ToString();
    }

    /// Flattens the [InlineSpan] tree to a list of
    /// [InlineSpanSemanticsInformation] objects.
    public List<InlineSpanSemanticsInformation> GetSemanticsInformation()
    {
        var collector = new List<InlineSpanSemanticsInformation>();
        ComputeSemanticsInformation(collector);
        return collector;
    }

    /// Walks the [InlineSpan] tree and accumulates a list of
    /// [InlineSpanSemanticsInformation] objects.
    ///
    /// This method should not be directly called. Use
    /// [GetSemanticsInformation] instead.
    protected internal abstract void ComputeSemanticsInformation(List<InlineSpanSemanticsInformation> collector);

    /// Walks the [InlineSpan] tree and writes the plain text representation to `buffer`.
    ///
    /// This method should not be directly called. Use [ToPlainText] instead.
    protected internal abstract void ComputeToPlainText(
        StringBuilder buffer,
        bool includeSemanticsLabels = true,
        bool includePlaceholders = true);

    /// Returns the UTF-16 code unit at the given `index` in the flattened string.
    ///
    /// This only accounts for the [TextSpan.Text] values and ignores [PlaceholderSpan]s.
    ///
    /// Returns null if the `index` is out of bounds.
    public int? CodeUnitAt(int index)
    {
        if (index < 0)
        {
            return null;
        }

        var offset = new Accumulator();
        int? result = null;
        VisitChildren(span =>
        {
            result = span.CodeUnitAtVisitor(index, offset);
            return result is null;
        });
        return result;
    }

    /// Performs the check at each [InlineSpan] for if the `index` falls within the
    /// range of the span and returns the corresponding code unit. Returns null otherwise.
    ///
    /// This method should not be directly called. Use [CodeUnitAt] instead.
    protected internal abstract int? CodeUnitAtVisitor(int index, Accumulator offset);

    /// Throws an exception if the object is not in a valid configuration.
    /// Otherwise, returns true.
    public virtual bool DebugAssertIsValid() => true;

    /// Describe the difference between this span and another, in terms of
    /// how much damage it will make to the rendering. The comparison is deep.
    ///
    /// Comparing [InlineSpan] objects of different types, for example, comparing
    /// a [TextSpan] to a [WidgetSpan], always results in [RenderComparison.Layout].
    public abstract RenderComparison CompareTo(InlineSpan other);

    public virtual bool Equals(InlineSpan? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null || other.GetType() != GetType())
        {
            return false;
        }

        return Equals(other.Style, Style);
    }

    public override bool Equals(object? obj) => Equals(obj as InlineSpan);

    public override int GetHashCode() => Style?.GetHashCode() ?? 0;
}
