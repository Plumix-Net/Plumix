using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity sources:
// - flutter/packages/flutter/lib/src/semantics/semantics.dart (SemanticsData, getSemanticsData,
//   SemanticsConfiguration.localeForSubtree)
// - flutter/packages/flutter/lib/src/rendering/object.dart (_SemanticsParentData.localeForChildren)
// - flutter/packages/flutter/lib/src/widgets/basic.dart (Semantics.localeForSubtree)
// Mirrors flutter/packages/flutter/test/semantics/semantics_test.dart,
// test/widgets/semantics_test.dart and test/widgets/localizations_test.dart.

namespace Plumix.Tests;

public sealed class SemanticsDataTests
{
    [Fact]
    public void GetSemanticsData_OnAnUnmergedNode_ReadsTheNodesOwnAnnotations()
    {
        var node = new SemanticsNode { Rect = new Rect(0, 0, 10, 10) };
        node.UpdateWith(new SemanticsConfiguration
        {
            Label = "Label",
            Value = "Value",
            TextDirection = TextDirection.Ltr,
            HeadingLevel = 3,
            Role = SemanticsRole.Tab,
            InputType = SemanticsInputType.Email,
            HitTestBehavior = SemanticsHitTestBehavior.Opaque,
            MinValue = "0",
            MaxValue = "10",
        });

        SemanticsData data = node.GetSemanticsData();
        Assert.Equal("Label", data.Label);
        Assert.Equal("Value", data.Value);
        Assert.Equal(TextDirection.Ltr, data.TextDirection);
        Assert.Equal(3, data.HeadingLevel);
        Assert.Equal(SemanticsRole.Tab, data.Role);
        Assert.Equal(SemanticsInputType.Email, data.InputType);
        Assert.Equal(SemanticsHitTestBehavior.Opaque, data.HitTestBehavior);
        Assert.Equal("0", data.MinValue);
        Assert.Equal("10", data.MaxValue);
        Assert.Equal(new Rect(0, 0, 10, 10), data.Rect);
        Assert.Empty(data.CustomSemanticsActionIds!);
    }

    [Fact]
    public void GetSemanticsData_IncludesTheNodesOwnTagsAndTheTagsOfEveryMergedDescendant()
    {
        var tag1 = new SemanticsTag("tag1");
        var tag2 = new SemanticsTag("tag2");
        var tag3 = new SemanticsTag("tag3");

        var node = new SemanticsNode { Rect = new Rect(0, 0, 10, 10) };
        node.ReplaceTags([tag1, tag2]);
        Assert.Equal([tag1, tag2], node.GetSemanticsData().Tags!.OrderBy(static tag => tag.Name));

        var child = new SemanticsNode { Rect = new Rect(0, 0, 10, 10) };
        child.ReplaceTags([tag3]);
        child.IsMergedIntoParent = true;
        node.UpdateWith(
            new SemanticsConfiguration { IsSemanticBoundary = true, IsMergingSemanticsOfDescendants = true },
            [child]);

        Assert.Equal(
            [tag1, tag2, tag3],
            node.GetSemanticsData().Tags!.OrderBy(static tag => tag.Name));
    }

    [Fact]
    public void GetSemanticsData_DefensivelyCopiesTheTagSet()
    {
        var tag = new SemanticsTag("tag");
        var node = new SemanticsNode { Rect = new Rect(0, 0, 10, 10) };
        node.ReplaceTags([tag]);

        IReadOnlySet<SemanticsTag> tags = node.GetSemanticsData().Tags!;
        node.ReplaceTags([tag, new SemanticsTag("added")]);

        Assert.Equal(1, tags.Count);
    }

    [Fact]
    public void GetSemanticsData_MergesDescendantsWithFlutterFieldRules()
    {
        SemanticsNode root = BuildMergeRoot(
            new SemanticsConfiguration
            {
                IsSemanticBoundary = true,
                IsMergingSemanticsOfDescendants = true,
                Label = "Root",
                TextDirection = TextDirection.Ltr,
            },
            new SemanticsConfiguration
            {
                Label = "One",
                Value = "value one",
                TextDirection = TextDirection.Ltr,
                Role = SemanticsRole.Row,
                HeadingLevel = 4,
                Tooltip = "tip one",
                MinValue = "1",
            },
            new SemanticsConfiguration
            {
                Label = "Two",
                Value = "value two",
                TextDirection = TextDirection.Ltr,
                Role = SemanticsRole.Cell,
                Tooltip = "tip two",
                MinValue = "2",
                MaxValue = "9",
            });

        SemanticsData data = root.GetSemanticsData();

        // Labels concatenate in pre-order with a newline; single-valued fields are first-wins.
        Assert.Equal("Root\nOne\nTwo", data.Label);
        Assert.Equal("value one", data.Value);
        Assert.Equal(SemanticsRole.Row, data.Role);
        Assert.Equal("tip one", data.Tooltip);
        Assert.Equal("1", data.MinValue);
        Assert.Equal("9", data.MaxValue);

        // The merge root is not a heading, so the first descendant level wins.
        Assert.Equal(4, data.HeadingLevel);
    }

    [Fact]
    public void GetSemanticsData_KeepsTheMergeRootsHeadingLevelWhenItHasOne()
    {
        SemanticsNode root = BuildMergeRoot(
            new SemanticsConfiguration
            {
                IsSemanticBoundary = true,
                IsMergingSemanticsOfDescendants = true,
                HeadingLevel = 2,
                TextDirection = TextDirection.Ltr,
            },
            new SemanticsConfiguration { HeadingLevel = 5, TextDirection = TextDirection.Ltr });

        Assert.Equal(2, root.GetSemanticsData().HeadingLevel);
    }

    [Theory]
    [InlineData(SemanticsValidationResult.None, SemanticsValidationResult.None, SemanticsValidationResult.None)]
    [InlineData(SemanticsValidationResult.None, SemanticsValidationResult.Valid, SemanticsValidationResult.Valid)]
    [InlineData(
        SemanticsValidationResult.None,
        SemanticsValidationResult.Invalid,
        SemanticsValidationResult.Invalid)]
    [InlineData(SemanticsValidationResult.Valid, SemanticsValidationResult.None, SemanticsValidationResult.Valid)]
    [InlineData(SemanticsValidationResult.Valid, SemanticsValidationResult.Valid, SemanticsValidationResult.Valid)]
    [InlineData(
        SemanticsValidationResult.Valid,
        SemanticsValidationResult.Invalid,
        SemanticsValidationResult.Invalid)]
    [InlineData(
        SemanticsValidationResult.Invalid,
        SemanticsValidationResult.None,
        SemanticsValidationResult.Invalid)]
    [InlineData(
        SemanticsValidationResult.Invalid,
        SemanticsValidationResult.Valid,
        SemanticsValidationResult.Invalid)]
    [InlineData(
        SemanticsValidationResult.Invalid,
        SemanticsValidationResult.Invalid,
        SemanticsValidationResult.Invalid)]
    public void GetSemanticsData_LetsAnInvalidDescendantWinTheValidationResult(
        SemanticsValidationResult outer,
        SemanticsValidationResult inner,
        SemanticsValidationResult expected)
    {
        SemanticsNode root = BuildMergeRoot(
            new SemanticsConfiguration
            {
                IsSemanticBoundary = true,
                IsMergingSemanticsOfDescendants = true,
                ValidationResult = outer,
            },
            new SemanticsConfiguration { ValidationResult = inner });

        Assert.Equal(expected, root.GetSemanticsData().ValidationResult);
    }

    [Fact]
    public void GetSemanticsData_UnionsTheControlsNodesOfEveryMergedDescendant()
    {
        SemanticsNode root = BuildMergeRoot(
            new SemanticsConfiguration
            {
                IsSemanticBoundary = true,
                IsMergingSemanticsOfDescendants = true,
            },
            new SemanticsConfiguration { ControlsNodes = new HashSet<string> { "a" } },
            new SemanticsConfiguration { ControlsNodes = new HashSet<string> { "b" } });

        Assert.Equal(["a", "b"], root.GetSemanticsData().ControlsNodes!.OrderBy(static id => id));
    }

    [Fact]
    public void GetSemanticsData_WrapsADescendantOfTheOppositeDirectionInABidiEmbedding()
    {
        SemanticsNode root = BuildMergeRoot(
            new SemanticsConfiguration
            {
                IsSemanticBoundary = true,
                IsMergingSemanticsOfDescendants = true,
                Label = "left",
                TextDirection = TextDirection.Ltr,
            },
            new SemanticsConfiguration { Label = "right", TextDirection = TextDirection.Rtl });

        string expected = "left\n"
                          + UnicodeMarks.RightToLeftEmbedding
                          + "right"
                          + UnicodeMarks.PopDirectionalFormatting;
        Assert.Equal(expected, root.GetSemanticsData().Label);
    }

    [Fact]
    public void GetSemanticsData_SortsTheCustomActionIdsAndIncludesTheHintOverrides()
    {
        CustomSemanticsAction.ResetForTests();
        var first = new CustomSemanticsAction("first");
        var second = new CustomSemanticsAction("second");

        var rootConfig = new SemanticsConfiguration
        {
            IsSemanticBoundary = true,
            IsMergingSemanticsOfDescendants = true,
            HintOverrides = new SemanticsHintOverrides(onTapHint: "tap hint"),
        };
        rootConfig.AddCustomActionHandler(second, static () => { });

        var childConfig = new SemanticsConfiguration();
        childConfig.AddCustomActionHandler(first, static () => { });

        SemanticsNode root = BuildMergeRoot(rootConfig, childConfig);

        int[] expected =
        [
            CustomSemanticsAction.GetIdentifier(second),
            CustomSemanticsAction.GetIdentifier(
                CustomSemanticsAction.OverridingAction("tap hint", SemanticsActions.Tap)),
            CustomSemanticsAction.GetIdentifier(first),
        ];
        Array.Sort(expected);

        Assert.Equal(expected, root.GetSemanticsData().CustomSemanticsActionIds!);
    }

    [Fact]
    public void GetSemanticsData_BlockedNodeOnlyKeepsTheAccessibilityFocusActions()
    {
        var config = new SemanticsConfiguration { IsBlockingUserActions = true };
        config.AddActionHandler(SemanticsActions.ScrollUp, static () => { });
        config.AddActionHandler(SemanticsActions.LongPress, static () => { });
        config.AddActionHandler(SemanticsActions.ShowOnScreen, static () => { });
        config.AddActionHandler(SemanticsActions.DidGainAccessibilityFocus, static () => { });

        var node = new SemanticsNode { Rect = new Rect(0, 0, 10, 10) };
        node.UpdateWith(config);

        SemanticsData data = node.GetSemanticsData();
        Assert.Equal(SemanticsActions.DidGainAccessibilityFocus, data.Actions);
        Assert.True(data.HasAction(SemanticsActions.DidGainAccessibilityFocus));
        Assert.False(data.HasAction(SemanticsActions.LongPress));
    }

    [Fact]
    public void GetSemanticsData_DropsOnlyTheBlockedDescendantsActions()
    {
        var blockedChild = new SemanticsConfiguration
        {
            Label = "label1",
            TextDirection = TextDirection.Ltr,
            IsBlockingUserActions = true,
        };
        blockedChild.AddActionHandler(SemanticsActions.Tap, static () => { });

        var openChild = new SemanticsConfiguration { Label = "label2", TextDirection = TextDirection.Ltr };
        openChild.AddActionHandler(SemanticsActions.LongPress, static () => { });

        SemanticsNode root = BuildMergeRoot(
            new SemanticsConfiguration
            {
                IsSemanticBoundary = true,
                IsMergingSemanticsOfDescendants = true,
                TextDirection = TextDirection.Ltr,
            },
            blockedChild,
            openChild);

        SemanticsData data = root.GetSemanticsData();
        Assert.Equal("label1\nlabel2", data.Label);
        Assert.True(data.HasAction(SemanticsActions.LongPress));
        Assert.False(data.HasAction(SemanticsActions.Tap));
    }

    [Fact]
    public void GetSemanticsData_IsRecomputedOnEveryCall()
    {
        var node = new SemanticsNode { Rect = new Rect(0, 0, 10, 10) };
        node.UpdateWith(new SemanticsConfiguration { Label = "before", TextDirection = TextDirection.Ltr });
        Assert.Equal("before", node.GetSemanticsData().Label);

        node.UpdateWith(new SemanticsConfiguration { Label = "after", TextDirection = TextDirection.Ltr });
        Assert.Equal("after", node.GetSemanticsData().Label);
    }

    [Fact]
    public void SemanticsData_EqualityIgnoresTheLocaleAndComparesTheActionIdsInOrder()
    {
        SemanticsData WithLocale(Locale? locale, int[] ids) => new(
            flags: SemanticsFlags.None,
            actions: SemanticsActions.None,
            identifier: string.Empty,
            traversalParentIdentifier: null,
            traversalChildIdentifier: null,
            attributedLabel: AttributedString.Empty,
            attributedValue: AttributedString.Empty,
            attributedIncreasedValue: AttributedString.Empty,
            attributedDecreasedValue: AttributedString.Empty,
            attributedHint: AttributedString.Empty,
            tooltip: string.Empty,
            textDirection: null,
            rect: new Rect(0, 0, 10, 10),
            textSelection: null,
            scrollIndex: null,
            scrollChildCount: null,
            scrollPosition: null,
            scrollExtentMax: null,
            scrollExtentMin: null,
            platformViewId: null,
            maxValueLength: null,
            currentValueLength: null,
            headingLevel: 0,
            linkUrl: null,
            role: SemanticsRole.None,
            controlsNodes: null,
            validationResult: SemanticsValidationResult.None,
            hitTestBehavior: SemanticsHitTestBehavior.Defer,
            inputType: SemanticsInputType.None,
            locale: locale,
            minValue: null,
            maxValue: null,
            customSemanticsActionIds: ids);

        SemanticsData first = WithLocale(new Locale("en"), [1, 2]);
        SemanticsData second = WithLocale(new Locale("fr"), [1, 2]);
        SemanticsData reordered = WithLocale(new Locale("en"), [2, 1]);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, reordered);
    }

    [Fact]
    public void SemanticsData_ToStringShowsOnlyTheAnnotatedProperties()
    {
        var node = new SemanticsNode
        {
            Rect = new Rect(50, 10, 20, 30),
            Transform = Matrix4.TranslationValues(10, 10, 0),
        };
        node.UpdateWith(new SemanticsConfiguration
        {
            Label = "Use all the properties",
            TextDirection = TextDirection.Rtl,
            IsChecked = false,
            IsSelected = true,
            IsButton = true,
            SortKey = new OrdinalSortKey(1.0),
        });

        string description = node.GetSemanticsData().ToString();
        Assert.StartsWith("SemanticsData(", description);
        Assert.Contains("label: \"Use all the properties\"", description);
        Assert.Contains("textDirection: rtl", description);
        Assert.Contains("flags:", description);

        // `sortKey` lives on the node, never on the flattened data.
        Assert.DoesNotContain("sortKey", description);

        var empty = new SemanticsNode { Rect = new Rect(50, 10, 20, 30) };
        string emptyDescription = empty.GetSemanticsData().ToString();
        Assert.DoesNotContain("label", emptyDescription);
        Assert.DoesNotContain("headingLevel", emptyDescription);
    }

    [Fact]
    public void SemanticsUpdate_CarriesTheFlattenedDataOfEveryNodeItSends()
    {
        var leaf = new RenderSemanticsAnnotations(
            new SemanticsProperties(label: "leaf", identifier: "leaf-id"),
            container: true,
            textDirection: TextDirection.Ltr,
            child: new RenderConstrainedBox(BoxConstraints.Tight(new Size(20, 10))));
        var renderView = new RenderView(new FlutterView(new Size(800, 600))) { Child = leaf };
        var pipeline = new PipelineOwner(renderView);
        pipeline.Attach(renderView);

        SemanticsUpdate? update = null;
        pipeline.SemanticsOwner!.OnSemanticsUpdate = produced => update = produced;
        pipeline.FlushLayout(new Size(320, 120));
        pipeline.FlushSemantics();

        Assert.NotNull(update);
        SemanticsNodeUpdate node = Assert.Single(
            update!.Nodes,
            candidate => candidate.Data.Label == "leaf");
        Assert.Equal("leaf-id", node.Data.Identifier);
        Assert.Equal(node.Node.Id, node.Id);
        Assert.Same(node.Data.CustomSemanticsActionIds, node.AdditionalActions);
    }

    /// <remarks>
    /// Flutter's `MergeSemantics merges CustomSemanticsActions from its children`: the merge root
    /// reports the labels and the custom actions of every node merged into it.
    /// </remarks>
    [Fact]
    public void MergeSemantics_FlattensTheLabelsAndCustomActionsOfItsChildren()
    {
        CustomSemanticsAction.ResetForTests();
        var action1 = new CustomSemanticsAction("action1");
        var action2 = new CustomSemanticsAction("action2");

        using var harness = new SemanticsHarness(new MergeSemantics(
            child: new Column(
                children:
                [
                    new Semantics(
                        label: "first",
                        textDirection: TextDirection.Ltr,
                        container: true,
                        customSemanticsActions: new Dictionary<CustomSemanticsAction, Action>
                        {
                            [action1] = static () => { },
                        },
                        child: new SizedBox(width: 10, height: 10)),
                    new Semantics(
                        label: "second",
                        textDirection: TextDirection.Ltr,
                        container: true,
                        customSemanticsActions: new Dictionary<CustomSemanticsAction, Action>
                        {
                            [action2] = static () => { },
                        },
                        child: new SizedBox(width: 10, height: 10)),
                ])));

        SemanticsNode root = harness.PumpAndGetSemantics(new Size(320, 120));
        SemanticsNode merged = Assert.Single(root.Children);
        SemanticsData data = merged.GetSemanticsData();

        Assert.Equal("first\nsecond", data.Label);
        int[] expected =
        [
            CustomSemanticsAction.GetIdentifier(action1),
            CustomSemanticsAction.GetIdentifier(action2),
        ];
        Array.Sort(expected);
        Assert.Equal(expected, data.CustomSemanticsActionIds!);
    }

    [Fact]
    public void SemanticsConfiguration_LocaleForSubtreeAnnotatesAndBlocksMerging()
    {
        var configuration = new SemanticsConfiguration();
        Assert.Null(configuration.LocaleForSubtree);

        configuration.LocaleForSubtree = new Locale("AB", "CD");
        Assert.True(configuration.HasBeenAnnotated);
        Assert.Throws<ArgumentNullException>(() => configuration.LocaleForSubtree = null);

        var other = new SemanticsConfiguration { LocaleForSubtree = new Locale("DE", "FG") };
        Assert.False(configuration.IsCompatibleWith(other));
        Assert.False(other.IsCompatibleWith(configuration));

        var same = new SemanticsConfiguration { LocaleForSubtree = new Locale("AB", "CD") };
        Assert.True(configuration.IsCompatibleWith(same));

        // `Clone` keeps both locales, and `Absorb` never adopts the child's.
        Assert.Equal(new Locale("AB", "CD"), configuration.Clone().LocaleForSubtree);
        configuration.Absorb(new SemanticsConfiguration { LocaleForSubtree = new Locale("ZZ") });
        Assert.Equal(new Locale("AB", "CD"), configuration.LocaleForSubtree);
    }

    [Fact]
    public void SemanticsConfiguration_LocaleDoesNotAnnotate()
    {
        var configuration = new SemanticsConfiguration { Locale = new Locale("fo") };
        Assert.False(configuration.HasBeenAnnotated);
        Assert.Equal(new Locale("fo"), configuration.Clone().Locale);
    }

    [Fact]
    public void LocaleForSubtree_KeepsTwoNestedSubtreesApartAndGivesEachItsOwnLocale()
    {
        using var harness = new SemanticsHarness(
            new Semantics(
                localeForSubtree: new Locale("AB", "CD"),
                child: new Semantics(
                    localeForSubtree: new Locale("DE", "FG"),
                    child: new SizedBox(width: 10, height: 10))));

        SemanticsNode root = harness.PumpAndGetSemantics(new Size(320, 120));
        SemanticsNode outer = Assert.Single(root.Children);
        SemanticsNode inner = Assert.Single(outer.Children);

        Assert.Equal(new Locale("AB", "CD"), outer.GetSemanticsData().Locale);
        Assert.Equal(new Locale("DE", "FG"), inner.GetSemanticsData().Locale);
    }

    [Fact]
    public void LocaleForSubtree_IsInheritedByEveryDescendantNodeThatDoesNotOverrideIt()
    {
        using var harness = new SemanticsHarness(
            new Semantics(
                localeForSubtree: new Locale("fo"),
                container: true,
                explicitChildNodes: true,
                child: new Semantics(
                    label: "inherited",
                    textDirection: TextDirection.Ltr,
                    container: true,
                    child: new SizedBox(width: 10, height: 10))));

        SemanticsNode root = harness.PumpAndGetSemantics(new Size(320, 120));
        SemanticsNode outer = Assert.Single(root.Children);
        SemanticsNode inner = Assert.Single(outer.Children);

        Assert.Equal(new Locale("fo"), outer.GetSemanticsData().Locale);
        Assert.Equal(new Locale("fo"), inner.GetSemanticsData().Locale);
    }

    [Fact]
    public void SemanticsNode_ReportsNoLocaleWhenNoAncestorNamesOne()
    {
        using var harness = new SemanticsHarness(
            new Semantics(
                label: "plain",
                textDirection: TextDirection.Ltr,
                container: true,
                child: new SizedBox(width: 10, height: 10)));

        SemanticsNode root = harness.PumpAndGetSemantics(new Size(320, 120));
        Assert.Null(Assert.Single(root.Children).GetSemanticsData().Locale);
    }

    /// <remarks>
    /// Flutter's `Localizations` marks its subtree with the locale unless it is the application-level
    /// one, which the engine already knows about.
    /// </remarks>
    [Fact]
    public void Localizations_MarksItsSubtreeUnlessItIsApplicationLevel()
    {
        using var scoped = new SemanticsHarness(new Localizations(
            locale: new Locale("fo"),
            delegates: [DefaultWidgetsLocalizations.Delegate],
            child: new Semantics(
                label: "scoped",
                textDirection: TextDirection.Ltr,
                container: true,
                child: new SizedBox(width: 10, height: 10))));
        SemanticsNode scopedRoot = scoped.PumpAndGetSemantics(new Size(320, 120));
        Assert.Equal(
            new Locale("fo"),
            FindLabelled(scopedRoot, "scoped").GetSemanticsData().Locale);

        using var application = new SemanticsHarness(new Localizations(
            locale: new Locale("fo"),
            delegates: [DefaultWidgetsLocalizations.Delegate],
            isApplicationLevel: true,
            child: new Semantics(
                label: "application",
                textDirection: TextDirection.Ltr,
                container: true,
                child: new SizedBox(width: 10, height: 10))));
        SemanticsNode applicationRoot = application.PumpAndGetSemantics(new Size(320, 120));
        Assert.Null(FindLabelled(applicationRoot, "application").GetSemanticsData().Locale);
    }

    private static SemanticsNode FindLabelled(SemanticsNode root, string label)
    {
        return Find(root)
               ?? throw new InvalidOperationException($"No semantics node labelled \"{label}\".");

        SemanticsNode? Find(SemanticsNode node)
        {
            if (node.Label == label)
            {
                return node;
            }

            foreach (SemanticsNode child in node.Children)
            {
                if (Find(child) is { } match)
                {
                    return match;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Builds a merge root over <paramref name="children"/>, the way a `MergeSemantics` subtree
    /// reaches <c>getSemanticsData</c>: every child forms a node and is marked merged.
    /// </summary>
    private static SemanticsNode BuildMergeRoot(
        SemanticsConfiguration rootConfig,
        params SemanticsConfiguration[] children)
    {
        var childNodes = new List<SemanticsNode>(children.Length);
        foreach (SemanticsConfiguration childConfig in children)
        {
            var child = new SemanticsNode { Rect = new Rect(0, 0, 10, 10) };
            child.UpdateWith(childConfig);
            child.IsMergedIntoParent = true;
            childNodes.Add(child);
        }

        var root = new SemanticsNode { Rect = new Rect(0, 0, 10, 10) };
        root.UpdateWith(rootConfig, childNodes);
        return root;
    }

    private sealed class SemanticsHarness : IDisposable
    {
        private readonly BuildOwner _owner = new();
        private readonly HarnessRootElement _rootElement;
        private readonly PipelineOwner _pipeline;
        private readonly RenderView _renderView;

        public SemanticsHarness(Widget rootWidget)
        {
            _renderView = new RenderView(new FlutterView(new Size(800, 600)));
            _pipeline = new PipelineOwner(_renderView);
            _pipeline.Attach(_renderView);
            _rootElement = new HarnessRootElement(_renderView, rootWidget);
            _rootElement.Attach(_owner);
            _owner.BuildScope(_rootElement, () => _rootElement.Mount(parent: null, newSlot: null));
            _owner.FlushBuild();
        }

        public SemanticsNode PumpAndGetSemantics(Size size)
        {
            _owner.FlushBuild();
            _pipeline.RequestLayout();
            _pipeline.FlushLayout(size);
            _pipeline.FlushCompositingBits();
            _pipeline.FlushPaint();
            _pipeline.RequestSemanticsUpdate();
            _pipeline.FlushSemantics();
            SemanticsNode? root = _pipeline.SemanticsOwner!.RootNode;
            Assert.NotNull(root);
            return root!;
        }

        public void Dispose() => _rootElement.UnmountRoot();

        private sealed class HarnessRootElement(RenderView renderView, Widget widget)
            : Element(widget), IRenderObjectHost
        {
            private Element? _child;

            public override RenderObject? RenderObject => _child?.RenderObject;

            public override Element? RenderObjectAttachingChild => _child;

            protected override void OnMount()
            {
                base.OnMount();
                Rebuild();
            }

            protected override void PerformRebuild()
            {
                base.PerformRebuild();
                _child = UpdateChild(_child, Widget, Slot);
            }

            public override void ForgetChild(Element child)
            {
                if (ReferenceEquals(_child, child))
                {
                    _child = null;
                }
            }

            public override void VisitChildren(Action<Element> visitor)
            {
                if (_child != null)
                {
                    visitor(_child);
                }
            }

            public void InsertRenderObjectChild(RenderObject child, object? slot) =>
                renderView.Child = (RenderBox)child;

            public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
            {
            }

            public void RemoveRenderObjectChild(RenderObject child, object? slot)
            {
                if (ReferenceEquals(renderView.Child, child))
                {
                    renderView.Child = null;
                }
            }
        }
    }
}
