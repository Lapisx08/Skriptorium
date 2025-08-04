using ControlzEx.Theming;
using MahApps.Metro;
using Skriptorium.Properties;
using System;
using System.Linq;
using System.Windows;

namespace Skriptorium
{
    public partial class App : Application
    {
        private const string ThemePathTemplate = "/AvalonDock.Themes.VS2013;component/{0}Theme.xaml";
        private const string DefaultTheme = "Light.Steel";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Settings.Default.Reload();

            // Theme aus Einstellungen oder Fallback
            string themeName = string.IsNullOrWhiteSpace(Settings.Default.Theme)
                ? DefaultTheme
                : Settings.Default.Theme;

            // Theme setzen
            ThemeManager.Current.ChangeTheme(this, themeName);

            // AvalonDock-Theme auf Basis des aktuell aktiven MahApps-Themes anwenden
            ApplyAvalonDockThemeFromMahApps();

            // Theme-Wechsel-Handler synchronisieren
            ThemeManager.Current.ThemeChanged += (_, __) =>
                ApplyAvalonDockThemeFromMahApps();

            // Hauptfenster starten
            new UI.MainWindow().Show();
        }

        private void ApplyAvalonDockThemeFromMahApps()
        {
            // Aktuelles MahApps-Theme ermitteln
            var currentTheme = ThemeManager.Current.DetectTheme(this);

            if (currentTheme == null)
            {
                MessageBox.Show("Konnte aktuelles MahApps-Theme nicht ermitteln. Fallback auf Light.",
                    "Theme-Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                ApplyAvalonDockTheme("Light");
                return;
            }

            // BaseColorScheme von MahApps ist "Light" oder "Dark"
            ApplyAvalonDockTheme(currentTheme.BaseColorScheme);
        }

        private void ApplyAvalonDockTheme(string baseTheme)
        {
            // Vorhandenes AvalonDock-Theme entfernen
            var themeDictionary = Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source?.ToString().Contains("Theme.xaml") == true);

            if (themeDictionary != null)
                Current.Resources.MergedDictionaries.Remove(themeDictionary);

            // Validiere baseTheme
            if (baseTheme != "Light" && baseTheme != "Dark")
            {
                MessageBox.Show($"Ungültiges Theme: {baseTheme}. Fallback auf Light.",
                    "Theme-Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                baseTheme = "Light";
            }

            try
            {
                Current.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri(string.Format(ThemePathTemplate, baseTheme), UriKind.Relative)
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden des {baseTheme} Themes: {ex.Message}",
                    "Theme-Ladefehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
