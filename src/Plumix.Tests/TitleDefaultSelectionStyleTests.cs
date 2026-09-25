using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

// Dart parity sources:
// flutter/packages/flutter/lib/src/widgets/title.dart
// flutter/packages/flutter/lib/src/widgets/default_selection_style.dart
// flutter/packages/flutter/lib/src/widgets/inherited_theme.dart

public sealed class TitleDefaultSelectionStyleTests : IDisposable
{
    public TitleDefaultSelectionStyleTests()
    {
        SystemChrome.ResetForTests();
    }

    public void Dispose()
    {
        SystemChrome.ResetForTests();
    }

    // title_test.dart: 'toString control test', 'should handle having no title'.
    [Fact]
    public void Title_ToStringAndDefaultTitle()
    {
        var widget = new Title(color: new Color(0xFF00FF00), title: "Awesome app", child: new SizedBox());
        _ = widget.ToString();

        var untitled = new Title(color: new Color(0xFF00FF00), child: new SizedBox());
        _ = untitled.ToString();
        Assert.Equal(string.Empty, untitled.TitleText);
        Assert.Equal(new Color(0xFF00FF00), untitled.Color);
    }

    // title_test.dart: 'should not allow non-opaque color'.
    [DebugOnlyFact]
    public void Title_RejectsNonOpaqueColor()
    {
        Assert.Throws<AssertionError>(() => new Title(color: new Color(0x00000000), child: new SizedBox()));
    }

    // title_test.dart: 'should not pass "null" to setApplicationSwitcherDescription'.
    [Fact]
    public void Title_SendsAnEmptyLabelRatherThanNull()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);
        using var tester = new FrameworkDartTester();

        tester.PumpWidget(new Title(color: new Color(0xFF00FF00), child: new SizedBox()));

        Assert.Equal(["SystemChrome.setApplicationSwitcherDescription"], platform.Methods);
        platform.AssertLastApplicationSwitcherDescription(string.Empty, 4278255360);
    }

    // title_test.dart: 'should call setApplicationSwitcherDescription once when widget is rebuilt
    // with same values'.
    [Fact]
    public void Title_RebuiltWithSameValues_SendsOnce()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);
        using var tester = new FrameworkDartTester();
        var title = new Title(color: new Color(0xFF00FF00), child: new SizedBox());

        tester.PumpWidget(title);
        tester.PumpWidget(title);
        tester.PumpWidget(title);

        Assert.Single(platform.Log);
        platform.AssertLastApplicationSwitcherDescription(string.Empty, 4278255360);
    }

    // title_test.dart: 'should call setApplicationSwitcherDescription again only when title or color
    // changes'.
    [Fact]
    public void Title_SendsAgainOnlyWhenTitleOrColorChanges()
    {
        using var platform = new MockMethodCallHandler(SystemChannels.Platform);
        using var tester = new FrameworkDartTester();
        var title = new Title(title: "title", color: new Color(0xFF00FF00), child: new SizedBox());
        var title2 = new Title(title: "title2", color: new Color(0xFF00FF02), child: new SizedBox());

        tester.PumpWidget(title);
        tester.PumpWidget(title);
        tester.PumpWidget(title2);
        tester.PumpWidget(title2);

        Assert.Equal(2, platform.Log.Count);
        var first = (System.Collections.IDictionary)platform.Log[0].Arguments!;
        Assert.Equal("title", first["label"]);
        Assert.Equal(4278255360L, first["primaryColor"]);
        platform.AssertLastApplicationSwitcherDescription("title2", 4278255362);
    }

    // title_test.dart: 'Title does not crash at zero area'.
    [Fact]
    public void Title_DoesNotCrashAtZeroArea()
    {
        using var tester = new FrameworkDartTester();

        tester.PumpWidget(new Directionality(
            textDirection: TextDirection.Ltr,
            child: new Center(
                child: SizedBox.Shrink(
                    child: new Title(color: new Color(0xFFFFFFFF), child: new Placeholder())))));

        var box = (RenderBox)tester.ElementOfType<Title>().RenderObject!;
        Assert.Equal(new Avalonia.Size(0, 0), box.Size);
    }

    [Fact]
    public void DefaultSelectionStyle_FallbackMergeAndNotificationMatchSourceContract()
    {
        DefaultSelectionStyle? resolved = null;
        var inheritedCursor = Colors.Crimson;
        var localSelection = Colors.CornflowerBlue;
        MouseCursor inheritedMouseCursor = SystemMouseCursors.Click;
        var owner = TestBuildOwner.Create();
        var root = new TestRootElement(new DefaultSelectionStyle(
            cursorColor: inheritedCursor,
            selectionColor: Colors.DarkGreen,
            mouseCursor: inheritedMouseCursor,
            child: DefaultSelectionStyle.Merge(
                selectionColor: localSelection,
                child: new Builder(context =>
                {
                    resolved = DefaultSelectionStyle.Of(context);
                    return new SizedBox();
                }))));
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        Assert.NotNull(resolved);
        Assert.Equal(inheritedCursor, resolved.CursorColor);
        Assert.Equal(localSelection, resolved.SelectionColor);
        Assert.Equal(inheritedMouseCursor, resolved.MouseCursor);
        Assert.Equal(Color.FromARGB(0x80, 0x80, 0x80, 0x80), DefaultSelectionStyle.DefaultColor);

        DefaultSelectionStyle? fallback = null;
        root.Update(new Builder(context =>
        {
            fallback = DefaultSelectionStyle.Of(context);
            return new SizedBox();
        }));
        owner.FlushBuild();

        Assert.NotNull(fallback);
        Assert.Null(fallback.CursorColor);
        Assert.Null(fallback.SelectionColor);
        Assert.Null(fallback.MouseCursor);
        root.UnmountRoot();
    }

    [Fact]
    public void InheritedTheme_CaptureFreezesNearestThemeOfEachType()
    {
        int? resolvedThemeValue = null;
        Color? resolvedSelectionColor = null;
        var owner = TestBuildOwner.Create();
        var root = new TestRootElement(new TestTheme(
            value: 1,
            child: new DefaultSelectionStyle(
                selectionColor: Colors.Crimson,
                child: new CaptureAndOverride(
                    onResolved: (theme, selection) =>
                    {
                        resolvedThemeValue = theme;
                        resolvedSelectionColor = selection;
                    }))));
        root.Attach(owner);
        owner.BuildScope(root, () => root.Mount(parent: null, newSlot: null));
        owner.FlushBuild();

        Assert.Equal(1, resolvedThemeValue);
        Assert.Equal(Colors.Crimson, resolvedSelectionColor);
        root.UnmountRoot();
    }

    private sealed class CaptureAndOverride : StatelessWidget
    {
        private readonly Action<int, Color?> _onResolved;

        public CaptureAndOverride(Action<int, Color?> onResolved)
        {
            _onResolved = onResolved;
        }

        public override Widget Build(BuildContext context)
        {
            CapturedThemes capturedThemes = InheritedTheme.Capture(context);
            return new TestTheme(
                value: 2,
                child: new DefaultSelectionStyle(
                    selectionColor: Colors.CornflowerBlue,
                    child: capturedThemes.Wrap(new Builder(capturedContext =>
                    {
                        int theme = TestTheme.Of(capturedContext);
                        Color? selection = DefaultSelectionStyle.Of(capturedContext).SelectionColor;
                        _onResolved(theme, selection);
                        return new SizedBox();
                    }))));
        }
    }

    private sealed class TestTheme : InheritedTheme
    {
        public TestTheme(int value, Widget child) : base(child)
        {
            Value = value;
        }

        public int Value { get; }

        public static int Of(BuildContext context)
        {
            return context.DependOnInheritedWidgetOfExactType<TestTheme>()?.Value ?? -1;
        }

        public override Widget Wrap(BuildContext context, Widget child)
        {
            return new TestTheme(Value, child);
        }

        public override bool UpdateShouldNotify(InheritedWidget oldWidget)
        {
            return ((TestTheme)oldWidget).Value != Value;
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Widget? _harnessChild;

        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        protected override void PerformRebuild()
        {
            base.PerformRebuild();
            _child = UpdateChild(_child, _harnessChild ?? Widget, Slot);
        }

        public override void Update(Widget newWidget)
        {
            _harnessChild = newWidget;
            Owner!.BuildScope(this, () => Rebuild(force: true));
        }

        public override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        public override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }


        public void InsertRenderObjectChild(RenderObject child, object? slot)
        {
        }

        public void MoveRenderObjectChild(RenderObject child, object? oldSlot, object? newSlot)
        {
        }

        public void RemoveRenderObjectChild(RenderObject child, object? slot)
        {
        }
    }
}
