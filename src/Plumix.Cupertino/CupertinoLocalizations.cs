using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: flutter/packages/flutter/lib/src/cupertino/localizations.dart

public abstract class CupertinoLocalizations
{
    public virtual string AlertDialogLabel => "Alert";

    public virtual string CutButtonLabel => "Cut";

    public virtual string CopyButtonLabel => "Copy";

    public virtual string PasteButtonLabel => "Paste";

    public virtual string SelectAllButtonLabel => "Select all";

    public static CupertinoLocalizations Of(BuildContext context)
    {
        return Localizations.Of<CupertinoLocalizations>(context);
    }
}

public sealed class DefaultCupertinoLocalizations : CupertinoLocalizations
{
    private DefaultCupertinoLocalizations()
    {
    }

    public static DefaultCupertinoLocalizations Instance { get; } = new();

    public static LocalizationsDelegate<CupertinoLocalizations> Delegate { get; } =
        new DefaultCupertinoLocalizationsDelegate();

    private sealed class DefaultCupertinoLocalizationsDelegate : LocalizationsDelegate<CupertinoLocalizations>
    {
        public override bool IsSupported(Locale locale) => true;

        public override CupertinoLocalizations LoadTyped(Locale locale)
        {
            return Instance;
        }

        public override bool ShouldReload(LocalizationsDelegate oldDelegate) => false;
    }
}
