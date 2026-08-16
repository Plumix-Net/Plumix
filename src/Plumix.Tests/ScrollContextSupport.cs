using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Tests;

/// <summary>
/// A stand-alone <see cref="IScrollContext"/> for tests that drive a <see cref="ScrollPosition"/>
/// without a <see cref="Scrollable"/>. Records what the position pushes into it.
/// </summary>
internal sealed class TestScrollContext : IScrollContext, ITickerProvider
{
    private readonly List<Ticker> _tickers = [];

    public TestScrollContext(
        AxisDirection axisDirection = AxisDirection.Down,
        double devicePixelRatio = 1.0,
        BuildContext? notificationContext = null)
    {
        AxisDirection = axisDirection;
        DevicePixelRatio = devicePixelRatio;
        NotificationContext = notificationContext;
    }

    public BuildContext? NotificationContext { get; set; }

    /// <summary>
    /// The storage context is the notification context when one is set; a position that touches
    /// <see cref="PageStorage"/> without a tree is a test bug, so it throws.
    /// </summary>
    public BuildContext StorageContext =>
        NotificationContext ?? throw new InvalidOperationException("TestScrollContext has no storage context.");

    public ITickerProvider Vsync => this;

    public AxisDirection AxisDirection { get; set; }

    public double DevicePixelRatio { get; set; }

    public bool IgnorePointer { get; private set; }

    public List<bool> IgnorePointerLog { get; } = [];

    public bool? CanDrag { get; private set; }

    public List<bool> CanDragLog { get; } = [];

    public SemanticsActions? SemanticsActions { get; private set; }

    public List<double> SavedOffsets { get; } = [];

    public void SetIgnorePointer(bool value)
    {
        IgnorePointer = value;
        IgnorePointerLog.Add(value);
    }

    public void SetCanDrag(bool value)
    {
        CanDrag = value;
        CanDragLog.Add(value);
    }

    public void SetSemanticsActions(SemanticsActions actions)
    {
        SemanticsActions = actions;
    }

    public void SaveOffset(double offset)
    {
        SavedOffsets.Add(offset);
    }

    public Ticker CreateTicker(TickerCallback onTick)
    {
        var ticker = new Ticker(onTick);
        _tickers.Add(ticker);
        return ticker;
    }
}
