// C#-only build tool; no Dart parity source. Copies the files an Avalonia resource assembly embeds
// (its `!AvaloniaResources` blob) into a directory, so the Impeller host can ship a package's assets (the Inter
// faces of Avalonia.Fonts.Inter) as Android assets without referencing the assembly.
// Usage: dotnet run extract-avalonia-resources.cs -- <assembly.dll> <output directory> [extension]
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

string assemblyPath = args[0];
string output = args[1];
string extension = args.Length > 2 ? args[2] : string.Empty;

using var stream = File.OpenRead(assemblyPath);
using var pe = new PEReader(stream);
MetadataReader metadata = pe.GetMetadataReader();
foreach (ManifestResourceHandle handle in metadata.ManifestResources)
{
    ManifestResource resource = metadata.GetManifestResource(handle);
    if (metadata.GetString(resource.Name) != "!AvaloniaResources")
    {
        continue;
    }

    // Embedded resources sit in the CLR resources directory, each prefixed by its 32-bit length.
    PEMemoryBlock block = pe.GetSectionData(pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress);
    BlobReader blob = block.GetReader((int)resource.Offset, block.Length - (int)resource.Offset);
    int length = blob.ReadInt32();
    byte[] bytes = blob.ReadBytes(length);
    using var reader = new BinaryReader(new MemoryStream(bytes));
    // Avalonia's index: its length, then a version, the entry count and (path, offset, size) entries.
    int indexLength = reader.ReadInt32();
    long dataStart = 4 + indexLength;
    reader.ReadInt32();
    int count = reader.ReadInt32();
    var entries = new List<(string Path, int Offset, int Size)>();
    for (int i = 0; i < count; i++)
    {
        entries.Add((reader.ReadString(), reader.ReadInt32(), reader.ReadInt32()));
    }

    foreach ((string path, int offset, int size) in entries)
    {
        if (!path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) || path.StartsWith("/!"))
        {
            continue;
        }

        string target = Path.Combine(output, path.TrimStart('/'));
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.WriteAllBytes(target, bytes.AsSpan((int)dataStart + offset, size).ToArray());
        Console.WriteLine(target);
    }
}
