using Avalonia;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: flutter/packages/flutter/lib/src/semantics/semantics.dart (SemanticsConfiguration)

namespace Plumix.Tests;

public sealed class SemanticsAnnotationHistoryTests
{
    public static IEnumerable<object[]> DefaultAssignments()
    {
        foreach (string property in new[]
                 {
                     "IsMergingSemanticsOfDescendants", "Label", "Hint", "Tooltip", "Value", "IncreasedValue",
                     "DecreasedValue", "AttributedLabel", "AttributedHint", "AttributedValue",
                     "AttributedIncreasedValue", "AttributedDecreasedValue", "Identifier", "TextDirection",
                     "MinValue", "MaxValue", "ControlsNodes", "ValidationResult", "TextSelection", "Role", "InputType",
                     "HitTestBehavior", "IndexInParent", "SortKey", "ScrollPosition", "ScrollExtentMin",
                     "ScrollExtentMax",
                     "IsHidden", "IsSlider", "IsButton", "IsLink", "IsHeader", "IsTextField", "IsReadOnly",
                     "IsObscured",
                     "IsMultiline", "IsKeyboardKey", "IsImage", "IsInMutuallyExclusiveGroup", "ScopesRoute",
                     "NamesRoute",
                     "LiveRegion", "IsSelected", "IsExpanded", "IsToggled", "IsRequired", "IsChecked",
                     "IsCheckStateMixed",
                     "IsEnabled", "IsFocusable", "IsFocused", "HasImplicitScrolling", "AccessibilityFocusBlockType",
                 })
        {
            yield return [property];
        }
    }

    [Theory]
    [MemberData(nameof(DefaultAssignments))]
    public void ExplicitDefaultSemanticAssignmentsRemainAnnotationsThroughCloneAndAbsorb(string name)
    {
        var config = new SemanticsConfiguration { IsSemanticBoundary = true };
        var property = typeof(SemanticsConfiguration).GetProperty(name)!;
        object? value = property.GetValue(config);
        Assert.False(config.HasBeenAnnotated);
        property.SetValue(config, value);
        Assert.True(config.HasBeenAnnotated);
        Assert.True(config.Clone().HasBeenAnnotated);
        var parent = new SemanticsConfiguration();
        parent.Absorb(config);
        Assert.True(parent.HasBeenAnnotated);
    }

    [Theory]
    [InlineData("HeadingLevel", 1)]
    [InlineData("MaxValueLength", 3)]
    [InlineData("CurrentValueLength", 2)]
    [InlineData("PlatformViewId", 7)]
    [InlineData("ScrollChildCount", 4)]
    [InlineData("ScrollIndex", 1)]
    public void EqualityGuardedNumericFieldsAnnotateOnlyOnChangeAndRememberClearing(string name, int value)
    {
        var config = new SemanticsConfiguration();
        var property = typeof(SemanticsConfiguration).GetProperty(name)!;
        object? initial = property.GetValue(config);
        property.SetValue(config, initial);
        Assert.False(config.HasBeenAnnotated);
        Assert.False(config.Clone().HasBeenAnnotated);
        property.SetValue(config, value);
        Assert.True(config.HasBeenAnnotated);
        property.SetValue(config, initial);
        Assert.True(config.HasBeenAnnotated);
        Assert.True(config.Clone().HasBeenAnnotated);
    }

    [Theory]
    [InlineData("TraversalParentIdentifier")]
    [InlineData("TraversalChildIdentifier")]
    [InlineData("LinkUrl")]
    public void EqualityGuardedReferenceFieldsRememberClearing(string name)
    {
        var config = new SemanticsConfiguration();
        var property = typeof(SemanticsConfiguration).GetProperty(name)!;
        property.SetValue(config, null);
        Assert.False(config.HasBeenAnnotated);
        property.SetValue(config, name == "LinkUrl" ? new Uri("https://flutter.dev") : "id");
        property.SetValue(config, null);
        Assert.True(config.HasBeenAnnotated);
        Assert.True(config.Clone().HasBeenAnnotated);
    }

    [Fact]
    public void StructuralSettingsTagsAndInheritedLocaleDoNotAnnotateAnEmptyConfiguration()
    {
        var config = new SemanticsConfiguration
        {
            IsSemanticBoundary = true,
            ExplicitChildNodes = true,
            IsBlockingSemanticsOfPreviouslyPaintedNodes = true,
            IsBlockingUserActions = true,
            Locale = new Locale("en"),
            ChildConfigurationsDelegate = children => new ChildSemanticsConfigurationsResult(children, []),
        };
        config.AddTagForChildren(new SemanticsTag("tag"));
        config.HintOverrides = null;
        Assert.False(config.HasBeenAnnotated);
        Assert.False(config.Clone().HasBeenAnnotated);
        var parent = new SemanticsConfiguration();
        parent.Absorb(config);
        Assert.False(parent.HasBeenAnnotated);
    }

    [Fact]
    public void HintOverridesAndSubtreeLocaleAnnotate()
    {
        var config = new SemanticsConfiguration { HintOverrides = new SemanticsHintOverrides() };
        Assert.True(config.HasBeenAnnotated);
        config.HintOverrides = null;
        Assert.True(config.Clone().HasBeenAnnotated);
        Assert.True(new SemanticsConfiguration { LocaleForSubtree = new Locale("en") }.HasBeenAnnotated);
    }

    [Fact]
    public void ClearingActionMapsAndRawBitsDoesNotEraseAnnotationHistory()
    {
        var config = new SemanticsConfiguration();
        config.OnTap = static () => { };
        config.ClearActionHandlers();
        config.Flags = SemanticsFlags.None;
        Assert.Equal(SemanticsActions.None, config.Actions);
        Assert.True(config.HasBeenAnnotated);
        Assert.True(config.Clone().HasBeenAnnotated);

        config = new SemanticsConfiguration();
        config.AddCustomActionHandler(new CustomSemanticsAction("custom"), static () => { });
        config.ClearActionHandlers();
        Assert.True(config.HasBeenAnnotated);
    }

    [Fact]
    public void AssigningAnEmptyCustomActionMapAnnotatesAndItsHandlerReadsTheCurrentMap()
    {
        var config = new SemanticsConfiguration();
        var action = new CustomSemanticsAction("custom");
        int calls = 0;
        config.CustomSemanticsActions[action] = () => calls++;
        Assert.False(config.HasBeenAnnotated);
        Assert.Equal(SemanticsActions.None, config.Actions);
        config.CustomSemanticsActions = [];
        Assert.True(config.HasBeenAnnotated);
        Assert.True(config.Actions.HasFlag(SemanticsActions.CustomAction));
        config.CustomSemanticsActions[action] = () => calls++;
        config.ActionHandlers[SemanticsActions.CustomAction](CustomSemanticsAction.GetIdentifier(action));
        Assert.Equal(1, calls);
        config.ActionHandlers[SemanticsActions.CustomAction](int.MaxValue);
        Assert.Equal(1, calls);
        config.CustomSemanticsActions = [];
        config.ActionHandlers[SemanticsActions.CustomAction](CustomSemanticsAction.GetIdentifier(action));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void CheckedStateAssignmentsPreserveHistoryAndReplaceMixedState()
    {
        var config = new SemanticsConfiguration();
        Assert.Null(config.IsChecked);
        Assert.Null(config.IsCheckStateMixed);
        config.IsCheckStateMixed = false;
        Assert.True(config.HasBeenAnnotated);
        Assert.Null(config.IsChecked);
        config.IsCheckStateMixed = true;
        config.IsChecked = false;
        Assert.False(config.IsChecked);
        Assert.False(config.IsCheckStateMixed);
        config.IsCheckStateMixed = true;
        config.IsChecked = null;
        config.IsCheckStateMixed = null;
        Assert.True(config.IsCheckStateMixed);
    }

    [Fact]
    public void CloningAnUntouchedConfigurationDoesNotCallSemanticSetters()
    {
        var config = new SemanticsConfiguration();
        for (int i = 0; i < 3; i++)
        {
            config = config.Clone();
            Assert.False(config.HasBeenAnnotated);
            Assert.True(config.IsCompatibleWith(new SemanticsConfiguration { Role = SemanticsRole.Region }));
        }
    }

    [Fact]
    public void ExplicitFalseAnnotationSurvivesRenderObjectCompilation()
    {
        var annotation = new RenderSemanticsAnnotations(
            new SemanticsProperties(button: false),
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(30, 20))));
        var view = new RenderView(new FlutterView(new Size(100, 100))) { Child = annotation };
        var pipeline = new PipelineOwner(view);
        pipeline.Attach(view);
        pipeline.FlushLayout(new Size(100, 100));
        pipeline.FlushSemantics();
        Assert.NotNull(annotation.SemanticsNode);
        Assert.False(annotation.SemanticsNode.Flags.HasFlag(SemanticsFlags.IsButton));
    }
}
