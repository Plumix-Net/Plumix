using Avalonia;

namespace Plumix;

public sealed class PlumixOptions
{
    public string Title { get; set; } = "Plumix App";

    /// <summary>
    /// Initial window size in logical (DIP) units. If null, the OS default is used.
    /// </summary>
    public Size? InitialWindowSize { get; set; }
}
