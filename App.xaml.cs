using ControlzEx.Theming;
using MahApps.Metro;
using Skriptorium.Properties;
using System;
using System.Windows;

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

            // Hauptfenster starten
            var main = new UI.MainWindow();
            main.Show();
        }
    }
}
