using ControlzEx.Theming;
using MahApps.Metro;
using Skriptorium.Properties;
using System;
using System.Windows;
using AvalonDock;
using AvalonDock.Controls;

namespace Skriptorium
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Settings.Default.Reload();

            string themeName = Settings.Default.Theme;
            if (string.IsNullOrWhiteSpace(themeName))
            {
                themeName = "Light.Steel";  // Fallback: z.B. Light.Steel
                Settings.Default.Theme = themeName;
                Settings.Default.Save();
            }

            // Theme aufteilen: Schema: Light.Steel → baseTheme: Light, accent: Steel
            var parts = themeName.Split('.');
            string baseTheme = parts[0];
            string accent = parts.Length > 1 ? parts[1] : "Steel";

            // Setze Theme mit MahApps ThemeManager
            ThemeManager.Current.ChangeTheme(this, $"{baseTheme}.{accent}");

            // AvalonDock Brush-Farben initial setzen
            ApplyAvalonDockBrushes(baseTheme);

            // Reagiere auf zukünftige Theme-Wechsel
            ThemeManager.Current.ThemeChanged += (s, args) =>
            {
                string newBaseTheme = args.NewTheme.BaseColorScheme;
                ApplyAvalonDockBrushes(newBaseTheme);
            };

            // Hauptfenster starten
            var main = new UI.MainWindow();
            main.Show();
        }

        private void ApplyAvalonDockBrushes(string baseTheme)
        {
            // Entferne alte AvalonDock-Brushes
            var existing = Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source?.ToString().Contains("Theme.xaml") == true);

            if (existing != null)
                Current.Resources.MergedDictionaries.Remove(existing);

            // Debugging: Prüfe das aktuelle Theme
            var currentTheme = ThemeManager.Current.DetectTheme(this)?.BaseColorScheme;
            MessageBox.Show($"Aktuelles Theme: {currentTheme}, Übergebenes baseTheme: {baseTheme}",
                            "Debug Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
