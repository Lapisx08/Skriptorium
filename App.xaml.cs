using ControlzEx.Theming;
using MahApps.Metro;
using Skriptorium.Managers;
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

            ThemeManager.Current.ChangeTheme(this, themeName);
            ApplyAvalonDockThemeFromMahApps();
            ThemeManager.Current.ThemeChanged += (_, __) =>
                ApplyAvalonDockThemeFromMahApps();

            // Hauptfenster starten
            var mainWindow = new UI.MainWindow();
            mainWindow.Show();

            // Wenn Windows beim Start eine Datei übergibt (Doppelklick im Explorer)
            if (e.Args.Length > 0)
            {
                string filePath = e.Args[0];
                if (System.IO.File.Exists(filePath))
                {
                    DataManager.OpenFile(filePath, (content, path) =>
                    {
                        mainWindow.Dispatcher.Invoke(() =>
                        {
                            mainWindow.OpenFileInNewTab(content, path);
                        });
                    });
                }
            }
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