using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/tab_scaffold.dart

/// <summary>Coordinates tab selection for a <see cref="CupertinoTabScaffold"/>.</summary>
public class CupertinoTabController : ChangeNotifier
{
    private int _index;

    public CupertinoTabController(int initialIndex = 0)
    {
        if (initialIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialIndex));
        }

        _index = initialIndex;
    }

    internal bool IsDisposed { get; private set; }

    public int Index
    {
        get => _index;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (_index == value)
            {
                return;
            }

            _index = value;
            NotifyListeners();
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        IsDisposed = true;
    }
}

/// <summary>Implements a tabbed iOS application's root layout and behavior.</summary>
public sealed class CupertinoTabScaffold : StatefulWidget
{
    public CupertinoTabScaffold(
        CupertinoTabBar tabBar,
        IndexedWidgetBuilder tabBuilder,
        CupertinoTabController? controller = null,
        CupertinoDynamicColor? backgroundColor = null,
        bool resizeToAvoidBottomInset = true,
        string? restorationId = null,
        Key? key = null) : base(key)
    {
        TabBar = tabBar ?? throw new ArgumentNullException(nameof(tabBar));
        TabBuilder = tabBuilder ?? throw new ArgumentNullException(nameof(tabBuilder));
        if (controller is not null && controller.Index >= tabBar.Items.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(controller),
                $"The controller's current index {controller.Index} is out of bounds for "
                + $"the tab bar with {tabBar.Items.Count} tabs.");
        }

        Controller = controller;
        BackgroundColor = backgroundColor;
        ResizeToAvoidBottomInset = resizeToAvoidBottomInset;
        RestorationId = restorationId;
    }

    public CupertinoTabBar TabBar { get; }

    public CupertinoTabController? Controller { get; }

    public IndexedWidgetBuilder TabBuilder { get; }

    public CupertinoDynamicColor? BackgroundColor { get; }

    public bool ResizeToAvoidBottomInset { get; }

    public string? RestorationId { get; }

    public override State CreateState() => new CupertinoTabScaffoldState();
}

internal sealed class CupertinoTabScaffoldState : RestorationState
{
    private RestorableCupertinoTabController? _internalController;

    private CupertinoTabScaffold CurrentWidget => (CupertinoTabScaffold)StateWidget;

    private CupertinoTabController Controller => CurrentWidget.Controller ?? _internalController!.Value;

    protected override string? RestorationId => CurrentWidget.RestorationId;

    protected override void RestoreState(RestorationBucket? oldBucket, bool initialRestore)
    {
        RestoreInternalController();
    }

    public override void InitState()
    {
        base.InitState();
        UpdateTabController();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var oldTabScaffold = (CupertinoTabScaffold)oldWidget;
        if (!ReferenceEquals(CurrentWidget.Controller, oldTabScaffold.Controller))
        {
            UpdateTabController(oldTabScaffold.Controller);
        }
        else if (Controller.Index >= CurrentWidget.TabBar.Items.Count)
        {
            Controller.Index = CurrentWidget.TabBar.Items.Count - 1;
        }
    }

    public override Widget Build(BuildContext context)
    {
        MediaQueryData existingMediaQuery = MediaQuery.Of(context);
        MediaQueryData newMediaQuery = existingMediaQuery;

        Widget content = new CupertinoTabSwitchingView(
            currentTabIndex: Controller.Index,
            tabCount: CurrentWidget.TabBar.Items.Count,
            tabBuilder: CurrentWidget.TabBuilder);
        Thickness contentPadding = default;

        if (CurrentWidget.ResizeToAvoidBottomInset)
        {
            newMediaQuery = newMediaQuery.RemoveViewInsets(removeBottom: true);
            contentPadding = new Thickness(0.0, 0.0, 0.0, existingMediaQuery.ViewInsets.Bottom);
        }

        if (!CurrentWidget.ResizeToAvoidBottomInset
            || CurrentWidget.TabBar.PreferredSize.Height > existingMediaQuery.ViewInsets.Bottom)
        {
            double bottomPadding =
                CurrentWidget.TabBar.PreferredSize.Height + existingMediaQuery.Padding.Bottom;

            if (CurrentWidget.TabBar.Opaque(context))
            {
                contentPadding = new Thickness(0.0, 0.0, 0.0, bottomPadding);
                newMediaQuery = newMediaQuery.RemovePadding(removeBottom: true);
            }
            else
            {
                newMediaQuery = newMediaQuery.CopyWith(
                    padding: CopyBottom(newMediaQuery.Padding, bottomPadding));
            }
        }

        content = new MediaQuery(
            data: newMediaQuery,
            child: new Padding(
                insets: contentPadding,
                child: content));

        Color backgroundColor = CupertinoDynamicColor.MaybeResolve(CurrentWidget.BackgroundColor, context)
            ?? CupertinoTheme.Of(context).ScaffoldBackgroundColor.Value;

        return new DecoratedBox(
            decoration: new BoxDecoration(Color: backgroundColor),
            child: new Stack(children:
            [
                content,
                MediaQuery.WithNoTextScaling(
                    context,
                    new Align(
                        alignment: Alignment.BottomCenter,
                        child: CurrentWidget.TabBar.CopyWith(
                            currentIndex: Controller.Index,
                            onTap: OnTap))),
            ]));
    }

    public override void Dispose()
    {
        if (CurrentWidget.Controller?.IsDisposed == false)
        {
            Controller.RemoveListener(OnCurrentIndexChange);
        }

        _internalController?.Dispose();
        base.Dispose();
    }

    private void RestoreInternalController()
    {
        if (_internalController is null)
        {
            return;
        }

        RegisterForRestoration(_internalController, "controller");
        _internalController.Value.AddListener(OnCurrentIndexChange);
    }

    private void UpdateTabController(CupertinoTabController? oldWidgetController = null)
    {
        if (CurrentWidget.Controller is null && _internalController is null)
        {
            _internalController = new RestorableCupertinoTabController(CurrentWidget.TabBar.CurrentIndex);
            if (!RestorePending)
            {
                RestoreInternalController();
            }
        }

        if (CurrentWidget.Controller is not null && _internalController is not null)
        {
            UnregisterFromRestoration(_internalController);
            _internalController.Dispose();
            _internalController = null;
        }

        if (!ReferenceEquals(oldWidgetController, CurrentWidget.Controller))
        {
            if (oldWidgetController?.IsDisposed == false)
            {
                oldWidgetController.RemoveListener(OnCurrentIndexChange);
            }

            CurrentWidget.Controller?.AddListener(OnCurrentIndexChange);
        }
    }

    private void OnCurrentIndexChange()
    {
        if (Controller.Index < 0 || Controller.Index >= CurrentWidget.TabBar.Items.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Controller.Index),
                $"The current index {Controller.Index} is out of bounds for "
                + $"the tab bar with {CurrentWidget.TabBar.Items.Count} tabs.");
        }

        SetState(() => { });
    }

    private void OnTap(int newIndex)
    {
        Controller.Index = newIndex;
        CurrentWidget.TabBar.OnTap?.Invoke(newIndex);
    }

    private static Thickness CopyBottom(Thickness source, double bottom)
    {
        return new Thickness(source.Left, source.Top, source.Right, bottom);
    }
}

internal sealed class CupertinoTabSwitchingView : StatefulWidget
{
    public CupertinoTabSwitchingView(
        int currentTabIndex,
        int tabCount,
        IndexedWidgetBuilder tabBuilder,
        Key? key = null) : base(key)
    {
        if (tabCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tabCount));
        }

        CurrentTabIndex = currentTabIndex;
        TabCount = tabCount;
        TabBuilder = tabBuilder ?? throw new ArgumentNullException(nameof(tabBuilder));
    }

    public int CurrentTabIndex { get; }

    public int TabCount { get; }

    public IndexedWidgetBuilder TabBuilder { get; }

    public override State CreateState() => new CupertinoTabSwitchingViewState();
}

internal sealed class CupertinoTabSwitchingViewState : State
{
    private readonly List<bool> _shouldBuildTab = [];
    private readonly List<FocusScopeNode> _tabFocusNodes = [];
    private readonly List<FocusScopeNode> _discardedNodes = [];

    private CupertinoTabSwitchingView CurrentWidget => (CupertinoTabSwitchingView)StateWidget;

    public override void InitState()
    {
        base.InitState();
        _shouldBuildTab.AddRange(Enumerable.Repeat(false, CurrentWidget.TabCount));
    }

    public override void DidChangeDependencies()
    {
        base.DidChangeDependencies();
        FocusActiveTab();
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);

        int lengthDiff = CurrentWidget.TabCount - _shouldBuildTab.Count;
        if (lengthDiff > 0)
        {
            _shouldBuildTab.AddRange(Enumerable.Repeat(false, lengthDiff));
        }
        else if (lengthDiff < 0)
        {
            _shouldBuildTab.RemoveRange(CurrentWidget.TabCount, -lengthDiff);
        }

        FocusActiveTab();
    }

    public override Widget Build(BuildContext context)
    {
        var children = new List<Widget>(CurrentWidget.TabCount);
        for (int index = 0; index < CurrentWidget.TabCount; index++)
        {
            int tabIndex = index;
            bool active = tabIndex == CurrentWidget.CurrentTabIndex;
            _shouldBuildTab[tabIndex] = active || _shouldBuildTab[tabIndex];

            children.Add(new HeroMode(
                enabled: active,
                child: new Offstage(
                    offstage: !active,
                    child: new TickerMode(
                        enabled: active,
                        child: new FocusScope(
                            focusScopeNode: _tabFocusNodes[tabIndex],
                            child: new Builder(context => _shouldBuildTab[tabIndex]
                                ? CurrentWidget.TabBuilder(context, tabIndex)
                                : new SizedBox(width: 0.0, height: 0.0)))))));
        }

        return new Stack(
            fit: StackFit.Expand,
            children: children);
    }

    public override void Dispose()
    {
        foreach (FocusScopeNode focusScopeNode in _tabFocusNodes)
        {
            focusScopeNode.Dispose();
        }

        foreach (FocusScopeNode focusScopeNode in _discardedNodes)
        {
            focusScopeNode.Dispose();
        }

        base.Dispose();
    }

    private void FocusActiveTab()
    {
        if (_tabFocusNodes.Count != CurrentWidget.TabCount)
        {
            if (_tabFocusNodes.Count > CurrentWidget.TabCount)
            {
                _discardedNodes.AddRange(_tabFocusNodes.GetRange(
                    CurrentWidget.TabCount,
                    _tabFocusNodes.Count - CurrentWidget.TabCount));
                _tabFocusNodes.RemoveRange(
                    CurrentWidget.TabCount,
                    _tabFocusNodes.Count - CurrentWidget.TabCount);
            }
            else
            {
                int nodesToAdd = CurrentWidget.TabCount - _tabFocusNodes.Count;
                for (int index = 0; index < nodesToAdd; index++)
                {
                    _tabFocusNodes.Add(new FocusScopeNode());
                }
            }
        }

        FocusScopeNode parentScope = FocusScope.MaybeOf(Context) ?? FocusManager.Instance.RootScope;
        parentScope.SetFirstFocus(_tabFocusNodes[CurrentWidget.CurrentTabIndex]);
    }
}

/// <summary>A restorable property that stores a <see cref="CupertinoTabController"/>.</summary>
public sealed class RestorableCupertinoTabController : RestorableChangeNotifier<CupertinoTabController>
{
    private readonly int _initialIndex;

    public RestorableCupertinoTabController(int initialIndex = 0)
    {
        if (initialIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialIndex));
        }

        _initialIndex = initialIndex;
    }

    public override CupertinoTabController CreateDefaultValue()
    {
        return new CupertinoTabController(_initialIndex);
    }

    public override CupertinoTabController FromPrimitives(object? data)
    {
        if (data is null)
        {
            throw new InvalidOperationException("A tab index cannot be restored from null.");
        }

        return new CupertinoTabController((int)data);
    }

    public override object? ToPrimitives()
    {
        return Value.Index;
    }
}
