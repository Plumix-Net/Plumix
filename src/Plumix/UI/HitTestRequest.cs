using Avalonia;

// C#-only host bridge for dart:ui's HitTestRequest and HitTestResponse.

namespace Plumix.UI;

/// <summary>A platform hit-test query for one logical point in a view.</summary>
public readonly record struct HitTestRequest(int ViewId, Point Offset);

/// <summary>Whether the framework path at the queried point includes a native platform view.</summary>
public readonly record struct HitTestResponse(bool HasPlatformView);
