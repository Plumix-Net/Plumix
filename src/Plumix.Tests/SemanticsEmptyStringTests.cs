using Avalonia;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/semantics/semantics.dart
// String contracts from test/semantics/semantics_test.dart and test/widgets/semantics_test.dart.

namespace Plumix.Tests;

public sealed class SemanticsEmptyStringTests
{
    [Fact]
    public void UntouchedConfigurationNodeAndDataHaveNonNullEmptyText()
    {
        var config = new SemanticsConfiguration();
        Assert.False(config.HasBeenAnnotated);
        Assert.False(config.Clone().HasBeenAnnotated);
        AssertEmpty(config);

        var node = new SemanticsNode();
        AssertEmpty(node.GetSemanticsData());
        Assert.Empty(node.Label);
        Assert.Empty(node.Hint);
        Assert.Empty(node.Value);
        Assert.Empty(node.IncreasedValue);
        Assert.Empty(node.DecreasedValue);
        Assert.Empty(node.Identifier);
        Assert.Empty(node.Tooltip);
    }

    [Theory]
    [InlineData("label")]
    [InlineData("value")]
    [InlineData("increasedValue")]
    [InlineData("decreasedValue")]
    [InlineData("hint")]
    [InlineData("tooltip")]
    [InlineData("identifier")]
    public void ExplicitEmptyTextIsAnAnnotationAndClonePreservesIt(string slot)
    {
        var config = new SemanticsConfiguration();
        SetText(config, slot, string.Empty);
        Assert.True(config.HasBeenAnnotated);
        Assert.True(config.Clone().HasBeenAnnotated);
        AssertEmpty(config);
        Assert.True(config.IsCompatibleWith(new SemanticsConfiguration()));

        var parent = new SemanticsConfiguration();
        parent.Absorb(config);
        Assert.True(parent.HasBeenAnnotated);
        AssertEmpty(parent);
    }

    [Fact]
    public void ExplicitNullTextDirectionIsAnAnnotationAndClonePreservesIt()
    {
        var config = new SemanticsConfiguration { TextDirection = null };
        Assert.True(config.HasBeenAnnotated);
        Assert.True(config.Clone().HasBeenAnnotated);
    }

    [Theory]
    [InlineData("label")]
    [InlineData("value")]
    [InlineData("increasedValue")]
    [InlineData("decreasedValue")]
    [InlineData("hint")]
    public void ClearingPlainTextDropsAttributesButRemainsAnnotated(string slot)
    {
        var text = new AttributedString("text", [new SpellOutStringAttribute(new TextRange(0, 4))]);
        var config = new SemanticsConfiguration();
        SetAttributedText(config, slot, text);
        Assert.Same(text, GetAttributedText(config, slot));

        SetText(config, slot, string.Empty);
        Assert.Empty(GetAttributedText(config, slot).String);
        Assert.Empty(GetAttributedText(config, slot).Attributes);
        Assert.True(config.Clone().HasBeenAnnotated);
    }

    [Theory]
    [InlineData("label")]
    [InlineData("value")]
    [InlineData("increasedValue")]
    [InlineData("decreasedValue")]
    [InlineData("hint")]
    public void ExplicitEmptyAttributedTextIsAnAnnotation(string slot)
    {
        var config = new SemanticsConfiguration();
        SetAttributedText(config, slot, AttributedString.Empty);
        Assert.True(config.HasBeenAnnotated);
        Assert.True(config.Clone().HasBeenAnnotated);
    }

    [Fact]
    public void AbsorbValuesTooltipAndIdentifierTakeTheFirstNonEmptyText()
    {
        var first = new AttributedString("first", [new SpellOutStringAttribute(new TextRange(0, 5))]);
        var parent = new SemanticsConfiguration
        {
            Value = string.Empty,
            IncreasedValue = string.Empty,
            DecreasedValue = string.Empty,
            Tooltip = string.Empty,
            Identifier = string.Empty,
            TextDirection = TextDirection.Ltr,
        };
        parent.Absorb(new SemanticsConfiguration { Label = "only a label", TextDirection = TextDirection.Ltr });
        parent.Absorb(new SemanticsConfiguration
        {
            AttributedValue = first,
            AttributedIncreasedValue = first,
            AttributedDecreasedValue = first,
            Tooltip = "first tooltip",
            Identifier = "first identifier",
        });
        parent.Absorb(new SemanticsConfiguration
        {
            Value = "second",
            IncreasedValue = "second",
            DecreasedValue = "second",
            Tooltip = "second tooltip",
            Identifier = "second identifier",
        });

        Assert.Same(first, parent.AttributedValue);
        Assert.Same(first, parent.AttributedIncreasedValue);
        Assert.Same(first, parent.AttributedDecreasedValue);
        Assert.Equal("first tooltip", parent.Tooltip);
        Assert.Equal("first identifier", parent.Identifier);
    }

    [Fact]
    public void EmptyValueDoesNotConflictButWhitespaceIsAValue()
    {
        var empty = new SemanticsConfiguration { Value = string.Empty };
        var whitespace = new SemanticsConfiguration { Value = " " };
        var value = new SemanticsConfiguration { Value = "value" };

        Assert.True(empty.IsCompatibleWith(value));
        Assert.True(value.IsCompatibleWith(empty));
        Assert.False(whitespace.IsCompatibleWith(value));
        Assert.False(value.IsCompatibleWith(whitespace));
    }

    [Fact]
    public void EmptyLabelAndHintStillPreserveOppositeDirectionEmbeddingAndAttributes()
    {
        var parent = new SemanticsConfiguration { TextDirection = TextDirection.Ltr };
        var text = new AttributedString("rtl", [new SpellOutStringAttribute(new TextRange(0, 3))]);
        parent.Absorb(new SemanticsConfiguration
        {
            AttributedLabel = text,
            AttributedHint = text,
            TextDirection = TextDirection.Rtl,
        });

        Assert.Equal("\u202brtl\u202c", parent.Label);
        Assert.Equal(parent.Label, parent.Hint);
        Assert.Equal(new TextRange(1, 4), Assert.Single(parent.AttributedLabel.Attributes).Range);
        Assert.Equal(new TextRange(1, 4), Assert.Single(parent.AttributedHint.Attributes).Range);
        parent.Absorb(new SemanticsConfiguration { Label = string.Empty, Hint = string.Empty });
        Assert.Equal("\u202brtl\u202c", parent.Label);
    }

    [Fact]
    public void NodeUpdateResetsPreviousTextToNonNullEmptyStrings()
    {
        var node = new SemanticsNode { Rect = new Rect(0, 0, 10, 10) };
        node.UpdateWith(new SemanticsConfiguration
        {
            Label = "label",
            Hint = "hint",
            Value = "value",
            IncreasedValue = "increase",
            DecreasedValue = "decrease",
            Tooltip = "tooltip",
            Identifier = "identifier",
            TextDirection = TextDirection.Ltr,
        });
        node.UpdateWith(null);
        AssertEmpty(node.GetSemanticsData());
        Assert.Empty(node.Label);
        Assert.Empty(node.Tooltip);
        Assert.Empty(node.Identifier);
    }

    [Fact]
    public void MergedDataSkipsEmptyDescendantsAndPreservesFirstNonEmptyValueAttributes()
    {
        var text = new AttributedString("value", [new SpellOutStringAttribute(new TextRange(0, 5))]);
        var empty = new SemanticsNode { IsMergedIntoParent = true };
        var populated = new SemanticsNode { IsMergedIntoParent = true };
        populated.UpdateWith(new SemanticsConfiguration
        {
            AttributedValue = text,
            AttributedIncreasedValue = text,
            AttributedDecreasedValue = text,
            Tooltip = "tip",
            Identifier = "id",
            TextDirection = TextDirection.Ltr,
        });
        var root = new SemanticsNode();
        root.UpdateWith(new SemanticsConfiguration
        {
            IsSemanticBoundary = true,
            IsMergingSemanticsOfDescendants = true,
        }, [empty, populated]);

        SemanticsData data = root.GetSemanticsData();
        Assert.Same(text, data.AttributedValue);
        Assert.Same(text, data.AttributedIncreasedValue);
        Assert.Same(text, data.AttributedDecreasedValue);
        Assert.Equal("tip", data.Tooltip);
        Assert.Equal("id", data.Identifier);
        Assert.Empty(data.Label);
        Assert.Empty(data.Hint);
    }

    [Fact]
    public void EmptyDataDiagnosticsOmitAllEmptyTextProperties()
    {
        string description = new SemanticsNode().GetSemanticsData().ToString();
        foreach (string name in new[] { "label", "hint", "value", "identifier", "tooltip" })
        {
            Assert.DoesNotContain($"{name}:", description, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RenderAnnotationsKeepAbsentPropertiesNullableButEmitEmptyText()
    {
        var properties = new SemanticsProperties();
        Assert.Null(properties.Label);
        Assert.Null(properties.Value);
        Assert.Null(properties.Hint);
        Assert.Null(properties.Tooltip);
        Assert.Null(properties.Identifier);

        var render = new RenderSemanticsAnnotations(properties);
        var config = new SemanticsConfiguration();
        render.InvokeDescribeSemanticsConfiguration(config);
        Assert.False(config.HasBeenAnnotated);
        AssertEmpty(config);
    }

    [Fact]
    public void IdentifiedEmptyTextWrapperMergesItsChildLabelWithoutChangingLayout()
    {
        var child = new RenderSemanticsAnnotations(
            new SemanticsProperties(label: "readout"),
            textDirection: TextDirection.Ltr,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        var wrapper = new RenderSemanticsAnnotations(
            new SemanticsProperties(
                identifier: "readout-id", label: string.Empty, value: string.Empty,
                hint: string.Empty, tooltip: string.Empty),
            textDirection: TextDirection.Ltr,
            child: child);
        var view = new RenderView(new FlutterView(new Size(320, 120))) { Child = wrapper };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        pipeline.FlushLayout(new Size(320, 120));
        pipeline.FlushSemantics();

        SemanticsNode node = Assert.Single(pipeline.SemanticsOwner!.RootNode!.Children);
        Assert.Equal("readout-id", node.Identifier);
        Assert.Equal("readout", node.Label);
        Assert.Empty(node.Children);
        Assert.Empty(node.Value);
        Assert.Empty(node.Hint);
        Assert.Empty(node.Tooltip);
        Assert.Equal(child.Size, wrapper.Size);
        Assert.Equal(wrapper.Size, node.Rect.Size);
    }

    private static void AssertEmpty(SemanticsConfiguration config)
    {
        Assert.Empty(config.Identifier);
        Assert.Empty(config.Tooltip);
        foreach (string slot in new[] { "label", "value", "increasedValue", "decreasedValue", "hint" })
        {
            Assert.Empty(GetAttributedText(config, slot).String);
            Assert.Empty(GetAttributedText(config, slot).Attributes);
        }
        Assert.Null(config.MinValue);
        Assert.Null(config.MaxValue);
        Assert.Null(config.HintOverrides);
    }

    private static void AssertEmpty(SemanticsData data)
    {
        Assert.Empty(data.Identifier);
        Assert.Empty(data.Tooltip);
        foreach (AttributedString text in new[]
                 {
                     data.AttributedLabel, data.AttributedValue, data.AttributedIncreasedValue,
                     data.AttributedDecreasedValue, data.AttributedHint,
                 })
        {
            Assert.Empty(text.String);
            Assert.Empty(text.Attributes);
        }
    }

    private static void SetText(SemanticsConfiguration config, string slot, string text)
    {
        switch (slot)
        {
            case "label": config.Label = text; break;
            case "value": config.Value = text; break;
            case "increasedValue": config.IncreasedValue = text; break;
            case "decreasedValue": config.DecreasedValue = text; break;
            case "hint": config.Hint = text; break;
            case "tooltip": config.Tooltip = text; break;
            case "identifier": config.Identifier = text; break;
            default: throw new ArgumentOutOfRangeException(nameof(slot));
        }
    }

    private static void SetAttributedText(SemanticsConfiguration config, string slot, AttributedString text)
    {
        switch (slot)
        {
            case "label": config.AttributedLabel = text; break;
            case "value": config.AttributedValue = text; break;
            case "increasedValue": config.AttributedIncreasedValue = text; break;
            case "decreasedValue": config.AttributedDecreasedValue = text; break;
            case "hint": config.AttributedHint = text; break;
            default: throw new ArgumentOutOfRangeException(nameof(slot));
        }
    }

    private static AttributedString GetAttributedText(SemanticsConfiguration config, string slot) => slot switch
    {
        "label" => config.AttributedLabel,
        "value" => config.AttributedValue,
        "increasedValue" => config.AttributedIncreasedValue,
        "decreasedValue" => config.AttributedDecreasedValue,
        "hint" => config.AttributedHint,
        _ => throw new ArgumentOutOfRangeException(nameof(slot)),
    };
}
