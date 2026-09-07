using Avalonia;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity sources:
// flutter/packages/flutter/lib/src/semantics/semantics.dart
// flutter/packages/flutter/lib/src/rendering/object.dart (SemanticsAnnotationsMixin)
// flutter/packages/flutter/lib/src/widgets/basic.dart (Semantics, SliverSemantics)
// Tests mirrored from flutter/packages/flutter/test/semantics/semantics_test.dart,
// test/widgets/semantics_test.dart, test/widgets/basic_test.dart and
// test/widgets/sliver_semantics_widget_test.dart.

namespace Plumix.Tests;

public sealed class SemanticsPropertiesTests
{
    private static readonly Size ViewSize = new(320, 120);

    // ---- AttributedString ---------------------------------------------------------------------

    [Fact]
    public void AttributedString_ConcatShiftsTheRightHandRanges()
    {
        var left = new AttributedString(
            "string1",
            [new SpellOutStringAttribute(new TextRange(0, 4))]);
        var right = new AttributedString(
            "string2",
            [new LocaleStringAttribute(new TextRange(0, 4), "es-MX")]);

        AttributedString merged = left + right;

        Assert.Equal("string1string2", merged.String);
        Assert.Equal(2, merged.Attributes.Count);
        Assert.Equal(new TextRange(0, 4), merged.Attributes[0].Range);
        Assert.IsType<SpellOutStringAttribute>(merged.Attributes[0]);
        Assert.Equal(new TextRange(7, 11), merged.Attributes[1].Range);
        var locale = Assert.IsType<LocaleStringAttribute>(merged.Attributes[1]);
        Assert.Equal("es-MX", locale.Locale);
    }

    [Fact]
    public void AttributedString_ConcatWithAnEmptySideReturnsTheOtherSide()
    {
        var value = new AttributedString("only", [new SpellOutStringAttribute(new TextRange(0, 2))]);

        Assert.Same(value, AttributedString.Empty + value);
        Assert.Same(value, value + AttributedString.Empty);
    }

    [Fact]
    public void AttributedString_EqualityComparesStringAndAttributes()
    {
        var first = new AttributedString("a", [new SpellOutStringAttribute(new TextRange(0, 1))]);
        var second = new AttributedString("a", [new SpellOutStringAttribute(new TextRange(0, 1))]);
        var third = new AttributedString("a", [new SpellOutStringAttribute(new TextRange(0, 0))]);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, third);
        Assert.NotEqual(first, new AttributedString("b"));
    }

    [Fact]
    public void AttributedString_ToStringMatchesDart()
    {
        var value = new AttributedString(
            "string1string2",
            [
                new SpellOutStringAttribute(new TextRange(0, 4)),
                new LocaleStringAttribute(new TextRange(7, 11), "es-MX")
            ]);

        Assert.Equal(
            "AttributedString('string1string2', attributes: "
            + "[SpellOutStringAttribute { Range = TextRange(start: 0, end: 4) }, "
            + "LocaleStringAttribute { Range = TextRange(start: 7, end: 11), Locale = es-MX }])",
            value.ToString());
    }

    [Fact]
    public void AttributedString_RejectsOutOfRangeAttributesAndAttributedEmptyStrings()
    {
        Assert.Throws<ArgumentException>(() =>
            new AttributedString("ab", [new SpellOutStringAttribute(new TextRange(0, 3))]));
        Assert.Throws<ArgumentException>(() =>
            new AttributedString(string.Empty, [new SpellOutStringAttribute(new TextRange(0, 0))]));
    }

    // ---- SemanticsConfiguration slots ---------------------------------------------------------

    [Fact]
    public void Configuration_PlainStringSettersReplaceTheAttributedValue()
    {
        var config = new SemanticsConfiguration { Label = "label1" };
        Assert.Equal("label1", config.AttributedLabel!.String);
        Assert.Empty(config.AttributedLabel.Attributes);

        config.AttributedLabel = new AttributedString(
            "label2",
            [new SpellOutStringAttribute(new TextRange(0, 1))]);
        Assert.Equal("label2", config.Label);
        Assert.Single(config.AttributedLabel.Attributes);

        // Re-assigning the plain string drops the attributes, exactly as Dart's setter does.
        config.Label = "label3";
        Assert.Equal("label3", config.Label);
        Assert.Empty(config.AttributedLabel.Attributes);
    }

    [Fact]
    public void Configuration_ValueAndHintRoundTripThroughTheirAttributedForm()
    {
        var config = new SemanticsConfiguration { Value = "value1", Hint = "hint1" };
        Assert.Equal("value1", config.AttributedValue!.String);
        Assert.Equal("hint1", config.AttributedHint!.String);

        config.AttributedValue = new AttributedString("value2", [new SpellOutStringAttribute(new TextRange(0, 1))]);
        config.AttributedHint = new AttributedString("hint2", [new SpellOutStringAttribute(new TextRange(0, 1))]);
        Assert.Equal("value2", config.Value);
        Assert.Equal("hint2", config.Hint);
    }

    [Fact]
    public void Configuration_HintOverridesCannotBeCleared()
    {
        var config = new SemanticsConfiguration
        {
            HintOverrides = new SemanticsHintOverrides(onTapHint: "tap", onLongPressHint: "press"),
        };
        Assert.Equal("tap", config.OnTapHint);
        Assert.Equal("press", config.OnLongPressHint);

        // Dart's setter silently ignores a null assignment.
        config.HintOverrides = null;
        Assert.Equal("tap", config.OnTapHint);
    }

    [Fact]
    public void Configuration_HeadingLevelRejectsValuesOutsideZeroToSix()
    {
        var config = new SemanticsConfiguration();
        foreach (int level in new[] { 0, 1, 2, 3, 4, 5, 6 })
        {
            config.HeadingLevel = level;
            Assert.Equal(level, config.HeadingLevel);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => config.HeadingLevel = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => config.HeadingLevel = 7);
    }

    [Theory]
    // (parent, child, merged) — Flutter's `_mergeHeadingLevels`: the parent wins unless it is 0.
    [InlineData(0, 0, 0)]
    [InlineData(0, 1, 1)]
    [InlineData(0, 2, 2)]
    [InlineData(1, 0, 1)]
    [InlineData(2, 0, 2)]
    [InlineData(3, 2, 3)]
    [InlineData(4, 1, 4)]
    [InlineData(2, 3, 2)]
    [InlineData(1, 5, 1)]
    public void Configuration_AbsorbTakesTheParentHeadingLevel(int parentLevel, int childLevel, int expected)
    {
        var parent = new SemanticsConfiguration { Label = "parent", HeadingLevel = parentLevel };
        var child = new SemanticsConfiguration { Label = "child", HeadingLevel = childLevel };

        parent.Absorb(child);

        Assert.Equal(expected, parent.HeadingLevel);
    }

    [Fact]
    public void Configuration_AbsorbConcatenatesLabelsAndHintsButNotValues()
    {
        var parent = new SemanticsConfiguration { Label = "label1", Hint = "hint1", Value = "value1" };
        var child = new SemanticsConfiguration { Label = "label2", Hint = "hint2", Value = "value2" };

        parent.Absorb(child);

        Assert.Equal("label1\nlabel2", parent.Label);
        Assert.Equal("hint1\nhint2", parent.Hint);
        Assert.Equal("value1", parent.Value);
    }

    [Fact]
    public void Configuration_AbsorbWrapsAChildOfTheOppositeDirectionInABidiEmbedding()
    {
        var parent = new SemanticsConfiguration { Label = "ltr", TextDirection = TextDirection.Ltr };
        var child = new SemanticsConfiguration { Label = "rtl", TextDirection = TextDirection.Rtl };

        parent.Absorb(child);

        Assert.Equal(
            "ltr\n"
            + UnicodeMarks.RightToLeftEmbedding
            + "rtl"
            + UnicodeMarks.PopDirectionalFormatting,
            parent.Label);
    }

    [Fact]
    public void Configuration_AbsorbKeepsTheChildAttributesAtTheirShiftedRanges()
    {
        var parent = new SemanticsConfiguration
        {
            AttributedLabel = new AttributedString("label", [new SpellOutStringAttribute(new TextRange(0, 5))]),
        };
        var child = new SemanticsConfiguration
        {
            AttributedLabel = new AttributedString("label", [new SpellOutStringAttribute(new TextRange(0, 5))]),
        };

        parent.Absorb(child);

        Assert.Equal("label\nlabel", parent.Label);
        Assert.Equal(2, parent.AttributedLabel!.Attributes.Count);
        Assert.Equal(new TextRange(0, 5), parent.AttributedLabel.Attributes[0].Range);
        Assert.Equal(new TextRange(6, 11), parent.AttributedLabel.Attributes[1].Range);
    }

    [Fact]
    public void Configuration_AbsorbUnionsControlsNodesAndTakesTheChildIdentifier()
    {
        var parent = new SemanticsConfiguration { Label = "parent", ControlsNodes = new HashSet<string> { "abc" } };
        var child = new SemanticsConfiguration
        {
            Label = "child",
            Identifier = "child-id",
            ControlsNodes = new HashSet<string> { "def", "ghi" },
        };

        parent.Absorb(child);

        Assert.Equal(3, parent.ControlsNodes!.Count);
        Assert.Contains("abc", parent.ControlsNodes);
        Assert.Contains("def", parent.ControlsNodes);
        Assert.Contains("ghi", parent.ControlsNodes);
        Assert.Equal("child-id", parent.Identifier);
    }

    [Theory]
    [InlineData(SemanticsValidationResult.None, SemanticsValidationResult.None, SemanticsValidationResult.None)]
    [InlineData(SemanticsValidationResult.None, SemanticsValidationResult.Valid, SemanticsValidationResult.Valid)]
    [InlineData(SemanticsValidationResult.None, SemanticsValidationResult.Invalid, SemanticsValidationResult.Invalid)]
    [InlineData(SemanticsValidationResult.Valid, SemanticsValidationResult.None, SemanticsValidationResult.Valid)]
    [InlineData(SemanticsValidationResult.Valid, SemanticsValidationResult.Valid, SemanticsValidationResult.Valid)]
    [InlineData(SemanticsValidationResult.Valid, SemanticsValidationResult.Invalid, SemanticsValidationResult.Invalid)]
    [InlineData(SemanticsValidationResult.Invalid, SemanticsValidationResult.None, SemanticsValidationResult.Invalid)]
    [InlineData(SemanticsValidationResult.Invalid, SemanticsValidationResult.Valid, SemanticsValidationResult.Invalid)]
    [InlineData(
        SemanticsValidationResult.Invalid,
        SemanticsValidationResult.Invalid,
        SemanticsValidationResult.Invalid)]
    public void Configuration_AbsorbLetsInvalidWin(
        SemanticsValidationResult parentResult,
        SemanticsValidationResult childResult,
        SemanticsValidationResult expected)
    {
        var parent = new SemanticsConfiguration { Label = "parent", ValidationResult = parentResult };
        var child = new SemanticsConfiguration { Label = "child", ValidationResult = childResult };

        parent.Absorb(child);

        Assert.Equal(expected, parent.ValidationResult);
    }

    [Fact]
    public void Configuration_AbsorbLetsTheChildActionHandlerWin()
    {
        int parentTaps = 0;
        int childTaps = 0;
        var parent = new SemanticsConfiguration { Label = "parent", OnTap = () => parentTaps++ };
        var child = new SemanticsConfiguration { Label = "child", OnTap = () => childTaps++ };

        parent.Absorb(child);
        parent.ActionHandlers[SemanticsActions.Tap](null);

        // Dart's `_actions.addAll(child._actions)` overwrites; the absorbed handler runs.
        Assert.Equal(0, parentTaps);
        Assert.Equal(1, childTaps);
    }

    [Fact]
    public void Configuration_AbsorbTakesTheChildLinkUrlAndLengthCounters()
    {
        var parent = new SemanticsConfiguration { Label = "parent" };
        var child = new SemanticsConfiguration
        {
            Label = "child",
            LinkUrl = new Uri("https://example.com/"),
            MaxValueLength = 10,
            CurrentValueLength = 3,
        };

        parent.Absorb(child);

        Assert.Equal(new Uri("https://example.com/"), parent.LinkUrl);
        Assert.Equal(10, parent.MaxValueLength);
        Assert.Equal(3, parent.CurrentValueLength);
    }

    [Fact]
    public void Configuration_IsCompatibleWithRejectsDoubledLengthsAndRangeBounds()
    {
        Assert.False(WithBoth(c => c.MaxValueLength = 4));
        Assert.False(WithBoth(c => c.CurrentValueLength = 4));
        Assert.False(WithBoth(c => c.PlatformViewId = 4));
        Assert.False(WithBoth(c => c.MinValue = "0"));
        Assert.False(WithBoth(c => c.MaxValue = "9"));
        Assert.False(WithBoth(c => c.Value = "v"));
        Assert.True(WithBoth(c => c.Label = "l"));

        static bool WithBoth(Action<SemanticsConfiguration> annotate)
        {
            var first = new SemanticsConfiguration();
            var second = new SemanticsConfiguration();
            annotate(first);
            annotate(second);
            return first.IsCompatibleWith(second);
        }
    }

    [Fact]
    public void Configuration_CloneCarriesTheNewSlots()
    {
        var config = new SemanticsConfiguration
        {
            Identifier = "id",
            HeadingLevel = 3,
            LinkUrl = new Uri("https://example.com/"),
            MaxValueLength = 10,
            CurrentValueLength = 4,
            ControlsNodes = new HashSet<string> { "abc" },
            ValidationResult = SemanticsValidationResult.Invalid,
            TextSelection = new TextSelection(1, 2),
            PlatformViewId = 7,
            HintOverrides = new SemanticsHintOverrides(onTapHint: "tap"),
            AttributedLabel = new AttributedString("l", [new SpellOutStringAttribute(new TextRange(0, 1))]),
        };

        SemanticsConfiguration clone = config.Clone();

        Assert.Equal("id", clone.Identifier);
        Assert.Equal(3, clone.HeadingLevel);
        Assert.Equal(new Uri("https://example.com/"), clone.LinkUrl);
        Assert.Equal(10, clone.MaxValueLength);
        Assert.Equal(4, clone.CurrentValueLength);
        Assert.Equal(config.ControlsNodes, clone.ControlsNodes);
        Assert.Equal(SemanticsValidationResult.Invalid, clone.ValidationResult);
        Assert.Equal(new TextSelection(1, 2), clone.TextSelection);
        Assert.Equal(7, clone.PlatformViewId);
        Assert.Equal("tap", clone.OnTapHint);
        Assert.Single(clone.AttributedLabel!.Attributes);
    }

    [Fact]
    public void Configuration_TristateFlagsClearTheirStateBitWhenSetToNull()
    {
        var config = new SemanticsConfiguration { IsSelected = true, IsExpanded = false, IsRequired = true };
        Assert.True(config.Flags.HasFlag(SemanticsFlags.HasSelectedState));
        Assert.True(config.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.True(config.Flags.HasFlag(SemanticsFlags.HasExpandedState));
        Assert.False(config.Flags.HasFlag(SemanticsFlags.IsExpanded));
        Assert.True(config.Flags.HasFlag(SemanticsFlags.HasRequiredState));
        Assert.True(config.Flags.HasFlag(SemanticsFlags.IsRequired));

        config.IsSelected = null;
        config.IsExpanded = null;
        config.IsRequired = null;
        Assert.False(config.Flags.HasFlag(SemanticsFlags.HasSelectedState));
        Assert.False(config.Flags.HasFlag(SemanticsFlags.HasExpandedState));
        Assert.False(config.Flags.HasFlag(SemanticsFlags.HasRequiredState));
    }

    [Fact]
    public void Configuration_MixedOnlyWritesTheFlagForTrue()
    {
        var config = new SemanticsConfiguration { IsChecked = false, IsCheckStateMixed = true };
        Assert.True(config.Flags.HasFlag(SemanticsFlags.HasCheckedState));
        Assert.True(config.Flags.HasFlag(SemanticsFlags.IsCheckStateMixed));
        Assert.False(config.Flags.HasFlag(SemanticsFlags.IsChecked));

        var untouched = new SemanticsConfiguration { IsCheckStateMixed = false };
        Assert.Equal(SemanticsFlags.None, untouched.Flags);
    }

    // ---- SemanticsProperties ------------------------------------------------------------------

    [Fact]
    public void Properties_RejectPairingAPlainStringWithItsAttributedForm()
    {
        var attributed = new AttributedString("x");
        Assert.Throws<ArgumentException>(() => new SemanticsProperties(label: "x", attributedLabel: attributed));
        Assert.Throws<ArgumentException>(() => new SemanticsProperties(value: "x", attributedValue: attributed));
        Assert.Throws<ArgumentException>(() =>
            new SemanticsProperties(increasedValue: "x", attributedIncreasedValue: attributed));
        Assert.Throws<ArgumentException>(() =>
            new SemanticsProperties(decreasedValue: "x", attributedDecreasedValue: attributed));
        Assert.Throws<ArgumentException>(() => new SemanticsProperties(hint: "x", attributedHint: attributed));
    }

    [Fact]
    public void Properties_RejectHeadingLevelsOutsideOneToSixAndAnUnflaggedLinkUrl()
    {
        foreach (int level in new[] { -1, 0, 7, 8, 9 })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SemanticsProperties(headingLevel: level));
        }

        for (int level = 1; level <= 6; level++)
        {
            Assert.Equal(level, new SemanticsProperties(headingLevel: level).HeadingLevel);
        }

        Assert.Throws<ArgumentException>(() => new SemanticsProperties(linkUrl: new Uri("https://example.com/")));
        Assert.Throws<ArgumentException>(() =>
            new SemanticsProperties(link: false, linkUrl: new Uri("https://example.com/")));
        Assert.NotNull(new SemanticsProperties(link: true, linkUrl: new Uri("https://example.com/")).LinkUrl);
    }

    [Fact]
    public void Properties_DebugFillPropertiesEmitsTheDartList()
    {
        var properties = new SemanticsProperties(
            @checked: true,
            label: "a label",
            attributedValue: new AttributedString("a value"),
            identifier: "an identifier",
            role: SemanticsRole.Dialog,
            validationResult: SemanticsValidationResult.Invalid,
            hintOverrides: new SemanticsHintOverrides(onTapHint: "tap"));

        var builder = new DiagnosticPropertiesBuilder();
        properties.DebugFillProperties(builder);
        List<string> names = [.. builder.Properties.Select(node => node.Name!)];

        Assert.Contains("checked", names);
        Assert.Contains("label", names);
        Assert.Contains("attributedValue", names);
        Assert.Contains("identifier", names);
        Assert.Contains("role", names);
        Assert.Contains("validationResult", names);
        Assert.Contains("hintOverrides", names);
        // Dart never dumps the handlers or the layout-only flags.
        Assert.DoesNotContain("onTap", names);
        Assert.DoesNotContain("headingLevel", names);
    }

    [Fact]
    public void AttributedStringProperty_HidesAnEmptyStringUnlessAskedToShowIt()
    {
        var empty = new AttributedStringProperty("label", AttributedString.Empty);
        Assert.False(empty.IsInteresting);
        Assert.True(new AttributedStringProperty("label", AttributedString.Empty, showWhenEmpty: true).IsInteresting);

        var attributed = new AttributedStringProperty(
            "label",
            new AttributedString("text", [new SpellOutStringAttribute(new TextRange(0, 1))]));
        Assert.True(attributed.IsInteresting);
        Assert.Equal(
            "\"text\" [SpellOutStringAttribute { Range = TextRange(start: 0, end: 1) }]",
            attributed.ValueToString());
        Assert.Equal("\"plain\"", new AttributedStringProperty("l", new AttributedString("plain")).ValueToString());
        Assert.Equal("null", new AttributedStringProperty("l", null).ValueToString());
    }

    // ---- The annotation write path ------------------------------------------------------------

    [Fact]
    public void Annotations_IdentifierForcesASemanticsBoundaryEvenWithoutAContainer()
    {
        var config = new SemanticsConfiguration();
        Describe(new SemanticsProperties(identifier: "id"), config);

        Assert.True(config.IsSemanticBoundary);
        Assert.Equal("id", config.Identifier);
    }

    [Fact]
    public void Annotations_WriteEveryNewSlotThroughToTheConfiguration()
    {
        var config = new SemanticsConfiguration();
        Describe(
            new SemanticsProperties(
                link: true,
                linkUrl: new Uri("https://example.com/"),
                headingLevel: 3,
                maxValueLength: 10,
                currentValueLength: 4,
                controlsNodes: new HashSet<string> { "abc" },
                validationResult: SemanticsValidationResult.Valid,
                readOnly: true,
                obscured: true,
                multiline: true,
                keyboardKey: true,
                isRequired: true,
                hintOverrides: new SemanticsHintOverrides(onTapHint: "tap", onLongPressHint: "press")),
            config);

        Assert.True(config.IsLink);
        Assert.Equal(new Uri("https://example.com/"), config.LinkUrl);
        Assert.Equal(3, config.HeadingLevel);
        Assert.Equal(10, config.MaxValueLength);
        Assert.Equal(4, config.CurrentValueLength);
        Assert.Equal(["abc"], config.ControlsNodes!);
        Assert.Equal(SemanticsValidationResult.Valid, config.ValidationResult);
        Assert.True(config.IsReadOnly);
        Assert.True(config.IsObscured);
        Assert.True(config.IsMultiline);
        Assert.True(config.IsKeyboardKey);
        Assert.True(config.IsRequired);
        Assert.Equal("tap", config.OnTapHint);
        Assert.Equal("press", config.OnLongPressHint);
    }

    [Fact]
    public void Annotations_DropAnAllNullHintOverrides()
    {
        var config = new SemanticsConfiguration();
        Describe(new SemanticsProperties(hintOverrides: new SemanticsHintOverrides()), config);

        Assert.Null(config.HintOverrides);
    }

    [Fact]
    public void Annotations_RejectScopesRouteWithoutExplicitChildNodes()
    {
        var render = new RenderSemanticsAnnotations(
            new SemanticsProperties(scopesRoute: true),
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));

        Assert.Throws<InvalidOperationException>(() => Describe(render));
    }

    [Fact]
    public void Annotations_RejectBeingToggledAndCheckedAtOnce()
    {
        var render = new RenderSemanticsAnnotations(
            new SemanticsProperties(toggled: true, @checked: true),
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));

        Assert.Throws<InvalidOperationException>(() => Describe(render));
    }

    [Fact]
    public void Annotations_RegisterEveryTextEditingActionWithItsDecodedArgument()
    {
        bool? forwardByCharacter = null;
        bool? backwardByCharacter = null;
        bool? forwardByWord = null;
        bool? backwardByWord = null;
        TextSelection? selection = null;
        string? text = null;
        int copies = 0;
        int cuts = 0;
        int pastes = 0;

        var config = new SemanticsConfiguration();
        Describe(
            new SemanticsProperties(
                onCopy: () => copies++,
                onCut: () => cuts++,
                onPaste: () => pastes++,
                onMoveCursorForwardByCharacter: extend => forwardByCharacter = extend,
                onMoveCursorBackwardByCharacter: extend => backwardByCharacter = extend,
                onMoveCursorForwardByWord: extend => forwardByWord = extend,
                onMoveCursorBackwardByWord: extend => backwardByWord = extend,
                onSetSelection: value => selection = value,
                onSetText: value => text = value),
            config);

        config.ActionHandlers[SemanticsActions.Copy](null);
        config.ActionHandlers[SemanticsActions.Cut](null);
        config.ActionHandlers[SemanticsActions.Paste](null);
        config.ActionHandlers[SemanticsActions.MoveCursorForwardByCharacter](true);
        config.ActionHandlers[SemanticsActions.MoveCursorBackwardByCharacter](false);
        config.ActionHandlers[SemanticsActions.MoveCursorForwardByWord](true);
        config.ActionHandlers[SemanticsActions.MoveCursorBackwardByWord](false);
        config.ActionHandlers[SemanticsActions.SetSelection](
            new Dictionary<string, int> { ["base"] = 4, ["extent"] = 5 });
        config.ActionHandlers[SemanticsActions.SetText]("new text");

        Assert.Equal(1, copies);
        Assert.Equal(1, cuts);
        Assert.Equal(1, pastes);
        Assert.True(forwardByCharacter);
        Assert.False(backwardByCharacter);
        Assert.True(forwardByWord);
        Assert.False(backwardByWord);
        Assert.Equal(new TextSelection(4, 5), selection);
        Assert.Equal("new text", text);
    }

    [Fact]
    public void Annotations_ReplacingAHandlerKeepsTheRegisteredTrampoline()
    {
        int first = 0;
        int second = 0;
        var render = new RenderSemanticsAnnotations(
            new SemanticsProperties(onTap: () => first++),
            container: true,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        SemanticsConfiguration before = Describe(render);

        render.Properties = new SemanticsProperties(onTap: () => second++);
        SemanticsConfiguration after = Describe(render);

        // Dart registers `_performTap` rather than the caller's closure, so the action bit set is
        // unchanged across the swap and the trampoline dispatches to the properties in force now.
        Assert.Equal(before.Actions, after.Actions);
        before.ActionHandlers[SemanticsActions.Tap](null);
        after.ActionHandlers[SemanticsActions.Tap](null);
        Assert.Equal(0, first);
        Assert.Equal(2, second);
    }

    [Fact]
    public void Annotations_ReplacingAHandlerDoesNotDirtyTheNode()
    {
        var render = new RenderSemanticsAnnotations(
            new SemanticsProperties(label: "tappable", onTap: () => { }),
            container: true,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        var renderView = new RenderView { Child = render };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);
        pipeline.FlushLayout(ViewSize);
        pipeline.FlushSemantics();

        int updates = 0;
        pipeline.SemanticsOwner!.OnSemanticsUpdate = _ => updates++;

        render.Properties = new SemanticsProperties(label: "tappable", onTap: () => { });
        pipeline.FlushSemantics();
        Assert.Equal(0, updates);

        // Removing the handler does change the node's action set.
        render.Properties = new SemanticsProperties(label: "tappable");
        pipeline.FlushSemantics();
        Assert.Equal(1, updates);
    }

    [Fact]
    public void Annotations_BlockUserActionsKeepsOnlyTheAccessibilityFocusActions()
    {
        var config = new SemanticsConfiguration();
        Describe(
            new SemanticsProperties(
                onTap: () => { },
                onLongPress: () => { },
                onDidGainAccessibilityFocus: () => { }),
            config,
            blockUserActions: true);

        Assert.True(config.IsBlockingUserActions);
        Assert.Equal(SemanticsActions.DidGainAccessibilityFocus, config.EffectiveActions);
    }

    // ---- The widget layer ---------------------------------------------------------------------

    [Fact]
    public void Semantics_FromPropertiesReachesTheNode()
    {
        using var harness = new CupertinoThemeTestHarness(new Directionality(
            TextDirection.Ltr,
            new Semantics(
                properties: new SemanticsProperties(
                    label: "from properties",
                    identifier: "the-identifier",
                    headingLevel: 2,
                    maxValueLength: 12,
                    currentValueLength: 5,
                    controlsNodes: new HashSet<string> { "controlled" },
                    validationResult: SemanticsValidationResult.Invalid),
                container: true,
                child: new SizedBox(width: 20, height: 10))));

        SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
        SemanticsNode node = Assert.Single(root.Children);
        Assert.Equal("from properties", node.Label);
        Assert.Equal("the-identifier", node.Identifier);
        Assert.Equal(2, node.HeadingLevel);
        Assert.Equal(12, node.MaxValueLength);
        Assert.Equal(5, node.CurrentValueLength);
        Assert.Equal(["controlled"], node.ControlsNodes!);
        Assert.Equal(SemanticsValidationResult.Invalid, node.ValidationResult);
    }

    [Fact]
    public void Semantics_AttributedLabelReachesTheNodeWithItsAttributes()
    {
        using var harness = new CupertinoThemeTestHarness(new Directionality(
            TextDirection.Ltr,
            new Semantics(
                container: true,
                attributedLabel: new AttributedString(
                    "label",
                    [new SpellOutStringAttribute(new TextRange(0, 5))]),
                attributedHint: new AttributedString(
                    "hint",
                    [new LocaleStringAttribute(new TextRange(1, 2), "en-MX")]),
                child: new SizedBox(width: 20, height: 10))));

        SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
        SemanticsNode node = Assert.Single(root.Children);
        Assert.Equal("label", node.Label);
        var spellOut = Assert.IsType<SpellOutStringAttribute>(Assert.Single(node.AttributedLabel!.Attributes));
        Assert.Equal(new TextRange(0, 5), spellOut.Range);
        var locale = Assert.IsType<LocaleStringAttribute>(Assert.Single(node.AttributedHint!.Attributes));
        Assert.Equal("en-MX", locale.Locale);
    }

    [Fact]
    public void Semantics_OnTapHintAndOnLongPressHintBuildTheOverrides()
    {
        using var harness = new CupertinoThemeTestHarness(new Directionality(
            TextDirection.Ltr,
            new Semantics(
                container: true,
                onTapHint: "tap it",
                onLongPressHint: "hold it",
                onTap: () => { },
                onLongPress: () => { },
                child: new SizedBox(width: 20, height: 10))));

        SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
        SemanticsNode node = Assert.Single(root.Children);
        Assert.Equal("tap it", node.OnTapHint);
        Assert.Equal("hold it", node.OnLongPressHint);
    }

    [Fact]
    public void Semantics_InheritsTheAmbientDirectionalityOnlyWhenItCarriesText()
    {
        Assert.Equal(TextDirection.Rtl, DirectionOf(new Semantics(label: "text", child: Leaf())));
        Assert.Equal(
            TextDirection.Rtl,
            DirectionOf(new Semantics(attributedValue: new AttributedString("v"), child: Leaf())));
        Assert.Equal(TextDirection.Rtl, DirectionOf(new Semantics(tooltip: "t", child: Leaf())));
        Assert.Null(DirectionOf(new Semantics(button: true, child: Leaf())));

        // An explicit direction always wins over the ambient one.
        Assert.Equal(
            TextDirection.Ltr,
            DirectionOf(new Semantics(label: "text", textDirection: TextDirection.Ltr, child: Leaf())));

        static Widget Leaf() => new SizedBox(width: 20, height: 10);

        static TextDirection? DirectionOf(Widget semantics)
        {
            using var harness = new CupertinoThemeTestHarness(new Directionality(TextDirection.Rtl, semantics));
            harness.Pump(ViewSize);
            return Assert
                .Single(FindDescendants<RenderSemanticsAnnotations>(harness.RenderView))
                .TextDirection;
        }
    }

    [Fact]
    public void SliverSemantics_AnnotatesASliverSubtreeLikeItsBoxCounterpart()
    {
        using var harness = new CupertinoThemeTestHarness(new Directionality(
            TextDirection.Ltr,
            new CustomScrollView(
                slivers:
                [
                    new SliverSemantics(
                        properties: new SemanticsProperties(label: "sliver label", header: true),
                        container: true,
                        sliver: new SliverToBoxAdapter(new SizedBox(width: 40, height: 20)))
                ])));

        SemanticsNode root = Assert.IsType<SemanticsNode>(harness.PumpAndGetSemantics(ViewSize));
        SemanticsNode? labelled = FindNode(root, node => node.Label == "sliver label");
        Assert.NotNull(labelled);
        Assert.True(labelled!.Flags.HasFlag(SemanticsFlags.IsHeader));

        var render = Assert.Single(FindDescendants<RenderSliverSemanticsAnnotations>(harness.RenderView));
        Assert.True(render.Container);
        Assert.Equal("sliver label", render.Properties.Label);
    }

    [Fact]
    public void SliverSemantics_ExcludeSemanticsDropsTheSliverSubtree()
    {
        var render = new RenderSliverSemanticsAnnotations(
            new SemanticsProperties(label: "outer"),
            excludeSemantics: true,
            child: new RenderSliverToBoxAdapter(
                new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10)))));

        int visits = 0;
        render.VisitChildrenForSemantics(_ => visits++);
        Assert.Equal(0, visits);

        render.ExcludeSemantics = false;
        render.VisitChildrenForSemantics(_ => visits++);
        Assert.Equal(1, visits);
    }

    // ---- Helpers ------------------------------------------------------------------------------

    private static SemanticsConfiguration Describe(
        SemanticsProperties properties,
        SemanticsConfiguration config,
        bool blockUserActions = false)
    {
        var render = new RenderSemanticsAnnotations(
            properties,
            blockUserActions: blockUserActions,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        render.InvokeDescribeSemanticsConfiguration(config);
        return config;
    }

    private static SemanticsConfiguration Describe(RenderSemanticsAnnotations render)
    {
        var config = new SemanticsConfiguration();
        render.InvokeDescribeSemanticsConfiguration(config);
        return config;
    }

    private static SemanticsNode? FindNode(SemanticsNode node, Func<SemanticsNode, bool> predicate)
    {
        if (predicate(node))
        {
            return node;
        }

        foreach (SemanticsNode child in node.Children)
        {
            SemanticsNode? found = FindNode(child, predicate);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static List<T> FindDescendants<T>(RenderObject root) where T : RenderObject
    {
        var found = new List<T>();
        Visit(root);
        return found;

        void Visit(RenderObject node)
        {
            if (node is T match)
            {
                found.Add(match);
            }

            node.VisitChildren(Visit);
        }
    }
}
