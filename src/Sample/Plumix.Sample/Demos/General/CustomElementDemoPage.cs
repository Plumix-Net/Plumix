using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/demos/general/custom_element_demo_page.dart
// (exact sample parity)

namespace Plumix;

public sealed class CustomElementDemoPage : StatefulWidget
{
    public override State CreateState()
    {
        return new CustomElementDemoPageState();
    }
}

internal sealed class CustomElementDemoPageState : State
{
    private readonly ElementLifecycleLog _log = new();
    private int _revision;
    private int _generation;
    private bool _attached = true;

    public override void InitState()
    {
        base.InitState();
        _log.Changed = HandleLogChanged;
    }

    public override void Dispose()
    {
        _log.Changed = null;
        base.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("Custom Element", fontSize: 20, color: Colors.Black),
                new Text(
                    "ElementLifecycleProbe is a widget declared in the sample assembly that returns its own "
                    + "Element from CreateElement. The element drives the child through UpdateChild and counts "
                    + "every lifecycle call the framework makes on it.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Container(
                    color: Color.Parse("#FFF4F7FA"),
                    padding: new Thickness(12),
                    child: new Column(
                        crossAxisAlignment: CrossAxisAlignment.Start,
                        spacing: 6,
                        children:
                        [
                            CountLine("Mount", _log.Mounts),
                            CountLine("Rebuild", _log.Rebuilds),
                            CountLine("Update", _log.Updates),
                            CountLine("Deactivate", _log.Deactivations),
                            CountLine("Unmount", _log.Unmounts),
                        ])),
                new Row(
                    spacing: 8,
                    children:
                    [
                        ActionButton("Rebuild", () => SetState(() => { })),
                        ActionButton("New child", () => SetState(() => _revision += 1)),
                        ActionButton("New key", () => SetState(() => _generation += 1)),
                        ActionButton(_attached ? "Detach" : "Attach", () => SetState(() => _attached = !_attached)),
                    ]),
                BuildProbe(),
            ]);
    }

    private Widget BuildProbe()
    {
        if (!_attached)
        {
            return new Container(
                color: Color.Parse("#FFF1F1F1"),
                padding: new Thickness(12),
                child: new Text("probe detached", fontSize: 14, color: Colors.DimGray));
        }

        return new ElementLifecycleProbe(
            log: _log,
            child: new Container(
                color: Color.Parse("#FFE1F5FE"),
                padding: new Thickness(12),
                child: new Text($"probe child revision {_revision}", fontSize: 14, color: Colors.Black)),
            key: new ValueKey<int>(_generation));
    }

    private static Widget CountLine(string label, int value)
    {
        return new Text($"{label}: {value}", fontSize: 14, color: Color.Parse("#FF31506F"));
    }

    private Widget ActionButton(string label, Action onTap)
    {
        return new Expanded(
            child: new CounterTapButton(
                label: label,
                onTap: onTap,
                background: Colors.SteelBlue,
                foreground: Colors.White,
                fontSize: 13));
    }

    private void HandleLogChanged()
    {
        if (!Mounted)
        {
            return;
        }

        SetState(() => { });
    }
}

/// <summary>Lifecycle counters shared between the demo page and the element it inflates.</summary>
internal sealed class ElementLifecycleLog
{
    public int Mounts { get; private set; }

    public int Rebuilds { get; private set; }

    public int Updates { get; private set; }

    public int Deactivations { get; private set; }

    public int Unmounts { get; private set; }

    public Action? Changed { get; set; }

    public void RecordMount() => Record(() => Mounts += 1);

    public void RecordRebuild() => Record(() => Rebuilds += 1);

    public void RecordUpdate() => Record(() => Updates += 1);

    public void RecordDeactivate() => Record(() => Deactivations += 1);

    public void RecordUnmount() => Record(() => Unmounts += 1);

    private void Record(Action mutate)
    {
        mutate();

        // The counters change while the framework is building or finalizing the tree, so the page is told
        // after the frame instead of from inside the call that changed them.
        Scheduler.AddPostFrameCallback(_ => Changed?.Invoke());
    }
}

/// <summary>
/// A widget that pairs with its own <see cref="Element"/>, authored outside the framework assembly.
/// </summary>
internal sealed class ElementLifecycleProbe : Widget
{
    public ElementLifecycleProbe(ElementLifecycleLog log, Widget child, Key? key = null) : base(key)
    {
        Log = log;
        Child = child;
    }

    public ElementLifecycleLog Log { get; }

    public Widget Child { get; }

    public override Element CreateElement() => new ElementLifecycleProbeElement(this);
}

/// <summary>The hand-written element behind <see cref="ElementLifecycleProbe"/>.</summary>
internal sealed class ElementLifecycleProbeElement : Element
{
    private Element? _child;

    public ElementLifecycleProbeElement(ElementLifecycleProbe widget) : base(widget)
    {
    }

    public override RenderObject? RenderObject => _child?.RenderObject;

    public override Element? RenderObjectAttachingChild => _child;

    private ElementLifecycleProbe Probe => (ElementLifecycleProbe)Widget;

    protected override void OnMount()
    {
        base.OnMount();
        Probe.Log.RecordMount();
        Rebuild();
    }

    public override void Rebuild()
    {
        Dirty = false;
        Probe.Log.RecordRebuild();
        _child = UpdateChild(_child, Probe.Child, Slot);
    }

    public override void Update(Widget newWidget)
    {
        base.Update(newWidget);
        Probe.Log.RecordUpdate();
        Rebuild();
    }

    protected override void OnDeactivate()
    {
        Probe.Log.RecordDeactivate();
        base.OnDeactivate();
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
        if (ReferenceEquals(child, _child))
        {
            _child = null;
        }
    }

    public override void Unmount()
    {
        if (_child != null)
        {
            UnmountChild(_child);
            _child = null;
        }

        Probe.Log.RecordUnmount();
        base.Unmount();
    }
}
