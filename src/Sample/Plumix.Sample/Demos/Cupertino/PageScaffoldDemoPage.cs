using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source (reference): dart_sample/lib/demos/cupertino/page_scaffold_demo_page.dart
// (exact sample parity)

public sealed class CupertinoPageScaffoldDemoPage : StatefulWidget
{
    public override State CreateState() => new CupertinoPageScaffoldDemoPageState();
}

internal sealed class CupertinoPageScaffoldDemoPageState : State
{
    private static readonly CupertinoDynamicColor PageBackground = CupertinoDynamicColor.WithBrightness(
        Color.FromUInt32(0xFFF2F2F7),
        Color.FromUInt32(0xFF1C1C1E));

    private bool _opaqueBar;
    private bool _showKeyboardInset;
    private bool _resizeToAvoidBottomInset = true;

    public override Widget Build(BuildContext context)
    {
        MediaQueryData mediaQuery = MediaQuery.Of(context);
        double bottomInset = _showKeyboardInset ? 96.0 : 0.0;
        return new MediaQuery(
            data: mediaQuery.CopyWith(
                viewInsets: new Thickness(
                    mediaQuery.ViewInsets.Left,
                    mediaQuery.ViewInsets.Top,
                    mediaQuery.ViewInsets.Right,
                    bottomInset)),
            child: new CupertinoTheme(
                data: new CupertinoThemeData(),
                child: new CupertinoPageScaffold(
                    navigationBar: new DemoNavigationBar(opaque: _opaqueBar),
                    backgroundColor: PageBackground,
                    resizeToAvoidBottomInset: _resizeToAvoidBottomInset,
                    child: new Builder(BuildContent))));
    }

    private Widget BuildContent(BuildContext context)
    {
        MediaQueryData mediaQuery = MediaQuery.Of(context);
        Color label = CupertinoDynamicColor.Resolve(CupertinoColors.Label, context);
        Color secondaryLabel = CupertinoDynamicColor.Resolve(CupertinoColors.SecondaryLabel, context);
        Color cardColor = CupertinoDynamicColor.Resolve(CupertinoColors.SecondarySystemBackground, context);

        return new SingleChildScrollView(
            child: new Padding(
                insets: new Thickness(16.0, mediaQuery.Padding.Top + 16.0, 16.0, 16.0),
                child: new Column(
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    spacing: 10.0,
                    children:
                    [
                        new Text("CupertinoPageScaffold", fontSize: 20.0, color: label),
                        new Text(
                            "The probe bar switches between translucent overlap guidance and opaque content offset.",
                            fontSize: 14.0,
                            color: secondaryLabel),
                        new Container(
                            padding: new Thickness(12.0),
                            decoration: new BoxDecoration(
                                Color: cardColor,
                                BorderRadius: BorderRadius.Circular(10.0)),
                            child: new Column(
                                crossAxisAlignment: CrossAxisAlignment.Stretch,
                                spacing: 6.0,
                                children:
                                [
                                    new Text(
                                        $"bar mode: {(_opaqueBar ? "opaque" : "translucent")}",
                                        fontSize: 13.0,
                                        color: label),
                                    new Text(
                                        $"child MediaQuery.padding.top: {mediaQuery.Padding.Top:0}",
                                        fontSize: 12.0,
                                        color: secondaryLabel),
                                    new Text(
                                        $"child MediaQuery.viewInsets.bottom: {mediaQuery.ViewInsets.Bottom:0}",
                                        fontSize: 12.0,
                                        color: secondaryLabel),
                                ])),
                        BuildAction(
                            _opaqueBar ? "Use translucent bar" : "Use opaque bar",
                            () => SetState(() => _opaqueBar = !_opaqueBar),
                            Color.FromUInt32(0xFFE9F0FF)),
                        BuildAction(
                            _showKeyboardInset ? "Hide simulated keyboard" : "Show simulated keyboard",
                            () => SetState(() => _showKeyboardInset = !_showKeyboardInset),
                            Color.FromUInt32(0xFFEAE4FF)),
                        BuildAction(
                            _resizeToAvoidBottomInset ? "Resize: on" : "Resize: off",
                            () => SetState(() => _resizeToAvoidBottomInset = !_resizeToAvoidBottomInset),
                            Color.FromUInt32(0xFFE8F4E8)),
                        new SizedBox(
                            height: 96.0,
                            child: new Container(
                                color: Color.FromUInt32(0xFFFFF3E0),
                                alignment: Alignment.Center,
                                child: new Text("Bottom inset probe", fontSize: 13.0, color: Colors.Black))),
                    ])));
    }

    private static Widget BuildAction(string label, Action onTap, Color background)
    {
        return new CounterTapButton(
            label: label,
            onTap: onTap,
            background: background,
            foreground: Colors.Black,
            fontSize: 12.0,
            padding: new Thickness(10.0, 8.0));
    }

    private sealed class DemoNavigationBar : StatelessWidget, IObstructingPreferredSizeWidget
    {
        private static readonly CupertinoDynamicColor TranslucentBackground =
            CupertinoDynamicColor.WithBrightness(
                Color.FromUInt32(0xCCFFFFFF),
                Color.FromUInt32(0xCC1C1C1E));

        public DemoNavigationBar(bool opaque)
        {
            Opaque = opaque;
        }

        public bool Opaque { get; }

        public Size PreferredSize => new(double.PositiveInfinity, 52.0);

        public bool ShouldFullyObstruct(BuildContext context) => Opaque;

        public override Widget Build(BuildContext context)
        {
            Color background = Opaque
                ? CupertinoDynamicColor.Resolve(CupertinoColors.SystemBackground, context)
                : CupertinoDynamicColor.Resolve(TranslucentBackground, context);
            Color label = CupertinoDynamicColor.Resolve(CupertinoColors.Label, context);
            return new Container(
                height: 52.0,
                color: background,
                alignment: Alignment.Center,
                child: new Text(Opaque ? "Opaque probe bar" : "Translucent probe bar", color: label));
        }
    }
}
