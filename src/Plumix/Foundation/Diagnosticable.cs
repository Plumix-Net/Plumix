using System.Text;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/diagnostics.dart

namespace Plumix.Foundation;

/// <summary>
/// A contract for objects that can provide a [DiagnosticsNode] describing themselves.
///
/// C# has no mixins, so Dart's `Diagnosticable` mixin is this interface (which value types and
/// records can implement) plus the <see cref="Diagnosticable"/> base class that carries the
/// bodies for ordinary classes.
/// </summary>
public interface IDiagnosticable
{
    /// A brief description of this object, usually just the [Type] name.
    string ToStringShort() => Diagnostics.DescribeIdentity(this);

    /// Returns a debug representation of the object that is used by debugging
    /// tools.
    DiagnosticsNode ToDiagnosticsNode(string? name = null, DiagnosticsTreeStyle? style = null)
        => new DiagnosticableNode<IDiagnosticable>(name, this, style);

    /// Add additional properties associated with the node.
    void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
    }
}

/// <summary>
/// A contract for [IDiagnosticable] objects that also expose their children.
/// </summary>
public interface IDiagnosticableTree : IDiagnosticable
{
    /// Returns a list of [DiagnosticsNode] objects describing this node's
    /// children.
    List<DiagnosticsNode> DebugDescribeChildren();
}

/// <summary>
/// A base class for providing string and [DiagnosticsNode] debug
/// representations describing the properties of an object.
/// </summary>
public abstract class Diagnosticable : IDiagnosticable
{
    /// A brief description of this object, usually just the [Type] name.
    public virtual string ToStringShort() => Diagnostics.DescribeIdentity(this);

    /// <inheritdoc />
    public override string ToString() => ToString(DiagnosticLevel.Info);

    /// Returns a string representation of this object, showing every property
    /// whose level is at least `minLevel`.
    ///
    /// C# cannot add an optional parameter to `Object.ToString`, so Dart's
    /// `toString({DiagnosticLevel minLevel})` is this overload.
    public virtual string ToString(DiagnosticLevel minLevel)
        => ToDiagnosticsNode(style: DiagnosticsTreeStyle.SingleLine).ToString(null, minLevel);

    /// Returns a debug representation of the object that is used by debugging
    /// tools.
    public virtual DiagnosticsNode ToDiagnosticsNode(string? name = null, DiagnosticsTreeStyle? style = null)
        => new DiagnosticableNode<IDiagnosticable>(name, this, style);

    /// Add additional properties associated with the node.
    public virtual void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
    }
}

/// <summary>
/// A base class for providing string and [DiagnosticsNode] debug
/// representations describing the properties and children of an object.
/// </summary>
public abstract class DiagnosticableTree : Diagnosticable, IDiagnosticableTree
{
    /// Returns a one-line detailed description of the object.
    ///
    /// This description is often somewhat long. This includes the same
    /// information given by [ToStringDeep], but does not recurse to any children.
    public string ToStringShallow(string joiner = ", ", DiagnosticLevel minLevel = DiagnosticLevel.Debug)
    {
        var result = new StringBuilder();
        result.Append(ToString());
        result.Append(joiner);
        var builder = new DiagnosticPropertiesBuilder();
        DebugFillProperties(builder);
        result.Append(string.Join(joiner, builder.Properties.Where(n => !n.IsFiltered(minLevel))));
        return result.ToString();
    }

    /// Returns a string representation of this node and its descendants.
    public string ToStringDeep(
        string prefixLineOne = "",
        string? prefixOtherLines = null,
        DiagnosticLevel minLevel = DiagnosticLevel.Debug,
        int wrapWidth = 65)
    {
        return ToDiagnosticsNode().ToStringDeep(
            prefixLineOne: prefixLineOne,
            prefixOtherLines: prefixOtherLines,
            minLevel: minLevel,
            wrapWidth: wrapWidth);
    }

    /// <inheritdoc />
    public override DiagnosticsNode ToDiagnosticsNode(string? name = null, DiagnosticsTreeStyle? style = null)
        => new DiagnosticableTreeNode(name, this, style);

    /// Returns a list of [DiagnosticsNode] objects describing this node's
    /// children.
    public virtual List<DiagnosticsNode> DebugDescribeChildren() => [];
}
