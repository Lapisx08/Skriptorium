using ControlzEx.Theming;
using MahApps.Metro;
using Skriptorium.Managers;
using Skriptorium.Properties;
using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

namespace Skriptorium
{
    public partial class App : Application
    {
        private const string ThemePathTemplate = "/AvalonDock.Themes.VS2013;component/{0}Theme.xaml";
        private const string DefaultTheme = "Light.Steel";
        private const string MutexName = "SkriptoriumSingleInstance";
        private const string PipeName = "SkriptoriumPipe";
        private Mutex _mutex;

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

            // Single Instance Check
            bool isNewInstance = false;
            _mutex = new Mutex(true, MutexName, out isNewInstance);

            if (!isNewInstance)
            {
                // Zweite Instanz: Sende Argumente an laufende Instanz und beende
                SendArgsToRunningInstance(e.Args);
                Shutdown();
                return;
            }

            // Erste Instanz: Starte Hauptfenster
            var mainWindow = new UI.MainWindow();
            mainWindow.Show();

            // Starte Pipe-Server in MainWindow (nachdem es geladen ist)
            mainWindow.Loaded += (s, ev) =>
            {
                StartPipeServer(mainWindow);
            };

            // Wenn Argumente übergeben (z.B. erste Instanz mit Datei)
            if (e.Args.Length > 0)
            {
                foreach (var arg in e.Args)
                {
                    string filePath = arg;
                    if (File.Exists(filePath))
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
        }

        private void SendArgsToRunningInstance(string[] args)
        {
            using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
            {
                try
                {
                    client.Connect(1000); // Timeout 1 Sekunde
                    using (var writer = new StreamWriter(client))
                    {
                        foreach (var arg in args)
                        {
                            writer.WriteLine(arg);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Optional: Zeige eine Fehlermeldung, falls die Pipe nicht erreichbar ist
                    MessageBox.Show($"Fehler beim Öffnen der Datei in laufender Instanz: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void StartPipeServer(UI.MainWindow mainWindow)
        {
            var thread = new Thread(() =>
            {
                while (true)
                {
                    using (var server = new NamedPipeServerStream(PipeName, PipeDirection.In))
                    {
                        try
                        {
                            server.WaitForConnection();
                            using (var reader = new StreamReader(server))
                            {
                                string filePath;
                                while ((filePath = reader.ReadLine()) != null)
                                {
                                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
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
                        }
                        catch (Exception ex)
                        {
                            // Optional: Logge Fehler im Pipe-Server
                        }
                    }
                }
            });
            thread.IsBackground = true;
            thread.Start();
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