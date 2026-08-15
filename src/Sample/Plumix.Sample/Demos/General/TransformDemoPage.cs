using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Transform = Plumix.Widgets.Transform;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/transform_demo_page.dart (exact sample parity)

public sealed class TransformDemoPage : StatefulWidget
{
    public override State CreateState() => new TransformDemoPageState();
}

internal sealed class TransformDemoPageState : State
{
    private double _turns;
    private double _scale = 1.0;
    private double _perspectiveTurns;
    private bool _flipX;
    private bool _flipY;
    private bool _alignTopLeft;

    public override Widget Build(BuildContext context)
    {
        Alignment alignment = _alignTopLeft ? Alignment.TopLeft : Alignment.Center;

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("Transform + Matrix4", fontSize: 20, color: Colors.Black),
                new Text(
                    "Transform carries a full 4x4 matrix, so rotations about the X/Y axes and a "
                    + "perspective row render and hit-test like Flutter's.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton("Rotate", () => SetState(() => _turns += 0.125)),
                        BuildButton("Scale", () => SetState(() => _scale = _scale >= 1.5 ? 0.5 : _scale + 0.25)),
                        BuildButton(_alignTopLeft ? "Anchor: top left" : "Anchor: center",
                            () => SetState(() => _alignTopLeft = !_alignTopLeft)),
                    ]),
                new Row(
                    spacing: 8,
                    children:
                    [
                        BuildButton(_flipX ? "Flip X: on" : "Flip X: off", () => SetState(() => _flipX = !_flipX)),
                        BuildButton(_flipY ? "Flip Y: on" : "Flip Y: off", () => SetState(() => _flipY = !_flipY)),
                        BuildButton("Perspective", () => SetState(() => _perspectiveTurns += 0.08)),
                    ]),
                new Text(
                    $"turns={_turns:0.000}, scale={_scale:0.00}, perspective={_perspectiveTurns:0.00}",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
                new Row(
                    spacing: 16,
                    children:
                    [
                        BuildStage(
                            "rotate + scale",
                            Transform.Rotate(
                                angle: _turns * Math.PI * 2.0,
                                alignment: alignment,
                                child: Transform.Scale(
                                    scale: _scale,
                                    alignment: alignment,
                                    child: BuildCard("Card", Color.Parse("#FF1565C0"))))),
                        BuildStage(
                            "flip",
                            Transform.Flip(
                                flipX: _flipX,
                                flipY: _flipY,
                                child: BuildCard("Flip", Color.Parse("#FF2E7D32")))),
                    ]),
                new Row(
                    spacing: 16,
                    children:
                    [
                        BuildStage("perspective rotateY", new Transform(
                            transform: BuildPerspectiveTransform(),
                            alignment: Alignment.Center,
                            child: BuildCard("3D", Color.Parse("#FFF57C00")))),
                        BuildStage("translate", Transform.Translate(
                            offset: new Point(_turns * 40.0, 0.0),
                            child: BuildCard("Move", Color.Parse("#FF6750A4")))),
                    ]),
            ]);
    }

    private Matrix4 BuildPerspectiveTransform()
    {
        Matrix4 transform = Matrix4.Identity();
        transform.SetEntry(3, 2, 0.002);
        transform.RotateY(_perspectiveTurns * Math.PI * 2.0);
        return transform;
    }

    private static Widget BuildStage(string label, Widget child)
    {
        return new Column(
            spacing: 6,
            children:
            [
                new Text(label, fontSize: 12, color: Colors.DarkSlateGray),
                new Container(
                    width: 150,
                    height: 120,
                    color: Color.Parse("#FFF3F6FA"),
                    alignment: Alignment.Center,
                    child: child),
            ]);
    }

    private static Widget BuildCard(string label, Color color)
    {
        return new Container(
            width: 90,
            height: 56,
            color: color,
            alignment: Alignment.Center,
            child: new Text(label, fontSize: 14, color: Colors.White));
    }

    private static Widget BuildButton(string label, Action onTap)
    {
        return new SizedBox(
            width: 140,
            child: new CounterTapButton(
                label: label,
                onTap: onTap,
                background: Color.Parse("#FFDCE3ED"),
                foreground: Colors.Black,
                fontSize: 12,
                padding: new Thickness(10, 8)));
    }
}
