﻿using ControlzEx.Theming;
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

            // MahApps Theme setzen
            ThemeManager.Current.ChangeTheme(this, themeName);

            // AvalonDock Theme anpassen
            ApplyAvalonDockThemeFromMahApps();

            // ToggleButton ActiveBackground an Theme anpassen
            UpdateToggleButtonBackgroundForTheme();

            // Theme-Wechsel Event abonnieren
            ThemeManager.Current.ThemeChanged += (_, __) =>
            {
                ApplyAvalonDockThemeFromMahApps();
                UpdateToggleButtonBackgroundForTheme();
            };

            // Single-Instance-Check
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

            var tabsToAdd = new System.Collections.Generic.List<(string Content, string? Title, string? FilePath)>();
            var tabStates = DataManager.LoadOpenTabs();

            foreach (var tabState in tabStates)
            {
                if (!string.IsNullOrWhiteSpace(tabState.Content) || !string.IsNullOrWhiteSpace(tabState.FilePath))
                {
                    string tabTitle = tabState.FilePath != null ? Path.GetFileName(tabState.FilePath) : null;
                    tabsToAdd.Add((tabState.Content, tabTitle, tabState.FilePath));
                }
            }

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
                            tabsToAdd.Add((content, tabTitle, path));
                        }, (error) =>
                        {
                            Console.WriteLine($"Fehler beim Laden der Datei {filePath}: {error}");
                        });
                    }
                }
            }

            RoutedEventHandler? onLoaded = null;
            onLoaded = (s, ev) =>
            {
                mainWindow.Loaded -= onLoaded;
                bool wasEnabled = mainWindow._tabManager.DisableDockingManager();
                try
                {
                    if (tabsToAdd.Count == 0 && e.Args.Length == 0)
                    {
                        if (!mainWindow._tabManager.GetAllOpenEditors().Any())
                        {
                            mainWindow._tabManager.AddNewTab(string.Empty, null, null, activate: true);
                        }
                    }
                    else
                    {
                        foreach (var (content, title, filePath) in tabsToAdd)
                        {
                            mainWindow._tabManager.AddNewTab(content, title, filePath, activate: false);
                        }
                        mainWindow._tabManager.MoveNewTabToEnd();
                    }
                }
                finally
                {
                    mainWindow._tabManager.RestoreDockingManager(wasEnabled);
                }
            };

            mainWindow.Loaded += onLoaded;
            mainWindow.Show();
        }

        // --- Neue Methode für ToggleButton Hintergrund ---
        private void UpdateToggleButtonBackgroundForTheme()
        {
            var currentTheme = ThemeManager.Current.DetectTheme(this);
            bool isDark = currentTheme?.BaseColorScheme == "Dark";

            Application.Current.Resources["ToggleButtonActiveBackground"] =
                isDark ?
                    Application.Current.Resources["ToggleButtonActiveBackgroundDark"] :
                    Application.Current.Resources["ToggleButtonActiveBackgroundLight"];
        }

        private void SendArgsToRunningInstance(string[] args)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    client.Connect(10000);
                    using (var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true })
                    {
                        foreach (var arg in args)
                            writer.WriteLine(arg);
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
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PipeServer] Fehler: {ex.Message}");
                        Thread.Sleep(1000);
                    }
                }
            });

            thread.IsBackground = true;
            thread.Start();
        }

        private void ApplyAvalonDockThemeFromMahApps()
        {
            var currentTheme = ThemeManager.Current.DetectTheme(this);
            if (currentTheme == null)
            {
                MessageBox.Show("Konnte aktuelles MahApps-Theme nicht ermitteln. Fallback auf Light.",
                    "Theme-Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                ApplyAvalonDockTheme("Light");
                return;
            }
            ApplyAvalonDockTheme(currentTheme.BaseColorScheme);
        }

        private void ApplyAvalonDockTheme(string baseTheme)
        {
            var themeDictionary = Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source?.ToString().Contains("Theme.xaml") == true);

            if (themeDictionary != null)
                Current.Resources.MergedDictionaries.Remove(themeDictionary);

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
