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
            string themeName = string.IsNullOrWhiteSpace(Settings.Default.Theme)
                ? DefaultTheme
                : Settings.Default.Theme;

            ThemeManager.Current.ChangeTheme(this, themeName);
            ApplyAvalonDockThemeFromMahApps();
            ThemeManager.Current.ThemeChanged += (_, __) =>
                ApplyAvalonDockThemeFromMahApps();

            bool isNewInstance = false;
            _mutex = new Mutex(true, MutexName, out isNewInstance);

            if (!isNewInstance)
            {
                SendArgsToRunningInstance(e.Args);
                Shutdown();
                return;
            }

            var mainWindow = new UI.MainWindow();
            StartPipeServer(mainWindow);

            // Liste für Tab-Daten vorbereiten
            var tabsToAdd = new List<(string Content, string Title, string FilePath)>();

            // Vorherige Tabs laden und leere, nicht gespeicherte Tabs filtern
            var tabStates = DataManager.LoadOpenTabs();
            foreach (var tabState in tabStates)
            {
                // Nur Tabs mit Inhalt oder Dateipfad hinzufügen
                if (!string.IsNullOrWhiteSpace(tabState.Content) || !string.IsNullOrWhiteSpace(tabState.FilePath))
                {
                    string tabTitle = tabState.FilePath != null ? Path.GetFileName(tabState.FilePath) : "Neu";
                    tabsToAdd.Add((tabState.Content, tabTitle, tabState.FilePath));
                }
                else
                {
                    Console.WriteLine($"Skipped empty unsaved tab: Content='{tabState.Content}', FilePath='{tabState.FilePath}'");
                }
            }

            // Neues Tab nur hinzufügen, wenn keine relevanten Tabs geladen wurden
            if (tabsToAdd.Count == 0)
            {
                tabsToAdd.Add((string.Empty, "Neu", null)); // Neues Tab am Ende
            }

            // Kommandozeilenargumente verarbeiten
            if (e.Args.Length > 0)
            {
                foreach (var arg in e.Args)
                {
                    string filePath = arg;
                    if (File.Exists(filePath))
                    {
                        DataManager.OpenFile(filePath, (content, path) =>
                        {
                            string tabTitle = Path.GetFileName(path);
                            tabsToAdd.Add((content, tabTitle, path)); // Dateien am Ende hinzufügen
                        }, (error) =>
                        {
                            Console.WriteLine($"Fehler beim Laden der Datei {filePath}: {error}");
                        });
                    }
                }
            }

            // Logging der Tab-Reihenfolge
            Console.WriteLine("Tabs to add: " + string.Join(", ", tabsToAdd.Select(t => t.Title)));

            // Alle Tabs auf einmal hinzufügen, ohne Aktivierung
            mainWindow.Dispatcher.Invoke(() =>
            {
                // DockingManager deaktivieren
                bool wasEnabled = mainWindow._tabManager.DisableDockingManager();
                try
                {
                    for (int i = 0; i < tabsToAdd.Count; i++)
                    {
                        var (content, title, filePath) = tabsToAdd[i];
                        mainWindow._tabManager.AddNewTab(content, title, filePath, activate: false);
                    }

                    // "Neu"-Tab ans Ende verschieben und fokussieren
                    mainWindow._tabManager.MoveNewTabToEnd();
                }
                finally
                {
                    // DockingManager wiederherstellen
                    mainWindow._tabManager.RestoreDockingManager(wasEnabled);
                }
            });

            // Fenster anzeigen, nachdem alle Tabs geladen sind
            mainWindow.Show();
        }

        private void SendArgsToRunningInstance(string[] args)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    // 🔧 FIX: Erhöhe Timeout leicht, um stabilere Verbindung zu ermöglichen
                    client.Connect(10000); // wartet bis zu 10 Sekunden auf Verbindung

                    using (var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true })
                    {
                        foreach (var arg in args)
                        {
                            writer.WriteLine(arg);
                        }
                    }
                }
            }
            catch (TimeoutException)
            {
                MessageBox.Show("Keine aktive Instanz gefunden (Pipe-Verbindung abgelaufen).",
                    "Skriptorium", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Datei in laufender Instanz: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartPipeServer(UI.MainWindow mainWindow)
        {
            var thread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        // PipeTransmissionMode.Message sorgt dafür, dass Nachrichten klar getrennt sind
                        using (var server = new NamedPipeServerStream(
                            PipeName,
                            PipeDirection.In,
                            NamedPipeServerStream.MaxAllowedServerInstances,
                            PipeTransmissionMode.Message,
                            PipeOptions.Asynchronous))
                        {
                            server.WaitForConnection();

                            using (var reader = new StreamReader(server, Encoding.UTF8))
                            {
                                string filePath;
                                while ((filePath = reader.ReadLine()) != null)
                                {
                                    if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
                                    {
                                        Console.WriteLine($"[PipeServer] Datei empfangen: {filePath}");

                                        DataManager.OpenFile(filePath, (content, path) =>
                                        {
                                            mainWindow.Dispatcher.Invoke(() =>
                                            {
                                                mainWindow.OpenFileInNewTab(content, path);
                                            });
                                        });
                                    }
                                    else
                                    {
                                        Console.WriteLine($"[PipeServer] Ungültiger Pfad empfangen: {filePath}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PipeServer] Fehler: {ex.Message}");
                        Thread.Sleep(1000); // kurze Pause, damit Schleife bei Fehler nicht rotiert
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
