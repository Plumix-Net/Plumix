import 'package:bloc_concurrency/bloc_concurrency.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

class BlocCounterDemoPage extends StatefulWidget {
  const BlocCounterDemoPage({super.key});

  @override
  State<BlocCounterDemoPage> createState() => _BlocCounterDemoPageState();

  static Widget buildActionButton({
    required String label,
    required Color background,
    required VoidCallback onPressed,
  }) {
    return Expanded(
      child: TextButton(
        onPressed: onPressed,
        style: TextButton.styleFrom(
          backgroundColor: background,
          foregroundColor: Colors.black,
          minimumSize: const Size(0, 38),
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
        child: Text(label, style: const TextStyle(fontSize: 12)),
      ),
    );
  }
}

class _BlocCounterDemoPageState extends State<BlocCounterDemoPage> {
  String _milestoneMessage =
      'Milestone listener: waiting for count divisible by 5.';

  @override
  Widget build(BuildContext context) {
    return BlocProvider<BlocCounterBloc>(
      create: (_) => BlocCounterBloc(),
      child: BlocListener<BlocCounterBloc, BlocCounterState>(
        listenWhen: (BlocCounterState previous, BlocCounterState next) =>
            previous.count != next.count &&
            next.count != 0 &&
            next.count % 5 == 0,
        listener: (_, BlocCounterState state) {
          setState(() {
            _milestoneMessage =
                'Milestone listener: count=${state.count} at ${_formatNow()}.';
          });
        },
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          spacing: 10,
          children: <Widget>[
            const Text(
              'Bloc counter demo',
              style: TextStyle(fontSize: 20, color: Colors.black),
            ),
            const Text(
              'BlocProvider + BlocBuilder + BlocListener + BlocSelector. Refresh event uses restartable transformer.',
              style: TextStyle(fontSize: 14, color: Colors.black54),
            ),
            Text(
              _milestoneMessage,
              style: const TextStyle(fontSize: 12, color: Color(0xFF607D8B)),
            ),
            BlocSelector<BlocCounterBloc, BlocCounterState, int>(
              selector: (BlocCounterState state) => state.count,
              builder: (_, int count) => Text(
                'count=$count',
                style: const TextStyle(fontSize: 18, color: Colors.blueGrey),
              ),
            ),
            BlocBuilder<BlocCounterBloc, BlocCounterState>(
              buildWhen: (BlocCounterState previous, BlocCounterState next) =>
                  previous.isLoading != next.isLoading,
              builder: (_, BlocCounterState state) => Text(
                state.isLoading
                    ? 'loading=true (refresh in-flight, restartable)'
                    : 'loading=false',
                style: TextStyle(
                  fontSize: 12,
                  color: state.isLoading
                      ? const Color(0xFF8E24AA)
                      : const Color(0xFF2E7D32),
                ),
              ),
            ),
            const _BlocCounterActionButtons(),
          ],
        ),
      ),
    );
  }

  String _formatNow() {
    final DateTime now = DateTime.now();
    return '${_twoDigits(now.hour)}:${_twoDigits(now.minute)}:${_twoDigits(now.second)}';
  }

  String _twoDigits(int value) => value.toString().padLeft(2, '0');
}

class _BlocCounterActionButtons extends StatelessWidget {
  const _BlocCounterActionButtons();

  @override
  Widget build(BuildContext context) {
    final BlocCounterBloc bloc = context.read<BlocCounterBloc>();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      spacing: 8,
      children: <Widget>[
        Row(
          spacing: 8,
          children: <Widget>[
            BlocCounterDemoPage.buildActionButton(
              label: '-1',
              background: const Color(0xFFECEFF1),
              onPressed: () => bloc.add(const DecrementCounterEvent()),
            ),
            BlocCounterDemoPage.buildActionButton(
              label: '+1',
              background: const Color(0xFFE3F2FD),
              onPressed: () => bloc.add(const IncrementCounterEvent()),
            ),
            BlocCounterDemoPage.buildActionButton(
              label: '+5',
              background: const Color(0xFFE8F5E9),
              onPressed: () => bloc.add(const IncrementByCounterEvent(5)),
            ),
          ],
        ),
        Row(
          spacing: 8,
          children: <Widget>[
            BlocCounterDemoPage.buildActionButton(
              label: 'Refresh +10 (350ms)',
              background: const Color(0xFFFFF3E0),
              onPressed: () => bloc.add(const RefreshCounterEvent(10, 350)),
            ),
            BlocCounterDemoPage.buildActionButton(
              label: 'Refresh +10 (80ms)',
              background: const Color(0xFFF3E5F5),
              onPressed: () => bloc.add(const RefreshCounterEvent(10, 80)),
            ),
          ],
        ),
        BlocCounterDemoPage.buildActionButton(
          label: 'Reset',
          background: const Color(0xFFFFEBEE),
          onPressed: () => bloc.add(const ResetCounterEvent()),
        ),
      ],
    );
  }
}

class BlocCounterState {
  const BlocCounterState({required this.count, required this.isLoading});

  final int count;
  final bool isLoading;

  BlocCounterState copyWith({int? count, bool? isLoading}) {
    return BlocCounterState(
      count: count ?? this.count,
      isLoading: isLoading ?? this.isLoading,
    );
  }
}

sealed class CounterEvent {
  const CounterEvent();
}

final class IncrementCounterEvent extends CounterEvent {
  const IncrementCounterEvent();
}

final class DecrementCounterEvent extends CounterEvent {
  const DecrementCounterEvent();
}

final class IncrementByCounterEvent extends CounterEvent {
  const IncrementByCounterEvent(this.delta);

  final int delta;
}

final class RefreshCounterEvent extends CounterEvent {
  const RefreshCounterEvent(this.delta, this.delayMs);

  final int delta;
  final int delayMs;
}

final class ResetCounterEvent extends CounterEvent {
  const ResetCounterEvent();
}

class BlocCounterBloc extends Bloc<CounterEvent, BlocCounterState> {
  BlocCounterBloc()
    : super(const BlocCounterState(count: 0, isLoading: false)) {
    on<IncrementCounterEvent>(_onIncrement);
    on<DecrementCounterEvent>(_onDecrement);
    on<IncrementByCounterEvent>(_onIncrementBy);
    on<ResetCounterEvent>(_onReset);
    on<RefreshCounterEvent>(_onRefresh, transformer: restartable());
  }

  void _onIncrement(
    IncrementCounterEvent event,
    Emitter<BlocCounterState> emit,
  ) {
    emit(state.copyWith(count: state.count + 1));
  }

  void _onDecrement(
    DecrementCounterEvent event,
    Emitter<BlocCounterState> emit,
  ) {
    emit(state.copyWith(count: state.count - 1));
  }

  void _onIncrementBy(
    IncrementByCounterEvent event,
    Emitter<BlocCounterState> emit,
  ) {
    emit(state.copyWith(count: state.count + event.delta));
  }

  void _onReset(ResetCounterEvent event, Emitter<BlocCounterState> emit) {
    emit(const BlocCounterState(count: 0, isLoading: false));
  }

  Future<void> _onRefresh(
    RefreshCounterEvent event,
    Emitter<BlocCounterState> emit,
  ) async {
    emit(state.copyWith(isLoading: true));
    await Future<void>.delayed(Duration(milliseconds: event.delayMs));
    if (emit.isDone) {
      return;
    }
    emit(BlocCounterState(count: state.count + event.delta, isLoading: false));
  }
}
