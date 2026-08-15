using System.Buffers.Binary;

namespace Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/serialization.dart

/// <summary>
/// Write-only buffer for incrementally building a <see cref="ByteData"/> instance.
/// </summary>
/// <remarks>
/// A <see cref="WriteBuffer"/> instance can be used only once. Attempts to reuse will result in
/// errors being thrown.
/// </remarks>
public sealed class WriteBuffer
{
    private static readonly byte[] ZeroBuffer = new byte[8];

    private byte[] _buffer;
    private int _currentSize;
    private bool _isDone;

    /// <summary>Creates an interface for incrementally building a <see cref="ByteData"/> instance.</summary>
    public WriteBuffer(int startCapacity = 8)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(startCapacity);
        _buffer = new byte[startCapacity];
    }

    /// <summary>Write a Uint8 into the buffer.</summary>
    public void PutUint8(byte byteValue)
    {
        ThrowIfDone();
        EnsureCapacity(1);
        _buffer[_currentSize++] = byteValue;
    }

    /// <summary>Write a Uint16 into the buffer.</summary>
    public void PutUint16(ushort value, Endian? endian = null)
    {
        ThrowIfDone();
        EnsureCapacity(2);
        Span<byte> span = _buffer.AsSpan(_currentSize, 2);
        if (IsLittleEndian(endian))
        {
            BinaryPrimitives.WriteUInt16LittleEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt16BigEndian(span, value);
        }

        _currentSize += 2;
    }

    /// <summary>Write a Uint32 into the buffer.</summary>
    public void PutUint32(uint value, Endian? endian = null)
    {
        ThrowIfDone();
        EnsureCapacity(4);
        Span<byte> span = _buffer.AsSpan(_currentSize, 4);
        if (IsLittleEndian(endian))
        {
            BinaryPrimitives.WriteUInt32LittleEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt32BigEndian(span, value);
        }

        _currentSize += 4;
    }

    /// <summary>Write an Int32 into the buffer.</summary>
    public void PutInt32(int value, Endian? endian = null)
    {
        ThrowIfDone();
        EnsureCapacity(4);
        Span<byte> span = _buffer.AsSpan(_currentSize, 4);
        if (IsLittleEndian(endian))
        {
            BinaryPrimitives.WriteInt32LittleEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteInt32BigEndian(span, value);
        }

        _currentSize += 4;
    }

    /// <summary>Write an Int64 into the buffer.</summary>
    public void PutInt64(long value, Endian? endian = null)
    {
        ThrowIfDone();
        EnsureCapacity(8);
        Span<byte> span = _buffer.AsSpan(_currentSize, 8);
        if (IsLittleEndian(endian))
        {
            BinaryPrimitives.WriteInt64LittleEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteInt64BigEndian(span, value);
        }

        _currentSize += 8;
    }

    /// <summary>Write a Float64 into the buffer, aligned to an 8-byte boundary first.</summary>
    public void PutFloat64(double value, Endian? endian = null)
    {
        ThrowIfDone();
        AlignTo(8);
        EnsureCapacity(8);
        Span<byte> span = _buffer.AsSpan(_currentSize, 8);
        if (IsLittleEndian(endian))
        {
            BinaryPrimitives.WriteDoubleLittleEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteDoubleBigEndian(span, value);
        }

        _currentSize += 8;
    }

    /// <summary>Write all the values from a Uint8 list into the buffer.</summary>
    public void PutUint8List(ReadOnlySpan<byte> list)
    {
        ThrowIfDone();
        EnsureCapacity(list.Length);
        list.CopyTo(_buffer.AsSpan(_currentSize, list.Length));
        _currentSize += list.Length;
    }

    /// <summary>Write all the values from an Int32 list into the buffer.</summary>
    public void PutInt32List(int[] list)
    {
        ArgumentNullException.ThrowIfNull(list);
        ThrowIfDone();
        AlignTo(4);
        EnsureCapacity(4 * list.Length);
        foreach (int value in list)
        {
            BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(_currentSize, 4), value);
            MaybeReverse(_currentSize, 4);
            _currentSize += 4;
        }
    }

    /// <summary>Write all the values from an Int64 list into the buffer.</summary>
    public void PutInt64List(long[] list)
    {
        ArgumentNullException.ThrowIfNull(list);
        ThrowIfDone();
        AlignTo(8);
        EnsureCapacity(8 * list.Length);
        foreach (long value in list)
        {
            BinaryPrimitives.WriteInt64LittleEndian(_buffer.AsSpan(_currentSize, 8), value);
            MaybeReverse(_currentSize, 8);
            _currentSize += 8;
        }
    }

    /// <summary>Write all the values from a Float32 list into the buffer.</summary>
    public void PutFloat32List(float[] list)
    {
        ArgumentNullException.ThrowIfNull(list);
        ThrowIfDone();
        AlignTo(4);
        EnsureCapacity(4 * list.Length);
        foreach (float value in list)
        {
            BinaryPrimitives.WriteSingleLittleEndian(_buffer.AsSpan(_currentSize, 4), value);
            MaybeReverse(_currentSize, 4);
            _currentSize += 4;
        }
    }

    /// <summary>Write all the values from a Float64 list into the buffer.</summary>
    public void PutFloat64List(double[] list)
    {
        ArgumentNullException.ThrowIfNull(list);
        ThrowIfDone();
        AlignTo(8);
        EnsureCapacity(8 * list.Length);
        foreach (double value in list)
        {
            BinaryPrimitives.WriteDoubleLittleEndian(_buffer.AsSpan(_currentSize, 8), value);
            MaybeReverse(_currentSize, 8);
            _currentSize += 8;
        }
    }

    /// <summary>Finalize and return the written <see cref="ByteData"/>.</summary>
    public ByteData Done()
    {
        if (_isDone)
        {
            throw new InvalidOperationException(
                $"Done() must not be called more than once on the same {nameof(WriteBuffer)}.");
        }

        var result = ByteData.View(_buffer, 0, _currentSize);
        _buffer = [];
        _isDone = true;
        return result;
    }

    private static bool IsLittleEndian(Endian? endian) => (endian ?? Endian.Host).IsLittleEndian;

    private void MaybeReverse(int offset, int size)
    {
        if (!BitConverter.IsLittleEndian)
        {
            _buffer.AsSpan(offset, size).Reverse();
        }
    }

    private void AlignTo(int alignment)
    {
        ThrowIfDone();
        int mod = _currentSize % alignment;
        if (mod != 0)
        {
            PutUint8List(ZeroBuffer.AsSpan(0, alignment - mod));
        }
    }

    private void EnsureCapacity(int required)
    {
        int newSize = _currentSize + required;
        if (newSize <= _buffer.Length)
        {
            return;
        }

        int capacity = Math.Max(newSize, _buffer.Length * 2);
        Array.Resize(ref _buffer, capacity);
    }

    private void ThrowIfDone()
    {
        if (_isDone)
        {
            throw new InvalidOperationException($"The {nameof(WriteBuffer)} is already done.");
        }
    }
}

/// <summary>Read-only buffer for reading sequentially from a <see cref="ByteData"/> instance.</summary>
/// <remarks>
/// The byte order used is <see cref="Endian.Host"/> throughout.
/// </remarks>
public sealed class ReadBuffer
{
    private int _position;

    /// <summary>Creates a <see cref="ReadBuffer"/> for reading from <paramref name="data"/>.</summary>
    public ReadBuffer(ByteData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Data = data;
    }

    /// <summary>The underlying data being read.</summary>
    public ByteData Data { get; }

    /// <summary>Whether the buffer has data remaining to read.</summary>
    public bool HasRemaining => _position < Data.LengthInBytes;

    /// <summary>Reads a Uint8 from the buffer.</summary>
    public byte GetUint8() => Data.GetUint8(_position++);

    /// <summary>Reads a Uint16 from the buffer.</summary>
    public ushort GetUint16(Endian? endian = null)
    {
        ushort value = Data.GetUint16(_position, endian ?? Endian.Host);
        _position += 2;
        return value;
    }

    /// <summary>Reads a Uint32 from the buffer.</summary>
    public uint GetUint32(Endian? endian = null)
    {
        uint value = Data.GetUint32(_position, endian ?? Endian.Host);
        _position += 4;
        return value;
    }

    /// <summary>Reads an Int32 from the buffer.</summary>
    public int GetInt32(Endian? endian = null)
    {
        int value = Data.GetInt32(_position, endian ?? Endian.Host);
        _position += 4;
        return value;
    }

    /// <summary>Reads an Int64 from the buffer.</summary>
    public long GetInt64(Endian? endian = null)
    {
        long value = Data.GetInt64(_position, endian ?? Endian.Host);
        _position += 8;
        return value;
    }

    /// <summary>Reads a Float64 from the buffer, aligning to an 8-byte boundary first.</summary>
    public double GetFloat64(Endian? endian = null)
    {
        AlignTo(8);
        double value = Data.GetFloat64(_position, endian ?? Endian.Host);
        _position += 8;
        return value;
    }

    /// <summary>Reads the given number of Uint8s from the buffer.</summary>
    /// <remarks>
    /// Dart hands back a view into the source buffer; .NET arrays cannot alias, so the bytes are copied.
    /// </remarks>
    public byte[] GetUint8List(int length)
    {
        byte[] list = Data.SublistView(_position, _position + length).ToUint8List();
        _position += length;
        return list;
    }

    /// <summary>Reads the given number of Int32s from the buffer.</summary>
    public int[] GetInt32List(int length)
    {
        AlignTo(4);
        int[] list = new int[length];
        for (int i = 0; i < length; i++)
        {
            list[i] = Data.GetInt32(_position + (i * 4), Endian.Host);
        }

        _position += 4 * length;
        return list;
    }

    /// <summary>Reads the given number of Int64s from the buffer.</summary>
    public long[] GetInt64List(int length)
    {
        AlignTo(8);
        long[] list = new long[length];
        for (int i = 0; i < length; i++)
        {
            list[i] = Data.GetInt64(_position + (i * 8), Endian.Host);
        }

        _position += 8 * length;
        return list;
    }

    /// <summary>Reads the given number of Float32s from the buffer.</summary>
    public float[] GetFloat32List(int length)
    {
        AlignTo(4);
        float[] list = new float[length];
        for (int i = 0; i < length; i++)
        {
            list[i] = Data.GetFloat32(_position + (i * 4), Endian.Host);
        }

        _position += 4 * length;
        return list;
    }

    /// <summary>Reads the given number of Float64s from the buffer.</summary>
    public double[] GetFloat64List(int length)
    {
        AlignTo(8);
        double[] list = new double[length];
        for (int i = 0; i < length; i++)
        {
            list[i] = Data.GetFloat64(_position + (i * 8), Endian.Host);
        }

        _position += 8 * length;
        return list;
    }

    private void AlignTo(int alignment)
    {
        int mod = _position % alignment;
        if (mod != 0)
        {
            _position += alignment - mod;
        }
    }
}
