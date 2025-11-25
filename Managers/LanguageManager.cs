using System;
using System.Linq;
using System.Windows;

namespace Skriptorium.Managers
{
    public static class LanguageManager
    {
        public static event EventHandler LanguageChanged;

        public static void ChangeLanguage(string langCode)
        {
            var dicts = Application.Current.Resources.MergedDictionaries;

            // Vorhandenes Sprach-Dictionary suchen und entfernen
            var oldDict = dicts.FirstOrDefault(d =>
                d.Source != null &&
                d.Source.OriginalString.Contains("StringResources.")
            );

            if (oldDict != null)
                dicts.Remove(oldDict);

            // Neues Sprach-Dictionary hinzufügen
            // Wichtig: Assembly-Name im Pack-URI anpassen (hier Skriptorium)
            var newDict = new ResourceDictionary
            {
                Source = new Uri($"/Skriptorium;component/UI/Languages/StringResources.{langCode}.xaml",
                                 UriKind.Relative)
            };

            dicts.Add(newDict);

            // UI über Sprachänderung informieren
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
