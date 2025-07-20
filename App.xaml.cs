using System;
using System.Windows;
using Skriptorium.Properties;   // <<< notwendig, damit Settings gefunden wird

namespace Skriptorium
{
    public partial class App : Application
    {
        // === NEU: Startup-Handler ===
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 1) Settings neu laden (holt Defaults aus Settings.settings)
            Settings.Default.Reload();

            // 2) Gespeichertes Theme ermitteln, Fallback "Light"
            string theme = Settings.Default.Theme;
            if (string.IsNullOrWhiteSpace(theme))
            {
                theme = "Light";
                Settings.Default.Theme = theme;
                Settings.Default.Save();
            }

            // 3) Pack-URI auf das ResourceDictionary im gleichen Assembly
            var uri = new Uri($"pack://application:,,,/UI/Themes/{theme}.xaml",
                              UriKind.Absolute);
            var dict = new ResourceDictionary { Source = uri };

            // 4) Globale Resources tauschen
            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(dict);

            // 5) Hauptfenster starten
            var main = new UI.MainWindow();
            main.Show();
        }
    }
}
