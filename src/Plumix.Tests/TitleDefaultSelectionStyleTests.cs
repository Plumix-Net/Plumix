using Avalonia.Media;
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
        SystemChrome.ResetApplicationSwitcherDescriptionForTests();
    }

    public void Dispose()
    {
        SystemChrome.ResetApplicationSwitcherDescriptionForTests();
    }

    [Fact]
    public void Title_ValidatesOpaqueColorAndUpdatesApplicationSwitcherDescription()
    {
        Assert.Throws<ArgumentException>(() => new Title(
            color: Color.FromArgb(0x80, 0x11, 0x22, 0x33),
            child: new SizedBox()));

        int notifications = 0;
        SystemChrome.ApplicationSwitcherDescriptionChanged += _ => notifications++;
        var owner = new BuildOwner();
        var root = new TestRootElement(new Title(
            title: "First",
            color: Color.FromArgb(0xFF, 0x12, 0x34, 0x56),
            child: new SizedBox()));
        root.Attach(owner);
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal(
            new ApplicationSwitcherDescription("First", 0xFF123456),
            SystemChrome.CurrentApplicationSwitcherDescription);
        Assert.Equal(1, notifications);

        root.Update(new Title(
            title: "First",
            color: Color.FromArgb(0xFF, 0x12, 0x34, 0x56),
            child: new SizedBox()));
        owner.FlushBuild();
        Assert.Equal(1, notifications);

        root.Update(new Title(
            title: "Second",
            color: Color.FromArgb(0xFF, 0x65, 0x43, 0x21),
            child: new SizedBox()));
        owner.FlushBuild();
        Assert.Equal(
            new ApplicationSwitcherDescription("Second", 0xFF654321),
            SystemChrome.CurrentApplicationSwitcherDescription);
        Assert.Equal(2, notifications);

        root.Unmount();
    }

    [Fact]
    public void DefaultSelectionStyle_FallbackMergeAndNotificationMatchSourceContract()
    {
        DefaultSelectionStyle? resolved = null;
        var inheritedCursor = Colors.Crimson;
        var localSelection = Colors.CornflowerBlue;
        MouseCursor inheritedMouseCursor = SystemMouseCursors.Click;
        var owner = new BuildOwner();
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
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.NotNull(resolved);
        Assert.Equal(inheritedCursor, resolved.CursorColor);
        Assert.Equal(localSelection, resolved.SelectionColor);
        Assert.Equal(inheritedMouseCursor, resolved.MouseCursor);
        Assert.Equal(Color.FromArgb(0x80, 0x80, 0x80, 0x80), DefaultSelectionStyle.DefaultColor);

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
        root.Unmount();
    }

    [Fact]
    public void InheritedTheme_CaptureFreezesNearestThemeOfEachType()
    {
        int? resolvedThemeValue = null;
        Color? resolvedSelectionColor = null;
        var owner = new BuildOwner();
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
        root.Mount(parent: null, newSlot: null);
        owner.FlushBuild();

        Assert.Equal(1, resolvedThemeValue);
        Assert.Equal(Colors.Crimson, resolvedSelectionColor);
        root.Unmount();
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
        public TestTheme(int value, Widget child)
        {
            Value = value;
            Child = child;
        }

        public int Value { get; }

        public Widget Child { get; }

        public static int Of(BuildContext context)
        {
            return context.DependOnInherited<TestTheme>()?.Value ?? -1;
        }

        public override Widget Build(BuildContext context)
        {
            return Child;
        }

        public override Widget Wrap(BuildContext context, Widget child)
        {
            return new TestTheme(Value, child);
        }

        protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
        {
            return ((TestTheme)oldWidget).Value != Value;
        }
    }

    private sealed class TestRootElement : Element, IRenderObjectHost
    {
        private Element? _child;

        public TestRootElement(Widget widget) : base(widget)
        {
        }

        protected override void OnMount()
        {
            base.OnMount();
            Rebuild();
        }

        internal override void Rebuild()
        {
            Dirty = false;
            _child = UpdateChild(_child, Widget, Slot);
        }

        internal override void Update(Widget newWidget)
        {
            base.Update(newWidget);
            Rebuild();
        }

        internal override void VisitChildren(Action<Element> visitor)
        {
            if (_child != null)
            {
                visitor(_child);
            }
        }

        internal override void ForgetChild(Element child)
        {
            if (ReferenceEquals(_child, child))
            {
                _child = null;
            }
        }

        internal override void Unmount()
        {
            if (_child != null)
            {
                UnmountChild(_child);
                _child = null;
            }

            base.Unmount();
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
