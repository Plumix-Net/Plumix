using System.Collections;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/text_input.dart

/// <summary>The state of a "floating cursor" drag on an iOS-style keyboard.</summary>
/// <remarks>Dart spells these members in PascalCase too; the wire encoding is lower camel case.
/// </remarks>
public enum FloatingCursorDragState
{
    /// <summary>A user has just activated a floating cursor.</summary>
    Start,

    /// <summary>A user is dragging a floating cursor.</summary>
    Update,

    /// <summary>A user has lifted their finger off the screen during a floating cursor drag.
    /// </summary>
    End,
}

/// <summary>The current state and position of the floating cursor.</summary>
public sealed class RawFloatingCursorPoint
{
    /// <summary>Creates information for setting the position and state of a floating cursor.
    /// </summary>
    public RawFloatingCursorPoint(
        FloatingCursorDragState state,
        Point? offset = null,
        (Point Offset, TextPosition Position)? startLocation = null)
    {
        if (state == FloatingCursorDragState.Update && offset is null)
        {
            throw new ArgumentNullException(
                nameof(offset),
                "An offset is required while the floating cursor is being dragged.");
        }

        State = state;
        Offset = offset;
        StartLocation = startLocation;
    }

    /// <summary>The raw position of the floating cursor as determined by the engine.</summary>
    public Point? Offset { get; }

    /// <summary>The position of the floating cursor when it first became visible, together with the
    /// caret position it maps to.</summary>
    /// <remarks>Nothing on the inbound channel populates this; only the editing layer does.
    /// </remarks>
    public (Point Offset, TextPosition Position)? StartLocation { get; }

    /// <summary>The state of the floating cursor.</summary>
    public FloatingCursorDragState State { get; }
}

/// <summary>Represents a selection rect for a character and its position in the text.</summary>
public sealed class SelectionRect : IEquatable<SelectionRect>
{
    /// <summary>Constructs a selection rect.</summary>
    public SelectionRect(int position, Rect bounds, TextDirection direction = TextDirection.Ltr)
    {
        Position = position;
        Bounds = bounds;
        Direction = direction;
    }

    /// <summary>The position of this character within the text.</summary>
    public int Position { get; }

    /// <summary>The rectangle representing the bounds of this character.</summary>
    public Rect Bounds { get; }

    /// <summary>The direction of the text.</summary>
    public TextDirection Direction { get; }

    /// <inheritdoc/>
    public bool Equals(SelectionRect? other) =>
        other is not null
        && other.GetType() == GetType()
        && other.Position == Position
        && other.Bounds.Equals(Bounds)
        && other.Direction == Direction;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as SelectionRect);

    /// <inheritdoc/>
    /// <remarks>Dart deliberately leaves <see cref="Direction"/> out of the hash.</remarks>
    public override int GetHashCode() => HashCode.Combine(Position, Bounds);

    /// <inheritdoc/>
    public override string ToString() => $"SelectionRect({Position}, {Bounds})";

    /// <summary>Whether two selection rects carry the same values.</summary>
    public static bool operator ==(SelectionRect? left, SelectionRect? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Whether two selection rects carry different values.</summary>
    public static bool operator !=(SelectionRect? left, SelectionRect? right) => !(left == right);
}

/// <summary>The style the text input control should use to render the composing text.</summary>
public sealed class TextInputStyle : Diagnosticable, IEquatable<TextInputStyle>
{
    /// <summary>Creates a text input style.</summary>
    public TextInputStyle(
        TextDirection textDirection,
        TextAlign textAlign,
        string? fontFamily = null,
        double? fontSize = null,
        FontWeight? fontWeight = null,
        double? letterSpacing = null,
        double? wordSpacing = null,
        double? lineHeight = null)
    {
        TextDirection = textDirection;
        TextAlign = textAlign;
        FontFamily = fontFamily;
        FontSize = fontSize;
        FontWeight = fontWeight;
        LetterSpacing = letterSpacing;
        WordSpacing = wordSpacing;
        LineHeight = lineHeight;
    }

    /// <summary>The font family to use.</summary>
    public string? FontFamily { get; }

    /// <summary>The font size to use.</summary>
    public double? FontSize { get; }

    /// <summary>The font weight to use.</summary>
    public FontWeight? FontWeight { get; }

    /// <summary>The text direction to use.</summary>
    public TextDirection TextDirection { get; }

    /// <summary>The text alignment to use.</summary>
    public TextAlign TextAlign { get; }

    /// <summary>The letter spacing to use.</summary>
    public double? LetterSpacing { get; }

    /// <summary>The word spacing to use.</summary>
    public double? WordSpacing { get; }

    /// <summary>The line height to use.</summary>
    public double? LineHeight { get; }

    /// <summary>The JSON payload the host receives with <c>TextInput.setStyle</c>.</summary>
    public Dictionary<string, object?> ToJson()
    {
        return new Dictionary<string, object?>
        {
            ["fontFamily"] = FontFamily,
            ["fontSize"] = FontSize,
            ["fontWeightIndex"] = FontWeight is null ? null : FontWeightIndex(FontWeight.Value),
            ["textAlignIndex"] = (int)TextAlign,
            ["textDirectionIndex"] = (int)TextDirection,
            ["letterSpacing"] = LetterSpacing,
            ["wordSpacing"] = WordSpacing,
            ["lineHeight"] = LineHeight,
        };
    }

    /// <summary>Maps an Avalonia numeric font weight onto Dart's <c>FontWeight.index</c> (0-8).
    /// </summary>
    /// <remarks>Plumix has no Flutter-shaped <c>FontWeight</c>; Avalonia's enum carries the numeric
    /// weight (<c>Normal == 400</c>), so the index is <c>weight / 100 - 1</c>, clamped to Dart's
    /// <c>w100</c>-<c>w900</c> range.</remarks>
    public static int FontWeightIndex(FontWeight weight) =>
        Math.Clamp((int)weight / 100 - 1, 0, 8);

    /// <inheritdoc/>
    public bool Equals(TextInputStyle? other) =>
        other is not null
        && other.GetType() == GetType()
        && other.FontFamily == FontFamily
        && Nullable.Equals(other.FontSize, FontSize)
        && Nullable.Equals(other.FontWeight, FontWeight)
        && other.TextDirection == TextDirection
        && other.TextAlign == TextAlign
        && Nullable.Equals(other.LetterSpacing, LetterSpacing)
        && Nullable.Equals(other.WordSpacing, WordSpacing)
        && Nullable.Equals(other.LineHeight, LineHeight);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as TextInputStyle);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(
            FontFamily,
            FontSize,
            FontWeight,
            TextDirection,
            TextAlign,
            LetterSpacing,
            WordSpacing,
            LineHeight);

    /// <summary>Whether two styles carry the same values.</summary>
    public static bool operator ==(TextInputStyle? left, TextInputStyle? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Whether two styles carry different values.</summary>
    public static bool operator !=(TextInputStyle? left, TextInputStyle? right) => !(left == right);

    /// <inheritdoc/>
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new StringProperty("fontFamily", FontFamily, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DoubleProperty("fontSize", FontSize, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(
            new DiagnosticsProperty<FontWeight?>(
                "fontWeight",
                FontWeight,
                defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new EnumProperty<TextDirection>("textDirection", TextDirection));
        properties.Add(new EnumProperty<TextAlign>("textAlign", TextAlign));
        properties.Add(
            new DoubleProperty("letterSpacing", LetterSpacing, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(
            new DoubleProperty("wordSpacing", WordSpacing, defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(
            new DoubleProperty("lineHeight", LineHeight, defaultValue: DiagnosticsDefaults.NullValue));
    }
}

/// <summary>
/// A class representing rich content (such as a PNG image) inserted via the system input method.
/// </summary>
public sealed class KeyboardInsertedContent : IEquatable<KeyboardInsertedContent>
{
    /// <summary>Creates an object to represent content that has been inserted.</summary>
    public KeyboardInsertedContent(string mimeType, string uri, byte[]? data = null)
    {
        MimeType = mimeType;
        Uri = uri;
        Data = data;
    }

    /// <summary>The mime type of the inserted content.</summary>
    public string MimeType { get; }

    /// <summary>The URI (location) of the inserted content, usually a "content://" URI.</summary>
    public string Uri { get; }

    /// <summary>The bytes of the inserted content.</summary>
    public byte[]? Data { get; }

    /// <summary>Whether this object carries any content data.</summary>
    public bool HasData => Data is { Length: > 0 };

    /// <summary>Creates content from the host's JSON payload.</summary>
    public static KeyboardInsertedContent FromJson(IDictionary metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        string mimeType = (string)metadata["mimeType"]!;
        string uri = (string)metadata["uri"]!;
        object? rawData = metadata.Contains("data") ? metadata["data"] : null;
        byte[]? data = null;
        if (rawData is IEnumerable bytes and not string)
        {
            var buffer = new List<byte>();
            foreach (object? entry in bytes)
            {
                buffer.Add(Convert.ToByte(entry));
            }

            data = buffer.ToArray();
        }

        return new KeyboardInsertedContent(mimeType, uri, data);
    }

    /// <inheritdoc/>
    public bool Equals(KeyboardInsertedContent? other) =>
        other is not null
        && other.GetType() == GetType()
        && other.MimeType == MimeType
        && other.Uri == Uri
        && ReferenceEquals(other.Data, Data);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as KeyboardInsertedContent);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(MimeType, Uri, Data);

    /// <inheritdoc/>
    public override string ToString() => $"KeyboardInsertedContent({MimeType}, {Uri}, {Data})";

    /// <summary>Whether two inserted contents carry the same values.</summary>
    public static bool operator ==(KeyboardInsertedContent? left, KeyboardInsertedContent? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Whether two inserted contents carry different values.</summary>
    public static bool operator !=(KeyboardInsertedContent? left, KeyboardInsertedContent? right) =>
        !(left == right);
}

/// <summary>
/// An interface to receive focus from the native scribble (Apple Pencil handwriting) engine.
/// </summary>
public interface IScribbleClient
{
    /// <summary>The identifier of this element, unique within the registered clients.</summary>
    string ElementIdentifier { get; }

    /// <summary>Requests that this client receive focus at the given global offset.</summary>
    void OnScribbleFocus(Point offset);

    /// <summary>Whether this client overlaps the given global rectangle.</summary>
    bool IsInScribbleRect(Rect rect);

    /// <summary>The global bounds of this client.</summary>
    Rect Bounds { get; }
}
