using System.Collections;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Views.Autofill;
using Plumix.UI;
using Rect = Android.Graphics.Rect;
using View = Android.Views.View;

namespace Plumix.Impeller.Android;

// C#-only host infrastructure parity sources:
// flutter/engine/src/flutter/shell/platform/android/io/flutter/plugin/editing/TextInputPlugin.java (the
// "Autofill" section: the virtual structure of the client's autofill group, notifyViewEntered/Exited/ValueChanged,
// applying filled values, saveEditableSizeAndTransform's client rect) and
// .../embedding/engine/systemchannels/TextInputChannel.java (Configuration.Autofill.fromJson and
// translateAutofillHint). Every field of the group is a virtual child of the view whose id is the hash of its
// autofill identifier.

/// <summary>The view's side of Android autofill for the attached text input client.</summary>
internal sealed class AndroidAutofill
{
    private readonly View _view;
    private readonly HostTextInputPlugin _plugin;
    private readonly AutofillManager? _afm;

    // The attached client's autofill entry, and the fields of its group (the client alone outside a group).
    private AutofillField? _current;
    private Dictionary<int, AutofillField>? _fields;

    public AndroidAutofill(View view, HostTextInputPlugin plugin)
    {
        _view = view;
        _plugin = plugin;
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            _afm = (AutofillManager?)view.Context?.GetSystemService(Java.Lang.Class.FromType(typeof(AutofillManager)));
        }
    }

    /// <summary><c>updateAutofillConfigurationIfNeeded</c>: reads the client's configuration.</summary>
    public void UpdateConfiguration(IDictionary? configuration)
    {
        _current = configuration?["autofill"] is IDictionary autofill ? AutofillField.FromJson(autofill) : null;
        if (!OperatingSystem.IsAndroidVersionAtLeast(26) || _current is null)
        {
            _fields = null;
            return;
        }

        _fields = [];
        if (configuration!["fields"] is not IList fields)
        {
            _fields[_current.Id] = _current;
            return;
        }

        foreach (object? field in fields)
        {
            if (field is IDictionary { } fieldConfiguration
                && fieldConfiguration["autofill"] is IDictionary fieldAutofill)
            {
                AutofillField entry = AutofillField.FromJson(fieldAutofill);
                _fields[entry.Id] = entry;
                _afm?.NotifyValueChanged(_view, entry.Id, AutofillValue.ForText(entry.Text));
            }
        }
    }

    public void NotifyViewEntered()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26) || _afm is null || _fields is null || _current is null)
        {
            return;
        }

        int[] offset = new int[2];
        _view.GetLocationOnScreen(offset);
        var rect = new Rect(ClientRect() ?? new Rect());
        rect.Offset(offset[0], offset[1]);
        _afm.NotifyViewEntered(_view, _current.Id, rect);
    }

    public void NotifyViewExited()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(26) && _afm is not null && _fields is not null
            && _current is not null)
        {
            _afm.NotifyViewExited(_view, _current.Id);
        }
    }

    public void NotifyValueChanged(string newValue)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(26) && _afm is not null && _fields is not null
            && _current is not null)
        {
            _afm.NotifyValueChanged(_view, _current.Id, AutofillValue.ForText(newValue));
        }
    }

    /// <summary>The <c>finishAutofillContext</c> call: save what was entered, or drop the session.</summary>
    public void FinishAutofillContext(bool shouldSave)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26) || _afm is null)
        {
            return;
        }

        if (shouldSave)
        {
            _afm.Commit();
        }
        else
        {
            _afm.Cancel();
        }
    }

    /// <summary>
    /// <c>onProvideAutofillVirtualStructure</c>: one text child per field; the attached one at the client rect
    /// with the editable's text, the others as visible 1×1 children with their last known text.
    /// </summary>
    public void OnProvideAutofillVirtualStructure(ViewStructure structure, string editableText)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26) || _fields is null || _current is null)
        {
            return;
        }

        AutofillId? parentId = structure.AutofillId;
        Rect? clientRect = ClientRect();
        int index = 0;
        foreach ((int autofillId, AutofillField field) in _fields)
        {
            structure.AddChildCount(1);
            ViewStructure child = structure.NewChild(index++)!;
            child.SetAutofillId(parentId!, autofillId);
            if (field.Hints.Length > 0)
            {
                child.SetAutofillHints(field.Hints);
            }

            child.SetAutofillType(AutofillType.Text);
            child.SetVisibility(ViewStates.Visible);
            if (field.HintText is { } hintText)
            {
                child.Hint = hintText;
            }

            if (_current.Id == autofillId && clientRect is not null)
            {
                child.SetDimens(clientRect.Left, clientRect.Top, 0, 0, clientRect.Width(), clientRect.Height());
                child.SetAutofillValue(AutofillValue.ForText(editableText));
            }
            else
            {
                child.SetDimens(0, 0, 0, 0, 1, 1);
                child.SetAutofillValue(AutofillValue.ForText(field.Text));
            }
        }
    }

    /// <summary>
    /// <c>autofill</c>: the attached field's value goes to <paramref name="setCurrentText"/> (the editable), the
    /// group's other fields to the framework by their autofill identifiers.
    /// </summary>
    public void Autofill(SparseArray values, Action<string> setCurrentText)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26) || _fields is null || _current is null)
        {
            return;
        }

        var editingValues = new Dictionary<string, string>();
        for (int i = 0; i < values.Size(); i++)
        {
            if (!_fields.TryGetValue(values.KeyAt(i), out AutofillField? field)
                || (values.ValueAt(i) as AutofillValue)?.TextValueFormatted?.ToString() is not { } value)
            {
                continue;
            }

            if (field.UniqueIdentifier == _current.UniqueIdentifier)
            {
                setCurrentText(value);
            }
            else
            {
                editingValues[field.UniqueIdentifier] = value;
            }
        }

        _plugin.UpdateEditingStateWithTag(editingValues);
    }

    // saveEditableSizeAndTransform: the bounds of the transformed editable, in physical pixels.
    private Rect? ClientRect()
    {
        if (_plugin.EditableSizeAndTransform is not ({ } size, { } transform))
        {
            return null;
        }

        double[] matrix = transform.Storage;
        bool isAffine = matrix[3] == 0 && matrix[7] == 0 && matrix[15] == 1;
        double minX = matrix[12] / matrix[15];
        double maxX = minX;
        double minY = matrix[13] / matrix[15];
        double maxY = minY;

        void Inspect(double x, double y)
        {
            double w = isAffine ? 1 : 1 / (matrix[3] * x + matrix[7] * y + matrix[15]);
            double tx = (matrix[0] * x + matrix[4] * y + matrix[12]) * w;
            double ty = (matrix[1] * x + matrix[5] * y + matrix[13]) * w;
            if (tx < minX)
            {
                minX = tx;
            }
            else if (tx > maxX)
            {
                maxX = tx;
            }

            if (ty < minY)
            {
                minY = ty;
            }
            else if (ty > maxY)
            {
                maxY = ty;
            }
        }

        Inspect(size.Width, 0);
        Inspect(size.Width, size.Height);
        Inspect(0, size.Height);
        float density = _view.Context!.Resources!.DisplayMetrics!.Density;
        return new Rect(
            (int)(minX * density),
            (int)(minY * density),
            (int)Math.Ceiling(maxX * density),
            (int)Math.Ceiling(maxY * density));
    }

    /// <summary><c>TextInputChannel.Configuration.Autofill</c>.</summary>
    private sealed record AutofillField(string UniqueIdentifier, string[] Hints, string? HintText, string Text)
    {
        // The virtual view id: Java's String.hashCode of the identifier, as the engine uses.
        public int Id { get; } = JavaHashCode(UniqueIdentifier);

        public static AutofillField FromJson(IDictionary json)
        {
            var hints = (IList)json["hints"]!;
            string[] autofillHints = new string[hints.Count];
            for (int i = 0; i < hints.Count; i++)
            {
                autofillHints[i] = TranslateAutofillHint((string)hints[i]!);
            }

            string text = (json["editingValue"] as IDictionary)?["text"] as string ?? string.Empty;
            return new AutofillField((string)json["uniqueIdentifier"]!, autofillHints, json["hintText"] as string,
                text);
        }

        private static int JavaHashCode(string value)
        {
            int hash = 0;
            foreach (char c in value)
            {
                hash = unchecked(31 * hash + c);
            }

            return hash;
        }

        // TextInputChannel.translateAutofillHint: the framework's hint names as Android's autofill hints.
        private static string TranslateAutofillHint(string hint)
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                return hint;
            }

            return hint switch
            {
                "addressCity" => "addressLocality",
                "addressState" => "addressRegion",
                "birthday" => "birthDateFull",
                "birthdayDay" => "birthDateDay",
                "birthdayMonth" => "birthDateMonth",
                "birthdayYear" => "birthDateYear",
                "countryName" => "addressCountry",
                "creditCardExpirationDate" => View.AutofillHintCreditCardExpirationDate,
                "creditCardExpirationDay" => View.AutofillHintCreditCardExpirationDay,
                "creditCardExpirationMonth" => View.AutofillHintCreditCardExpirationMonth,
                "creditCardExpirationYear" => View.AutofillHintCreditCardExpirationYear,
                "creditCardNumber" => View.AutofillHintCreditCardNumber,
                "creditCardSecurityCode" => View.AutofillHintCreditCardSecurityCode,
                "email" => View.AutofillHintEmailAddress,
                "familyName" => "personFamilyName",
                "fullStreetAddress" => "streetAddress",
                "gender" => "gender",
                "givenName" => "personGivenName",
                "middleInitial" => "personMiddleInitial",
                "middleName" => "personMiddleName",
                "name" => "personName",
                "namePrefix" => "personNamePrefix",
                "nameSuffix" => "personNameSuffix",
                "newPassword" => "newPassword",
                "newUsername" => "newUsername",
                "oneTimeCode" => "smsOTPCode",
                "password" => View.AutofillHintPassword,
                "postalAddress" => View.AutofillHintPostalAddress,
                "postalAddressExtended" => "extendedAddress",
                "postalAddressExtendedPostalCode" => "extendedPostalCode",
                "postalCode" => View.AutofillHintPostalCode,
                "telephoneNumber" => "phoneNumber",
                "telephoneNumberCountryCode" => "phoneCountryCode",
                "telephoneNumberDevice" => "phoneNumberDevice",
                "telephoneNumberNational" => "phoneNational",
                "username" => View.AutofillHintUsername,
                _ => hint,
            };
        }
    }
}
