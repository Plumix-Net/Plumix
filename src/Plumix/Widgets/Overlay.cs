using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/overlay.dart

public delegate Widget OverlayWidgetBuilder(BuildContext context);

public sealed class OverlayEntry : IListenable, IDisposable
{
    private OverlayState? _overlay;
    private bool _disposed;
    private bool _opaque;
    private bool _maintainState;
    private bool _widgetMounted;
    private readonly List<Action> _listeners = [];

    public OverlayEntry(
        OverlayWidgetBuilder builder,
        bool opaque = false,
        bool maintainState = false,
        bool canSizeOverlay = false)
    {
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _opaque = opaque;
        _maintainState = maintainState;
        CanSizeOverlay = canSizeOverlay;
    }

    public OverlayWidgetBuilder Builder { get; }

    public bool Opaque
    {
        get => _opaque;
        set
        {
            ThrowIfDisposed();
            if (_opaque == value)
            {
                return;
            }

            RequireOverlay();
            _opaque = value;
            _overlay!.MarkDirty();
        }
    }

    public bool MaintainState
    {
        get => _maintainState;
        set
        {
            ThrowIfDisposed();
            if (_maintainState == value)
            {
                return;
            }

            RequireOverlay();
            _maintainState = value;
            _overlay!.MarkDirty();
        }
    }

    public bool CanSizeOverlay { get; }

    public bool Mounted => _widgetMounted;

    internal OverlayState? Owner => _overlay;

    internal event Action? Changed;

    public void MarkNeedsBuild()
    {
        ThrowIfDisposed();
        Changed?.Invoke();
    }

    public void Remove()
    {
        ThrowIfDisposed();
        RequireOverlay();
        OverlayState overlay = _overlay!;
        _overlay = null;
        overlay.Remove(this);
    }

    public void AddListener(Action listener)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(listener);
        _listeners.Add(listener);
    }

    public void RemoveListener(Action listener)
    {
        if (_disposed)
        {
            return;
        }

        _listeners.Remove(listener);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_overlay is not null)
        {
            throw new InvalidOperationException("An OverlayEntry must be removed before it is disposed.");
        }

        _disposed = true;
        if (!Mounted)
        {
            ReleaseListeners();
        }
    }

    internal void Attach(OverlayState overlay)
    {
        ThrowIfDisposed();
        if (_overlay is not null)
        {
            throw new InvalidOperationException("The OverlayEntry is already present in an Overlay.");
        }

        _overlay = overlay;
    }

    internal void Detach(OverlayState overlay)
    {
        if (ReferenceEquals(_overlay, overlay))
        {
            _overlay = null;
        }
    }

    internal void SetWidgetMounted(bool mounted)
    {
        if (_widgetMounted == mounted)
        {
            return;
        }

        _widgetMounted = mounted;
        foreach (Action listener in _listeners.ToArray())
        {
            listener();
        }

        if (!mounted && _disposed)
        {
            ReleaseListeners();
        }
    }

    private void RequireOverlay()
    {
        if (_overlay is null)
        {
            throw new InvalidOperationException("The OverlayEntry is not present in an Overlay.");
        }
    }

    private void ReleaseListeners()
    {
        Changed = null;
        _listeners.Clear();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}

public sealed class Overlay : StatefulWidget
{
    public Overlay(
        IReadOnlyList<OverlayEntry>? initialEntries = null,
        Clip clipBehavior = Clip.HardEdge,
        bool alwaysSizeToContent = false,
        Key? key = null) : base(key)
    {
        InitialEntries = initialEntries ?? [];
        if (InitialEntries.Count != InitialEntries.Distinct().Count())
        {
            throw new ArgumentException("Overlay initial entries must be unique.", nameof(initialEntries));
        }

        ClipBehavior = clipBehavior;
        AlwaysSizeToContent = alwaysSizeToContent;
    }

    public IReadOnlyList<OverlayEntry> InitialEntries { get; }

    public Clip ClipBehavior { get; }

    public bool AlwaysSizeToContent { get; }

    public static Widget Wrap(
        Widget child,
        Clip clipBehavior = Clip.HardEdge,
        bool alwaysSizeToContent = false,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(child);
        return new WrappingOverlay(
            child,
            clipBehavior,
            alwaysSizeToContent,
            key);
    }

    public static OverlayState Of(BuildContext context, bool rootOverlay = false)
    {
        return MaybeOf(context, rootOverlay)
               ?? throw new InvalidOperationException("No Overlay ancestor was found.");
    }

    public static OverlayState? MaybeOf(BuildContext context, bool rootOverlay = false)
    {
        return rootOverlay
            ? context.FindRootAncestorStateOfType<OverlayState>()
            : context.FindAncestorStateOfType<OverlayState>();
    }

    public override State CreateState() => new OverlayState();
}

public sealed class OverlayState : State
{
    private readonly List<OverlayEntry> _entries = [];

    private Overlay CurrentWidget => (Overlay)StateWidget;

    public IReadOnlyList<OverlayEntry> Entries => _entries;

    public override void InitState()
    {
        base.InitState();
        foreach (OverlayEntry entry in CurrentWidget.InitialEntries)
        {
            entry.Attach(this);
            _entries.Add(entry);
        }
    }

    public override void Dispose()
    {
        foreach (OverlayEntry entry in _entries)
        {
            entry.Detach(this);
        }

        _entries.Clear();
        base.Dispose();
    }

    public void Insert(OverlayEntry entry, OverlayEntry? below = null, OverlayEntry? above = null)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (below is not null && above is not null)
        {
            throw new ArgumentException("Only one of below and above may be specified.");
        }

        int index = ResolveInsertionIndex(below, above);
        entry.Attach(this);
        SetState(() => _entries.Insert(index, entry));
    }

    public void InsertAll(
        IReadOnlyList<OverlayEntry> entries,
        OverlayEntry? below = null,
        OverlayEntry? above = null)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (below is not null && above is not null)
        {
            throw new ArgumentException("Only one of below and above may be specified.");
        }

        if (entries.Count != entries.Distinct().Count())
        {
            throw new ArgumentException("Overlay entries must be unique.", nameof(entries));
        }

        foreach (OverlayEntry entry in entries)
        {
            if (entry.Owner is not null)
            {
                throw new InvalidOperationException("An entry is already present in an Overlay.");
            }
        }

        if (entries.Count == 0)
        {
            return;
        }

        int index = ResolveInsertionIndex(below, above);
        foreach (OverlayEntry entry in entries)
        {
            entry.Attach(this);
        }

        SetState(() => _entries.InsertRange(index, entries));
    }

    public void Rearrange(
        IReadOnlyList<OverlayEntry> newEntries,
        OverlayEntry? below = null,
        OverlayEntry? above = null)
    {
        ArgumentNullException.ThrowIfNull(newEntries);
        ValidatePositionArguments(below, above);
        if (newEntries.Count == 0)
        {
            return;
        }

        if (newEntries.Count != newEntries.Distinct().Count())
        {
            throw new ArgumentException("Overlay entries must be unique.", nameof(newEntries));
        }

        if (below is not null && !newEntries.Contains(below))
        {
            throw new ArgumentException("The below entry must be present in newEntries.", nameof(below));
        }

        if (above is not null && !newEntries.Contains(above))
        {
            throw new ArgumentException("The above entry must be present in newEntries.", nameof(above));
        }

        foreach (OverlayEntry entry in newEntries)
        {
            if (entry.Owner is not null && !ReferenceEquals(entry.Owner, this))
            {
                throw new InvalidOperationException("An entry is already present in another Overlay.");
            }
        }

        if (_entries.SequenceEqual(newEntries))
        {
            return;
        }

        var oldEntries = _entries.Where(entry => !newEntries.Contains(entry)).ToList();
        foreach (OverlayEntry entry in newEntries)
        {
            if (entry.Owner is null)
            {
                entry.Attach(this);
            }
        }

        SetState(() =>
        {
            _entries.Clear();
            _entries.AddRange(newEntries);
            int insertionIndex = ResolveInsertionIndex(below, above);
            _entries.InsertRange(insertionIndex, oldEntries);
        });
    }

    public bool DebugIsVisible(OverlayEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        int entryIndex = _entries.IndexOf(entry);
        if (entryIndex < 0)
        {
            throw new ArgumentException("The entry is not present in this Overlay.", nameof(entry));
        }

        for (int index = _entries.Count - 1; index > entryIndex; index--)
        {
            if (_entries[index].Opaque)
            {
                return false;
            }
        }

        return true;
    }

    internal void Remove(OverlayEntry entry)
    {
        if (!_entries.Contains(entry))
        {
            return;
        }

        SetState(() => _entries.Remove(entry));
    }

    internal void MarkDirty()
    {
        if (Mounted)
        {
            SetState(static () => { });
        }
    }

    public override Widget Build(BuildContext context)
    {
        bool onstage = true;
        int onstageCount = 0;
        var children = new List<Widget>();
        for (int index = _entries.Count - 1; index >= 0; index--)
        {
            OverlayEntry entry = _entries[index];
            if (onstage)
            {
                onstageCount += 1;
                children.Add(
                    BuildEntry(entry, tickerEnabled: true, isOnstage: true));

                if (entry.Opaque)
                {
                    onstage = false;
                }
            }
            else if (entry.MaintainState)
            {
                children.Add(
                    BuildEntry(entry, tickerEnabled: false, isOnstage: false));
            }
        }

        children.Reverse();
        return new OverlayTheater(
            offstageCount: children.Count - onstageCount,
            clipBehavior: CurrentWidget.ClipBehavior,
            alwaysSizeToContent: CurrentWidget.AlwaysSizeToContent,
            children: children);
    }

    private static Widget BuildEntry(
        OverlayEntry entry,
        bool tickerEnabled,
        bool isOnstage)
    {
        return new OverlayTheaterEntry(
            canSizeOverlay: entry.CanSizeOverlay,
            isOnstage: isOnstage,
            child: new OverlayEntryWidget(
                entry,
                tickerEnabled,
                key: new ObjectKey(entry)));
    }

    private int ResolveInsertionIndex(OverlayEntry? below, OverlayEntry? above)
    {
        if (below is not null)
        {
            int index = _entries.IndexOf(below);
            if (index < 0)
            {
                throw new ArgumentException("The below entry is not present in this Overlay.", nameof(below));
            }

            return index;
        }

        if (above is not null)
        {
            int index = _entries.IndexOf(above);
            if (index < 0)
            {
                throw new ArgumentException("The above entry is not present in this Overlay.", nameof(above));
            }

            return index + 1;
        }

        return _entries.Count;
    }

    private static void ValidatePositionArguments(OverlayEntry? below, OverlayEntry? above)
    {
        if (below is not null && above is not null)
        {
            throw new ArgumentException("Only one of below and above may be specified.");
        }
    }
}

internal sealed class WrappingOverlay : StatefulWidget
{
    public WrappingOverlay(
        Widget child,
        Clip clipBehavior,
        bool alwaysSizeToContent,
        Key? key = null) : base(key)
    {
        Child = child;
        ClipBehavior = clipBehavior;
        AlwaysSizeToContent = alwaysSizeToContent;
    }

    public Widget Child { get; }

    public Clip ClipBehavior { get; }

    public bool AlwaysSizeToContent { get; }

    public override State CreateState() => new WrappingOverlayState();
}

internal sealed class WrappingOverlayState : State
{
    private OverlayEntry? _entry;

    private WrappingOverlay CurrentWidget => (WrappingOverlay)StateWidget;

    public override void InitState()
    {
        base.InitState();
        _entry = new OverlayEntry(
            _ => CurrentWidget.Child,
            opaque: true,
            canSizeOverlay: true);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        _entry!.MarkNeedsBuild();
    }

    public override Widget Build(BuildContext context)
    {
        return new Overlay(
            initialEntries: [_entry!],
            clipBehavior: CurrentWidget.ClipBehavior,
            alwaysSizeToContent: CurrentWidget.AlwaysSizeToContent);
    }

    public override void Dispose()
    {
        if (_entry!.Owner is not null)
        {
            _entry.Remove();
        }

        _entry.Dispose();
        _entry = null;
        base.Dispose();
    }
}

internal sealed class OverlayTheater : MultiChildRenderObjectWidget
{
    public OverlayTheater(
        IReadOnlyList<Widget> children,
        int offstageCount,
        Clip clipBehavior,
        bool alwaysSizeToContent) : base(children)
    {
        if (offstageCount < 0 || offstageCount > children.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(offstageCount));
        }

        OffstageCount = offstageCount;
        ClipBehavior = clipBehavior;
        AlwaysSizeToContent = alwaysSizeToContent;
    }

    public int OffstageCount { get; }

    public Clip ClipBehavior { get; }

    public bool AlwaysSizeToContent { get; }

    internal override RenderObject CreateRenderObject(BuildContext context)
    {
        Alignment alignment = Directionality.Of(context) == TextDirection.Rtl
            ? Alignment.TopRight
            : Alignment.TopLeft;
        return new RenderOverlayTheater(
            alignment,
            ClipBehavior,
            AlwaysSizeToContent);
    }

    internal override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var theater = (RenderOverlayTheater)renderObject;
        theater.Alignment = Directionality.Of(context) == TextDirection.Rtl
            ? Alignment.TopRight
            : Alignment.TopLeft;
        theater.ClipBehavior = ClipBehavior;
        theater.AlwaysSizeToContent = AlwaysSizeToContent;
    }
}

internal sealed class OverlayTheaterEntry : ParentDataWidget<OverlayTheaterParentData>
{
    public OverlayTheaterEntry(
        Widget child,
        bool canSizeOverlay,
        bool isOnstage) : base(child)
    {
        CanSizeOverlay = canSizeOverlay;
        IsOnstage = isOnstage;
    }

    public bool CanSizeOverlay { get; }

    public bool IsOnstage { get; }

    public override Type DebugTypicalAncestorWidgetType => typeof(OverlayTheater);

    protected override void ApplyParentData(RenderObject renderObject)
    {
        var parentData = (OverlayTheaterParentData)renderObject.parentData!;
        bool needsLayout = false;
        if (parentData.CanSizeOverlay != CanSizeOverlay)
        {
            parentData.CanSizeOverlay = CanSizeOverlay;
            needsLayout = true;
        }

        if (parentData.IsOnstage != IsOnstage)
        {
            parentData.IsOnstage = IsOnstage;
            needsLayout = true;
        }

        if (needsLayout)
        {
            renderObject.Parent?.MarkNeedsLayout();
        }
    }
}

internal sealed class OverlayEntryWidget : StatefulWidget
{
    public OverlayEntryWidget(OverlayEntry entry, bool tickerEnabled, Key? key = null) : base(key)
    {
        Entry = entry;
        TickerEnabled = tickerEnabled;
    }

    public OverlayEntry Entry { get; }

    public bool TickerEnabled { get; }

    public override State CreateState() => new OverlayEntryWidgetState();
}

internal sealed class OverlayEntryWidgetState : State
{
    private OverlayEntryWidget CurrentWidget => (OverlayEntryWidget)StateWidget;

    public override void InitState()
    {
        base.InitState();
        CurrentWidget.Entry.Changed += HandleEntryChanged;
        CurrentWidget.Entry.SetWidgetMounted(true);
    }

    public override void DidUpdateWidget(StatefulWidget oldWidget)
    {
        base.DidUpdateWidget(oldWidget);
        var oldEntryWidget = (OverlayEntryWidget)oldWidget;
        if (ReferenceEquals(oldEntryWidget.Entry, CurrentWidget.Entry))
        {
            return;
        }

        oldEntryWidget.Entry.Changed -= HandleEntryChanged;
        oldEntryWidget.Entry.SetWidgetMounted(false);
        CurrentWidget.Entry.Changed += HandleEntryChanged;
        CurrentWidget.Entry.SetWidgetMounted(true);
    }

    public override void Dispose()
    {
        CurrentWidget.Entry.Changed -= HandleEntryChanged;
        CurrentWidget.Entry.SetWidgetMounted(false);
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new TickerMode(
            enabled: CurrentWidget.TickerEnabled,
            child: CurrentWidget.Entry.Builder(context));
    }

    private void HandleEntryChanged()
    {
        if (Mounted)
        {
            SetState(static () => { });
        }
    }
}
