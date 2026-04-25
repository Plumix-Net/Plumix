using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source (reference): dart_sample/lib/counter_widgets.dart (exact sample parity)

namespace Plumix;

internal sealed class CounterTapButton : StatelessWidget
{
    public CounterTapButton(
        string label,
        Action? onTap,
        Color background,
        Color foreground,
        double fontSize,
        Thickness? padding = null,
        Key? key = null) : base(key)
    {
        Label = label;
        OnTap = onTap;
        Background = background;
        Foreground = foreground;
        FontSize = fontSize;
        Padding = padding ?? new Thickness(14, 10);
    }

    public string Label { get; }

    public Action? OnTap { get; }

    public Color Background { get; }

    public Color Foreground { get; }

    public double FontSize { get; }

    public Thickness Padding { get; }

    public override Widget Build(BuildContext context)
    {
        return new GestureDetector(
            behavior: HitTestBehavior.Opaque,
            onTap: OnTap,
            child: new Container(
                color: OnTap == null ? DisabledColor(Background) : Background,
                padding: Padding,
                child: new Text(Label, fontSize: FontSize, color: Foreground, textAlign: TextAlign.Center)));
    }

    private static Color DisabledColor(Color color)
    {
        var alpha = (byte)Math.Clamp((int)(color.A * 0.45), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}

internal sealed class KeyedListItem : StatefulWidget
{
    private readonly int _id;

    public KeyedListItem(int id, Key? key = null) : base(key)
    {
        _id = id;
    }

    public override State CreateState() => new KeyedListItemState(_id);
}

internal sealed class KeyedListItemState : State
{
    private readonly int _id;
    private readonly int _token;
    private int _taps;

    public KeyedListItemState(int id)
    {
        _id = id;
        _token = Random.Shared.Next(1000, 9999);
    }

    public override Widget Build(BuildContext context)
    {
        return new CounterTapButton(
            label: $"id={_id} token={_token} taps={_taps}",
            onTap: () => SetState(() => _taps++),
            background: Color.Parse("#FFF5F5F5"),
            foreground: Colors.Black,
            fontSize: 14,
            padding: new Thickness(10, 8));
    }
}

internal sealed class MovableBadge : StatefulWidget
{
    public MovableBadge(Key? key = null) : base(key)
    {
    }

    public override State CreateState() => new MovableBadgeState();
}

internal sealed class MovableBadgeState : State
{
    private int _taps;

    public override Widget Build(BuildContext context)
    {
        return new CounterTapButton(
            label: $"global taps={_taps}",
            onTap: () => SetState(() => _taps++),
            background: Colors.DarkOrange,
            foreground: Colors.White,
            fontSize: 14,
            padding: new Thickness(10, 8));
    }
}

internal sealed class KeepAliveListItem : StatefulWidget
{
    public KeepAliveListItem(
        int index,
        bool keepAlive,
        Action? onTap,
        Key? key = null) : base(key)
    {
        Index = index;
        KeepAlive = keepAlive;
        OnTap = onTap;
    }

    public int Index { get; }

    public bool KeepAlive { get; }

    public Action? OnTap { get; }

    public override State CreateState()
    {
        return new KeepAliveListItemState(
            index: Index,
            keepAlive: KeepAlive,
            onTap: OnTap);
    }
}

internal sealed class KeepAliveListItemState : AutomaticKeepAliveClientMixin
{
    private readonly int _index;
    private readonly bool _keepAlive;
    private readonly Action? _onTap;
    private readonly int _token = Random.Shared.Next(1000, 9999);
    private int _localTaps;

    public KeepAliveListItemState(int index, bool keepAlive, Action? onTap)
    {
        _index = index;
        _keepAlive = keepAlive;
        _onTap = onTap;
    }

    protected override bool WantKeepAlive => _keepAlive;

    public override Widget Build(BuildContext context)
    {
        var keepAlive = _keepAlive;
        return new CounterTapButton(
            label: $"row #{_index} keepAlive={(keepAlive ? "on" : "off")} token={_token} local taps={_localTaps}",
            onTap: () =>
            {
                SetState(() => _localTaps += 1);
                _onTap?.Invoke();
            },
            background: keepAlive ? Color.Parse("#FFE8F5E9") : Colors.White,
            foreground: Colors.Black,
            fontSize: 13,
            padding: new Thickness(10, 8));
    }
}
