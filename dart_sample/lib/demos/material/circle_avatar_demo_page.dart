import 'dart:convert';
import 'dart:typed_data';

import 'package:flutter/material.dart';

class CircleAvatarDemoPage extends StatefulWidget {
  const CircleAvatarDemoPage({super.key});

  @override
  State<CircleAvatarDemoPage> createState() => _CircleAvatarDemoPageState();
}

class _CircleAvatarDemoPageState extends State<CircleAvatarDemoPage> {
  static const String _facePng =
      'iVBORw0KGgoAAAANSUhEUgAAADAAAAAwCAIAAADYYG7QAAAA+0lEQVR42u3ZwQ3CMAwFUK/CRizAmcUYgXFQz4zADXJClUoT/x9/N0KRfOvBT5Gi2L/2XF5DlU1QFuh6vh0JKu09lQFyUmiWSSkEyxIoEMsyNR6TJWuaJsvX1E3Wo3k/7ntFm0yh6TGRoKbGaXKBojScCQZBGo+pAYo9HuKQxgaFXK7O6zZBE/THIP9DHa5Zm4ybN0QaHhT12keCTnapaMrXJFDphJYQRGicJhhEU5ws7LUP0VRM8PhxJGhrCtT8NGEjbLhma8K2DjUIXoNGAX1NUhCz2+tAZPohAnXlQ5kab4KWpsEyRjWFTGF1lK6cWkEJTvK59vPnC14fYcoDfci8GWgAAAAASUVORK5CYII=';
  static const String _starPng =
      'iVBORw0KGgoAAAANSUhEUgAAADAAAAAwCAIAAADYYG7QAAAAwklEQVR42u2Y3RGAIAyDGc1BXMCJHUcXUGiTxp8jd30M9DsILdDatn4rDDQJ0LEvkZgYKML0tIcMxDG9c+wnBorPyNBcKztppEC3ymGOcqCBOJKDSpCdPOjTElOHxKlyBxfGRAnNtkyguebaMNDDFWEgA/0KSNvtDWSgghujkOb3K/TEyVejpLF4GkaMAAEJKCZym2oH9oAYW1CWKl8edrjof6N+y0h74r6GT5lqoO4Br6pD2JMZ0FPNVSH2T76BauIEUZcWnwhUP8AAAAAASUVORK5CYII=';

  static final MemoryImage _faceImage = MemoryImage(base64Decode(_facePng));
  static final MemoryImage _starImage = MemoryImage(base64Decode(_starPng));
  static final MemoryImage _brokenImage = MemoryImage(
    Uint8List.fromList(<int>[0]),
  );

  bool _largeRadius = false;
  bool _showForeground = true;
  bool _breakForeground = false;
  String _status = 'Foreground image loaded';

  @override
  Widget build(BuildContext context) {
    final double radius = _largeRadius ? 38 : 26;
    final ImageProvider<Object>? foreground = _breakForeground
        ? _brokenImage
        : _showForeground
        ? _starImage
        : null;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        const Text('CircleAvatar', style: TextStyle(fontSize: 20)),
        const SizedBox(height: 14),
        const Text(
          'Initials, theme colors, animated radius, foreground/background images, and error fallback.',
          style: TextStyle(fontSize: 14, color: Color(0x8A000000)),
        ),
        const SizedBox(height: 14),
        Row(
          children: <Widget>[
            _controlButton(
              _largeRadius ? 'Radius 38' : 'Radius 26',
              () => setState(() => _largeRadius = !_largeRadius),
            ),
            const SizedBox(width: 8),
            _controlButton(
              _showForeground ? 'Foreground on' : 'Foreground off',
              () => setState(() {
                _showForeground = !_showForeground;
                _breakForeground = false;
                _status = _showForeground
                    ? 'Foreground image loaded'
                    : 'Background image only';
              }),
            ),
            const SizedBox(width: 8),
            _controlButton(
              _breakForeground ? 'Fallback active' : 'Break foreground',
              () => setState(() {
                _breakForeground = !_breakForeground;
                _showForeground = true;
                _status = _breakForeground
                    ? 'Waiting for image error...'
                    : 'Foreground image loaded';
              }),
            ),
          ],
        ),
        const SizedBox(height: 14),
        Container(
          color: const Color(0xFFF7F2FA),
          padding: const EdgeInsets.all(20),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceAround,
            children: <Widget>[
              _probe('Theme defaults', const CircleAvatar(child: Text('ER'))),
              _probe(
                'Color + radius',
                CircleAvatar(
                  radius: radius,
                  backgroundColor: const Color(0xFF00695C),
                  foregroundColor: Colors.white,
                  child: const Text('42'),
                ),
              ),
              _probe(
                'Image layers',
                CircleAvatar(
                  radius: radius,
                  backgroundImage: _faceImage,
                  foregroundImage: _showForeground ? _starImage : null,
                ),
              ),
              _probe(
                'Error fallback',
                CircleAvatar(
                  radius: radius,
                  backgroundImage: _faceImage,
                  foregroundImage: foreground,
                  onForegroundImageError: foreground == null
                      ? null
                      : _handleForegroundError,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 14),
        Text(
          _status,
          style: const TextStyle(fontSize: 13, color: Color(0xFF49454F)),
        ),
      ],
    );
  }

  void _handleForegroundError(Object exception, StackTrace? stackTrace) {
    if (!_breakForeground ||
        _status == 'Foreground error -> background fallback') {
      return;
    }
    setState(() => _status = 'Foreground error -> background fallback');
  }

  Widget _probe(String label, Widget avatar) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        avatar,
        const SizedBox(height: 8),
        Text(label, style: const TextStyle(fontSize: 12)),
      ],
    );
  }

  Widget _controlButton(String label, VoidCallback onPressed) {
    return TextButton(
      onPressed: onPressed,
      style: TextButton.styleFrom(
        backgroundColor: const Color(0xFFEADDFF),
        foregroundColor: const Color(0xFF21005D),
        minimumSize: const Size(0, 36),
      ),
      child: Text(label, style: const TextStyle(fontSize: 12)),
    );
  }
}
