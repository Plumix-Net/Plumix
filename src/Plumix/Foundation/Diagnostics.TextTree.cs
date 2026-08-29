using System.Text;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/diagnostics.dart

namespace Plumix.Foundation;

/// <summary>
/// Styles for displaying a node in a [DiagnosticsNode] tree.
///
/// See also:
///
///  * [DiagnosticsNode.toStringDeep], which dumps text art trees for these styles.
/// </summary>
public enum DiagnosticsTreeStyle
{
    /// A style that does not display the tree, for release mode.
    None,

    /// Sparse style for displaying trees.
    ///
    /// See also:
    ///
    ///  * [RenderObject], which uses this style.
    Sparse,

    /// Connects a node to its parent with a dashed line.
    ///
    /// See also:
    ///
    ///  * [RenderSliverMultiBoxAdaptor], which uses this style to distinguish
    ///    offstage children.
    Offstage,

    /// Slightly more compact version of the [Sparse] style.
    ///
    /// See also:
    ///
    ///  * [Element], which uses this style.
    Dense,

    /// Style that enables transitioning from nodes of one style to children of
    /// another.
    ///
    /// See also:
    ///
    ///  * [RenderParagraph], which uses this style to display a [TextSpan] child.
    Transition,

    /// Style for displaying content describing an error.
    ///
    /// See also:
    ///
    ///  * [FlutterError], which uses this style for the root node of a tree
    ///    describing an error.
    Error,

    /// Render the tree just using whitespace without connecting parents to
    /// children using lines.
    ///
    /// See also:
    ///
    ///  * [SliverGeometry], which uses this style.
    Whitespace,

    /// Render the tree without indenting children at all.
    ///
    /// See also:
    ///
    ///  * [DiagnosticsStackTrace], which uses this style.
    Flat,

    /// Render only the immediate properties of a node instead of the full tree.
    ///
    /// See also:
    ///
    ///  * [DebugOverflowIndicatorMixin], which uses this style to display just
    ///    the immediate children of a node.
    SingleLine,

    /// Render the tree on a single line without showing children.
    ErrorProperty,

    /// Render only the children of a node truncating before the tree becomes too
    /// large.
    Shallow,

    /// Render only the children of a node truncating before the tree becomes too large.
    TruncateChildren,
}

/// <summary>
/// Configuration specifying how a particular [DiagnosticsTreeStyle] should be
/// rendered as text art.
/// </summary>
public sealed class TextTreeConfiguration
{
    /// Create a configuration object describing how to render a tree as text art.
    public TextTreeConfiguration(
        string prefixLineOne,
        string prefixOtherLines,
        string prefixLastChildLineOne,
        string prefixOtherLinesRootNode,
        string linkCharacter,
        string propertyPrefixIfChildren,
        string propertyPrefixNoChildren,
        string lineBreak = "\n",
        bool lineBreakProperties = true,
        string afterName = ":",
        string afterDescriptionIfBody = "",
        string afterDescription = "",
        string beforeProperties = "",
        string afterProperties = "",
        string mandatoryAfterProperties = "",
        string propertySeparator = "",
        string bodyIndent = "",
        string footer = "",
        bool showChildren = true,
        bool addBlankLineIfNoChildren = true,
        bool isNameOnOwnLine = false,
        bool isBlankLineBetweenPropertiesAndChildren = true,
        string beforeName = "",
        string suffixLineOne = "",
        string mandatoryFooter = "")
    {
        PrefixLineOne = prefixLineOne;
        PrefixOtherLines = prefixOtherLines;
        PrefixLastChildLineOne = prefixLastChildLineOne;
        PrefixOtherLinesRootNode = prefixOtherLinesRootNode;
        LinkCharacter = linkCharacter;
        PropertyPrefixIfChildren = propertyPrefixIfChildren;
        PropertyPrefixNoChildren = propertyPrefixNoChildren;
        LineBreak = lineBreak;
        LineBreakProperties = lineBreakProperties;
        AfterName = afterName;
        AfterDescriptionIfBody = afterDescriptionIfBody;
        AfterDescription = afterDescription;
        BeforeProperties = beforeProperties;
        AfterProperties = afterProperties;
        MandatoryAfterProperties = mandatoryAfterProperties;
        PropertySeparator = propertySeparator;
        BodyIndent = bodyIndent;
        Footer = footer;
        ShowChildren = showChildren;
        AddBlankLineIfNoChildren = addBlankLineIfNoChildren;
        IsNameOnOwnLine = isNameOnOwnLine;
        IsBlankLineBetweenPropertiesAndChildren = isBlankLineBetweenPropertiesAndChildren;
        BeforeName = beforeName;
        SuffixLineOne = suffixLineOne;
        MandatoryFooter = mandatoryFooter;
        ChildLinkSpace = new string(' ', linkCharacter.Length);
    }

    /// Prefix to add to the first line to display a child with this style.
    public string PrefixLineOne { get; }

    /// Suffix to add to end of each line to make the value it displays look like
    /// it fits with the style of the parent node.
    public string SuffixLineOne { get; }

    /// Prefix to add to other lines to display a child with this style.
    public string PrefixOtherLines { get; }

    /// Prefix to add to the first line to display the last child of a node with
    /// this style.
    public string PrefixLastChildLineOne { get; }

    /// Additional prefix to add to other lines of a node if this is the root node
    /// of the tree.
    public string PrefixOtherLinesRootNode { get; }

    /// Prefix to add before each property if the node as children.
    public string PropertyPrefixIfChildren { get; }

    /// Prefix to add before each property if the node does not have children.
    public string PropertyPrefixNoChildren { get; }

    /// Character to use to draw line linking parent to child.
    public string LinkCharacter { get; }

    /// Whitespace to draw instead of the childLink character if this node is the
    /// last child of its parent so no link line is required.
    public string ChildLinkSpace { get; }

    /// Character(s) to use to separate lines.
    public string LineBreak { get; }

    /// Whether to place line breaks between properties or to leave all
    /// properties on one line.
    public bool LineBreakProperties { get; }

    /// Text added immediately before the name of the node.
    public string BeforeName { get; }

    /// Text added immediately after the name of the node.
    public string AfterName { get; }

    /// Text to add immediately after the description line of a node with
    /// properties and/or children.
    public string AfterDescriptionIfBody { get; }

    /// Text to add immediately after the description line of a node with
    /// properties and/or children.
    public string AfterDescription { get; }

    /// Optional string to add before the properties of a node.
    public string BeforeProperties { get; }

    /// Optional string to add after the properties of a node.
    public string AfterProperties { get; }

    /// Optional string to add after the properties of a node regardless of
    /// whether the node has any properties.
    public string MandatoryAfterProperties { get; }

    /// Property separator to add between properties.
    public string PropertySeparator { get; }

    /// Prefix to add to all lines of the body of the tree node.
    public string BodyIndent { get; }

    /// Whether the children of a node should be shown.
    public bool ShowChildren { get; }

    /// Whether to add a blank line at the end of the output for a node if it has
    /// no children.
    public bool AddBlankLineIfNoChildren { get; }

    /// Whether the name should be displayed on the same line as the description.
    public bool IsNameOnOwnLine { get; }

    /// Footer to add as its own line at the end of a non-root node.
    public string Footer { get; }

    /// Footer to add even for root nodes.
    public string MandatoryFooter { get; }

    /// Add a blank line between properties and children if both are present.
    public bool IsBlankLineBetweenPropertiesAndChildren { get; }
}

/// <summary>
/// Flutter's module-level <c>TextTreeConfiguration</c> instances. C# has no module-level values, so
/// they live as static members here; the Dart names are the member names plus
/// <c>TextConfiguration</c>.
/// </summary>
public static class TextTreeConfigurations
{
    /// Default text tree configuration.
    public static TextTreeConfiguration Sparse { get; } = new(
        prefixLineOne: "├─",
        prefixOtherLines: " ",
        prefixLastChildLineOne: "└─",
        linkCharacter: "│",
        propertyPrefixIfChildren: "│ ",
        propertyPrefixNoChildren: "  ",
        prefixOtherLinesRootNode: " ");

    /// Identical to [Sparse] except that the lines connecting parent to children
    /// are dashed.
    public static TextTreeConfiguration Dashed { get; } = new(
        prefixLineOne: "╎╌",
        prefixLastChildLineOne: "└╌",
        prefixOtherLines: " ",
        linkCharacter: "╎",
        // Intentionally not set as a dashed line as that would make the properties
        // look like they are disconnected from the parent.
        propertyPrefixIfChildren: "│ ",
        propertyPrefixNoChildren: "  ",
        prefixOtherLinesRootNode: " ");

    /// Dense text tree configuration that minimizes horizontal whitespace.
    public static TextTreeConfiguration Dense { get; } = new(
        propertySeparator: ", ",
        beforeProperties: "(",
        afterProperties: ")",
        lineBreakProperties: false,
        prefixLineOne: "├",
        prefixOtherLines: "",
        prefixLastChildLineOne: "└",
        linkCharacter: "│",
        propertyPrefixIfChildren: "│",
        propertyPrefixNoChildren: " ",
        prefixOtherLinesRootNode: "",
        addBlankLineIfNoChildren: false,
        isBlankLineBetweenPropertiesAndChildren: false);

    /// Configuration that draws a box around a leaf node.
    public static TextTreeConfiguration Transition { get; } = new(
        prefixLineOne: "╞═╦══ ",
        prefixLastChildLineOne: "╘═╦══ ",
        prefixOtherLines: " ║ ",
        footer: " ╚═══════════",
        linkCharacter: "│",
        // Subtree boundaries are clear due to the border around the node so omit the
        // property prefix.
        propertyPrefixIfChildren: "",
        propertyPrefixNoChildren: "",
        prefixOtherLinesRootNode: "",
        afterName: " ═══",
        afterDescriptionIfBody: ":",
        bodyIndent: "  ",
        isNameOnOwnLine: true,
        // No need to add a blank line as the footer makes the boundary of this
        // subtree unambiguous.
        addBlankLineIfNoChildren: false,
        isBlankLineBetweenPropertiesAndChildren: false);

    /// Configuration that draws a box around a node ignoring the connection to the
    /// parents.
    public static TextTreeConfiguration Error { get; } = new(
        prefixLineOne: "╞═╦",
        prefixLastChildLineOne: "╘═╦",
        prefixOtherLines: " ║ ",
        footer: " ╚═══════════",
        linkCharacter: "│",
        // Subtree boundaries are clear due to the border around the node so omit the
        // property prefix.
        propertyPrefixIfChildren: "",
        propertyPrefixNoChildren: "",
        prefixOtherLinesRootNode: "",
        beforeName: "══╡ ",
        suffixLineOne: " ╞══",
        mandatoryFooter: "═════",
        // No need to add a blank line as the footer makes the boundary of this
        // subtree unambiguous.
        addBlankLineIfNoChildren: false,
        isBlankLineBetweenPropertiesAndChildren: false);

    /// Whitespace only configuration where children are consistently indented two
    /// spaces.
    public static TextTreeConfiguration Whitespace { get; } = new(
        prefixLineOne: "",
        prefixLastChildLineOne: "",
        prefixOtherLines: " ",
        prefixOtherLinesRootNode: "  ",
        propertyPrefixIfChildren: "",
        propertyPrefixNoChildren: "",
        linkCharacter: " ",
        addBlankLineIfNoChildren: false,
        // Add a colon after the description if the node has a body to make the
        // connection between the description and the body clearer.
        afterDescriptionIfBody: ":",
        // Members are indented an extra two spaces to disambiguate as the children
        // are placed within the parent.
        isBlankLineBetweenPropertiesAndChildren: false);

    /// Whitespace only configuration where children are not indented.
    public static TextTreeConfiguration Flat { get; } = new(
        prefixLineOne: "",
        prefixLastChildLineOne: "",
        prefixOtherLines: "",
        prefixOtherLinesRootNode: "",
        propertyPrefixIfChildren: "",
        propertyPrefixNoChildren: "",
        linkCharacter: "",
        addBlankLineIfNoChildren: false,
        // Add a colon after the description if the node has a body to make the
        // connection between the description and the body clearer.
        afterDescriptionIfBody: ":",
        isBlankLineBetweenPropertiesAndChildren: false);

    /// Render a node on multiple lines omitting children.
    public static TextTreeConfiguration SingleLine { get; } = new(
        propertySeparator: ", ",
        beforeProperties: "(",
        afterProperties: ")",
        prefixLineOne: "",
        prefixOtherLines: "",
        prefixLastChildLineOne: "",
        lineBreak: "",
        lineBreakProperties: false,
        addBlankLineIfNoChildren: false,
        showChildren: false,
        propertyPrefixIfChildren: "  ",
        propertyPrefixNoChildren: "  ",
        linkCharacter: "",
        prefixOtherLinesRootNode: "");

    /// Render the name on a line followed by the body and properties on the next
    /// line omitting the children.
    public static TextTreeConfiguration ErrorProperty { get; } = new(
        propertySeparator: ", ",
        beforeProperties: "(",
        afterProperties: ")",
        prefixLineOne: "",
        prefixOtherLines: "",
        prefixLastChildLineOne: "",
        lineBreakProperties: false,
        addBlankLineIfNoChildren: false,
        showChildren: false,
        propertyPrefixIfChildren: "  ",
        propertyPrefixNoChildren: "  ",
        linkCharacter: "",
        prefixOtherLinesRootNode: "",
        isNameOnOwnLine: true);

    /// Render a node as entirely whitespace without displaying children.
    public static TextTreeConfiguration Shallow { get; } = new(
        prefixLineOne: "",
        prefixLastChildLineOne: "",
        prefixOtherLines: " ",
        prefixOtherLinesRootNode: "  ",
        propertyPrefixIfChildren: "",
        propertyPrefixNoChildren: "",
        linkCharacter: " ",
        addBlankLineIfNoChildren: false,
        // Add a colon after the description if the node has a body to make the
        // connection between the description and the body clearer.
        afterDescriptionIfBody: ":",
        isBlankLineBetweenPropertiesAndChildren: false,
        showChildren: false);
}

/// States used by <see cref="PrefixedStringBuilder.WordWrapLine"/>.
internal enum WordWrapParseMode
{
    InSpace,
    InWord,
    AtBreak,
}

/// <summary>
/// Builder that builds a String with specified prefixes for the first and
/// subsequent lines.
///
/// Allows for the incremental building of strings using `write*` methods.
/// The strings are concatenated into a single string with the first line
/// prefixed by <see cref="PrefixLineOne"/> and subsequent lines prefixed by
/// <see cref="PrefixOtherLines"/>.
/// </summary>
internal sealed class PrefixedStringBuilder
{
    private readonly StringBuilder _buffer = new();

    /// Line that is currently being assembled.
    private readonly StringBuilder _currentLine = new();

    /// List of pairs of integers indicating the start and end of each block of
    /// text within `_currentLine` that can be wrapped.
    private readonly List<int> _wrappableRanges = [];

    private string? _prefixOtherLines;

    /// The next prefix to add to other lines, applied at the next line boundary.
    private string? _nextPrefixOtherLines;

    private int _numLines;

    internal PrefixedStringBuilder(string prefixLineOne, string? prefixOtherLines, int? wrapWidth = null)
    {
        PrefixLineOne = prefixLineOne;
        _prefixOtherLines = prefixOtherLines;
        WrapWidth = wrapWidth;
    }

    /// Prefix to add to the first line.
    internal string PrefixLineOne { get; }

    /// Prefix to add to subsequent lines.
    ///
    /// The prefix can be modified while the string is being built in which case
    /// subsequent lines will be added with the modified prefix.
    internal string? PrefixOtherLines
    {
        get => _nextPrefixOtherLines ?? _prefixOtherLines;
        set
        {
            _prefixOtherLines = value;
            _nextPrefixOtherLines = null;
        }
    }

    /// Wrap the text at the given width, or do not wrap at all when null.
    internal int? WrapWidth { get; }

    /// Whether the string being built already has more than one line of content.
    internal bool RequiresMultipleLines =>
        _numLines > 1
        || (_numLines == 1 && _currentLine.Length > 0)
        || (_currentLine.Length + GetCurrentPrefix(true)!.Length > WrapWidth!.Value);

    internal bool IsCurrentLineEmpty => _currentLine.Length == 0;

    internal void IncrementPrefixOtherLines(string suffix, bool updateCurrentLine)
    {
        if (_currentLine.Length == 0 || updateCurrentLine)
        {
            _prefixOtherLines = PrefixOtherLines! + suffix;
            _nextPrefixOtherLines = null;
        }
        else
        {
            _nextPrefixOtherLines = PrefixOtherLines! + suffix;
        }
    }

    /// Write text ensuring the specified prefixes for the first and subsequent
    /// lines.
    ///
    /// If `allowWrap` is true, the text may be wrapped to fit within the
    /// specified wrap width.
    internal void Write(string s, bool allowWrap = false)
    {
        if (s.Length == 0)
        {
            return;
        }

        string[] lines = s.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                FinalizeLine(true);
                UpdatePrefix();
            }

            string line = lines[i];
            if (line.Length == 0)
            {
                continue;
            }

            if (allowWrap && WrapWidth is not null)
            {
                int wrapStart = _currentLine.Length;
                int wrapEnd = wrapStart + line.Length;
                if (_wrappableRanges.Count > 0 && _wrappableRanges[^1] == wrapStart)
                {
                    // Extend last range.
                    _wrappableRanges[^1] = wrapEnd;
                }
                else
                {
                    _wrappableRanges.Add(wrapStart);
                    _wrappableRanges.Add(wrapEnd);
                }
            }

            _currentLine.Append(line);
        }
    }

    /// Write text assuming the text already obeys the specified prefixes for the
    /// first and subsequent lines.
    internal void WriteRawLines(string lines)
    {
        if (lines.Length == 0)
        {
            return;
        }

        if (_currentLine.Length > 0)
        {
            FinalizeLine(true);
        }

        _buffer.Append(lines);
        if (!lines.EndsWith('\n'))
        {
            _buffer.Append('\n');
        }

        _numLines++;
        UpdatePrefix();
    }

    /// Finishes the current line with a stretched version of `text`.
    internal void WriteStretched(string text, int targetLineLength)
    {
        Write(text);
        int currentLineLength = _currentLine.Length + GetCurrentPrefix(_buffer.Length == 0)!.Length;
        int targetLength = targetLineLength - currentLineLength;
        if (targetLength > 0)
        {
            char lastChar = text[^1];
            _currentLine.Append(lastChar, targetLength);
        }

        // Mark the entire line as not wrappable.
        _wrappableRanges.Clear();
    }

    internal string Build()
    {
        if (_currentLine.Length > 0)
        {
            FinalizeLine(false);
        }

        return _buffer.ToString();
    }

    /// Wraps the given string at the given width, honoring the ranges within
    /// `wrapRanges` as the only places a wrap is permitted.
    ///
    /// Wrapping occurs at space characters (U+0020). Sequences of spaces are
    /// collapsed into a single line break. A word that is longer than the wrap
    /// width is never split; the produced line simply overflows.
    internal static List<string> WordWrapLine(
        string message,
        List<int> wrapRanges,
        int width,
        int startOffset = 0,
        int otherLineOffset = 0)
    {
        if (message.Length + startOffset < width)
        {
            // Nothing to do. The line doesn't wrap.
            return [message];
        }

        var wrappedLine = new List<string>();
        int startForLengthCalculations = -startOffset;
        int index = 0;
        var mode = WordWrapParseMode.InSpace;
        int lastWordStart = 0;
        int? lastWordEnd = null;
        int start = 0;
        int currentChunk = 0;

        // This helper is called with increasing indexes.
        bool NoWrap(int i)
        {
            while (true)
            {
                if (currentChunk >= wrapRanges.Count)
                {
                    return true;
                }

                if (i < wrapRanges[currentChunk + 1])
                {
                    break; // Found nearest chunk.
                }

                currentChunk += 2;
            }

            return i < wrapRanges[currentChunk];
        }

        while (true)
        {
            switch (mode)
            {
                case WordWrapParseMode.InSpace:
                    // At start of break point (or start of line); can't break until we've
                    // seen a word.
                    while (index < message.Length && message[index] == ' ')
                    {
                        index++;
                    }

                    lastWordStart = index;
                    mode = WordWrapParseMode.InWord;
                    break;
                case WordWrapParseMode.InWord:
                    // Looking for a good break point.
                    while (index < message.Length && (message[index] != ' ' || NoWrap(index)))
                    {
                        index++;
                    }

                    mode = WordWrapParseMode.AtBreak;
                    break;
                case WordWrapParseMode.AtBreak:
                    // At start of break point.
                    if (index - startForLengthCalculations > width || index == message.Length)
                    {
                        // Overflow, must break.
                        if (index - startForLengthCalculations <= width || lastWordEnd is null)
                        {
                            // The word doesn't fit anywhere; break at the end of the word.
                            lastWordEnd = index;
                        }

                        wrappedLine.Add(message[start..lastWordEnd.Value]);
                        if (lastWordEnd.Value >= message.Length)
                        {
                            return wrappedLine;
                        }

                        if (lastWordEnd.Value == index)
                        {
                            // We broke at the last break point; skip the spaces.
                            while (index < message.Length && message[index] == ' ')
                            {
                                index++;
                            }

                            start = index;
                            mode = WordWrapParseMode.InWord;
                        }
                        else
                        {
                            // We broke at an earlier break point; we are already at the start
                            // of the next word.
                            start = lastWordStart;
                            mode = WordWrapParseMode.AtBreak;
                        }

                        startForLengthCalculations = start - otherLineOffset;
                        lastWordEnd = null;
                    }
                    else
                    {
                        // Save this break point.
                        lastWordEnd = index;
                        // Skip to the end of this break point.
                        mode = WordWrapParseMode.InSpace;
                    }

                    break;
            }
        }
    }

    private void UpdatePrefix()
    {
        if (_nextPrefixOtherLines is not null)
        {
            _prefixOtherLines = _nextPrefixOtherLines;
            _nextPrefixOtherLines = null;
        }
    }

    private string? GetCurrentPrefix(bool firstLine)
    {
        // The `firstLine` argument is unused in Dart as well: the decision is made
        // on whether anything has been flushed to the buffer yet.
        _ = firstLine;
        return _buffer.Length == 0 ? PrefixLineOne : _prefixOtherLines;
    }

    private void WriteLine(string line, bool includeLineBreak, bool firstLine)
    {
        line = GetCurrentPrefix(firstLine) + line;
        _buffer.Append(line.TrimEnd());
        if (includeLineBreak)
        {
            _buffer.Append('\n');
        }

        _numLines++;
    }

    private void FinalizeLine(bool addTrailingLineBreak)
    {
        bool firstLine = _buffer.Length == 0;
        string text = _currentLine.ToString();
        _currentLine.Clear();

        if (_wrappableRanges.Count == 0)
        {
            // Fast path. There are no wrappable spans of text.
            WriteLine(text, addTrailingLineBreak, firstLine);
            return;
        }

        List<string> lines = WordWrapLine(
            text,
            _wrappableRanges,
            WrapWidth!.Value,
            startOffset: firstLine ? PrefixLineOne.Length : _prefixOtherLines!.Length,
            otherLineOffset: _prefixOtherLines!.Length);

        int i = 0;
        int length = lines.Count;
        foreach (string line in lines)
        {
            i++;
            WriteLine(line, addTrailingLineBreak || i < length, firstLine);
        }

        _wrappableRanges.Clear();
    }
}

/// <summary>
/// Renderer that creates ASCII art representations of trees of
/// [DiagnosticsNode] objects.
/// </summary>
public sealed class TextTreeRenderer
{
    private readonly int _wrapWidth;
    private readonly int _wrapWidthProperties;
    private readonly DiagnosticLevel _minLevel;
    private readonly int _maxDescendentsTruncatableNode;

    /// Creates a [TextTreeRenderer] object with the given arguments specifying
    /// how the tree is rendered.
    ///
    /// Lines are wrapped at `wrapWidth` if the line does not include a
    /// [DiagnosticsNode] object and at `wrapWidthProperties` if the line does.
    public TextTreeRenderer(
        DiagnosticLevel minLevel = DiagnosticLevel.Debug,
        int wrapWidth = 100,
        int wrapWidthProperties = 65,
        int maxDescendentsTruncatableNode = -1)
    {
        _minLevel = minLevel;
        _wrapWidth = wrapWidth;
        _wrapWidthProperties = wrapWidthProperties;
        _maxDescendentsTruncatableNode = maxDescendentsTruncatableNode;
    }

    /// Returns a string representation of the specified node and its descendants.
    public string Render(
        DiagnosticsNode node,
        string prefixLineOne = "",
        string? prefixOtherLines = null,
        TextTreeConfiguration? parentConfiguration = null)
    {
        if (Constants.KReleaseMode)
        {
            return string.Empty;
        }

        return DebugRender(node, prefixLineOne, prefixOtherLines, parentConfiguration);
    }

    private static TextTreeConfiguration ChildTextConfiguration(
        DiagnosticsNode child,
        TextTreeConfiguration textStyle)
    {
        DiagnosticsTreeStyle? style = child.Style;
        return style == DiagnosticsTreeStyle.SingleLine || style == DiagnosticsTreeStyle.ErrorProperty
            ? textStyle
            : child.TextTreeConfiguration!;
    }

    private string DebugRender(
        DiagnosticsNode node,
        string prefixLineOne,
        string? prefixOtherLines,
        TextTreeConfiguration? parentConfiguration)
    {
        bool isSingleLine = Diagnostics.IsSingleLine(node.Style) && parentConfiguration?.LineBreakProperties != true;
        prefixOtherLines ??= prefixLineOne;
        if (node.LinePrefix is not null)
        {
            prefixLineOne += node.LinePrefix;
            prefixOtherLines += node.LinePrefix;
        }

        TextTreeConfiguration config = node.TextTreeConfiguration!;
        if (prefixOtherLines.Length == 0)
        {
            prefixOtherLines += config.PrefixOtherLinesRootNode;
        }

        if (node.Style == DiagnosticsTreeStyle.TruncateChildren)
        {
            return RenderTruncatedChildren(node, prefixLineOne, prefixOtherLines);
        }

        var builder = new PrefixedStringBuilder(
            prefixLineOne,
            prefixOtherLines,
            Math.Max(_wrapWidth, prefixOtherLines.Length + _wrapWidthProperties));

        List<DiagnosticsNode> children = node.GetChildren();
        string description = node.ToDescription(parentConfiguration);
        if (config.BeforeName.Length > 0)
        {
            builder.Write(config.BeforeName);
        }

        bool wrapName = !isSingleLine && node.AllowNameWrap;
        bool wrapDescription = !isSingleLine && node.AllowWrap;
        bool uppercaseTitle = node.Style == DiagnosticsTreeStyle.Error;
        string? name = node.Name;
        if (uppercaseTitle)
        {
            name = name?.ToUpperInvariant();
        }

        if (description.Length == 0)
        {
            if (node.ShowName && name is not null)
            {
                builder.Write(name, allowWrap: wrapName);
            }
        }
        else
        {
            bool includeName = name is not null && name.Length > 0 && node.ShowName;
            if (includeName)
            {
                builder.Write(name!, allowWrap: wrapName);
                if (node.ShowSeparator)
                {
                    builder.Write(config.AfterName, allowWrap: wrapName);
                }

                builder.Write(
                    config.IsNameOnOwnLine || description.Contains('\n', StringComparison.Ordinal) ? "\n" : " ",
                    allowWrap: wrapName);
            }

            if (!isSingleLine && builder.RequiresMultipleLines && !builder.IsCurrentLineEmpty)
            {
                // Make sure there is a break between the current line and the body.
                builder.Write("\n");
            }

            if (includeName)
            {
                builder.IncrementPrefixOtherLines(
                    children.Count == 0 ? config.PropertyPrefixNoChildren : config.PropertyPrefixIfChildren,
                    updateCurrentLine: true);
            }

            if (uppercaseTitle)
            {
                description = description.ToUpperInvariant();
            }

            builder.Write(description.TrimEnd(), allowWrap: wrapDescription);

            if (!includeName)
            {
                builder.IncrementPrefixOtherLines(
                    children.Count == 0 ? config.PropertyPrefixNoChildren : config.PropertyPrefixIfChildren,
                    updateCurrentLine: false);
            }
        }

        if (config.SuffixLineOne.Length > 0)
        {
            builder.WriteStretched(config.SuffixLineOne, builder.WrapWidth!.Value);
        }

        List<DiagnosticsNode> properties = node
            .GetProperties()
            .Where(n => !n.IsFiltered(_minLevel))
            .ToList();

        if (_maxDescendentsTruncatableNode >= 0 && node.AllowTruncate)
        {
            if (properties.Count < _maxDescendentsTruncatableNode)
            {
                properties = properties.Take(_maxDescendentsTruncatableNode).ToList();
                properties.Add(DiagnosticsNode.Message("..."));
            }

            if (_maxDescendentsTruncatableNode < children.Count)
            {
                children = children.Take(_maxDescendentsTruncatableNode).ToList();
                children.Add(DiagnosticsNode.Message("..."));
            }
        }

        if ((properties.Count > 0 || children.Count > 0 || node.EmptyBodyDescription is not null)
            && (node.ShowSeparator || description.Length > 0))
        {
            builder.Write(config.AfterDescriptionIfBody);
        }

        if (config.LineBreakProperties)
        {
            builder.Write(config.LineBreak);
        }

        if (properties.Count > 0)
        {
            builder.Write(config.BeforeProperties);
        }

        builder.IncrementPrefixOtherLines(config.BodyIndent, updateCurrentLine: false);

        if (node.EmptyBodyDescription is not null
            && properties.Count == 0
            && children.Count == 0
            && prefixLineOne.Length > 0)
        {
            builder.Write(node.EmptyBodyDescription);
            if (config.LineBreakProperties)
            {
                builder.Write(config.LineBreak);
            }
        }

        for (int i = 0; i < properties.Count; i++)
        {
            DiagnosticsNode property = properties[i];
            if (i > 0)
            {
                builder.Write(config.PropertySeparator);
            }

            TextTreeConfiguration propertyStyle = property.TextTreeConfiguration!;
            if (Diagnostics.IsSingleLine(property.Style))
            {
                string propertyRender = Render(
                    property,
                    prefixLineOne: propertyStyle.PrefixLineOne,
                    prefixOtherLines: $"{propertyStyle.ChildLinkSpace}{propertyStyle.PrefixOtherLines}",
                    parentConfiguration: config);
                string[] propertyLines = propertyRender.Split('\n');
                if (propertyLines.Length == 1 && !config.LineBreakProperties)
                {
                    builder.Write(propertyLines[0]);
                }
                else
                {
                    builder.Write(propertyRender);
                    if (!propertyRender.EndsWith('\n'))
                    {
                        builder.Write("\n");
                    }
                }
            }
            else
            {
                string propertyRender = Render(
                    property,
                    prefixLineOne: $"{builder.PrefixOtherLines}{propertyStyle.PrefixLineOne}",
                    prefixOtherLines:
                        $"{builder.PrefixOtherLines}{propertyStyle.ChildLinkSpace}{propertyStyle.PrefixOtherLines}",
                    parentConfiguration: config);
                builder.WriteRawLines(propertyRender);
            }
        }

        if (properties.Count > 0)
        {
            builder.Write(config.AfterProperties);
        }

        builder.Write(config.MandatoryAfterProperties);

        if (!config.LineBreakProperties)
        {
            builder.Write(config.LineBreak);
        }

        string prefixChildren = config.BodyIndent;
        string prefixChildrenRaw = $"{prefixOtherLines}{prefixChildren}";
        if (children.Count == 0
            && config.AddBlankLineIfNoChildren
            && builder.RequiresMultipleLines
            && builder.PrefixOtherLines!.TrimEnd().Length > 0)
        {
            builder.Write(config.LineBreak);
        }

        if (children.Count > 0 && config.ShowChildren)
        {
            if (config.IsBlankLineBetweenPropertiesAndChildren
                && properties.Count > 0
                && children[0].TextTreeConfiguration!.IsBlankLineBetweenPropertiesAndChildren)
            {
                builder.Write(config.LineBreak);
            }

            builder.PrefixOtherLines = prefixOtherLines;

            for (int i = 0; i < children.Count; i++)
            {
                DiagnosticsNode child = children[i];
                TextTreeConfiguration childConfig = ChildTextConfiguration(child, config);
                if (i == children.Count - 1)
                {
                    string lastChildPrefixLineOne = $"{prefixChildrenRaw}{childConfig.PrefixLastChildLineOne}";
                    string childPrefixOtherLines =
                        $"{prefixChildrenRaw}{childConfig.ChildLinkSpace}{childConfig.PrefixOtherLines}";
                    builder.WriteRawLines(Render(
                        child,
                        prefixLineOne: lastChildPrefixLineOne,
                        prefixOtherLines: childPrefixOtherLines,
                        parentConfiguration: config));
                    if (childConfig.Footer.Length > 0)
                    {
                        builder.PrefixOtherLines = prefixChildrenRaw;
                        builder.Write($"{childConfig.ChildLinkSpace}{childConfig.Footer}");
                        if (childConfig.MandatoryFooter.Length > 0)
                        {
                            builder.WriteStretched(
                                childConfig.MandatoryFooter,
                                Math.Max(
                                    builder.WrapWidth!.Value,
                                    _wrapWidthProperties + childPrefixOtherLines.Length));
                        }

                        builder.Write(config.LineBreak);
                    }
                }
                else
                {
                    TextTreeConfiguration nextChildStyle = ChildTextConfiguration(children[i + 1], config);
                    string childPrefixLineOne = $"{prefixChildrenRaw}{childConfig.PrefixLineOne}";
                    string childPrefixOtherLines =
                        $"{prefixChildrenRaw}{nextChildStyle.LinkCharacter}{childConfig.PrefixOtherLines}";
                    builder.WriteRawLines(Render(
                        child,
                        prefixLineOne: childPrefixLineOne,
                        prefixOtherLines: childPrefixOtherLines,
                        parentConfiguration: config));
                    if (childConfig.Footer.Length > 0)
                    {
                        builder.PrefixOtherLines = prefixChildrenRaw;
                        builder.Write($"{childConfig.LinkCharacter}{childConfig.Footer}");
                        if (childConfig.MandatoryFooter.Length > 0)
                        {
                            builder.WriteStretched(
                                childConfig.MandatoryFooter,
                                Math.Max(
                                    builder.WrapWidth!.Value,
                                    _wrapWidthProperties + childPrefixOtherLines.Length));
                        }

                        builder.Write(config.LineBreak);
                    }
                }
            }
        }

        if (parentConfiguration is null && config.MandatoryFooter.Length > 0)
        {
            builder.WriteStretched(config.MandatoryFooter, builder.WrapWidth!.Value);
            builder.Write(config.LineBreak);
        }

        return builder.Build();
    }

    private static string RenderTruncatedChildren(
        DiagnosticsNode node,
        string prefixLineOne,
        string prefixOtherLines)
    {
        var descendants = new List<string>();
        const int maxDepth = 5;
        int depth = 0;
        const int maxLines = 25;
        int lines = 0;

        void Visitor(DiagnosticsNode current)
        {
            foreach (DiagnosticsNode child in current.GetChildren())
            {
                if (lines < maxLines)
                {
                    depth += 1;
                    descendants.Add($"{prefixOtherLines}{new string(' ', depth * 2)}{child}");
                    if (depth < maxDepth)
                    {
                        Visitor(child);
                    }

                    depth -= 1;
                }
                else if (lines == maxLines)
                {
                    descendants.Add(
                        $"{prefixOtherLines}  ...(descendants list truncated after {lines} lines)");
                }

                lines += 1;
            }
        }

        Visitor(node);
        var information = new StringBuilder(prefixLineOne);
        if (lines > 1)
        {
            information.Append(
                $"This {node.Name} had the following descendants (showing up to depth {maxDepth}):\n");
        }
        else if (descendants.Count == 1)
        {
            information.Append($"This {node.Name} had the following child:\n");
        }
        else
        {
            information.Append($"This {node.Name} has no descendants.\n");
        }

        information.Append(string.Join("\n", descendants));
        return information.ToString();
    }
}
