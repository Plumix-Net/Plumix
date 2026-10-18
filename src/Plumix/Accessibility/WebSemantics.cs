using System.Globalization;
using System.Text;
using System.Text.Json;
using Plumix.Rendering;
using Plumix.Widgets;

// C#-only infrastructure: the DOM-free half of the browser accessibility bridge — which semantic role
// a node gets and which element, ARIA attributes and label representation it renders as — written out
// as JSON for the browser host's script to apply. Role selection and the per-role output follow the
// web engine's engine/src/flutter/lib/web_ui/lib/src/engine/semantics/*.dart; the deliberate
// departures (no text-editing input, pointer events left to the canvas, the desktop placeholder only)
// are listed in docs/ai/DIVERGENCES.md.

namespace Plumix.Accessibility;

/// <summary>The web engine's <c>EngineSemanticsRole</c>: what a node renders as in the DOM.</summary>
public enum WebSemanticsRole
{
    /// <summary>A plain node.</summary>
    Generic,

    /// <summary>A button, or a childless tappable node.</summary>
    Button,

    /// <summary>A checkbox, radio button or switch.</summary>
    Checkable,

    /// <summary>A text field.</summary>
    TextField,

    /// <summary>A node with increase or decrease actions.</summary>
    Incrementable,

    /// <summary>A node with implicit scrolling.</summary>
    Scrollable,

    /// <summary>A heading.</summary>
    Heading,

    /// <summary>A header that is not a heading.</summary>
    Header,

    /// <summary>A link.</summary>
    Link,

    /// <summary>A purely visual image.</summary>
    Image,

    /// <summary>A route scope.</summary>
    Route,

    /// <summary>A dialog.</summary>
    Dialog,

    /// <summary>An alert dialog.</summary>
    AlertDialog,

    /// <summary>A tab.</summary>
    Tab,

    /// <summary>A tab panel.</summary>
    TabPanel,

    /// <summary>A tab bar.</summary>
    TabList,

    /// <summary>A table.</summary>
    Table,

    /// <summary>A table row.</summary>
    Row,

    /// <summary>A table cell.</summary>
    Cell,

    /// <summary>A table column header.</summary>
    ColumnHeader,

    /// <summary>A radio group.</summary>
    RadioGroup,

    /// <summary>A menu.</summary>
    Menu,

    /// <summary>A menu bar.</summary>
    MenuBar,

    /// <summary>A menu item.</summary>
    MenuItem,

    /// <summary>A menu item with a checkbox.</summary>
    MenuItemCheckbox,

    /// <summary>A menu item with a radio button.</summary>
    MenuItemRadio,

    /// <summary>An alert.</summary>
    Alert,

    /// <summary>A status message.</summary>
    Status,

    /// <summary>A list.</summary>
    List,

    /// <summary>A list item.</summary>
    ListItem,

    /// <summary>A complementary landmark.</summary>
    Complementary,

    /// <summary>A content-info landmark.</summary>
    ContentInfo,

    /// <summary>A main landmark.</summary>
    Main,

    /// <summary>A navigation landmark.</summary>
    Navigation,

    /// <summary>A region landmark.</summary>
    Region,

    /// <summary>A form.</summary>
    Form,

    /// <summary>A loading spinner.</summary>
    LoadingSpinner,

    /// <summary>A progress bar.</summary>
    ProgressBar,
}

/// <summary>How a node's label reaches the DOM.</summary>
/// <remarks>The web engine's <c>LabelRepresentation</c>.</remarks>
public enum WebLabelRepresentation
{
    /// <summary>An <c>aria-label</c> attribute.</summary>
    AriaLabel,

    /// <summary>A text node inside the element.</summary>
    DomText,

    /// <summary>A text span inside the element.</summary>
    SizedSpan,
}

/// <summary>The DOM rendering of one semantics node.</summary>
/// <param name="Role">The node's semantic role.</param>
/// <param name="Tag">The element's tag name.</param>
/// <param name="Attributes">Every attribute the element carries, besides positioning.</param>
/// <param name="Text">The label text rendered inside the element, or <c>null</c>.</param>
/// <param name="TextAsSpan">Whether <paramref name="Text"/> goes into a span rather than a text node.</param>
/// <param name="Tappable">Whether a click on the element taps the node.</param>
/// <param name="Focusable">Whether the element takes keyboard focus.</param>
/// <param name="Focused">Whether the element should hold focus after the update.</param>
/// <param name="ImageLabel">The label of the image child a visual-only image with children renders.</param>
public sealed record WebSemanticsElement(
    WebSemanticsRole Role,
    string Tag,
    IReadOnlyDictionary<string, string> Attributes,
    string? Text,
    bool TextAsSpan,
    bool Tappable,
    bool Focusable,
    bool Focused,
    string? ImageLabel);

/// <summary>Maps semantics nodes to DOM elements the way the web engine does.</summary>
public static class WebSemantics
{
    /// <summary>The placeholder's label: <c>ui_web.accessibilityPlaceholderMessage</c>.</summary>
    public const string PlaceholderMessage = "Enable accessibility";

    /// <summary>The node's role: the web engine's <c>_getEngineSemanticsRole</c>, in its order.</summary>
    /// <remarks>
    /// The framework role decides first (the roles the web engine has no element for fall through),
    /// then heading, text field, incrementable, visual-only image, checkable, link, button, scrollable,
    /// route, header and a childless tappable node.
    /// </remarks>
    public static WebSemanticsRole RoleOf(AccessibilityNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        WebSemanticsRole? fromRole = node.Data.Role switch
        {
            SemanticsRole.Tab => WebSemanticsRole.Tab,
            SemanticsRole.TabPanel => WebSemanticsRole.TabPanel,
            SemanticsRole.TabBar => WebSemanticsRole.TabList,
            SemanticsRole.Dialog => WebSemanticsRole.Dialog,
            SemanticsRole.AlertDialog => WebSemanticsRole.AlertDialog,
            SemanticsRole.Table => WebSemanticsRole.Table,
            SemanticsRole.Cell => WebSemanticsRole.Cell,
            SemanticsRole.Row => WebSemanticsRole.Row,
            SemanticsRole.ColumnHeader => WebSemanticsRole.ColumnHeader,
            SemanticsRole.RadioGroup => WebSemanticsRole.RadioGroup,
            SemanticsRole.Menu => WebSemanticsRole.Menu,
            SemanticsRole.MenuBar => WebSemanticsRole.MenuBar,
            SemanticsRole.MenuItem => WebSemanticsRole.MenuItem,
            SemanticsRole.MenuItemCheckbox => WebSemanticsRole.MenuItemCheckbox,
            SemanticsRole.MenuItemRadio => WebSemanticsRole.MenuItemRadio,
            SemanticsRole.Alert => WebSemanticsRole.Alert,
            SemanticsRole.Status => WebSemanticsRole.Status,
            SemanticsRole.List => WebSemanticsRole.List,
            SemanticsRole.ListItem => WebSemanticsRole.ListItem,
            SemanticsRole.Complementary => WebSemanticsRole.Complementary,
            SemanticsRole.ContentInfo => WebSemanticsRole.ContentInfo,
            SemanticsRole.Main => WebSemanticsRole.Main,
            SemanticsRole.Navigation => WebSemanticsRole.Navigation,
            SemanticsRole.Region => WebSemanticsRole.Region,
            SemanticsRole.Form => WebSemanticsRole.Form,
            SemanticsRole.LoadingSpinner => WebSemanticsRole.LoadingSpinner,
            SemanticsRole.ProgressBar => WebSemanticsRole.ProgressBar,
            _ => null,
        };
        if (fromRole is { } role)
        {
            return role;
        }

        if (IsHeading(node))
        {
            return WebSemanticsRole.Heading;
        }

        if (node.HasFlag(SemanticsFlags.IsTextField))
        {
            return WebSemanticsRole.TextField;
        }

        if (node.HasAction(SemanticsActions.Increase | SemanticsActions.Decrease))
        {
            return WebSemanticsRole.Incrementable;
        }

        if (node.HasFlag(SemanticsFlags.IsImage)
            && !node.HasAction(SemanticsActions.Tap)
            && !node.HasFlag(SemanticsFlags.IsButton))
        {
            return WebSemanticsRole.Image;
        }

        if (node.HasFlag(SemanticsFlags.HasCheckedState | SemanticsFlags.HasToggledState))
        {
            return WebSemanticsRole.Checkable;
        }

        if (node.HasFlag(SemanticsFlags.IsLink))
        {
            return WebSemanticsRole.Link;
        }

        if (node.HasFlag(SemanticsFlags.IsButton))
        {
            return WebSemanticsRole.Button;
        }

        if (node.HasFlag(SemanticsFlags.HasImplicitScrolling))
        {
            return WebSemanticsRole.Scrollable;
        }

        if (node.HasFlag(SemanticsFlags.ScopesRoute))
        {
            return WebSemanticsRole.Route;
        }

        if (node.HasFlag(SemanticsFlags.IsHeader))
        {
            return WebSemanticsRole.Header;
        }

        return node.HasAction(SemanticsActions.Tap) && !node.HasChildren
            ? WebSemanticsRole.Button
            : WebSemanticsRole.Generic;
    }

    /// <summary>
    /// The DOM label: the web engine's <c>computeDomSemanticsLabel</c> — the tooltip, then the label and
    /// value joined by a space (the value is left out for an incrementable node).
    /// </summary>
    public static string? ComputeDomLabel(AccessibilityNode node, WebSemanticsRole role)
    {
        ArgumentNullException.ThrowIfNull(node);
        var parts = new List<string>(2);
        if (node.Label.Trim().Length > 0)
        {
            parts.Add(node.Label);
        }

        if (role != WebSemanticsRole.Incrementable && node.Value.Trim().Length > 0)
        {
            parts.Add(node.Value);
        }

        string content = string.Join(' ', parts);
        string tooltip = node.Tooltip;
        if (tooltip.Length == 0)
        {
            return content.Length == 0 ? null : content;
        }

        return content.Length == 0 ? tooltip : tooltip + "\n" + content;
    }

    /// <summary>The element, attributes and label of <paramref name="node"/>.</summary>
    public static WebSemanticsElement Describe(AccessibilityNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        WebSemanticsRole role = RoleOf(node);
        var attributes = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["id"] = "flt-semantic-node-" + node.Id.ToString(CultureInfo.InvariantCulture),
        };
        if (node.Identifier.Length > 0)
        {
            attributes["flt-semantics-identifier"] = node.Identifier;
        }

        string tag = role switch
        {
            WebSemanticsRole.Heading => "h" + EffectiveHeadingLevel(node).ToString(CultureInfo.InvariantCulture),
            WebSemanticsRole.Header => "header",
            WebSemanticsRole.Link => "a",
            WebSemanticsRole.Form => "form",
            _ => "flt-semantics",
        };

        string? ariaRole = role switch
        {
            WebSemanticsRole.Button => "button",
            WebSemanticsRole.Checkable => node.HasFlag(SemanticsFlags.IsInMutuallyExclusiveGroup)
                ? "radio"
                : node.HasFlag(SemanticsFlags.HasToggledState) ? "switch" : "checkbox",
            WebSemanticsRole.TextField => "textbox",
            WebSemanticsRole.Scrollable => "group",
            WebSemanticsRole.Dialog => "dialog",
            WebSemanticsRole.AlertDialog => "alertdialog",
            WebSemanticsRole.Tab => "tab",
            WebSemanticsRole.TabPanel => "tabpanel",
            WebSemanticsRole.TabList => "tablist",
            WebSemanticsRole.Table => "table",
            WebSemanticsRole.Row => "row",
            WebSemanticsRole.Cell => "cell",
            WebSemanticsRole.ColumnHeader => "columnheader",
            WebSemanticsRole.RadioGroup => "radiogroup",
            WebSemanticsRole.Menu => "menu",
            WebSemanticsRole.MenuBar => "menubar",
            WebSemanticsRole.MenuItem => "menuitem",
            WebSemanticsRole.MenuItemCheckbox => "menuitemcheckbox",
            WebSemanticsRole.MenuItemRadio => "menuitemradio",
            WebSemanticsRole.Alert => "alert",
            WebSemanticsRole.Status => "status",
            WebSemanticsRole.List => "list",
            WebSemanticsRole.ListItem => "listitem",
            WebSemanticsRole.Complementary => "complementary",
            WebSemanticsRole.ContentInfo => "contentinfo",
            WebSemanticsRole.Main => "main",
            WebSemanticsRole.Navigation => "navigation",
            WebSemanticsRole.Region => "region",
            WebSemanticsRole.ProgressBar => "progressbar",
            _ => null,
        };

        // LabelAndValue: a node with children always uses aria-label; a leaf uses its role's preference.
        string? label = ComputeDomLabel(node, role);
        string? text = null;
        bool textAsSpan = false;
        string? imageLabel = null;
        switch (role)
        {
            case WebSemanticsRole.Generic:
                if (label is not null && node.HasChildren)
                {
                    ariaRole = "group";
                    attributes["aria-label"] = label;
                }
                else if (label is not null)
                {
                    text = label;
                    textAsSpan = true;
                }

                break;
            case WebSemanticsRole.Route:
            case WebSemanticsRole.Dialog:
            case WebSemanticsRole.AlertDialog:
                if (node.HasFlag(SemanticsFlags.NamesRoute))
                {
                    attributes["aria-label"] = node.Label;
                }

                if (role != WebSemanticsRole.Route)
                {
                    attributes["aria-modal"] = "true";
                }

                break;
            case WebSemanticsRole.Image:
                if (!node.HasChildren && label is not null)
                {
                    ariaRole = "img";
                    attributes["aria-label"] = label;
                }
                else if (node.HasChildren)
                {
                    imageLabel = label;
                }

                break;
            case WebSemanticsRole.TextField:
                if (node.Label.Length > 0)
                {
                    attributes["aria-label"] = node.Label;
                }

                if (node.HasFlag(SemanticsFlags.IsMultiline))
                {
                    attributes["aria-multiline"] = "true";
                }

                if (node.HasFlag(SemanticsFlags.IsReadOnly))
                {
                    attributes["aria-readonly"] = "true";
                }

                break;
            case WebSemanticsRole.LoadingSpinner:
                break;
            default:
                if (label is not null)
                {
                    WebLabelRepresentation representation = node.HasChildren
                        ? WebLabelRepresentation.AriaLabel
                        : PreferredRepresentation(role);
                    if (representation == WebLabelRepresentation.AriaLabel)
                    {
                        attributes["aria-label"] = label;
                    }
                    else
                    {
                        text = label;
                        textAsSpan = representation == WebLabelRepresentation.SizedSpan;
                    }
                }

                break;
        }

        if (ariaRole is not null)
        {
            attributes["role"] = ariaRole;
        }

        // The hint describes the label: written only when the node has one.
        if (label is not null && node.Hint.Length > 0 && role != WebSemanticsRole.TextField)
        {
            attributes["aria-description"] = node.Hint;
        }
        else if (role == WebSemanticsRole.TextField && node.Hint.Trim().Length > 0)
        {
            attributes["aria-description"] = node.Hint;
        }

        bool enabled = node.IsEnabled;
        if (!enabled && role is WebSemanticsRole.Button or WebSemanticsRole.Checkable or WebSemanticsRole.MenuItem
                or WebSemanticsRole.MenuItemCheckbox or WebSemanticsRole.MenuItemRadio or WebSemanticsRole.TextField)
        {
            attributes["aria-disabled"] = "true";
        }

        bool checkable = role is WebSemanticsRole.Checkable
            or WebSemanticsRole.MenuItemCheckbox
            or WebSemanticsRole.MenuItemRadio;
        if (checkable)
        {
            bool isChecked = node.HasFlag(SemanticsFlags.IsChecked) || node.HasFlag(SemanticsFlags.IsToggled);
            attributes["aria-checked"] = isChecked ? "true" : "false";
        }

        if (role == WebSemanticsRole.MenuItem && node.HasFlag(SemanticsFlags.HasExpandedState))
        {
            attributes["aria-haspopup"] = "menu";
        }

        if (role == WebSemanticsRole.Link && node.Data.LinkUrl is { } url && url.OriginalString.Length > 0)
        {
            attributes["href"] = url.OriginalString;
        }

        if (role == WebSemanticsRole.ProgressBar)
        {
            AddProgressAttributes(node, attributes);
        }

        // Selectable: rows and tabs are selected, everything else is current; a checkable wins.
        if (node.HasFlag(SemanticsFlags.HasSelectedState) && !node.HasFlag(SemanticsFlags.HasCheckedState
                | SemanticsFlags.HasToggledState))
        {
            string selected = node.HasFlag(SemanticsFlags.IsSelected) ? "true" : "false";
            attributes[role is WebSemanticsRole.Row or WebSemanticsRole.Tab ? "aria-selected" : "aria-current"] =
                selected;
        }

        if (node.HasFlag(SemanticsFlags.HasExpandedState))
        {
            attributes["aria-expanded"] = node.HasFlag(SemanticsFlags.IsExpanded) ? "true" : "false";
        }

        if (node.HasFlag(SemanticsFlags.HasRequiredState))
        {
            attributes["aria-required"] = node.HasFlag(SemanticsFlags.IsRequired) ? "true" : "false";
        }

        if (node.Data.ValidationResult != SemanticsValidationResult.None)
        {
            attributes["aria-invalid"] =
                node.Data.ValidationResult == SemanticsValidationResult.Invalid ? "true" : "false";
        }

        if (node.Data.Locale is { } locale
            && (node.Parent?.Data.Locale is not { } parentLocale || parentLocale != locale))
        {
            attributes["lang"] = locale.ToLanguageTag();
        }

        bool tappable = enabled
                        && node.HasAction(SemanticsActions.Tap)
                        && role is not (WebSemanticsRole.MenuItemCheckbox or WebSemanticsRole.MenuItemRadio
                            or WebSemanticsRole.TextField or WebSemanticsRole.Incrementable);
        if (tappable)
        {
            attributes["flt-tappable"] = string.Empty;
        }

        bool focusable = node.HasFlag(SemanticsFlags.IsFocusable) && role != WebSemanticsRole.Incrementable;
        bool focused = focusable && node.IsFocused && enabled;
        return new WebSemanticsElement(
            role,
            tag,
            attributes,
            text,
            textAsSpan,
            tappable,
            focusable,
            focused,
            imageLabel);
    }

    /// <summary>Writes the DOM update for one commit as the JSON the browser script applies.</summary>
    /// <param name="bridge">The bridge that committed.</param>
    /// <param name="nodes">The nodes to (re)render; every node of the tree for a full render.</param>
    /// <param name="removed">The ids of the nodes whose elements are removed.</param>
    public static string WriteUpdate(
        AccessibilityBridge bridge,
        IEnumerable<AccessibilityNode> nodes,
        IEnumerable<int> removed)
    {
        ArgumentNullException.ThrowIfNull(bridge);
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(removed);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            if (bridge.Root is { } root)
            {
                writer.WriteNumber("root", root.Id);
            }
            else
            {
                writer.WriteNull("root");
            }

            writer.WriteNumber("devicePixelRatio", bridge.DevicePixelRatio);
            writer.WriteStartArray("removed");
            foreach (int id in removed)
            {
                writer.WriteNumberValue(id);
            }

            writer.WriteEndArray();
            writer.WriteStartArray("nodes");
            foreach (AccessibilityNode node in nodes)
            {
                WriteNode(writer, node);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Every node of the bridge's tree, parents before children.</summary>
    public static IReadOnlyList<AccessibilityNode> AllNodes(AccessibilityBridge bridge)
    {
        ArgumentNullException.ThrowIfNull(bridge);
        var nodes = new List<AccessibilityNode>(bridge.NodeCount);
        if (bridge.Root is { } root)
        {
            Collect(root);
        }

        return nodes;

        void Collect(AccessibilityNode node)
        {
            nodes.Add(node);
            foreach (AccessibilityNode child in node.Children)
            {
                Collect(child);
            }
        }
    }

    /// <summary>Turns an action name the browser script reports into a framework action.</summary>
    /// <returns>The action and its argument, or <c>null</c> for an unknown name.</returns>
    public static (SemanticsActions Action, object? Arguments)? ParseAction(string name, string? argument)
    {
        ArgumentNullException.ThrowIfNull(name);
        switch (name)
        {
            case "tap":
                return (SemanticsActions.Tap, null);
            case "focus":
                return (SemanticsActions.Focus, null);
            case "increase":
                return (SemanticsActions.Increase, null);
            case "decrease":
                return (SemanticsActions.Decrease, null);
            case "scrollToOffset":
                string[] parts = (argument ?? "0,0").Split(',');
                double x = double.Parse(parts[0], CultureInfo.InvariantCulture);
                double y = parts.Length > 1 ? double.Parse(parts[1], CultureInfo.InvariantCulture) : 0;
                return (SemanticsActions.ScrollToOffset, new[] { x, y });
            default:
                return null;
        }
    }

    private static void WriteNode(Utf8JsonWriter writer, AccessibilityNode node)
    {
        WebSemanticsElement element = Describe(node);
        writer.WriteStartObject();
        writer.WriteNumber("id", node.Id);
        writer.WriteString("tag", element.Tag);
        writer.WriteString("kind", JsonNamingPolicy.CamelCase.ConvertName(element.Role.ToString()));
        writer.WriteStartObject("attrs");
        foreach (KeyValuePair<string, string> attribute in element.Attributes)
        {
            writer.WriteString(attribute.Key, attribute.Value);
        }

        writer.WriteEndObject();
        if (element.Text is { } text)
        {
            writer.WriteString("text", text);
            writer.WriteBoolean("textAsSpan", element.TextAsSpan);
        }

        if (element.ImageLabel is { } imageLabel)
        {
            writer.WriteString("imageLabel", imageLabel);
        }

        writer.WriteBoolean("tappable", element.Tappable);
        writer.WriteBoolean("focusable", element.Focusable);
        writer.WriteBoolean("focused", element.Focused);
        writer.WriteStartArray("rect");
        writer.WriteNumberValue(node.Rect.Left);
        writer.WriteNumberValue(node.Rect.Top);
        writer.WriteNumberValue(node.Rect.Width);
        writer.WriteNumberValue(node.Rect.Height);
        writer.WriteEndArray();
        if (node.Transform is { } transform && !transform.IsIdentity())
        {
            writer.WriteStartArray("transform");
            foreach (double value in transform.Storage)
            {
                writer.WriteNumberValue(value);
            }

            writer.WriteEndArray();
        }

        writer.WriteStartArray("children");
        foreach (AccessibilityNode child in node.Children)
        {
            writer.WriteNumberValue(child.Id);
        }

        writer.WriteEndArray();
        writer.WriteStartArray("hitTest");
        foreach (AccessibilityNode child in node.ChildrenInHitTestOrder)
        {
            writer.WriteNumberValue(child.Id);
        }

        writer.WriteEndArray();
        if (element.Role == WebSemanticsRole.Incrementable)
        {
            writer.WriteStartObject("range");
            writer.WriteString("value", node.Value);
            writer.WriteBoolean("canIncrease", node.Data.IncreasedValue.Length > 0);
            writer.WriteBoolean("canDecrease", node.Data.DecreasedValue.Length > 0);
            writer.WriteBoolean("enabled", node.IsEnabled);
            writer.WriteEndObject();
        }

        if (element.Role == WebSemanticsRole.Scrollable)
        {
            WriteScroll(writer, node);
        }

        writer.WriteEndObject();
    }

    // SemanticScrollable: the axis comes from the scroll actions; the overflow spans the scroll extent
    // plus the viewport, and the DOM scroll offset mirrors the framework's scroll position.
    private static void WriteScroll(Utf8JsonWriter writer, AccessibilityNode node)
    {
        bool vertical = node.HasAction(SemanticsActions.ScrollUp | SemanticsActions.ScrollDown);
        bool horizontal = node.HasAction(SemanticsActions.ScrollLeft | SemanticsActions.ScrollRight);
        writer.WriteStartObject("scroll");
        writer.WriteString("axis", vertical ? "vertical" : horizontal ? "horizontal" : "none");
        writer.WriteNumber("position", Finite(node.Data.ScrollPosition));
        writer.WriteNumber("extentMin", Finite(node.Data.ScrollExtentMin));
        writer.WriteNumber("extentMax", Finite(node.Data.ScrollExtentMax));
        writer.WriteEndObject();

        static double Finite(double? value) => value is { } number && double.IsFinite(number) ? number : 0;
    }

    private static bool IsHeading(AccessibilityNode node) =>
        node.Data.HeadingLevel != 0
        || (node.HasFlag(SemanticsFlags.IsHeader) && node.Label.Length > 0 && !node.HasChildren);

    private static int EffectiveHeadingLevel(AccessibilityNode node) =>
        node.Data.HeadingLevel is > 0 and <= 6 ? node.Data.HeadingLevel : 2;

    private static WebLabelRepresentation PreferredRepresentation(WebSemanticsRole role) => role switch
    {
        WebSemanticsRole.Button or WebSemanticsRole.Link or WebSemanticsRole.Heading =>
            WebLabelRepresentation.DomText,
        WebSemanticsRole.Header or WebSemanticsRole.Cell => WebLabelRepresentation.SizedSpan,
        _ => WebLabelRepresentation.AriaLabel,
    };

    // ProgressBar: a numeric value is the current value; a percentage maps into the min/max range.
    private static void AddProgressAttributes(AccessibilityNode node, SortedDictionary<string, string> attributes)
    {
        double? min = Parse(node.Data.MinValue);
        double? max = Parse(node.Data.MaxValue);
        if (node.Data.MinValue is { Length: > 0 } minText)
        {
            attributes["aria-valuemin"] = minText;
        }

        if (node.Data.MaxValue is { Length: > 0 } maxText)
        {
            attributes["aria-valuemax"] = maxText;
        }

        string value = node.Value;
        if (value.Length == 0)
        {
            return;
        }

        if (Parse(value) is { } number)
        {
            attributes["aria-valuenow"] = number.ToString(CultureInfo.InvariantCulture);
        }
        else if (value.EndsWith('%') && Parse(value[..^1]) is { } percent && min is { } low && max is { } high)
        {
            attributes["aria-valuenow"] = (low + (percent / 100 * (high - low))).ToString(CultureInfo.InvariantCulture);
            attributes["aria-valuetext"] = value;
        }
        else
        {
            attributes["aria-valuetext"] = value;
        }

        static double? Parse(string? text) =>
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) ? number : null;
    }
}
