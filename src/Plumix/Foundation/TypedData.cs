using System.Buffers.Binary;

namespace Plumix.Foundation;

// C#-only infrastructure: mirrors the `dart:typed_data` surface the platform-channel codecs depend on
// (`Endian`, `ByteData`). Dart gets these from its SDK; .NET has no equivalent byte-view type, so the
// framework owns one. See `docs/ai/DIVERGENCES.md`.

/// <summary>Describes the endianness a multi-byte value is stored with.</summary>
public sealed class Endian
{
    private Endian(bool isLittleEndian)
    {
        IsLittleEndian = isLittleEndian;
    }

    /// <summary>Most significant byte first.</summary>
    public static Endian Big { get; } = new Endian(isLittleEndian: false);

    /// <summary>Least significant byte first.</summary>
    public static Endian Little { get; } = new Endian(isLittleEndian: true);

    /// <summary>The endianness of the machine the application runs on.</summary>
    public static Endian Host { get; } = BitConverter.IsLittleEndian ? Little : Big;

    internal bool IsLittleEndian { get; }
}

/// <summary>
/// A fixed-length window onto a byte buffer, with accessors for the primitive scalar types.
/// </summary>
/// <remarks>
/// Dart parity source: <c>dart:typed_data</c>'s <c>ByteData</c>. Like Dart's type, a
/// <see cref="ByteData"/> is a *view*: <see cref="Buffer"/> can be longer than
/// <see cref="LengthInBytes"/>, and two views can share one backing array. Every accessor is
/// big-endian by default, exactly like <c>dart:typed_data</c>; the channel buffers ask for
/// <see cref="Endian.Host"/> explicitly.
/// </remarks>
public sealed class ByteData
{
    private ByteData(byte[] buffer, int offsetInBytes, int lengthInBytes)
    {
        Buffer = buffer;
        OffsetInBytes = offsetInBytes;
        LengthInBytes = lengthInBytes;
    }

    /// <summary>Creates a zero-filled view of <paramref name="length"/> bytes over a fresh buffer.</summary>
    public ByteData(int length) : this(new byte[length], 0, length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
    }

    /// <summary>The backing store this view reads from and writes to.</summary>
    public byte[] Buffer { get; }

    /// <summary>The offset of this view inside <see cref="Buffer"/>.</summary>
    public int OffsetInBytes { get; }

    /// <summary>The length of this view in bytes.</summary>
    public int LengthInBytes { get; }

    /// <summary>Dart parity source: <c>ByteData.view(buffer, offsetInBytes, length)</c>.</summary>
    public static ByteData View(byte[] buffer, int offsetInBytes, int? length = null)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offsetInBytes);
        int viewLength = length ?? buffer.Length - offsetInBytes;
        ArgumentOutOfRangeException.ThrowIfNegative(viewLength);
        if (offsetInBytes + viewLength > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "The view does not fit in the buffer.");
        }

        return new ByteData(buffer, offsetInBytes, viewLength);
    }

    /// <summary>Dart parity source: <c>ByteData.sublistView(bytes)</c>.</summary>
    public static ByteData SublistView(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        return new ByteData(bytes, 0, bytes.Length);
    }

    /// <summary>Dart parity source: <c>ByteData.sublistView(data, start, end)</c>.</summary>
    public ByteData SublistView(int start, int? end = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        int endOffset = end ?? LengthInBytes;
        if (endOffset < start || endOffset > LengthInBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(end), "The sublist does not fit in the view.");
        }

        return new ByteData(Buffer, OffsetInBytes + start, endOffset - start);
    }

    /// <summary>
    /// Copies this view out as a byte array. Dart's <c>buffer.asUint8List(offset, length)</c> hands back a
    /// view; .NET arrays cannot alias, so the bytes are copied instead.
    /// </summary>
    public byte[] ToUint8List() => Span.ToArray();

    internal Span<byte> Span => Buffer.AsSpan(OffsetInBytes, LengthInBytes);

    public byte GetUint8(int byteOffset) => Buffer[Index(byteOffset, 1)];

    public void SetUint8(int byteOffset, byte value) => Buffer[Index(byteOffset, 1)] = value;

    public ushort GetUint16(int byteOffset, Endian? endian = null)
    {
        Span<byte> span = Read(byteOffset, 2, endian);
        return Little(endian)
            ? BinaryPrimitives.ReadUInt16LittleEndian(span)
            : BinaryPrimitives.ReadUInt16BigEndian(span);
    }

    public void SetUint16(int byteOffset, ushort value, Endian? endian = null)
    {
        Span<byte> span = Read(byteOffset, 2, endian);
        if (Little(endian))
        {
            BinaryPrimitives.WriteUInt16LittleEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt16BigEndian(span, value);
        }
    }

    public uint GetUint32(int byteOffset, Endian? endian = null)
    {
        Span<byte> span = Read(byteOffset, 4, endian);
        return Little(endian)
            ? BinaryPrimitives.ReadUInt32LittleEndian(span)
            : BinaryPrimitives.ReadUInt32BigEndian(span);
    }

    public void SetUint32(int byteOffset, uint value, Endian? endian = null)
    {
        Span<byte> span = Read(byteOffset, 4, endian);
        if (Little(endian))
        {
            BinaryPrimitives.WriteUInt32LittleEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt32BigEndian(span, value);
        }
    }

    public int GetInt32(int byteOffset, Endian? endian = null)
    {
        Span<byte> span = Read(byteOffset, 4, endian);
        return Little(endian)
            ? BinaryPrimitives.ReadInt32LittleEndian(span)
            : BinaryPrimitives.ReadInt32BigEndian(span);
    }

    public void SetInt32(int byteOffset, int value, Endian? endian = null)
    {
        Span<byte> span = Read(byteOffset, 4, endian);
        if (Little(endian))
        {
            BinaryPrimitives.WriteInt32LittleEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteInt32BigEndian(span, value);
        }
    }

    public long GetInt64(int byteOffset, Endian? endian = null)
    {
        Span<byte> span = Read(byteOffset, 8, endian);
        return Little(endian)
            ? BinaryPrimitives.ReadInt64LittleEndian(span)
            : BinaryPrimitives.ReadInt64BigEndian(span);
    }

    public void SetInt64(int byteOffset, long value, Endian? endian = null)
    {
        Span<byte> span = Read(byteOffset, 8, endian);
        if (Little(endian))
        {
            BinaryPrimitives.WriteInt64LittleEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteInt64BigEndian(span, value);
        }
    }

    public float GetFloat32(int byteOffset, Endian? endian = null)
    {
        Span<byte> span = Read(byteOffset, 4, endian);
        return Little(endian)
            ? BinaryPrimitives.ReadSingleLittleEndian(span)
            : BinaryPrimitives.ReadSingleBigEndian(span);
    }

    public void SetFloat32(int byteOffset, float value, Endian? endian = null)
    {
        Span<byte> span = Read(byteOffset, 4, endian);
        if (Little(endian))
        {
            BinaryPrimitives.WriteSingleLittleEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteSingleBigEndian(span, value);
        }
    }

    public double GetFloat64(int byteOffset, Endian? endian = null)
    {
        Span<byte> span = Read(byteOffset, 8, endian);
        return Little(endian)
            ? BinaryPrimitives.ReadDoubleLittleEndian(span)
            : BinaryPrimitives.ReadDoubleBigEndian(span);
    }

    public void SetFloat64(int byteOffset, double value, Endian? endian = null)
    {
        Span<byte> span = Read(byteOffset, 8, endian);
        if (Little(endian))
        {
            BinaryPrimitives.WriteDoubleLittleEndian(span, value);
        }
        else
        {
            BinaryPrimitives.WriteDoubleBigEndian(span, value);
        }
    }

    private static bool Little(Endian? endian) => (endian ?? Endian.Big).IsLittleEndian;

    private Span<byte> Read(int byteOffset, int size, Endian? endian)
    {
        _ = endian;
        return Buffer.AsSpan(Index(byteOffset, size), size);
    }

    private int Index(int byteOffset, int size)
    {
        if (byteOffset < 0 || byteOffset + size > LengthInBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(byteOffset));
        }

        return OffsetInBytes + byteOffset;
    }
}
