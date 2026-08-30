using System.Text.RegularExpressions;
using Plumix.Foundation;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

/// <summary>
/// Parity tests for the ported `foundation/diagnostics.dart` layer. The tree goldens are
/// Flutter's own (`test/foundation/diagnostics_test.dart`), compared after normalizing identity
/// hashes the way Flutter's `equalsIgnoringHashCodes` matcher does.
/// </summary>
public class DiagnosticsTests
{
    private const string Example = "--- example property at max length --";

    private const string LongUrl =
        "http://someverylongurl.com/that-might-be-tempting-to-wrap-even-though-it-is-a-url-so-should-not-wrap.html";

    private const string VeryLongText =
        "This is a very long message that must wrap as it cannot fit on one line. "
        + "This is a very long message that must wrap as it cannot fit on one line. "
        + "This is a very long message that must wrap as it cannot fit on one line.";

    private const string NotAllowedToWrap =
        "Message that is not allowed to wrap even though it is very long. "
        + "Message that is not allowed to wrap even though it is very long. "
        + "Message that is not allowed to wrap even though it is very long. "
        + "Message that is not allowed to wrap.";

    private const string LongPropertyName =
        "This property has a very long property name that will be allowed to wrap unlike most property names. "
        + "This property has a very long property name that will be allowed to wrap unlike most property names";

    private const string Matrix =
        "[1.0, 0.0, 0.0, 0.0]\n[1.0, 1.0, 0.0, 0.0]\n[1.0, 0.0, 1.0, 0.0]\n[1.0, 0.0, 0.0, 1.0]\n";

    private enum ExampleEnum
    {
        Hello,
        World,
        DeferToChild,
    }

    [Fact]
    public void BuildModeGates_MatchFlutterDiagnosticsContract()
    {
        Assert.Equal(1, new[] { Constants.KDebugMode, Constants.KProfileMode, Constants.KReleaseMode }.Count(v => v));

        var value = new BuildModeTree();
        DiagnosticsNode node = value.ToDiagnosticsNode(style: DiagnosticsTreeStyle.SingleLine);
        var directNode = new DiagnosticsBlock(
            description: "direct",
            style: DiagnosticsTreeStyle.SingleLine,
            properties: [new StringProperty("property", "value", quoted: false)]);
        var renderer = new TextTreeRenderer();
        var builder = new DiagnosticPropertiesBuilder();
        builder.Add(new StringProperty("property", "value"));

        if (Constants.KDebugMode)
        {
            Assert.Equal("BuildModeTree", Diagnostics.ObjectRuntimeType(value, "optimized"));
            Assert.StartsWith("BuildModeTree#", Diagnostics.DescribeIdentity(value), StringComparison.Ordinal);
            Assert.Equal(DiagnosticLevel.Info, node.Level);
            Assert.False(directNode.IsFiltered(DiagnosticLevel.Info));
            Assert.Single(builder.Properties);
            Assert.Single(node.GetProperties());
            Assert.Single(node.GetProperties());

            // The builder is cached, so `debugFillProperties` runs once however often it is read.
            Assert.Equal(1, value.DebugFillPropertiesCalls);
            Assert.Equal(DiagnosticsTreeStyle.SingleLine, node.Style);
            Assert.Equal("BuildModeTree", node.ToDescription());
            Assert.Contains("property: value", value.ToString(), StringComparison.Ordinal);
            Assert.Contains("property: value", value.ToStringShallow(), StringComparison.Ordinal);
            Assert.Contains("property: value", value.ToStringDeep(), StringComparison.Ordinal);
            Assert.Equal("direct(property: value)", renderer.Render(directNode));
            Assert.NotEmpty(directNode.ToJsonMap(DiagnosticsSerializationDelegate.Create()));
            Assert.NotEmpty(directNode.ToJsonMapIterative(DiagnosticsSerializationDelegate.Create()));
            Dictionary<string, string>? timeline = directNode.ToTimelineArguments();
            Assert.Equal("value", timeline!["property"]);
            Assert.Throws<ArgumentException>(() => new StringProperty("name:", "value"));
            Assert.Throws<ArgumentException>(() => new FlagProperty("flag", true));
#pragma warning disable CS0618 // Deprecated in Dart too; retained for build-mode parity.
            Assert.Throws<ArgumentException>(() => Diagnostics.DescribeEnum("not-an-enum"));
#pragma warning restore CS0618
            return;
        }

        Assert.Equal("optimized", Diagnostics.ObjectRuntimeType(value, "optimized"));
        Assert.StartsWith("<optimized out>#", Diagnostics.DescribeIdentity(value), StringComparison.Ordinal);
        Assert.Empty(builder.Properties);
        Assert.Empty(node.GetProperties());
        Assert.Equal(0, value.DebugFillPropertiesCalls);
        Assert.Equal(string.Empty, node.EmptyBodyDescription);
        Assert.Equal(string.Empty, node.ToDescription());
        Assert.Equal(value.ToStringShort(), value.ToString());
        Assert.Equal(value.ToString(), value.ToStringShallow());
        Assert.Equal(string.Empty, value.ToStringDeep());
        Assert.Empty(directNode.ToJsonMap(DiagnosticsSerializationDelegate.Create()));
        Assert.Empty(directNode.ToJsonMapIterative(DiagnosticsSerializationDelegate.Create()));
        _ = new StringProperty("name:", "value");
        _ = new FlagProperty("flag", true);
#pragma warning disable CS0618 // Deprecated in Dart too; retained for build-mode parity.
        Assert.Equal("not-an-enum", Diagnostics.DescribeEnum("not-an-enum"));
#pragma warning restore CS0618

        Dictionary<string, object?> propertyJson =
            new StringProperty("property", "value").ToJsonMap(DiagnosticsSerializationDelegate.Create());
        Assert.DoesNotContain("description", propertyJson);
        Assert.Equal("String", propertyJson["propertyType"]);

        if (Constants.KProfileMode)
        {
            Assert.Equal(DiagnosticsTreeStyle.SingleLine, node.Style);
            Assert.Equal("direct(property: value)", renderer.Render(directNode));
            FlutterError error = Assert.Throws<FlutterError>(() => directNode.ToTimelineArguments());
            Assert.Contains("toTimelineArguments used in non-debug build", error.Message, StringComparison.Ordinal);
        }
        else
        {
            Assert.Equal(DiagnosticsTreeStyle.None, node.Style);
            Assert.Equal(DiagnosticLevel.Hidden, node.Level);
            Assert.Equal(string.Empty, renderer.Render(directNode));
            Assert.True(directNode.IsFiltered(DiagnosticLevel.Hidden));
            Assert.Null(directNode.ToTimelineArguments());
        }
    }

    [DebugOnlyFact]
    public void ShortHashAndDescribeIdentity_MatchDartFormat()
    {
        object value = new();
        string hash = Diagnostics.ShortHash(value);
        Assert.Equal(5, hash.Length);
        Assert.Matches("^[0-9a-f]{5}$", hash);
        Assert.Equal($"Object#{hash}", Diagnostics.DescribeIdentity(value));
        Assert.Equal("Null", Diagnostics.ObjectRuntimeType(null, "<optimized out>"));
        Assert.Equal("DiagnosticsProperty<String>", Diagnostics.DescribeType(typeof(DiagnosticsProperty<string>)));
    }

    [DebugOnlyFact]
    public void DescribeEnum_ReturnsBareLowerCamelName()
    {
#pragma warning disable CS0618 // Deprecated in Dart too; ported for parity.
        Assert.Equal("hello", Diagnostics.DescribeEnum(ExampleEnum.Hello));
        Assert.Equal("world", Diagnostics.DescribeEnum(ExampleEnum.World));
        Assert.Equal("deferToChild", Diagnostics.DescribeEnum(ExampleEnum.DeferToChild));
        Assert.Throws<ArgumentException>(() => Diagnostics.DescribeEnum("Hello World"));
#pragma warning restore CS0618
    }

    [DebugOnlyFact]
    public void ToStringDeep_DenseStyle()
    {
        Assert.Equal(
            Lines(
                "TestTree#00000",
                "├child node A: TestTree#00000",
                "├child node B: TestTree#00000",
                "│├child node B1: TestTree#00000",
                "│├child node B2: TestTree#00000",
                "│└child node B3: TestTree#00000",
                "└child node C: TestTree#00000"),
            RenderStyleTree(DiagnosticsTreeStyle.Dense));
    }

    [DebugOnlyFact]
    public void ToStringDeep_SparseStyle()
    {
        Assert.Equal(
            Lines(
                "TestTree#00000",
                " ├─child node A: TestTree#00000",
                " ├─child node B: TestTree#00000",
                " │ ├─child node B1: TestTree#00000",
                " │ ├─child node B2: TestTree#00000",
                " │ └─child node B3: TestTree#00000",
                " └─child node C: TestTree#00000"),
            RenderStyleTree(DiagnosticsTreeStyle.Sparse));
    }

    [DebugOnlyFact]
    public void ToStringDeep_OffstageStyleUsesDashedLinks()
    {
        Assert.Equal(
            Lines(
                "TestTree#00000",
                " ╎╌child node A: TestTree#00000",
                " ╎╌child node B: TestTree#00000",
                " ╎ ╎╌child node B1: TestTree#00000",
                " ╎ ╎╌child node B2: TestTree#00000",
                " ╎ └╌child node B3: TestTree#00000",
                " └╌child node C: TestTree#00000"),
            RenderStyleTree(DiagnosticsTreeStyle.Offstage));
    }

    [DebugOnlyFact]
    public void ToStringDeep_TransitionLastChildDrawsBox()
    {
        Assert.Equal(
            Lines(
                "TestTree#00000",
                " ├─child node A: TestTree#00000",
                " ├─child node B: TestTree#00000",
                " │ ├─child node B1: TestTree#00000",
                " │ ├─child node B2: TestTree#00000",
                " │ ╘═╦══ child node B3 ═══",
                " │   ║ TestTree#00000",
                " │   ╚═══════════",
                " ╘═╦══ child node C ═══",
                "   ║ TestTree#00000",
                "   ╚═══════════"),
            RenderStyleTree(DiagnosticsTreeStyle.Sparse, DiagnosticsTreeStyle.Transition));
    }

    [DebugOnlyFact]
    public void ToStringDeep_ErrorLastChildUppercasesAndStretchesHeader()
    {
        Assert.Equal(
            Lines(
                "TestTree#00000",
                " ├─child node A: TestTree#00000",
                " ├─child node B: TestTree#00000",
                " │ ├─child node B1: TestTree#00000",
                " │ ├─child node B2: TestTree#00000",
                " │ ╘═╦══╡ CHILD NODE B3: TESTTREE#00000 ╞═══════════════════════════════",
                " │   ╚══════════════════════════════════════════════════════════════════",
                " ╘═╦══╡ CHILD NODE C: TESTTREE#00000 ╞════════════════════════════════",
                "   ╚══════════════════════════════════════════════════════════════════"),
            RenderStyleTree(DiagnosticsTreeStyle.Sparse, DiagnosticsTreeStyle.Error));
    }

    [DebugOnlyFact]
    public void ToStringDeep_TransitionStyleThroughout()
    {
        Assert.Equal(
            Lines(
                "TestTree#00000:",
                "  ╞═╦══ child node A ═══",
                "  │ ║ TestTree#00000",
                "  │ ╚═══════════",
                "  ╞═╦══ child node B ═══",
                "  │ ║ TestTree#00000:",
                "  │ ║   ╞═╦══ child node B1 ═══",
                "  │ ║   │ ║ TestTree#00000",
                "  │ ║   │ ╚═══════════",
                "  │ ║   ╞═╦══ child node B2 ═══",
                "  │ ║   │ ║ TestTree#00000",
                "  │ ║   │ ╚═══════════",
                "  │ ║   ╘═╦══ child node B3 ═══",
                "  │ ║     ║ TestTree#00000",
                "  │ ║     ╚═══════════",
                "  │ ╚═══════════",
                "  ╘═╦══ child node C ═══",
                "    ║ TestTree#00000",
                "    ╚═══════════"),
            RenderStyleTree(DiagnosticsTreeStyle.Transition));
    }

    [DebugOnlyFact]
    public void ToStringDeep_ErrorStyleThroughoutAddsRootHeaderAndFooter()
    {
        Assert.Equal(
            Lines(
                "══╡ TESTTREE#00000 ╞═════════════════════════════════════════════",
                "╞═╦══╡ CHILD NODE A: TESTTREE#00000 ╞════════════════════════════════",
                "│ ╚══════════════════════════════════════════════════════════════════",
                "╞═╦══╡ CHILD NODE B: TESTTREE#00000 ╞════════════════════════════════",
                "│ ║ ╞═╦══╡ CHILD NODE B1: TESTTREE#00000 ╞═══════════════════════════════",
                "│ ║ │ ╚══════════════════════════════════════════════════════════════════",
                "│ ║ ╞═╦══╡ CHILD NODE B2: TESTTREE#00000 ╞═══════════════════════════════",
                "│ ║ │ ╚══════════════════════════════════════════════════════════════════",
                "│ ║ ╘═╦══╡ CHILD NODE B3: TESTTREE#00000 ╞═══════════════════════════════",
                "│ ║   ╚══════════════════════════════════════════════════════════════════",
                "│ ╚══════════════════════════════════════════════════════════════════",
                "╘═╦══╡ CHILD NODE C: TESTTREE#00000 ╞════════════════════════════════",
                "  ╚══════════════════════════════════════════════════════════════════",
                "═════════════════════════════════════════════════════════════════"),
            RenderStyleTree(DiagnosticsTreeStyle.Error));
    }

    [DebugOnlyFact]
    public void ToStringDeep_WhitespaceStyleIndentsTwoSpaces()
    {
        Assert.Equal(
            Lines(
                "TestTree#00000:",
                "  child node A: TestTree#00000",
                "  child node B: TestTree#00000:",
                "    child node B1: TestTree#00000",
                "    child node B2: TestTree#00000",
                "    child node B3: TestTree#00000",
                "  child node C: TestTree#00000"),
            RenderStyleTree(DiagnosticsTreeStyle.Whitespace));
    }

    [DebugOnlyFact]
    public void ToStringDeep_SingleLineStyleOmitsChildrenAndTrailingNewline()
    {
        Assert.Equal("TestTree#00000", RenderStyleTree(DiagnosticsTreeStyle.SingleLine));
    }

    [DebugOnlyFact]
    public void ToStringDeep_SparseStyleWithProperties()
    {
        Assert.Equal(
            Lines(
                "TestTree#00000",
                " │ stringProperty1: value1",
                " │ doubleProperty1: 42.5",
                " │ roundedProperty: 0.3",
                " │ nullProperty: null",
                " │ <root node>",
                " │",
                " ├─child node A: TestTree#00000",
                " ├─child node B: TestTree#00000",
                " │ │ p1: v1",
                " │ │ p2: v2",
                " │ │",
                " │ ├─child node B1: TestTree#00000",
                " │ ├─child node B2: TestTree#00000",
                " │ │   property1: value1",
                " │ │",
                " │ └─child node B3: TestTree#00000",
                " │     <leaf node>",
                " │     foo: 42",
                " │",
                " └─child node C: TestTree#00000",
                "     foo:",
                "       multi",
                "       line",
                "       value!"),
            RenderPropertyTree(DiagnosticsTreeStyle.Sparse));
    }

    [DebugOnlyFact]
    public void ToStringDeep_DenseStyleWithPropertiesInlinesAndEscapesNewlines()
    {
        Assert.Equal(
            Lines(
                "TestTree#00000(stringProperty1: value1, doubleProperty1: 42.5, roundedProperty: 0.3, "
                + "nullProperty: null, <root node>)",
                "├child node A: TestTree#00000",
                "├child node B: TestTree#00000(p1: v1, p2: v2)",
                "│├child node B1: TestTree#00000",
                "│├child node B2: TestTree#00000(property1: value1)",
                "│└child node B3: TestTree#00000(<leaf node>, foo: 42)",
                @"└child node C: TestTree#00000(foo: multi\nline\nvalue!)"),
            RenderPropertyTree(DiagnosticsTreeStyle.Dense));
    }

    [DebugOnlyFact]
    public void ToStringDeep_WhitespaceStyleWithProperties()
    {
        Assert.Equal(
            Lines(
                "TestTree#00000:",
                "  stringProperty1: value1",
                "  doubleProperty1: 42.5",
                "  roundedProperty: 0.3",
                "  nullProperty: null",
                "  <root node>",
                "  child node A: TestTree#00000",
                "  child node B: TestTree#00000:",
                "    p1: v1",
                "    p2: v2",
                "    child node B1: TestTree#00000",
                "    child node B2: TestTree#00000:",
                "      property1: value1",
                "    child node B3: TestTree#00000:",
                "      <leaf node>",
                "      foo: 42",
                "  child node C: TestTree#00000:",
                "    foo:",
                "      multi",
                "      line",
                "      value!"),
            RenderPropertyTree(DiagnosticsTreeStyle.Whitespace));
    }

    [DebugOnlyFact]
    public void ToStringDeep_FlatStyleAddsNoIndentation()
    {
        Assert.Equal(
            Lines(
                "TestTree#00000:",
                "stringProperty1: value1",
                "doubleProperty1: 42.5",
                "roundedProperty: 0.3",
                "nullProperty: null",
                "<root node>",
                "child node A: TestTree#00000",
                "child node B: TestTree#00000:",
                "p1: v1",
                "p2: v2",
                "child node B1: TestTree#00000",
                "child node B2: TestTree#00000:",
                "property1: value1",
                "child node B3: TestTree#00000:",
                "<leaf node>",
                "foo: 42",
                "child node C: TestTree#00000:",
                "foo:",
                "  multi",
                "  line",
                "  value!"),
            RenderPropertyTree(DiagnosticsTreeStyle.Flat));
    }

    [DebugOnlyFact]
    public void ToStringDeep_ErrorPropertyStyleSplitsNameAndValue()
    {
        Assert.Equal(
            Lines(
                "TestTree#00000",
                " │ stringProperty1:",
                " │   value1",
                " │ doubleProperty1:",
                " │   42.5",
                " │ roundedProperty:",
                " │   0.3",
                " │ nullProperty:",
                " │   null",
                " │ <root node>",
                " │",
                " ├─child node A: TestTree#00000",
                " ├─child node B: TestTree#00000",
                " │ │ p1:",
                " │ │   v1",
                " │ │ p2:",
                " │ │   v2",
                " │ │",
                " │ ├─child node B1: TestTree#00000",
                " │ ├─child node B2: TestTree#00000",
                " │ │   property1:",
                " │ │     value1",
                " │ │",
                " │ └─child node B3: TestTree#00000",
                " │     <leaf node>",
                " │     foo:",
                " │       42",
                " │",
                " └─child node C: TestTree#00000",
                "     foo:",
                "       multi",
                "       line",
                "       value!"),
            RenderPropertyTree(DiagnosticsTreeStyle.Sparse, propertyStyle: DiagnosticsTreeStyle.ErrorProperty));
    }

    [DebugOnlyFact]
    public void ToStringDeep_SingleLineRootWithAndWithoutName()
    {
        const string body = "TestTree#00000(stringProperty1: value1, doubleProperty1: 42.5, "
            + "roundedProperty: 0.3, nullProperty: null, <root node>)";

        Assert.Equal(body, RenderPropertyTree(DiagnosticsTreeStyle.SingleLine));
        Assert.Equal($"some name: {body}", RenderPropertyTree(DiagnosticsTreeStyle.SingleLine, name: "some name"));
        Assert.Equal(body + "\n", RenderPropertyTree(DiagnosticsTreeStyle.ErrorProperty));
        Assert.Equal(
            $"some name:\n  {body}\n",
            RenderPropertyTree(DiagnosticsTreeStyle.ErrorProperty, name: "some name"));
    }

    [DebugOnlyFact]
    public void ToStringDeep_MixedStylesCompose()
    {
        var tree = new TestTree(
            properties: [new StringProperty("stringProperty1", "value1")],
            children:
            [
                new TestTree(
                    name: "node transition",
                    style: DiagnosticsTreeStyle.Transition,
                    properties:
                    [
                        new StringProperty("p1", "v1"),
                        new TestTree(properties: [new DiagnosticsProperty<bool?>("survived", true)])
                            .ToDiagnosticsNode(name: "tree property", style: DiagnosticsTreeStyle.Whitespace),
                    ],
                    children:
                    [
                        new TestTree(name: "dense child", style: DiagnosticsTreeStyle.Dense),
                        new TestTree(
                            name: "dense",
                            style: DiagnosticsTreeStyle.Dense,
                            properties: [new StringProperty("property1", "value1")]),
                        new TestTree(
                            name: "node B3",
                            style: DiagnosticsTreeStyle.Dense,
                            properties:
                            [
                                new StringProperty("node_type", "<leaf node>", showName: false, quoted: false),
                                new IntProperty("foo", 42),
                            ]),
                    ]),
                new TestTree(
                    name: "node C",
                    style: DiagnosticsTreeStyle.Sparse,
                    properties: [new StringProperty("foo", "multi\nline\nvalue!", quoted: false)]),
            ]);

        Assert.Equal(
            Lines(
                "TestTree#00000",
                " │ stringProperty1: \"value1\"",
                " ╞═╦══ child node transition ═══",
                " │ ║ TestTree#00000:",
                " │ ║   p1: \"v1\"",
                " │ ║   tree property: TestTree#00000:",
                " │ ║     survived: true",
                " │ ║   ├child dense child: TestTree#00000",
                " │ ║   ├child dense: TestTree#00000(property1: \"value1\")",
                " │ ║   └child node B3: TestTree#00000(<leaf node>, foo: 42)",
                " │ ╚═══════════",
                " └─child node C: TestTree#00000",
                "     foo:",
                "       multi",
                "       line",
                "       value!"),
            Normalize(tree.ToStringDeep()));
    }

    [DebugOnlyFact]
    public void TextTreeRenderer_WrapsAtWrapWidthWithoutBreakingWords()
    {
        DiagnosticsNode node = CreateTreeWithWrappingNodes(
            DiagnosticsTreeStyle.Error,
            DiagnosticsTreeStyle.SingleLine);
        string rendered = Normalize(
            new TextTreeRenderer(wrapWidth: 40, wrapWidthProperties: 40).Render(node));

        Assert.Equal(
            Lines(
                "══╡ TESTTREE#00000 ╞════════════════════",
                Example,
                "This is a very long message that must",
                "wrap as it cannot fit on one line. This",
                "is a very long message that must wrap as",
                "it cannot fit on one line. This is a",
                "very long message that must wrap as it",
                "cannot fit on one line.",
                Example,
                NotAllowedToWrap,
                Example,
                "This property has a very long property",
                "name that will be allowed to wrap unlike",
                "most property names. This property has a",
                "very long property name that will be",
                "allowed to wrap unlike most property",
                "names:",
                "  " + LongUrl,
                "This property has a very long property",
                "name that will be allowed to wrap unlike",
                "most property names. This property has a",
                "very long property name that will be",
                "allowed to wrap unlike most property",
                "names:",
                "  https://goo.gl/",
                "Click on the following url:",
                "  " + LongUrl,
                "Click on the following url",
                "  https://goo.gl/",
                Example,
                "multi-line value:",
                "  [1.0, 0.0, 0.0, 0.0]",
                "  [1.0, 1.0, 0.0, 0.0]",
                "  [1.0, 0.0, 1.0, 0.0]",
                "  [1.0, 0.0, 0.0, 1.0]",
                Example,
                "This property has a very long property",
                "name that will be allowed to wrap unlike",
                "most property names. This property has a",
                "very long property name that will be",
                "allowed to wrap unlike most property",
                "names:",
                "  This is a very long message that must",
                "  wrap as it cannot fit on one line.",
                "  This is a very long message that must",
                "  wrap as it cannot fit on one line.",
                "  This is a very long message that must",
                "  wrap as it cannot fit on one line.",
                Example,
                "This property has a very long property",
                "name that will be allowed to wrap unlike",
                "most property names. This property has a",
                "very long property name that will be",
                "allowed to wrap unlike most property",
                "names:",
                "  [1.0, 0.0, 0.0, 0.0]",
                "  [1.0, 1.0, 0.0, 0.0]",
                "  [1.0, 0.0, 1.0, 0.0]",
                "  [1.0, 0.0, 0.0, 1.0]",
                Example,
                "diagnosis: insufficient data to draw",
                "  conclusion (less than five repaints)",
                "════════════════════════════════════════"),
            rendered);
    }

    [DebugOnlyFact]
    public void TextTreeRenderer_ErrorPropertyStyleMovesValueToItsOwnLine()
    {
        DiagnosticsNode node = CreateTreeWithWrappingNodes(
            DiagnosticsTreeStyle.Error,
            DiagnosticsTreeStyle.ErrorProperty);
        string rendered = Normalize(
            new TextTreeRenderer(wrapWidth: 40, wrapWidthProperties: 40).Render(node));

        Assert.EndsWith(
            Lines(
                Example,
                "diagnosis:",
                "  insufficient data to draw conclusion",
                "  (less than five repaints)",
                "════════════════════════════════════════"),
            rendered,
            StringComparison.Ordinal);
    }

    [DebugOnlyFact]
    public void ToStringDeep_HonorsSuppliedPrefixesOnEveryLine()
    {
        string rendered = RenderStyleTree(DiagnosticsTreeStyle.Sparse);
        Assert.EndsWith("\n", rendered, StringComparison.Ordinal);

        var tree = new TestTree(
            properties: [new StringProperty("foo", "multi\nline\nvalue!", quoted: false)],
            children: [new TestTree(name: "node A")]);
        string prefixed = tree.ToStringDeep(
            prefixLineOne: "PREFIX_LINE_ONE____",
            prefixOtherLines: "PREFIX_OTHER_LINES_");
        string[] lines = prefixed.TrimEnd('\n').Split('\n');
        Assert.StartsWith("PREFIX_LINE_ONE____", lines[0], StringComparison.Ordinal);
        foreach (string line in lines.Skip(1))
        {
            Assert.StartsWith("PREFIX_OTHER_LINES_", line, StringComparison.Ordinal);
        }

        foreach (string line in lines)
        {
            Assert.Equal(line.TrimEnd(), line);
            Assert.NotEqual(string.Empty, line);
            Assert.DoesNotContain("Instance of ", line, StringComparison.Ordinal);
        }
    }

    [DebugOnlyFact]
    public void Diagnosticable_ToStringUsesSingleLineStyleAndFiltersByLevel()
    {
        var tree = new TestTree(
            properties:
            [
                new StringProperty("stringProperty1", "value1", quoted: false),
                new DoubleProperty("doubleProperty1", 42.5),
                new DoubleProperty("roundedProperty", 1.0 / 3.0),
                new StringProperty("DO_NOT_SHOW", "DO_NOT_SHOW", level: DiagnosticLevel.Hidden, quoted: false),
                new StringProperty("DEBUG_ONLY", "DEBUG_ONLY", level: DiagnosticLevel.Debug, quoted: false),
            ],
            children: [new TestTree(name: "node A")]);

        Assert.Equal(
            "TestTree#00000(stringProperty1: value1, doubleProperty1: 42.5, roundedProperty: 0.3)",
            Normalize(tree.ToString()));
        Assert.Equal(
            "TestTree#00000(stringProperty1: value1, doubleProperty1: 42.5, roundedProperty: 0.3, "
            + "DEBUG_ONLY: DEBUG_ONLY)",
            Normalize(tree.ToString(DiagnosticLevel.Debug)));
        Assert.Equal(Diagnostics.DescribeIdentity(tree), tree.ToStringShort());
    }

    [DebugOnlyFact]
    public void DiagnosticableTree_ToStringShallowJoinsPropertiesOnOneLine()
    {
        var tree = new TestTree(
            properties: [new StringProperty("p1", "v1", quoted: false), new IntProperty("foo", 42)],
            children: [new TestTree(name: "node A")]);

        string shallow = Normalize(tree.ToStringShallow());
        Assert.Equal("TestTree#00000(p1: v1, foo: 42), p1: v1, foo: 42", shallow);
        Assert.DoesNotContain('\n', shallow);
    }

    [DebugOnlyFact]
    public void DiagnosticsNodeMessage_AndMessageProperty()
    {
        DiagnosticsNode message = DiagnosticsNode.Message("hello world");
        Assert.Equal("hello world", message.ToString());
        Assert.Equal(string.Empty, message.Name);
        Assert.Null(message.Value);
        Assert.False(message.ShowName);

        var property = new MessageProperty("diagnostics", "hello world");
        Assert.Equal("diagnostics: hello world", property.ToString());
        Assert.Equal("diagnostics", property.Name);
        Assert.Null(property.Value);
        Assert.True(property.ShowName);
    }

    [DebugOnlyFact]
    public void DiagnosticsProperty_CoreDescriptionAndLevelRules()
    {
        Assert.Equal(
            "Creator 20x20",
            new DiagnosticsProperty<string>("Creator", "20x20", showSeparator: false).ToString());
        Assert.Equal(
            "name: small rect",
            new DiagnosticsProperty<string>("name", "20x20", description: "small rect").ToString());
        Assert.Equal(
            "name: value (tooltip)",
            new DiagnosticsProperty<string>("name", "value", tooltip: "tooltip").ToString());
        Assert.False(new DiagnosticsProperty<string>("name", "value", tooltip: "tooltip")
            .IsFiltered(DiagnosticLevel.Fine));
        Assert.Equal(
            "name: missing",
            new DiagnosticsProperty<object>("name", null, ifNull: "missing").ToString());

        var nullNoDefault = new DiagnosticsProperty<object>("name", null);
        Assert.Equal("name: null", nullNoDefault.ToString());
        Assert.Equal(DiagnosticLevel.Info, nullNoDefault.Level);
        Assert.False(nullNoDefault.IsFiltered(DiagnosticLevel.Info));

        var nullDefault = new DiagnosticsProperty<object>(
            "name",
            null,
            defaultValue: DiagnosticsDefaults.NullValue);
        Assert.True(nullDefault.IsFiltered(DiagnosticLevel.Info));
        Assert.Equal("name: null", nullDefault.ToString());

        var warned = new DiagnosticsProperty<object>("name", null, missingIfNull: true);
        Assert.Equal(DiagnosticLevel.Warning, warned.Level);
        Assert.Equal("name: MISSING", warned.ToString());

        Assert.Equal(
            DiagnosticLevel.Warning,
            new DiagnosticsProperty<string>("name", "v", showName: false, level: DiagnosticLevel.Warning).Level);
        Assert.Equal(typeof(string), new DiagnosticsProperty<string>("name", "v").PropertyType);
    }

    [DebugOnlyFact]
    public void DiagnosticsProperty_LazyComputesOnceAndCapturesExceptions()
    {
        int calls = 0;
        DiagnosticsProperty<string> lazy = DiagnosticsProperty<string>.Lazy(
            "name",
            () =>
            {
                calls++;
                return "20x20";
            },
            description: "small rect");
        Assert.Equal("name: small rect", lazy.ToString());
        Assert.Equal("20x20", lazy.TypedValue);
        Assert.Equal(1, calls);

        DiagnosticsProperty<string> throwing = DiagnosticsProperty<string>.Lazy(
            "name",
            () => throw new InvalidOperationException("boom"));
        Assert.Null(throwing.TypedValue);
        Assert.IsType<InvalidOperationException>(throwing.Exception);
        Assert.Equal(DiagnosticLevel.Error, throwing.Level);
        Assert.False(throwing.IsFiltered(DiagnosticLevel.Info));
        Assert.Equal("name: EXCEPTION (InvalidOperationException)", throwing.ToString());

        DiagnosticsProperty<string> throwingDescribed = DiagnosticsProperty<string>.Lazy(
            "name",
            () => throw new InvalidOperationException("boom"),
            description: "missing");
        Assert.Equal("name: missing", throwingDescribed.ToString());
        Assert.NotNull(throwingDescribed.Exception);
    }

    [DebugOnlyFact]
    public void StringProperty_QuotingRules()
    {
        Assert.Equal("name: \"value\"", new StringProperty("name", "value").ToString());
        Assert.Equal("name: value", new StringProperty("name", "value", quoted: false).ToString());
        Assert.Equal(
            "name: VALUE",
            new StringProperty("name", "value", description: "VALUE", ifEmpty: "<hidden>", quoted: false).ToString());
        Assert.Equal(
            "value",
            new StringProperty("name", "value", showName: false, ifEmpty: "<hidden>", quoted: false).ToString());
        Assert.Equal("\"value\"", new StringProperty("name", "value", showName: false).ToString());
        Assert.Equal("name: <hidden>", new StringProperty("name", string.Empty, ifEmpty: "<hidden>").ToString());
        Assert.Equal(
            "<hidden>",
            new StringProperty("name", string.Empty, showName: false, ifEmpty: "<hidden>").ToString());
        Assert.Equal("null", new StringProperty("name", null, showName: false).ToString());
        Assert.False(new StringProperty("name", null).IsFiltered(DiagnosticLevel.Info));
        Assert.True(new StringProperty("name", "value", level: DiagnosticLevel.Hidden)
            .IsFiltered(DiagnosticLevel.Info));
        Assert.True(new StringProperty("name", null, defaultValue: DiagnosticsDefaults.NullValue)
            .IsFiltered(DiagnosticLevel.Info));
    }

    [DebugOnlyFact]
    public void DoubleProperty_FormatsWithOneDecimal()
    {
        Assert.Equal("name: 42.0", new DoubleProperty("name", 42.0).ToString());
        Assert.Equal("name: 1.3", new DoubleProperty("name", 1.3333).ToString());
        Assert.Equal("name: null", new DoubleProperty("name", null).ToString());
        Assert.False(new DoubleProperty("name", null).IsFiltered(DiagnosticLevel.Info));
        Assert.Equal("name: missing", new DoubleProperty("name", null, ifNull: "missing").ToString());
        Assert.Equal("name: 42.0px", new DoubleProperty("name", 42.0, unit: "px").ToString());
        Assert.Equal("double1: Infinity", new DoubleProperty("double1", double.PositiveInfinity).ToString());
        Assert.Equal("double2: -Infinity", new DoubleProperty("double2", double.NegativeInfinity).ToString());

        Assert.Equal("name: 42.0", DoubleProperty.Lazy("name", () => 42.0).ToString());
        DoubleProperty throwing = DoubleProperty.Lazy("name", () => throw new InvalidOperationException());
        Assert.Equal(DiagnosticLevel.Error, throwing.Level);
        Assert.Equal("name: EXCEPTION (InvalidOperationException)", throwing.ToString());
    }

    [DebugOnlyFact]
    public void IntProperty_FormatsAndFilters()
    {
        Assert.Equal("name: 42", new IntProperty("name", 42).ToString());
        Assert.Equal(DiagnosticLevel.Info, new IntProperty("name", 42).Level);
        Assert.Equal("name: null", new IntProperty("name", null).ToString());
        Assert.True(new IntProperty("name", null, defaultValue: DiagnosticsDefaults.NullValue)
            .IsFiltered(DiagnosticLevel.Info));
        Assert.Equal("name: missing", new IntProperty("name", null, ifNull: "missing").ToString());
        Assert.Equal("42", new IntProperty("name", 42, showName: false).ToString());
        Assert.Equal("name: 42pt", new IntProperty("name", 42, unit: "pt").ToString());
        Assert.True(new IntProperty("name", 42, defaultValue: 42).IsFiltered(DiagnosticLevel.Info));
        Assert.Equal(DiagnosticLevel.Info, new IntProperty("name", 43, defaultValue: 42).Level);
        Assert.Equal(DiagnosticLevel.Hidden, new IntProperty("name", 42, level: DiagnosticLevel.Hidden).Level);

        var passedThrough = new IntProperty(
            "Example",
            0,
            ifNull: "is null",
            showName: false,
            defaultValue: 1,
            style: DiagnosticsTreeStyle.None,
            level: DiagnosticLevel.Off);
        Assert.Equal(0, passedThrough.TypedValue);
        Assert.Equal("is null", passedThrough.IfNull);
        Assert.False(passedThrough.ShowName);
        Assert.Equal(1, passedThrough.DefaultValue);
        Assert.Equal(DiagnosticsTreeStyle.None, passedThrough.Style);
        Assert.Equal(DiagnosticLevel.Off, passedThrough.Level);
    }

    [DebugOnlyFact]
    public void PercentProperty_ClampsAndFormats()
    {
        Assert.Equal("name: 40.0%", new PercentProperty("name", 0.4).ToString());
        Assert.Equal(0.4, new PercentProperty("name", 0.4).TypedValue);
        Assert.Equal("name: 0.0%", new PercentProperty("name", -10.0).ToString());
        Assert.Equal("name: 100.0%", new PercentProperty("name", 3.0).ToString());
        Assert.Equal("name: 0.0%", new PercentProperty("name", 0.0).ToString());
        Assert.Equal("name: 100.0%", new PercentProperty("name", 1.0).ToString());
        Assert.Equal(
            "name: 99.0% invisible (almost transparent)",
            new PercentProperty("name", 0.99, unit: "invisible", tooltip: "almost transparent").ToString());
        Assert.Equal(
            "name: null (!)",
            new PercentProperty("name", null, unit: "invisible", tooltip: "!").ToString());
        Assert.Equal("name: null", new PercentProperty("name", null).ToString());
        Assert.Equal("name: missing", new PercentProperty("name", null, ifNull: "missing").ToString());
        Assert.Equal("50.0%", new PercentProperty("name", 0.5, showName: false).ToString());
    }

    [DebugOnlyFact]
    public void FlagProperty_DescribesPresentStatesOnly()
    {
        Assert.Equal("myFlag", new FlagProperty("myFlag", true, ifTrue: "myFlag").ToString());
        Assert.False(new FlagProperty("myFlag", true, ifTrue: "myFlag").IsFiltered(DiagnosticLevel.Fine));

        var falseFlag = new FlagProperty("wasLayout", false, ifTrue: "layout computed");
        Assert.Equal(DiagnosticLevel.Hidden, falseFlag.Level);
        Assert.Equal("wasLayout: false", falseFlag.ToString());

        var trueWithoutIfTrue = new FlagProperty("wasLayout", true, ifFalse: "no layout computed");
        Assert.Equal(DiagnosticLevel.Hidden, trueWithoutIfTrue.Level);
        Assert.Equal("wasLayout: true", trueWithoutIfTrue.ToString());

        Assert.Equal(
            "name: YES",
            new FlagProperty("name", true, ifTrue: "YES", ifFalse: "NO", showName: true).ToString());
        Assert.Equal(
            "name: NO",
            new FlagProperty("name", false, ifTrue: "YES", ifFalse: "NO", showName: true).ToString());
        Assert.Equal("YES", new FlagProperty("name", true, ifTrue: "YES", ifFalse: "NO").ToString());
        Assert.Equal("NO", new FlagProperty("name", false, ifTrue: "YES", ifFalse: "NO").ToString());
        Assert.Equal(
            DiagnosticLevel.Hidden,
            new FlagProperty("name", true, ifTrue: "YES", ifFalse: "NO", level: DiagnosticLevel.Hidden).Level);

        // An omitted `defaultValue` means "the default is null", so a null flag is boring.
        Assert.Equal(DiagnosticLevel.Fine, new FlagProperty("name", null, ifTrue: "YES").Level);
        Assert.True(new FlagProperty("name", null, ifTrue: "YES").ShowName);
        Assert.Throws<ArgumentException>(() => new FlagProperty("name", true));
    }

    [DebugOnlyFact]
    public void ObjectFlagProperty_DescribesPresenceOrAbsence()
    {
        Action onClick = () => { };
        Assert.Equal(
            "clickable",
            new ObjectFlagProperty<Action>("onClick", onClick, ifPresent: "clickable").ToString());
        Assert.False(new ObjectFlagProperty<Action>("onClick", onClick, ifPresent: "clickable")
            .IsFiltered(DiagnosticLevel.Info));

        var missing = new ObjectFlagProperty<Action>("onClick", null, ifPresent: "clickable");
        Assert.Equal("onClick: null", missing.ToString());
        Assert.True(missing.IsFiltered(DiagnosticLevel.Fine));

        var presentWithIfNull = new ObjectFlagProperty<Action>("onClick", onClick, ifNull: "disabled");
        Assert.True(presentWithIfNull.IsFiltered(DiagnosticLevel.Fine));

        var absentWithIfNull = new ObjectFlagProperty<Action>("onClick", null, ifNull: "disabled");
        Assert.Equal("disabled", absentWithIfNull.ToString());
        Assert.False(absentWithIfNull.IsFiltered(DiagnosticLevel.Info));

        Assert.Equal("has onClick", ObjectFlagProperty<Action>.Has("onClick", onClick).ToString());
        Assert.False(ObjectFlagProperty<Action>.Has("onClick", onClick).IsFiltered(DiagnosticLevel.Info));
        Assert.Equal("onClick: null", ObjectFlagProperty<Action>.Has("onClick", null).ToString());
        Assert.True(ObjectFlagProperty<Action>.Has("onClick", null).IsFiltered(DiagnosticLevel.Info));
        Assert.Throws<ArgumentException>(() => new ObjectFlagProperty<Action>("onClick", onClick));
    }

    [DebugOnlyFact]
    public void FlagsSummary_ListsPresentKeysInOrder()
    {
        Action onClick = () => { };
        Action onMove = () => { };

        var summary = new FlagsSummary<Action>(
            "listeners",
            [new("click", onClick), new("move", onMove)]);
        Assert.Equal("listeners", summary.Name);
        Assert.False(summary.IsFiltered(DiagnosticLevel.Info));
        Assert.Equal("listeners: click, move", summary.ToString());

        Assert.Equal(
            "listeners: move, click",
            new FlagsSummary<Action>("listeners", [new("move", onMove), new("click", onClick)]).ToString());
        Assert.Equal(
            "listeners: click",
            new FlagsSummary<Action>("listeners", [new("move", null), new("click", onClick)]).ToString());
        Assert.True(new FlagsSummary<Action>("listeners", [new("enter", null)])
            .IsFiltered(DiagnosticLevel.Info));

        var withIfEmpty = new FlagsSummary<Action>("listeners", [new("enter", null)], ifEmpty: "<none>");
        Assert.Equal("listeners: <none>", withIfEmpty.ToString());
        Assert.False(withIfEmpty.IsFiltered(DiagnosticLevel.Info));
    }

    [DebugOnlyFact]
    public void IterableProperty_FormatsPerStyle()
    {
        Assert.Equal("ints: 1, 2, 3", new IterableProperty<int>("ints", [1, 2, 3]).ToString());
        Assert.Equal("doubles: 1.0, 2.0, 3.0", new IterableProperty<double>("doubles", [1, 2, 3]).ToString());
        Assert.Equal("name: []", new IterableProperty<object>("name", []).ToString());
        Assert.False(new IterableProperty<object>("name", []).IsFiltered(DiagnosticLevel.Info));
        Assert.Equal("list: null", new IterableProperty<object>("list", null).ToString());
        Assert.False(new IterableProperty<object>("list", null).IsFiltered(DiagnosticLevel.Info));

        var defaulted = new IterableProperty<object>("list", null, defaultValue: DiagnosticsDefaults.NullValue);
        Assert.True(defaulted.IsFiltered(DiagnosticLevel.Info));
        Assert.Equal(DiagnosticLevel.Fine, defaulted.Level);
        Assert.Equal("list: null", defaulted.ToString());

        var multiline = new IterableProperty<string>(
            "objects",
            ["first", "second"],
            style: DiagnosticsTreeStyle.Whitespace);
        Assert.Equal("objects:\nfirst\nsecond", multiline.ToString());
        Assert.Equal("objects:\n  first\n  second\n", multiline.ToStringDeep(wrapWidth: 100));

        var single = new IterableProperty<string>("object", ["only"], style: DiagnosticsTreeStyle.Whitespace);
        Assert.Equal("object: only", single.ToString());

        var inline = new IterableProperty<string>("objects", ["first", "second"]);
        Assert.Equal(
            "TestTree#00000(objects: [first, second], foo: 42)",
            Normalize(new TestTree(
                    properties: [inline, new IntProperty("foo", 42)],
                    style: DiagnosticsTreeStyle.SingleLine)
                .ToDiagnosticsNode(style: DiagnosticsTreeStyle.SingleLine)
                .ToStringDeep()));
    }

    [DebugOnlyFact]
    public void EnumProperty_UsesBareLowerCamelName()
    {
        Assert.Equal("name: hello", new EnumProperty<ExampleEnum>("name", ExampleEnum.Hello).ToString());
        Assert.Equal(
            "name: deferToChild",
            new EnumProperty<ExampleEnum>("name", ExampleEnum.DeferToChild).ToString());
        Assert.Equal("name: null", new EnumProperty<ExampleEnum>("name", null).ToString());
        Assert.Equal(DiagnosticLevel.Info, new EnumProperty<ExampleEnum>("name", null).Level);
        Assert.True(new EnumProperty<ExampleEnum>("name", ExampleEnum.Hello, defaultValue: ExampleEnum.Hello)
            .IsFiltered(DiagnosticLevel.Info));
        Assert.Equal(
            DiagnosticLevel.Hidden,
            new EnumProperty<ExampleEnum>("name", ExampleEnum.Hello, level: DiagnosticLevel.Hidden).Level);
    }

    [DebugOnlyFact]
    public void ToJsonMap_EmitsOptionalKeysOnlyWhenNeeded()
    {
        DiagnosticsSerializationDelegate serializer = DiagnosticsSerializationDelegate.Create();
        Dictionary<string, object?> json = new StringProperty("name", "value").ToJsonMap(serializer);

        Assert.Equal("\"value\"", json["description"]);
        Assert.Equal("StringProperty", json["type"]);
        Assert.Equal("name", json["name"]);
        Assert.Equal("String", json["propertyType"]);
        Assert.Equal("info", json["defaultLevel"]);
        Assert.Equal(false, json["missingIfNull"]);
        Assert.Equal(true, json["quoted"]);
        Assert.Equal("value", json["value"]);
        Assert.Equal("singleLine", json["style"]);
        Assert.DoesNotContain("defaultValue", json);
        Assert.DoesNotContain("exception", json);
        Assert.DoesNotContain("level", json);
        Assert.DoesNotContain("showName", json);
        Assert.DoesNotContain("hasChildren", json);
        Assert.DoesNotContain("properties", json);
        Assert.DoesNotContain("children", json);

        Dictionary<string, object?> hidden = new StringProperty(
                "name",
                "value",
                showName: false,
                level: DiagnosticLevel.Hidden,
                defaultValue: DiagnosticsDefaults.NullValue)
            .ToJsonMap(serializer);
        Assert.Equal("hidden", hidden["level"]);
        Assert.Equal(false, hidden["showName"]);
        Assert.Equal("null", hidden["defaultValue"]);

        Dictionary<string, object?> noSeparator =
            new DiagnosticsProperty<string>("name", "value", showSeparator: false).ToJsonMap(serializer);
        Assert.Equal(false, noSeparator["showSeparator"]);

        Assert.Equal(10, new DiagnosticsProperty<int?>("int1", 10).ToJsonMap(serializer)["value"]);
        Assert.Equal(20, new IntProperty("int2", 20).ToJsonMap(serializer)["value"]);
        Assert.Equal(33.3, new DoubleProperty("double", 33.3).ToJsonMap(serializer)["value"]);
        Assert.Equal(true, new DiagnosticsProperty<bool?>("bool", true).ToJsonMap(serializer)["value"]);
        Assert.Equal(
            "Infinity",
            new DoubleProperty("double", double.PositiveInfinity).ToJsonMap(serializer)["value"]);
    }

    [DebugOnlyFact]
    public void SerializationDelegate_ControlsDepthPropertiesAndTruncation()
    {
        DiagnosticsNode node = CreatePropertyTree().ToDiagnosticsNode();

        Dictionary<string, object?> shallow = node.ToJsonMap(DiagnosticsSerializationDelegate.Create());
        Assert.DoesNotContain("properties", shallow);
        Assert.DoesNotContain("children", shallow);
        Assert.Equal(true, shallow["hasChildren"]);

        Dictionary<string, object?> depth1 =
            node.ToJsonMap(DiagnosticsSerializationDelegate.Create(subtreeDepth: 1));
        var children = Assert.IsType<List<Dictionary<string, object?>>>(depth1["children"]);
        Assert.Equal(3, children.Count);
        Assert.All(children, child => Assert.DoesNotContain("children", child));

        Dictionary<string, object?> depth5 =
            node.ToJsonMap(DiagnosticsSerializationDelegate.Create(subtreeDepth: 5));
        var deepChildren = Assert.IsType<List<Dictionary<string, object?>>>(depth5["children"]);
        Assert.Equal(
            3,
            Assert.IsType<List<Dictionary<string, object?>>>(deepChildren[1]["children"]).Count);

        Dictionary<string, object?> withProperties =
            node.ToJsonMap(DiagnosticsSerializationDelegate.Create(includeProperties: true));
        Assert.Equal(
            7,
            Assert.IsType<List<Dictionary<string, object?>>>(withProperties["properties"]).Count);
        Assert.DoesNotContain("children", withProperties);

        Dictionary<string, object?> truncated = node.ToJsonMap(new TruncatingDelegate(2, 1));
        var truncatedChildren = Assert.IsType<List<Dictionary<string, object?>>>(truncated["children"]);
        Assert.Equal(3, truncatedChildren.Count);
        Assert.Equal(true, truncatedChildren[^1]["truncated"]);
    }

    [DebugOnlyFact]
    public void DiagnosticsBlock_CarriesItsOwnPropertiesAndChildren()
    {
        var block = new DiagnosticsBlock(
            name: "block",
            description: "a block",
            properties: [new StringProperty("p", "v", quoted: false)],
            children: [DiagnosticsNode.Message("child")]);

        Assert.Equal("a block", block.ToDescription());
        Assert.Single(block.GetProperties());
        Assert.Single(block.GetChildren());
        Assert.Equal(
            Lines("block: a block:", "  p: v", "  child"),
            block.ToStringDeep());

        var unnamed = new DiagnosticsBlock(description: "anonymous");
        Assert.False(unnamed.ShowName);
    }

    [DebugOnlyFact]
    public void DiagnosticsNode_RejectsNamesEndingWithColon()
    {
        Assert.Throws<ArgumentException>(() => new StringProperty("name:", "value"));
    }

    [DebugOnlyFact]
    public void Widget_IsDiagnosticableTreeWithDenseStyleAndKeyedShortName()
    {
        var widget = new SizedBox(width: 10, height: 10);
        Assert.Equal("SizedBox", widget.ToStringShort());

        var keyed = new SizedBox(key: Key.Create("k"), width: 10, height: 10);
        Assert.Equal("SizedBox-[<'k'>]", keyed.ToStringShort());

        var builder = new DiagnosticPropertiesBuilder();
        widget.DebugFillProperties(builder);
        Assert.Equal(DiagnosticsTreeStyle.Dense, builder.DefaultDiagnosticsTreeStyle);
    }

    private static string Lines(params string[] lines) => string.Concat(lines.Select(line => line + "\n"));

    private static string Normalize(string value) => Regex.Replace(value, "#[0-9a-fA-F]{5}", "#00000");

    private static string RenderStyleTree(
        DiagnosticsTreeStyle style,
        DiagnosticsTreeStyle? lastChildStyle = null)
    {
        var root = new TestTree(
            style: lastChildStyle,
            children:
            [
                new TestTree(name: "node A", style: style),
                new TestTree(
                    name: "node B",
                    style: style,
                    children:
                    [
                        new TestTree(name: "node B1", style: style),
                        new TestTree(name: "node B2", style: style),
                        new TestTree(name: "node B3", style: lastChildStyle ?? style),
                    ]),
                new TestTree(name: "node C", style: lastChildStyle ?? style),
            ]);

        return Normalize(root.ToDiagnosticsNode(style: style).ToStringDeep());
    }

    private static TestTree CreatePropertyTree(
        DiagnosticsTreeStyle? style = null,
        DiagnosticsTreeStyle? lastChildStyle = null,
        DiagnosticsTreeStyle propertyStyle = DiagnosticsTreeStyle.SingleLine)
    {
        return new TestTree(
            style: lastChildStyle,
            properties:
            [
                new StringProperty("stringProperty1", "value1", quoted: false, style: propertyStyle),
                new DoubleProperty("doubleProperty1", 42.5, style: propertyStyle),
                new DoubleProperty("roundedProperty", 1.0 / 3.0, style: propertyStyle),
                new StringProperty(
                    "DO_NOT_SHOW",
                    "DO_NOT_SHOW",
                    level: DiagnosticLevel.Hidden,
                    quoted: false,
                    style: propertyStyle),
                new DiagnosticsProperty<object>(
                    "DO_NOT_SHOW_NULL",
                    null,
                    defaultValue: DiagnosticsDefaults.NullValue,
                    style: propertyStyle),
                new DiagnosticsProperty<object>("nullProperty", null, style: propertyStyle),
                new StringProperty(
                    "node_type",
                    "<root node>",
                    showName: false,
                    quoted: false,
                    style: propertyStyle),
            ],
            children:
            [
                new TestTree(name: "node A", style: style),
                new TestTree(
                    name: "node B",
                    style: style,
                    properties:
                    [
                        new StringProperty("p1", "v1", quoted: false, style: propertyStyle),
                        new StringProperty("p2", "v2", quoted: false, style: propertyStyle),
                    ],
                    children:
                    [
                        new TestTree(name: "node B1", style: style),
                        new TestTree(
                            name: "node B2",
                            style: style,
                            properties:
                            [
                                new StringProperty("property1", "value1", quoted: false, style: propertyStyle),
                            ]),
                        new TestTree(
                            name: "node B3",
                            style: lastChildStyle ?? style,
                            properties:
                            [
                                new StringProperty(
                                    "node_type",
                                    "<leaf node>",
                                    showName: false,
                                    quoted: false,
                                    style: propertyStyle),
                                new IntProperty("foo", 42, style: propertyStyle),
                            ]),
                    ]),
                new TestTree(
                    name: "node C",
                    style: lastChildStyle ?? style,
                    properties:
                    [
                        new StringProperty("foo", "multi\nline\nvalue!", quoted: false, style: propertyStyle),
                    ]),
            ]);
    }

    private static string RenderPropertyTree(
        DiagnosticsTreeStyle style,
        DiagnosticsTreeStyle? lastChildStyle = null,
        DiagnosticsTreeStyle propertyStyle = DiagnosticsTreeStyle.SingleLine,
        string? name = null)
    {
        TestTree tree = CreatePropertyTree(style, lastChildStyle, propertyStyle);
        return Normalize(tree.ToDiagnosticsNode(name: name, style: style).ToStringDeep());
    }

    private static DiagnosticsNode CreateTreeWithWrappingNodes(
        DiagnosticsTreeStyle rootStyle,
        DiagnosticsTreeStyle propertyStyle)
    {
        return new TestTree(
            name: "Test tree",
            properties:
            [
                DiagnosticsNode.Message(Example, style: propertyStyle),
                DiagnosticsNode.Message(VeryLongText, style: propertyStyle),
                DiagnosticsNode.Message(Example, style: propertyStyle),
                new DiagnosticsProperty<string>(null, NotAllowedToWrap, allowWrap: false, style: propertyStyle),
                DiagnosticsNode.Message(Example, style: propertyStyle),
                DiagnosticsNode.Message($"{LongPropertyName}:\n  {LongUrl}", style: propertyStyle),
                DiagnosticsNode.Message($"{LongPropertyName}:\n  https://goo.gl/", style: propertyStyle),
                DiagnosticsNode.Message($"Click on the following url:\n  {LongUrl}", style: propertyStyle),
                DiagnosticsNode.Message("Click on the following url\n  https://goo.gl/", style: propertyStyle),
                DiagnosticsNode.Message(Example, style: propertyStyle),
                new DiagnosticsProperty<string>("multi-line value", Matrix, style: propertyStyle),
                DiagnosticsNode.Message(Example, style: propertyStyle),
                new DiagnosticsProperty<string>(LongPropertyName, VeryLongText, style: propertyStyle),
                DiagnosticsNode.Message(Example, style: propertyStyle),
                new DiagnosticsProperty<string>(LongPropertyName, Matrix, style: propertyStyle),
                DiagnosticsNode.Message(Example, style: propertyStyle),
                new MessageProperty(
                    "diagnosis",
                    "insufficient data to draw conclusion (less than five repaints)",
                    style: propertyStyle),
            ]).ToDiagnosticsNode(style: rootStyle);
    }

    private sealed class TestTree : DiagnosticableTree
    {
        internal TestTree(
            string name = "",
            DiagnosticsTreeStyle? style = null,
            IEnumerable<TestTree>? children = null,
            IEnumerable<DiagnosticsNode>? properties = null)
        {
            TreeName = name;
            TreeStyle = style;
            Children = children is null ? [] : [.. children];
            TreeProperties = properties is null ? [] : [.. properties];
        }

        internal string TreeName { get; }

        internal DiagnosticsTreeStyle? TreeStyle { get; }

        internal List<TestTree> Children { get; }

        internal List<DiagnosticsNode> TreeProperties { get; }

        public override List<DiagnosticsNode> DebugDescribeChildren() =>
            Children.Select(child => child.ToDiagnosticsNode($"child {child.TreeName}", child.TreeStyle)).ToList();

        public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
        {
            base.DebugFillProperties(properties);
            if (TreeStyle is not null)
            {
                properties.DefaultDiagnosticsTreeStyle = TreeStyle.Value;
            }

            foreach (DiagnosticsNode property in TreeProperties)
            {
                properties.Add(property);
            }
        }
    }

    private sealed class BuildModeTree : DiagnosticableTree
    {
        internal int DebugFillPropertiesCalls { get; private set; }

        public override string ToStringShort() => "BuildModeTree";

        public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
        {
            DebugFillPropertiesCalls++;
            properties.Add(new StringProperty("property", "value", quoted: false));
        }
    }

    private sealed class TruncatingDelegate : DiagnosticsSerializationDelegate
    {
        private readonly int _limit;

        internal TruncatingDelegate(int limit, int subtreeDepth)
        {
            _limit = limit;
            SubtreeDepth = subtreeDepth;
        }

        public override int SubtreeDepth { get; }

        public override bool IncludeProperties => false;

        public override bool ExpandPropertyValues => false;

        public override IReadOnlyDictionary<string, object?> AdditionalNodeProperties(
            DiagnosticsNode node,
            bool fullDetails = true) => new Dictionary<string, object?>(StringComparer.Ordinal);

        public override DiagnosticsSerializationDelegate DelegateForNode(DiagnosticsNode node)
            => SubtreeDepth > 0 ? new TruncatingDelegate(_limit, SubtreeDepth - 1) : this;

        public override List<DiagnosticsNode> FilterChildren(List<DiagnosticsNode> nodes, DiagnosticsNode owner)
            => nodes;

        public override List<DiagnosticsNode> FilterProperties(List<DiagnosticsNode> nodes, DiagnosticsNode owner)
            => nodes;

        public override List<DiagnosticsNode> TruncateNodesList(List<DiagnosticsNode> nodes, DiagnosticsNode? owner)
            => nodes.Take(_limit).ToList();

        public override DiagnosticsSerializationDelegate CopyWith(
            int? subtreeDepth = null,
            bool? includeProperties = null)
            => new TruncatingDelegate(_limit, subtreeDepth ?? SubtreeDepth);
    }
}
