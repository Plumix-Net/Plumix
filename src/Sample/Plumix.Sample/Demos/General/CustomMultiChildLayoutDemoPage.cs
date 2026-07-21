using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/custom_multi_child_layout_demo_page.dart

public sealed class CustomMultiChildLayoutDemoPage : StatefulWidget
{
    public override State CreateState() => new CustomMultiChildLayoutDemoPageState();
}

internal sealed class CustomMultiChildLayoutDemoPageState : State
{
    private bool _centerMiddle = true;
    private bool _rightToLeft;

    public override Widget Build(BuildContext context)
    {
        TextDirection textDirection = _rightToLeft ? TextDirection.Rtl : TextDirection.Ltr;
        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("CustomMultiChildLayout + NavigationToolbar", fontSize: 20, color: Colors.Black),
                new Text(
                    "LayoutId slots drive dependent child constraints; NavigationToolbar applies the same " +
                    "delegate pipeline to leading, middle, and trailing content.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(
                            _centerMiddle ? "Middle: centered" : "Middle: start",
                            () => SetState(() => _centerMiddle = !_centerMiddle)),
                        BuildButton(
                            _rightToLeft ? "Direction: RTL" : "Direction: LTR",
                            () => SetState(() => _rightToLeft = !_rightToLeft)),
                    ]),
                new Directionality(
                    textDirection,
                    new Container(
                        height: 64,
                        color: Color.Parse("#FFE7EDF6"),
                        child: new NavigationToolbar(
                            leading: BuildSlot("L", 56, 64, Color.Parse("#FF1565C0")),
                            middle: BuildSlot("MIDDLE", 150, 32, Color.Parse("#FF2E7D32")),
                            trailing: BuildSlot("TRAIL", 72, 32, Color.Parse("#FFF57C00")),
                            centerMiddle: _centerMiddle))),
                new Expanded(
                    child: new Container(
                        color: Color.Parse("#FFF3F6FA"),
                        alignment: Alignment.Center,
                        child: new SizedBox(
                            width: 320,
                            height: 170,
                            child: new CustomMultiChildLayout(
                                new FollowLeaderDemoDelegate(),
                                children:
                                [
                                    new LayoutId(
                                        DemoLayoutSlot.Leader,
                                        BuildSlot("LEADER", 96, 56, Color.Parse("#FF6A1B9A"))),
                                    new LayoutId(
                                        DemoLayoutSlot.Follower,
                                        BuildSlot("FOLLOWER", 140, 80, Color.Parse("#FF00838F"))),
                                    new LayoutId(
                                        DemoLayoutSlot.Caption,
                                        BuildSlot("same size", 100, 28, Color.Parse("#FF455A64"))),
                                ])))),
            ]);
    }

    private static Widget BuildSlot(string label, double width, double height, Color color)
    {
        return new SizedBox(
            width: width,
            height: height,
            child: new Container(
                color: color,
                alignment: Alignment.Center,
                child: new Text(label, fontSize: 12, color: Colors.White)));
    }

    private static Widget BuildButton(string label, Action onTap)
    {
        return new SizedBox(
            width: 150,
            child: new CounterTapButton(
                label: label,
                onTap: onTap,
                background: Color.Parse("#FFDCE3ED"),
                foreground: Colors.Black,
                fontSize: 12,
                padding: new Thickness(10, 8)));
    }
}

internal enum DemoLayoutSlot
{
    Leader,
    Follower,
    Caption
}

internal sealed class FollowLeaderDemoDelegate : MultiChildLayoutDelegate
{
    public override void PerformLayout(Size size)
    {
        Size leaderSize = LayoutChild(DemoLayoutSlot.Leader, BoxConstraints.Loose(size));
        PositionChild(DemoLayoutSlot.Leader, new Point(16, 18));

        LayoutChild(DemoLayoutSlot.Follower, BoxConstraints.Tight(leaderSize));
        PositionChild(
            DemoLayoutSlot.Follower,
            new Point(size.Width - leaderSize.Width - 16, size.Height - leaderSize.Height - 18));

        Size captionSize = LayoutChild(DemoLayoutSlot.Caption, BoxConstraints.Loose(size));
        PositionChild(
            DemoLayoutSlot.Caption,
            new Point(
                (size.Width - captionSize.Width) / 2.0,
                (size.Height - captionSize.Height) / 2.0));
    }

    public override bool ShouldRelayout(MultiChildLayoutDelegate oldDelegate) => false;
}
