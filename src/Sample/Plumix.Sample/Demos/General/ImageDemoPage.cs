using System;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix;

// Dart parity source: dart_sample/lib/demos/general/image_demo_page.dart (exact sample parity)

public sealed class ImageDemoPage : StatefulWidget
{
    public override State CreateState() => new ImageDemoPageState();
}

internal sealed class ImageDemoPageState : State
{
    private const string SamplePng =
        "iVBORw0KGgoAAAANSUhEUgAAADAAAAAwCAIAAADYYG7QAAAA+0lEQVR42u3ZwQ3CMAwFUK/CRizAmcUYgXFQz4z" +
        "ADXJClUoT/x9/N0KRfOvBT5Gi2L/2XF5DlU1QFuh6vh0JKu09lQFyUmiWSSkEyxIoEMsyNR6TJWuaJsvX1E3Wo3" +
        "k/7ntFm0yh6TGRoKbGaXKBojScCQZBGo+pAYo9HuKQxgaFXK7O6zZBE/THIP9DHa5Zm4ybN0QaHhT12keCTnapa" +
        "MrXJFDphJYQRGicJhhEU5ws7LUP0VRM8PhxJGhrCtT8NGEjbLhma8K2DjUIXoNGAX1NUhCz2+tAZPohAnXlQ5ka" +
        "b4KWpsEyRjWFTGF1lK6cWkEJTvK59vPnC14fYcoDfci8GWgAAAAASUVORK5CYII=";

    private static readonly byte[] SampleBytes = Convert.FromBase64String(SamplePng);
    private static readonly MemoryImage SampleProvider = new(SampleBytes);
    private ImageStream? _rawStream;
    private ImageStreamListener? _rawListener;
    private ImageInfo? _rawInfo;
    private bool _cover;
    private bool _rtl;
    private bool _dimmed;
    private int _fadeGeneration;
    private byte[] _fadeTargetBytes = (byte[])SampleBytes.Clone();

    public override void DidChangeDependencies()
    {
        if (_rawStream is not null)
        {
            return;
        }

        _rawStream = SampleProvider.Resolve(ImageConfigurationUtils.CreateLocalImageConfiguration(Context));
        _rawListener = new ImageStreamListener((info, _) =>
        {
            if (!Mounted)
            {
                info.Dispose();
                return;
            }

            SetState(() =>
            {
                _rawInfo?.Dispose();
                _rawInfo = info;
            });
        });
        _rawStream.AddListener(_rawListener);
    }

    public override Widget Build(BuildContext context)
    {
        BoxFit fit = _cover ? BoxFit.Cover : BoxFit.Contain;
        double opacity = _dimmed ? 0.45 : 1.0;
        Widget content = new Column(
            crossAxisAlignment: CrossAxisAlignment.Stretch,
            spacing: 12,
            children:
            [
                new Text("Image controls", fontSize: 20, color: Colors.Black),
                new Text(
                    "Provider streams, decoded handles, placeholder cross-fades, and image-backed icons.",
                    fontSize: 14,
                    color: Colors.DimGray),
                new Row(
                    spacing: 8,
                    children:
                    [
                        ControlButton(_cover ? "fit: cover" : "fit: contain", () => _cover = !_cover),
                        ControlButton(_rtl ? "direction: RTL" : "direction: LTR", () => _rtl = !_rtl),
                        ControlButton(_dimmed ? "opacity: 45%" : "opacity: 100%", () => _dimmed = !_dimmed),
                        ControlButton("restart fade", () =>
                        {
                            _fadeGeneration++;
                            _fadeTargetBytes = (byte[])SampleBytes.Clone();
                        }),
                    ]),
                new Expanded(
                    child: new Column(
                        mainAxisAlignment: MainAxisAlignment.SpaceAround,
                        spacing: 10,
                        children:
                        [
                            new Row(
                                mainAxisAlignment: MainAxisAlignment.SpaceAround,
                                children:
                                [
                                    Probe(
                                        "Image.memory",
                                        Plumix.Widgets.Image.Memory(
                                            SampleBytes,
                                            width: 96,
                                            height: 96,
                                            fit: fit,
                                            alignment: AlignmentDirectional.CenterStart,
                                            matchTextDirection: true,
                                            opacity: new AlwaysStoppedAnimation<double>(opacity),
                                            semanticLabel: "Memory image sample",
                                            frameBuilder: (_, child, frame, _) => frame.HasValue
                                                ? child
                                                : new Placeholder(fallbackWidth: 96, fallbackHeight: 96))),
                                    Probe(
                                        "RawImage",
                                        new RawImage(
                                            image: _rawInfo?.Image,
                                            debugImageLabel: _rawInfo?.DebugLabel,
                                            width: 96,
                                            height: 96,
                                            scale: _rawInfo?.Scale ?? 1.0,
                                            fit: fit,
                                            alignment: AlignmentDirectional.CenterStart,
                                            matchTextDirection: true,
                                            opacity: new AlwaysStoppedAnimation<double>(opacity))),
                                ]),
                            new Row(
                                mainAxisAlignment: MainAxisAlignment.SpaceAround,
                                children:
                                [
                                    Probe(
                                        "FadeInImage",
                                        new FadeInImage(
                                            key: new ValueKey<int>(_fadeGeneration),
                                            placeholder: SampleProvider,
                                            image: new MemoryImage(_fadeTargetBytes),
                                            width: 96,
                                            height: 96,
                                            fit: fit,
                                            placeholderColor: Color.Parse("#FF808080"),
                                            color: Color.Parse("#FF4682B4"),
                                            imageSemanticLabel: "Cross-fading image sample")),
                                    Probe(
                                        "ImageIcon",
                                        new IconTheme(
                                            new IconThemeData(
                                                Color: Color.Parse("#FF800080"),
                                                Size: 64,
                                                Opacity: opacity),
                                            new ImageIcon(SampleProvider, semanticLabel: "Image icon sample"))),
                                ]),
                        ])),
                new Text(
                    _rawInfo is null ? "Raw image: decoding..." : "Raw image: decoded handle is active",
                    fontSize: 12,
                    color: Colors.DarkSlateGray),
            ]);
        return new Directionality(_rtl ? Plumix.UI.TextDirection.Rtl : Plumix.UI.TextDirection.Ltr, content);
    }

    public override void Dispose()
    {
        if (_rawStream is not null && _rawListener is not null)
        {
            _rawStream.RemoveListener(_rawListener);
        }

        _rawInfo?.Dispose();
    }

    private Widget ControlButton(string label, Action update)
    {
        return new SizedBox(
            width: 130,
            child: new CounterTapButton(
                label: label,
                onTap: () => SetState(update),
                background: Color.Parse("#FFDCE3ED"),
                foreground: Colors.Black,
                fontSize: 12,
                padding: new Thickness(10, 8)));
    }

    private static Widget Probe(string label, Widget child)
    {
        return new Column(
            spacing: 8,
            children:
            [
                new Text(label, fontSize: 13, color: Colors.Black),
                new Container(
                    width: 150,
                    height: 112,
                    color: Color.Parse("#FFE8EEF5"),
                    alignment: Alignment.Center,
                    child: child),
            ]);
    }
}
