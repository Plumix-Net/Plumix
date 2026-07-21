import 'dart:async';

import 'package:flutter/material.dart';

class AsyncBuilderDemoPage extends StatefulWidget {
  const AsyncBuilderDemoPage({super.key});

  @override
  State<AsyncBuilderDemoPage> createState() => _AsyncBuilderDemoPageState();
}

class _AsyncBuilderDemoPageState extends State<AsyncBuilderDemoPage> {
  Completer<String>? _futureCompleter;
  Future<String>? _future;
  StreamController<int> _streamController = StreamController<int>();
  int _nextStreamValue = 0;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 16,
        children: <Widget>[
          const Text(
            'FutureBuilder + StreamBuilder',
            style: TextStyle(fontSize: 20, color: Colors.black),
          ),
          const Text(
            'Restart either source, then complete it with data or an error. '
            'Snapshots retain the previous value while a replacement source '
            'enters waiting.',
            style: TextStyle(fontSize: 14, color: Colors.black54),
          ),
          _buildFutureSection(),
          _buildStreamSection(),
        ],
      ),
    );
  }

  @override
  void dispose() {
    _streamController.close();
    super.dispose();
  }

  Widget _buildFutureSection() {
    return Container(
      color: const Color(0xFFF4F7FA),
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 10,
        children: <Widget>[
          const Text(
            'FutureBuilder',
            style: TextStyle(fontSize: 16, color: Colors.black),
          ),
          FutureBuilder<String>(
            future: _future,
            initialData: 'No result yet',
            builder: _buildFutureSnapshot,
          ),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: <Widget>[
              TextButton(
                onPressed: _restartFuture,
                child: const Text('Restart future'),
              ),
              TextButton(
                onPressed: _completeFuture,
                child: const Text('Complete'),
              ),
              TextButton(onPressed: _failFuture, child: const Text('Fail')),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildStreamSection() {
    return Container(
      color: const Color(0xFFF4F7FA),
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        spacing: 10,
        children: <Widget>[
          const Text(
            'StreamBuilder',
            style: TextStyle(fontSize: 16, color: Colors.black),
          ),
          StreamBuilder<int>(
            stream: _streamController.stream,
            initialData: 0,
            builder: _buildStreamSnapshot,
          ),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: <Widget>[
              TextButton(
                onPressed: _restartStream,
                child: const Text('Restart stream'),
              ),
              TextButton(
                onPressed: _addStreamValue,
                child: const Text('Add value'),
              ),
              TextButton(
                onPressed: _addStreamError,
                child: const Text('Add error'),
              ),
              TextButton(onPressed: _closeStream, child: const Text('Close')),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildFutureSnapshot(
    BuildContext context,
    AsyncSnapshot<String> snapshot,
  ) {
    final String value = snapshot.hasError
        ? 'error: ${snapshot.error}'
        : 'data: ${snapshot.data ?? 'null'}';
    return Text(
      'state: ${snapshot.connectionState.name} · $value',
      style: const TextStyle(color: Color(0xFF31506F)),
    );
  }

  Widget _buildStreamSnapshot(
    BuildContext context,
    AsyncSnapshot<int> snapshot,
  ) {
    final String value = snapshot.hasError
        ? 'error: ${snapshot.error}'
        : snapshot.hasData
        ? 'data: ${snapshot.data}'
        : 'data: null';
    return Text(
      'state: ${snapshot.connectionState.name} · $value',
      style: const TextStyle(color: Color(0xFF31506F)),
    );
  }

  void _restartFuture() {
    setState(() {
      _futureCompleter = Completer<String>();
      _future = _futureCompleter!.future;
    });
  }

  void _completeFuture() {
    final Completer<String>? completer = _futureCompleter;
    if (completer != null && !completer.isCompleted) {
      completer.complete('Future completed');
    }
  }

  void _failFuture() {
    final Completer<String>? completer = _futureCompleter;
    if (completer != null && !completer.isCompleted) {
      completer.completeError(StateError('Future failed'));
    }
  }

  void _restartStream() {
    final StreamController<int> previous = _streamController;
    setState(() {
      _streamController = StreamController<int>();
      _nextStreamValue = 0;
    });
    previous.close();
  }

  void _addStreamValue() {
    if (_streamController.isClosed) {
      return;
    }
    _nextStreamValue += 1;
    _streamController.add(_nextStreamValue);
  }

  void _addStreamError() {
    if (!_streamController.isClosed) {
      _streamController.addError(StateError('Stream failed'));
    }
  }

  void _closeStream() {
    _streamController.close();
  }
}
