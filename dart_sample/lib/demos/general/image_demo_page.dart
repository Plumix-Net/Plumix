import 'dart:convert';
import 'dart:typed_data';
import 'dart:ui' as ui;

import 'package:flutter/material.dart';

import '../../counter_widgets.dart';

class ImageDemoPage extends StatefulWidget {
  const ImageDemoPage({super.key});

  @override
  State<ImageDemoPage> createState() => _ImageDemoPageState();
}

class _ImageDemoPageState extends State<ImageDemoPage> {
  static const String _samplePng =
      'iVBORw0KGgoAAAANSUhEUgAAADAAAAAwCAIAAADYYG7QAAAA+0lEQVR42u3ZwQ3CMAwFUK/CRizAmcUYgXFQz4zADXJClUoT/x9/N0KRfOvBT5Gi2L/2XF5DlU1QFuh6vh0JKu09lQFyUmiWSSkEyxIoEMsyNR6TJWuaJsvX1E3Wo3k/7ntFm0yh6TGRoKbGaXKBojScCQZBGo+pAYo9HuKQxgaFXK7O6zZBE/THIP9DHa5Zm4ybN0QaHhT12keCTnapaMrXJFDphJYQRGicJhhEU5ws7LUP0VRM8PhxJGhrCtT8NGEjbLhma8K2DjUIXoNGAX1NUhCz2+tAZPohAnXlQ5kab4KWpsEyRjWFTGF1lK6cWkEJTvK59vPnC14fYcoDfci8GWgAAAAASUVORK5CYII=';

  late final Uint8List _sampleBytes = base64Decode(_samplePng);
  ui.Image? _rawImage;
  bool _cover = false;
  bool _rtl = false;
  bool _dimmed = false;

  @override
  void initState() {
    super.initState();
    _decodeRawImage();
  }

  Future<void> _decodeRawImage() async {
    final ui.Image image = await decodeImageFromList(base64Decode(_samplePng));
    if (!mounted) {
      image.dispose();
      return;
    }
    setState(() {
      _rawImage = image;
    });
  }

  @override
  void dispose() {
    _rawImage?.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final BoxFit fit = _cover ? BoxFit.cover : BoxFit.contain;
    final double opacity = _dimmed ? 0.45 : 1;
    return Directionality(
      textDirection: _rtl ? TextDirection.rtl : TextDirection.ltr,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 12,
        children: <Widget>[
          const Text('Image + RawImage', style: TextStyle(fontSize: 20)),
          const Text(
            'Image owns provider/stream state and builders; RawImage paints an already decoded image.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          Row(
            spacing: 8,
            children: <Widget>[
              _controlButton(
                _cover ? 'fit: cover' : 'fit: contain',
                () => _cover = !_cover,
              ),
              _controlButton(
                _rtl ? 'direction: RTL' : 'direction: LTR',
                () => _rtl = !_rtl,
              ),
              _controlButton(
                _dimmed ? 'opacity: 45%' : 'opacity: 100%',
                () => _dimmed = !_dimmed,
              ),
            ],
          ),
          Expanded(
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: <Widget>[
                _probe(
                  'Image.memory',
                  Image.memory(
                    _sampleBytes,
                    width: 150,
                    height: 150,
                    fit: fit,
                    alignment: AlignmentDirectional.centerStart,
                    matchTextDirection: true,
                    opacity: AlwaysStoppedAnimation<double>(opacity),
                    semanticLabel: 'Memory image sample',
                    frameBuilder: (context, child, frame, synchronous) {
                      return frame != null
                          ? child
                          : const Placeholder(
                              fallbackWidth: 150,
                              fallbackHeight: 150,
                            );
                    },
                  ),
                ),
                _probe(
                  'RawImage',
                  RawImage(
                    image: _rawImage,
                    width: 150,
                    height: 150,
                    fit: fit,
                    alignment: AlignmentDirectional.centerStart,
                    matchTextDirection: true,
                    opacity: AlwaysStoppedAnimation<double>(opacity),
                  ),
                ),
              ],
            ),
          ),
          Text(
            _rawImage == null
                ? 'Raw image: decoding...'
                : 'Raw image: decoded handle is active',
            style: const TextStyle(fontSize: 12, color: Colors.blueGrey),
          ),
        ],
      ),
    );
  }

  Widget _controlButton(String label, VoidCallback update) {
    return SizedBox(
      width: 130,
      child: CounterTapButton(
        label: label,
        onTap: () {
          setState(update);
        },
        background: const Color(0xFFDCE3ED),
        foreground: Colors.black,
        fontSize: 12,
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      ),
    );
  }

  static Widget _probe(String label, Widget child) {
    return Column(
      spacing: 8,
      children: <Widget>[
        Text(label, style: const TextStyle(fontSize: 13)),
        Container(
          width: 180,
          height: 180,
          color: const Color(0xFFE8EEF5),
          alignment: Alignment.center,
          child: child,
        ),
      ],
    );
  }
}
