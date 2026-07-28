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
  bool _filtersEnabled = true;
  int _fadeGeneration = 0;
  late Uint8List _fadeTargetBytes = Uint8List.fromList(_sampleBytes);

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
          const Text('Image controls', style: TextStyle(fontSize: 20)),
          const Text(
            'Provider streams, decoded handles, placeholder cross-fades, and image-backed icons.',
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
              _controlButton(
                _filtersEnabled ? 'filters: on' : 'filters: off',
                () => _filtersEnabled = !_filtersEnabled,
              ),
              _controlButton('restart fade', () {
                _fadeGeneration++;
                _fadeTargetBytes = Uint8List.fromList(_sampleBytes);
              }),
            ],
          ),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              spacing: 10,
              children: <Widget>[
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceAround,
                  children: <Widget>[
                    _probe(
                      'Image.memory',
                      Image.memory(
                        _sampleBytes,
                        width: 96,
                        height: 96,
                        fit: fit,
                        alignment: AlignmentDirectional.centerStart,
                        matchTextDirection: true,
                        opacity: AlwaysStoppedAnimation<double>(opacity),
                        semanticLabel: 'Memory image sample',
                        frameBuilder: (context, child, frame, synchronous) {
                          return frame != null
                              ? child
                              : const Placeholder(
                                  fallbackWidth: 96,
                                  fallbackHeight: 96,
                                );
                        },
                      ),
                    ),
                    _probe(
                      'RawImage',
                      RawImage(
                        image: _rawImage,
                        width: 96,
                        height: 96,
                        fit: fit,
                        alignment: AlignmentDirectional.centerStart,
                        matchTextDirection: true,
                        opacity: AlwaysStoppedAnimation<double>(opacity),
                      ),
                    ),
                  ],
                ),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceAround,
                  children: <Widget>[
                    _probe(
                      'FadeInImage',
                      FadeInImage(
                        key: ValueKey<int>(_fadeGeneration),
                        placeholder: MemoryImage(_sampleBytes),
                        image: MemoryImage(_fadeTargetBytes),
                        width: 96,
                        height: 96,
                        fit: fit,
                        placeholderColor: const Color(0xFF808080),
                        color: const Color(0xFF4682B4),
                        imageSemanticLabel: 'Cross-fading image sample',
                      ),
                    ),
                    _probe(
                      'ImageIcon',
                      IconTheme(
                        data: IconThemeData(
                          color: const Color(0xFF800080),
                          size: 64,
                          opacity: opacity,
                        ),
                        child: ImageIcon(
                          MemoryImage(_sampleBytes),
                          semanticLabel: 'Image icon sample',
                        ),
                      ),
                    ),
                  ],
                ),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceAround,
                  children: <Widget>[
                    _probe(
                      'ColorFiltered',
                      _filtersEnabled
                          ? ColorFiltered(
                              colorFilter: const ColorFilter.matrix(<double>[
                                0.2126,
                                0.7152,
                                0.0722,
                                0,
                                0,
                                0.2126,
                                0.7152,
                                0.0722,
                                0,
                                0,
                                0.2126,
                                0.7152,
                                0.0722,
                                0,
                                0,
                                0,
                                0,
                                0,
                                1,
                                0,
                              ]),
                              child: Image.memory(
                                _sampleBytes,
                                width: 96,
                                height: 96,
                                fit: fit,
                              ),
                            )
                          : Image.memory(
                              _sampleBytes,
                              width: 96,
                              height: 96,
                              fit: fit,
                            ),
                    ),
                    _probe(
                      'ImageFiltered',
                      ImageFiltered(
                        imageFilter: ui.ImageFilter.blur(
                          sigmaX: 3,
                          sigmaY: 3,
                          tileMode: ui.TileMode.decal,
                        ),
                        enabled: _filtersEnabled,
                        child: Image.memory(
                          _sampleBytes,
                          width: 96,
                          height: 96,
                          fit: fit,
                        ),
                      ),
                    ),
                  ],
                ),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceAround,
                  children: <Widget>[
                    _probe(
                      'ShaderMask',
                      _filtersEnabled
                          ? ShaderMask(
                              shaderCallback: (Rect bounds) {
                                return const LinearGradient(
                                  colors: <Color>[Colors.yellow, Colors.purple],
                                ).createShader(bounds);
                              },
                              blendMode: BlendMode.modulate,
                              child: Image.memory(
                                _sampleBytes,
                                width: 96,
                                height: 96,
                                fit: fit,
                              ),
                            )
                          : Image.memory(
                              _sampleBytes,
                              width: 96,
                              height: 96,
                              fit: fit,
                            ),
                    ),
                    _probe(
                      'BackdropFilter.grouped',
                      BackdropGroup(
                        child: SizedBox(
                          width: 96,
                          height: 96,
                          child: Stack(
                            children: <Widget>[
                              Image.memory(
                                _sampleBytes,
                                width: 96,
                                height: 96,
                                fit: BoxFit.cover,
                              ),
                              Center(
                                child: ClipRect(
                                  child: BackdropFilter.grouped(
                                    filterConfig: const ImageFilterConfig.blur(
                                      sigmaX: 5,
                                      sigmaY: 5,
                                      bounded: true,
                                    ),
                                    enabled: _filtersEnabled,
                                    child: Container(
                                      width: 64,
                                      height: 64,
                                      color: const Color(0x30FFFFFF),
                                    ),
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                  ],
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
          width: 150,
          height: 112,
          color: const Color(0xFFE8EEF5),
          alignment: Alignment.center,
          child: child,
        ),
      ],
    );
  }
}
