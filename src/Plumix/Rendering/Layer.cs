using Avalonia;
using Avalonia.Media;

// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/layer.dart (approximate)

namespace Plumix.Rendering;

public abstract class Layer
{
    public ContainerLayer? Parent { get; private set; }

    internal void Attach(ContainerLayer parent)
    {
        Parent = parent;
    }

    internal void Detach()
    {
        Parent = null;
    }

    internal abstract void AddToScene(DrawingContext context, Point offset);
}

public class ContainerLayer : Layer
{
    private readonly List<Layer> _children = [];

    public IReadOnlyList<Layer> Children => _children;

    public void Append(Layer child)
    {
        if (ReferenceEquals(child.Parent, this))
        {
            return;
        }

        child.Parent?.Remove(child);
        _children.Add(child);
        child.Attach(this);
    }

    public void Remove(Layer child)
    {
        if (_children.Remove(child))
        {
            child.Detach();
        }
    }

    public void RemoveAllChildren()
    {
        foreach (var child in _children)
        {
            child.Detach();
        }

        _children.Clear();
    }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        AddChildrenToScene(context, offset);
    }

    protected void AddChildrenToScene(DrawingContext context, Point offset)
    {
        for (var index = 0; index < _children.Count; index++)
        {
            _children[index].AddToScene(context, offset);
        }
    }
}

public class OffsetLayer : ContainerLayer
{
    public Point Offset { get; set; }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        base.AddToScene(context, offset + Offset);
    }
}

public sealed class OpacityOffsetLayer : OffsetLayer
{
    private double _opacity = 1.0;

    public double Opacity
    {
        get => _opacity;
        set => _opacity = Math.Clamp(value, 0.0, 1.0);
    }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        using (context.PushOpacity(Opacity))
        {
            base.AddToScene(context, offset);
        }
    }
}

public sealed class TransformOffsetLayer : OffsetLayer
{
    public Matrix Transform { get; set; } = Matrix.Identity;

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        var sceneOffset = offset + Offset;
        using (context.PushTransform(Matrix.CreateTranslation(sceneOffset.X, sceneOffset.Y)))
        using (context.PushTransform(Transform))
        {
            AddChildrenToScene(context, new Point(0, 0));
        }
    }
}

public sealed class ClipRectOffsetLayer : OffsetLayer
{
    public Rect ClipRect { get; set; }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        var sceneOffset = offset + Offset;
        var translatedRect = new Rect(ClipRect.Position + sceneOffset, ClipRect.Size);
        using (context.PushClip(translatedRect))
        {
            AddChildrenToScene(context, sceneOffset);
        }
    }
}

public sealed class ClipRRectOffsetLayer : OffsetLayer
{
    public Rect ClipRect { get; set; }

    public BorderRadius BorderRadius { get; set; } = BorderRadius.Zero;

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        var sceneOffset = offset + Offset;
        var translatedRect = new Rect(ClipRect.Position + sceneOffset, ClipRect.Size);
        using (context.PushClip(new RoundedRect(translatedRect, ClampRadius(translatedRect, BorderRadius))))
        {
            AddChildrenToScene(context, sceneOffset);
        }
    }

    private static double ClampRadius(Rect clipRect, BorderRadius borderRadius)
    {
        var maxRadius = Math.Max(0, Math.Min(clipRect.Width, clipRect.Height) / 2);
        return Math.Min(borderRadius.Radius, maxRadius);
    }
}

public sealed class ClipRectLayer : ContainerLayer
{
    public Rect ClipRect { get; set; }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        var translatedRect = new Rect(ClipRect.Position + offset, ClipRect.Size);
        using (context.PushClip(translatedRect))
        {
            base.AddToScene(context, offset);
        }
    }
}

public sealed class ClipRRectLayer : ContainerLayer
{
    public Rect ClipRect { get; set; }

    public BorderRadius BorderRadius { get; set; } = BorderRadius.Zero;

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        var translatedRect = new Rect(ClipRect.Position + offset, ClipRect.Size);
        using (context.PushClip(new RoundedRect(translatedRect, ClampRadius(translatedRect, BorderRadius))))
        {
            base.AddToScene(context, offset);
        }
    }

    private static double ClampRadius(Rect clipRect, BorderRadius borderRadius)
    {
        var maxRadius = Math.Max(0, Math.Min(clipRect.Width, clipRect.Height) / 2);
        return Math.Min(borderRadius.Radius, maxRadius);
    }
}

public sealed class TransformLayer : ContainerLayer
{
    public Matrix Transform { get; set; } = Matrix.Identity;

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        using (context.PushTransform(Matrix.CreateTranslation(offset.X, offset.Y)))
        using (context.PushTransform(Transform))
        {
            base.AddToScene(context, new Point(0, 0));
        }
    }
}

public sealed class OpacityLayer : ContainerLayer
{
    private double _opacity = 1.0;

    public double Opacity
    {
        get => _opacity;
        set => _opacity = Math.Clamp(value, 0.0, 1.0);
    }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        using (context.PushOpacity(Opacity))
        {
            base.AddToScene(context, offset);
        }
    }
}

public sealed class PictureLayer : Layer
{
    private readonly List<Action<DrawingContext, Point>> _commands = [];

    public bool IsEmpty => _commands.Count == 0;

    public void AddDrawCommand(Action<DrawingContext, Point> command)
    {
        _commands.Add(command);
    }

    internal override void AddToScene(DrawingContext context, Point offset)
    {
        for (var index = 0; index < _commands.Count; index++)
        {
            _commands[index](context, offset);
        }
    }
}
