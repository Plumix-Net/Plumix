using Plumix.UI;

namespace Plumix.Impeller;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/lib/ui/window/pointer_data_packet_converter.cc (and pointer_data.h): the engine
// side between an embedder's raw pointer packets and dart:ui. It keeps each device's state, synthesizes the
// add, hover, move, cancel and remove events the framework expects, gives every down (or pan-zoom start) a new
// pointer identifier and computes the physical and pan deltas. Embedders fill `EnginePointerData` the way they
// fill the engine's packet; `ToPointerData` is dart:ui's unpacking.

/// <summary>The engine's mutable <c>flutter::PointerData</c>, as an embedder writes it into a packet.</summary>
internal struct EnginePointerData
{
    public int ViewId;
    public TimeSpan TimeStamp;
    public PointerChange Change;
    public PointerDeviceKind Kind;
    public PointerSignalKind SignalKind;
    public int Device;
    public int PointerIdentifier;
    public double PhysicalX;
    public double PhysicalY;
    public double PhysicalDeltaX;
    public double PhysicalDeltaY;
    public long Buttons;
    public bool Obscured;
    public bool Synthesized;
    public double Pressure;
    public double PressureMin;
    public double PressureMax;
    public double Distance;
    public double DistanceMax;
    public double Size;
    public double RadiusMajor;
    public double RadiusMinor;
    public double RadiusMin;
    public double RadiusMax;
    public double Orientation;
    public double Tilt;
    public int PlatformData;
    public double ScrollDeltaX;
    public double ScrollDeltaY;
    public double PanX;
    public double PanY;
    public double PanDeltaX;
    public double PanDeltaY;
    public double Scale;
    public double Rotation;

    /// <summary>dart:ui's <c>_unpackPointerDataPacket</c> for one entry.</summary>
    public readonly PointerData ToPointerData() => new(
        viewId: ViewId,
        timeStamp: TimeStamp,
        change: Change,
        kind: Kind,
        signalKind: SignalKind,
        device: Device,
        pointerIdentifier: PointerIdentifier,
        physicalX: PhysicalX,
        physicalY: PhysicalY,
        physicalDeltaX: PhysicalDeltaX,
        physicalDeltaY: PhysicalDeltaY,
        buttons: (PointerButtons)Buttons,
        obscured: Obscured,
        synthesized: Synthesized,
        pressure: Pressure,
        pressureMin: PressureMin,
        pressureMax: PressureMax,
        distance: Distance,
        distanceMax: DistanceMax,
        size: Size,
        radiusMajor: RadiusMajor,
        radiusMinor: RadiusMinor,
        radiusMin: RadiusMin,
        radiusMax: RadiusMax,
        orientation: Orientation,
        tilt: Tilt,
        platformData: PlatformData,
        scrollDeltaX: ScrollDeltaX,
        scrollDeltaY: ScrollDeltaY,
        panX: PanX,
        panY: PanY,
        panDeltaX: PanDeltaX,
        panDeltaY: PanDeltaY,
        scale: Scale,
        rotation: Rotation);
}

/// <summary>The engine's <c>PointerDataPacketConverter</c>.</summary>
internal sealed class PointerDataPacketConverter(Func<int, bool> viewExists)
{
    private readonly Dictionary<int, PointerState> _states = [];
    private int _pointer;

    /// <summary>Converts an embedder's packet into the packet dart:ui hands the framework.</summary>
    public PointerDataPacket Convert(IEnumerable<EnginePointerData> packet)
    {
        List<PointerData> converted = [];
        foreach (EnginePointerData pointerData in packet)
        {
            ConvertPointerData(pointerData, converted);
        }

        return new PointerDataPacket(converted);
    }

    private void ConvertPointerData(EnginePointerData pointerData, List<PointerData> converted)
    {
        if (!viewExists(pointerData.ViewId))
        {
            return;
        }

        if (pointerData.SignalKind == PointerSignalKind.None)
        {
            ConvertChange(pointerData, converted);
        }
        else if (pointerData.SignalKind is PointerSignalKind.Scroll or PointerSignalKind.ScrollInertiaCancel
                 or PointerSignalKind.Scale)
        {
            ConvertSignal(pointerData, converted);
        }
    }

    private void ConvertChange(EnginePointerData pointerData, List<PointerData> converted)
    {
        PointerState state;
        switch (pointerData.Change)
        {
            case PointerChange.Cancel:
                if (_states.TryGetValue(pointerData.Device, out state))
                {
                    UpdatePointerIdentifier(ref pointerData, ref state, startNewPointer: false);
                    if (LocationNeedsUpdate(pointerData, state))
                    {
                        EnginePointerData synthesizedMove = pointerData;
                        synthesizedMove.Change = PointerChange.Move;
                        synthesizedMove.Synthesized = true;
                        UpdateDeltaAndState(ref synthesizedMove, ref state);
                        converted.Add(synthesizedMove.ToPointerData());
                    }

                    state.IsDown = false;
                    _states[pointerData.Device] = state;
                    converted.Add(pointerData.ToPointerData());
                }

                break;
            case PointerChange.Add:
                if (_states.TryGetValue(pointerData.Device, out state))
                {
                    EnginePointerData synthesized = pointerData;
                    synthesized.PhysicalX = state.PhysicalX;
                    synthesized.PhysicalY = state.PhysicalY;
                    synthesized.PanX = state.PanX;
                    synthesized.PanY = state.PanY;
                    synthesized.Scale = state.Scale;
                    synthesized.Rotation = state.Rotation;
                    synthesized.Buttons = state.Buttons;
                    synthesized.Synthesized = true;
                    if (viewExists(state.ViewId))
                    {
                        synthesized.ViewId = state.ViewId;
                        if (state.IsDown)
                        {
                            EnginePointerData synthesizedCancel = synthesized;
                            synthesizedCancel.Change = PointerChange.Cancel;
                            UpdatePointerIdentifier(ref synthesizedCancel, ref state, startNewPointer: false);
                            state.IsDown = false;
                            converted.Add(synthesizedCancel.ToPointerData());
                        }
                    }

                    EnginePointerData synthesizedRemove = synthesized;
                    synthesizedRemove.Change = PointerChange.Remove;
                    converted.Add(synthesizedRemove.ToPointerData());
                }

                EnsurePointerState(pointerData);
                converted.Add(pointerData.ToPointerData());
                break;
            case PointerChange.Remove:
                if (!_states.TryGetValue(pointerData.Device, out state) || state.ViewId != pointerData.ViewId)
                {
                    break;
                }

                if (state.IsDown)
                {
                    EnginePointerData synthesizedCancel = pointerData;
                    synthesizedCancel.Change = PointerChange.Cancel;
                    synthesizedCancel.Synthesized = true;
                    UpdatePointerIdentifier(ref synthesizedCancel, ref state, startNewPointer: false);
                    state.IsDown = false;
                    _states[synthesizedCancel.Device] = state;
                    converted.Add(synthesizedCancel.ToPointerData());
                }

                if (LocationNeedsUpdate(pointerData, state))
                {
                    EnginePointerData synthesizedHover = pointerData;
                    synthesizedHover.Change = PointerChange.Hover;
                    synthesizedHover.Synthesized = true;
                    UpdateDeltaAndState(ref synthesizedHover, ref state);
                    converted.Add(synthesizedHover.ToPointerData());
                }

                _states.Remove(pointerData.Device);
                converted.Add(pointerData.ToPointerData());
                break;
            case PointerChange.Hover:
                state = StateOrSynthesizedAdd(pointerData, converted);
                state.Buttons = pointerData.Buttons;
                if (LocationNeedsUpdate(pointerData, state))
                {
                    UpdateDeltaAndState(ref pointerData, ref state);
                    converted.Add(pointerData.ToPointerData());
                }

                break;
            case PointerChange.Down:
                state = StateOrSynthesizedAdd(pointerData, converted);
                SynthesizeHoverIfMoved(pointerData, ref state, converted);
                UpdatePointerIdentifier(ref pointerData, ref state, startNewPointer: true);
                state.IsDown = true;
                state.Buttons = pointerData.Buttons;
                _states[pointerData.Device] = state;
                converted.Add(pointerData.ToPointerData());
                break;
            case PointerChange.Move:
                if (!_states.TryGetValue(pointerData.Device, out state))
                {
                    break;
                }

                UpdatePointerIdentifier(ref pointerData, ref state, startNewPointer: false);
                UpdateDeltaAndState(ref pointerData, ref state);
                // As the engine, on its local copy only: UpdateDeltaAndState already stored the state.
                state.Buttons = pointerData.Buttons;
                converted.Add(pointerData.ToPointerData());
                break;
            case PointerChange.Up:
                if (!_states.TryGetValue(pointerData.Device, out state))
                {
                    break;
                }

                UpdatePointerIdentifier(ref pointerData, ref state, startNewPointer: false);
                if (LocationNeedsUpdate(pointerData, state))
                {
                    EnginePointerData synthesizedMove = pointerData;
                    synthesizedMove.Change = PointerChange.Move;
                    synthesizedMove.Buttons = state.Buttons;
                    synthesizedMove.Synthesized = true;
                    UpdateDeltaAndState(ref synthesizedMove, ref state);
                    converted.Add(synthesizedMove.ToPointerData());
                }

                state.IsDown = false;
                state.Buttons = pointerData.Buttons;
                _states[pointerData.Device] = state;
                converted.Add(pointerData.ToPointerData());
                break;
            case PointerChange.PanZoomStart:
                state = StateOrSynthesizedAdd(pointerData, converted);
                SynthesizeHoverIfMoved(pointerData, ref state, converted);
                UpdatePointerIdentifier(ref pointerData, ref state, startNewPointer: true);
                state.IsPanZoomActive = true;
                state.PanX = 0;
                state.PanY = 0;
                state.Scale = 1;
                state.Rotation = 0;
                _states[pointerData.Device] = state;
                converted.Add(pointerData.ToPointerData());
                break;
            case PointerChange.PanZoomUpdate:
                if (!_states.TryGetValue(pointerData.Device, out state))
                {
                    break;
                }

                UpdatePointerIdentifier(ref pointerData, ref state, startNewPointer: false);
                UpdateDeltaAndState(ref pointerData, ref state);
                converted.Add(pointerData.ToPointerData());
                break;
            case PointerChange.PanZoomEnd:
                if (!_states.TryGetValue(pointerData.Device, out state))
                {
                    break;
                }

                UpdatePointerIdentifier(ref pointerData, ref state, startNewPointer: false);
                if (LocationNeedsUpdate(pointerData, state))
                {
                    EnginePointerData synthesizedUpdate = pointerData;
                    synthesizedUpdate.Change = PointerChange.PanZoomUpdate;
                    synthesizedUpdate.PanX = state.PanX;
                    synthesizedUpdate.PanY = state.PanY;
                    synthesizedUpdate.PanDeltaX = 0;
                    synthesizedUpdate.PanDeltaY = 0;
                    synthesizedUpdate.Scale = state.Scale;
                    synthesizedUpdate.Rotation = state.Rotation;
                    synthesizedUpdate.Synthesized = true;
                    UpdateDeltaAndState(ref synthesizedUpdate, ref state);
                    converted.Add(synthesizedUpdate.ToPointerData());
                }

                state.IsPanZoomActive = false;
                _states[pointerData.Device] = state;
                converted.Add(pointerData.ToPointerData());
                break;
            default:
                converted.Add(pointerData.ToPointerData());
                break;
        }
    }

    // Scroll, scroll-inertia-cancel and scale signals: the pointer is added and brought to the signal's position
    // first, with a move while it is down and a hover otherwise.
    private void ConvertSignal(EnginePointerData pointerData, List<PointerData> converted)
    {
        PointerState state;
        if (!_states.TryGetValue(pointerData.Device, out state))
        {
            EnginePointerData synthesizedAdd = pointerData;
            synthesizedAdd.SignalKind = PointerSignalKind.None;
            synthesizedAdd.Change = PointerChange.Add;
            synthesizedAdd.Synthesized = true;
            synthesizedAdd.Buttons = 0;
            state = EnsurePointerState(synthesizedAdd);
            converted.Add(synthesizedAdd.ToPointerData());
        }

        if (LocationNeedsUpdate(pointerData, state))
        {
            EnginePointerData synthesized = pointerData;
            synthesized.SignalKind = PointerSignalKind.None;
            synthesized.Synthesized = true;
            if (state.IsDown)
            {
                synthesized.Change = PointerChange.Move;
                synthesized.Buttons = state.Buttons;
            }
            else
            {
                synthesized.Change = PointerChange.Hover;
                synthesized.Buttons = 0;
            }

            UpdateDeltaAndState(ref synthesized, ref state);
            converted.Add(synthesized.ToPointerData());
        }

        converted.Add(pointerData.ToPointerData());
    }

    // The hover, down and pan-zoom start cases: a device without state is added first.
    private PointerState StateOrSynthesizedAdd(EnginePointerData pointerData, List<PointerData> converted)
    {
        if (_states.TryGetValue(pointerData.Device, out PointerState state))
        {
            return state;
        }

        EnginePointerData synthesizedAdd = pointerData;
        synthesizedAdd.Change = PointerChange.Add;
        synthesizedAdd.Synthesized = true;
        synthesizedAdd.Buttons = 0;
        converted.Add(synthesizedAdd.ToPointerData());
        return EnsurePointerState(synthesizedAdd);
    }

    private void SynthesizeHoverIfMoved(
        EnginePointerData pointerData,
        ref PointerState state,
        List<PointerData> converted)
    {
        if (LocationNeedsUpdate(pointerData, state))
        {
            EnginePointerData synthesizedHover = pointerData;
            synthesizedHover.Change = PointerChange.Hover;
            synthesizedHover.Synthesized = true;
            synthesizedHover.Buttons = 0;
            UpdateDeltaAndState(ref synthesizedHover, ref state);
            converted.Add(synthesizedHover.ToPointerData());
        }
    }

    private PointerState EnsurePointerState(EnginePointerData pointerData)
    {
        var state = new PointerState
        {
            PhysicalX = pointerData.PhysicalX,
            PhysicalY = pointerData.PhysicalY,
            ViewId = pointerData.ViewId,
        };
        _states[pointerData.Device] = state;
        return state;
    }

    private void UpdateDeltaAndState(ref EnginePointerData pointerData, ref PointerState state)
    {
        pointerData.PhysicalDeltaX = pointerData.PhysicalX - state.PhysicalX;
        pointerData.PhysicalDeltaY = pointerData.PhysicalY - state.PhysicalY;
        pointerData.PanDeltaX = pointerData.PanX - state.PanX;
        pointerData.PanDeltaY = pointerData.PanY - state.PanY;
        state.PhysicalX = pointerData.PhysicalX;
        state.PhysicalY = pointerData.PhysicalY;
        state.PanX = pointerData.PanX;
        state.PanY = pointerData.PanY;
        state.Scale = pointerData.Scale;
        state.Rotation = pointerData.Rotation;
        _states[pointerData.Device] = state;
    }

    private static bool LocationNeedsUpdate(EnginePointerData pointerData, PointerState state) =>
        state.PhysicalX != pointerData.PhysicalX || state.PhysicalY != pointerData.PhysicalY;

    private void UpdatePointerIdentifier(
        ref EnginePointerData pointerData,
        ref PointerState state,
        bool startNewPointer)
    {
        if (startNewPointer)
        {
            state.PointerIdentifier = ++_pointer;
            _states[pointerData.Device] = state;
        }

        pointerData.PointerIdentifier = state.PointerIdentifier;
    }

    private struct PointerState
    {
        public int PointerIdentifier;
        public bool IsDown;
        public bool IsPanZoomActive;
        public double PhysicalX;
        public double PhysicalY;
        public double PanX;
        public double PanY;
        public double Scale;
        public double Rotation;
        public long Buttons;
        public int ViewId;
    }
}
