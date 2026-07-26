using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/overlay.dart

public delegate Widget OverlayWidgetBuilder(BuildContext context);

public sealed class OverlayEntry : IDisposable
{
    private OverlayState? _overlay;
    private bool _disposed;

    public OverlayEntry(
        OverlayWidgetBuilder builder,
        bool opaque = false,
        bool maintainState = false,
        bool canSizeOverlay = false)
    {
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        Opaque = opaque;
        MaintainState = maintainState;
        CanSizeOverlay = canSizeOverlay;
    }

    public OverlayWidgetBuilder Builder { get; }

    public bool Opaque { get; }

    public bool MaintainState { get; }

    public bool CanSizeOverlay { get; }

    public bool Mounted => _overlay is not null;

    internal event Action? Changed;

    public void MarkNeedsBuild()
    {
        ThrowIfDisposed();
        Changed?.Invoke();
    }

    public void Remove()
    {
        ThrowIfDisposed();
        _overlay?.Remove(this);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (Mounted)
        {
            throw new InvalidOperationException("An OverlayEntry must be removed before it is disposed.");
        }

        _disposed = true;
        Changed = null;
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
        Key? key = null) : base(key)
    {
        InitialEntries = initialEntries ?? [];
        if (InitialEntries.Count != InitialEntries.Distinct().Count())
        {
            throw new ArgumentException("Overlay initial entries must be unique.", nameof(initialEntries));
        }

        ClipBehavior = clipBehavior;
    }

    public IReadOnlyList<OverlayEntry> InitialEntries { get; }

    public Clip ClipBehavior { get; }

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

        int index = ResolveInsertionIndex(below, above);
        foreach (OverlayEntry entry in entries)
        {
            entry.Attach(this);
        }

        SetState(() => _entries.InsertRange(index, entries));
    }

    internal void Remove(OverlayEntry entry)
    {
        if (!_entries.Contains(entry))
        {
            return;
        }

        SetState(() => _entries.Remove(entry));
        entry.Detach(this);
    }

    public override Widget Build(BuildContext context)
    {
        bool onstage = true;
        var children = new List<Widget>();
        for (int index = _entries.Count - 1; index >= 0; index--)
        {
            OverlayEntry entry = _entries[index];
            if (onstage || entry.MaintainState)
            {
                children.Add(
                    new OverlayEntryWidget(
                        entry,
                        tickerEnabled: onstage,
                        key: new ObjectKey(entry)));
            }

            if (entry.Opaque)
            {
                onstage = false;
            }
        }

        children.Reverse();
        return new Stack(
            fit: StackFit.Expand,
            clipBehavior: CurrentWidget.ClipBehavior,
            children: children);
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
        CurrentWidget.Entry.Changed += HandleEntryChanged;
    }

    public override void Dispose()
    {
        CurrentWidget.Entry.Changed -= HandleEntryChanged;
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
