using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

// Dart parity source: dart_sample/lib/demos/material/circle_avatar_demo_page.dart (exact sample parity)

namespace Plumix;

public sealed class CircleAvatarDemoPage : StatefulWidget
{
    public override State CreateState() => new CircleAvatarDemoPageState();
}

internal sealed class CircleAvatarDemoPageState : State
{
    private const string FacePng =
        "iVBORw0KGgoAAAANSUhEUgAAADAAAAAwCAIAAADYYG7QAAAA+0lEQVR42u3ZwQ3CMAwFUK/CRizAmcUYgXFQz4zADXJClUoT/x9/N0KRfOvBT5Gi2L/2XF5DlU1QFuh6vh0JKu09lQFyUmiWSSkEyxIoEMsyNR6TJWuaJsvX1E3Wo3k/7ntFm0yh6TGRoKbGaXKBojScCQZBGo+pAYo9HuKQxgaFXK7O6zZBE/THIP9DHa5Zm4ybN0QaHhT12keCTnapaMrXJFDphJYQRGicJhhEU5ws7LUP0VRM8PhxJGhrCtT8NGEjbLhma8K2DjUIXoNGAX1NUhCz2+tAZPohAnXlQ5kab4KWpsEyRjWFTGF1lK6cWkEJTvK59vPnC14fYcoDfci8GWgAAAAASUVORK5CYII=";
    private const string StarPng =
        "iVBORw0KGgoAAAANSUhEUgAAADAAAAAwCAIAAADYYG7QAAAAwklEQVR42u2Y3RGAIAyDGc1BXMCJHUcXUGiTxp8jd30M9DsILdDatn4rDDQJ0LEvkZgYKML0tIcMxDG9c+wnBorPyNBcKztppEC3ymGOcqCBOJKDSpCdPOjTElOHxKlyBxfGRAnNtkyguebaMNDDFWEgA/0KSNvtDWSgghujkOb3K/TEyVejpLF4GkaMAAEJKCZym2oH9oAYW1CWKl8edrjof6N+y0h74r6GT5lqoO4Br6pD2JMZ0FPNVSH2T76BauIEUZcWnwhUP8AAAAAASUVORK5CYII=";

    private static readonly MemoryImage FaceImage = new(Convert.FromBase64String(FacePng));
    private static readonly MemoryImage StarImage = new(Convert.FromBase64String(StarPng));
    private static readonly MemoryImage BrokenImage = new([0]);

    private bool _largeRadius;
    private bool _showForeground = true;
    private bool _breakForeground;
    private string _status = "Foreground image loaded";

    public override Widget Build(BuildContext context)
    {
        double radius = _largeRadius ? 38.0 : 26.0;
        var foreground = _breakForeground
            ? BrokenImage
            : _showForeground
                ? StarImage
                : null;

        return new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 14,
            children:
            [
                new Text("CircleAvatar", fontSize: 20, color: Colors.Black),
                new Text(
                    "Initials, theme colors, animated radius, foreground/background images, and error fallback.",
                    fontSize: 14,
                    color: Color.Parse("#8A000000")),
                new Row(
                    spacing: 8,
                    children:
                    [
                        ControlButton(
                            _largeRadius ? "Radius 38" : "Radius 26",
                            () => SetState(() => _largeRadius = !_largeRadius)),
                        ControlButton(
                            _showForeground ? "Foreground on" : "Foreground off",
                            () => SetState(() =>
                            {
                                _showForeground = !_showForeground;
                                _breakForeground = false;
                                _status = _showForeground ? "Foreground image loaded" : "Background image only";
                            })),
                        ControlButton(
                            _breakForeground ? "Fallback active" : "Break foreground",
                            () => SetState(() =>
                            {
                                _breakForeground = !_breakForeground;
                                _showForeground = true;
                                _status = _breakForeground ? "Waiting for image error..." : "Foreground image loaded";
                            })),
                    ]),
                new Container(
                    color: Color.Parse("#FFF7F2FA"),
                    padding: new Thickness(20),
                    child: new Row(
                        mainAxisAlignment: MainAxisAlignment.SpaceAround,
                        children:
                        [
                            Probe("Theme defaults", new CircleAvatar(child: new Text("ER"))),
                            Probe("Color + radius", new CircleAvatar(
                                radius: radius,
                                backgroundColor: Color.Parse("#FF00695C"),
                                foregroundColor: Colors.White,
                                child: new Text("42"))),
                            Probe("Image layers", new CircleAvatar(
                                radius: radius,
                                backgroundImage: FaceImage,
                                foregroundImage: _showForeground ? StarImage : null)),
                            Probe("Error fallback", new CircleAvatar(
                                radius: radius,
                                backgroundImage: FaceImage,
                                foregroundImage: foreground,
                                onForegroundImageError: foreground is null ? null : HandleForegroundError)),
                        ])),
                new Text(_status, fontSize: 13, color: Color.Parse("#FF49454F")),
            ]);
    }

    private void HandleForegroundError(Exception exception, System.Diagnostics.StackTrace? stackTrace)
    {
        if (!_breakForeground || _status == "Foreground error -> background fallback") return;
        SetState(() => _status = "Foreground error -> background fallback");
    }

    private static Widget Probe(string label, Widget avatar)
    {
        return new Column(
            mainAxisSize: MainAxisSize.Min,
            spacing: 8,
            children: [avatar, new Text(label, fontSize: 12, color: Colors.Black)]);
    }

    private static Widget ControlButton(string label, Action onPressed)
    {
        return new TextButton(
            onPressed: onPressed,
            backgroundColor: Color.Parse("#FFEADDFF"),
            foregroundColor: Color.Parse("#FF21005D"),
            minHeight: 36,
            child: new Text(label, fontSize: 12));
    }
}
