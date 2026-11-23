using System.Runtime.InteropServices;
using HarfBuzzSharp;

namespace Plumix.Impeller;

// C#-only infrastructure: no Dart parity source. Impeller's C API takes a typeface's bytes as they are and has no
// font variations, so a variable font would always draw its default instance (Android 14's one Roboto file is
// weight 400; Material's 500 labels drew as 400). Skia applies `wght` itself; here HarfBuzz's instancer (hb-subset,
// which libHarfBuzzSharp exports but HarfBuzzSharp does not bind) pins the axis and writes a static font, which
// both HarfBuzz and Impeller then load like any other face.

/// <summary>Static instances of variable fonts at a weight, through HarfBuzz's subset instancer.</summary>
internal static partial class HarfBuzzInstancer
{
    private const string Library = "libHarfBuzzSharp";

    // On Apple mobile platforms the library ships as an embedded framework, which HarfBuzzSharp's own iOS build names
    // by its rpath; this assembly targets plain .NET, so the same path is resolved here.
    static HarfBuzzInstancer()
    {
        if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsMacCatalyst())
        {
            NativeLibrary.SetDllImportResolver(typeof(HarfBuzzInstancer).Assembly, (name, assembly, searchPath) =>
                name == Library
                    ? NativeLibrary.Load("@rpath/libHarfBuzzSharp.framework/libHarfBuzzSharp", assembly, searchPath)
                    : IntPtr.Zero);
        }
    }

    // HB_TAG('w','g','h','t').
    private const uint WeightTag = ('w' << 24) | ('g' << 16) | ('h' << 8) | 't';

    /// <summary>The face's weight axis (minimum, default, maximum), or null for a font without one.</summary>
    public static (float Min, float Default, float Max)? WeightAxis(Face face) =>
        hb_ot_var_find_axis_info(face.Handle, WeightTag, out AxisInfo info) != 0
            ? (info.MinValue, info.DefaultValue, info.MaxValue)
            : null;

    /// <summary>The font file of <paramref name="face"/>, weight axis pinned at <paramref name="weight"/>.</summary>
    public static byte[]? Instantiate(Face face, float weight)
    {
        IntPtr input = hb_subset_input_create_or_fail();
        if (input == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            // Every glyph, table and name stays: only the variation is removed.
            hb_subset_input_keep_everything(input);
            if (hb_subset_input_pin_axis_location(input, face.Handle, WeightTag, weight) == 0)
            {
                return null;
            }

            IntPtr instance = hb_subset_or_fail(face.Handle, input);
            if (instance == IntPtr.Zero)
            {
                return null;
            }

            IntPtr blob = hb_face_reference_blob(instance);
            try
            {
                IntPtr data = hb_blob_get_data(blob, out uint length);
                if (data == IntPtr.Zero || length == 0)
                {
                    return null;
                }

                byte[] bytes = new byte[length];
                Marshal.Copy(data, bytes, 0, (int)length);
                return bytes;
            }
            finally
            {
                hb_blob_destroy(blob);
                hb_face_destroy(instance);
            }
        }
        finally
        {
            hb_subset_input_destroy(input);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AxisInfo
    {
        public uint AxisIndex;
        public uint Tag;
        public uint NameId;
        public uint Flags;
        public float MinValue;
        public float DefaultValue;
        public float MaxValue;
        public uint Reserved;
    }

    [LibraryImport(Library)]
    private static partial int hb_ot_var_find_axis_info(IntPtr face, uint axisTag, out AxisInfo axisInfo);

    [LibraryImport(Library)]
    private static partial IntPtr hb_subset_input_create_or_fail();

    [LibraryImport(Library)]
    private static partial void hb_subset_input_keep_everything(IntPtr input);

    [LibraryImport(Library)]
    private static partial int hb_subset_input_pin_axis_location(IntPtr input, IntPtr face, uint axisTag, float value);

    [LibraryImport(Library)]
    private static partial IntPtr hb_subset_or_fail(IntPtr source, IntPtr input);

    [LibraryImport(Library)]
    private static partial void hb_subset_input_destroy(IntPtr input);

    [LibraryImport(Library)]
    private static partial IntPtr hb_face_reference_blob(IntPtr face);

    [LibraryImport(Library)]
    private static partial IntPtr hb_blob_get_data(IntPtr blob, out uint length);

    [LibraryImport(Library)]
    private static partial void hb_blob_destroy(IntPtr blob);

    [LibraryImport(Library)]
    private static partial void hb_face_destroy(IntPtr face);
}
