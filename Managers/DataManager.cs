using Microsoft.Win32;
using Skriptorium.UI;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using UtfUnknown;

namespace Skriptorium.Managers
{
    public static class DataManager
    {
        private const int MaxRecentFiles = 100;
        private static readonly string RecentFilesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Skriptorium", "recent_files.txt"
        );
        private static readonly string TabStateFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Skriptorium", "tabs.json"
        );

        private static readonly List<string> recentFiles = new();
        private static string? lastUsedDirectory = null;

        // Speichert erkannte Kodierungen pro Datei
        public static readonly ConcurrentDictionary<string, Encoding> fileEncodings = new();

        // Liest Datei mit automatischer Kodierungserkennung via UtfUnknown
        public static string ReadFileAutoEncoding(string filePath)
        {
            try
            {
                var result = CharsetDetector.DetectFromFile(filePath);
                Encoding encoding = result.Detected?.Encoding ?? Encoding.UTF8;
                fileEncodings[filePath] = encoding;
                return File.ReadAllText(filePath, encoding);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Lesen der Datei '{filePath}': {ex.Message}");
                fileEncodings[filePath] = Encoding.Latin1;
                return File.ReadAllText(filePath, Encoding.Latin1);
            }
        }

        // Speichert Datei
        private static void WriteFileAutoEncoding(string filePath, string content)
        {
            try
            {
                // Immer Latin-1 verwenden
                Encoding encodingToUse = Encoding.GetEncoding("ISO-8859-1");
                File.WriteAllText(filePath, content, encodingToUse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Speichern der Datei '{filePath}': {ex.Message}");
            }
        }

        public static void SaveOpenTabs(IEnumerable<ScriptEditor> editors)
        {
            try
            {
                var tabStates = new List<TabState>();
                foreach (var editor in editors)
                {
                    if (string.IsNullOrWhiteSpace(editor.Text) || string.IsNullOrWhiteSpace(editor.FilePath))
                        continue;

                    tabStates.Add(new TabState
                    {
                        FilePath = editor.FilePath,
                        Content = editor.Text
                    });
                }

                Directory.CreateDirectory(Path.GetDirectoryName(TabStateFilePath)!);
                var json = JsonSerializer.Serialize(tabStates, new JsonSerializerOptions { WriteIndented = true });
                WriteFileAutoEncoding(TabStateFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Speichern der offenen Tabs: {ex.Message}");
            }
        }

        public static List<TabState> LoadOpenTabs()
        {
            try
            {
                if (File.Exists(TabStateFilePath))
                {
                    var json = ReadFileAutoEncoding(TabStateFilePath);
                    return JsonSerializer.Deserialize<List<TabState>>(json) ?? new List<TabState>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Laden der offenen Tabs: {ex.Message}");
            }
            return new List<TabState>();
        }

        public static void OpenFile(Action<string, string> onFileLoaded)
        {
            string filter = string.Join("|",
                Application.Current.TryFindResource("TextFiles") as string ?? "Textdateien (*.txt)|*.txt",
                Application.Current.TryFindResource("DaedalusFiles") as string ?? "Daedalus-Skripte (*.d)|*.d",
                Application.Current.TryFindResource("SourceFiles") as string ?? "Source-Skripte (*.src)|*.src",
                Application.Current.TryFindResource("AllSupported") as string ?? "Alle unterstützten Dateien (*.d;*.txt;*.src)|*.d;*.txt;*.src"
            );

            var dlg = new OpenFileDialog
            {
                Filter = filter,
                DefaultExt = ".d",
                FilterIndex = 4,
                InitialDirectory = GetInitialDirectory()
            };

            if (dlg.ShowDialog() == true)
                OpenFile(dlg.FileName, onFileLoaded);
        }


        public static void OpenFile(string filePath, Action<string, string> onFileLoaded, Action<string>? onError = null)
        {
            try
            {
                var content = ReadFileAutoEncoding(filePath);
                onFileLoaded(content, filePath);
                AddRecentFile(filePath);
                lastUsedDirectory = Path.GetDirectoryName(filePath);
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
            }
        }

        public static void OpenFile(string filePath, Action<string, string> onFileLoaded, ScriptTabManager tabManager)
        {
            try
            {
                if (tabManager != null && tabManager.TryActivateTabByFilePath(filePath))
                    return;

                var content = ReadFileAutoEncoding(filePath);
                onFileLoaded?.Invoke(content, filePath);
                AddRecentFile(filePath);
                lastUsedDirectory = Path.GetDirectoryName(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Laden der Datei:\n" + ex.Message);
            }
        }

        public static bool SaveFile(ScriptEditor activeEditor)
        {
            if (activeEditor == null)
                return false;

            if (string.IsNullOrWhiteSpace(activeEditor.FilePath))
                return SaveFileAs(activeEditor);

            try
            {
                WriteFileAutoEncoding(activeEditor.FilePath, activeEditor.Text);
                activeEditor.ResetModifiedFlag();
                AddRecentFile(activeEditor.FilePath);
                lastUsedDirectory = Path.GetDirectoryName(activeEditor.FilePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool SaveFileAs(ScriptEditor activeEditor)
        {
            if (activeEditor == null)
                return false;

            var dlg = new SaveFileDialog
            {
                Filter =
                    (Application.Current.TryFindResource("TextFiles") as string ?? "Textdateien (*.txt)|*.txt") + "|" +
                    (Application.Current.TryFindResource("DaedalusFiles") as string ?? "Daedalus-Skripte (*.d)|*.d") + "|" +
                    (Application.Current.TryFindResource("SourceFiles") as string ?? "Source-Skripte (*.src)|*.src") + "|" +
                    (Application.Current.TryFindResource("AllSupported") as string ?? "Alle unterstützten Dateien (*.d;*.txt;*.src)|*.d;*.txt;*.src"),
                DefaultExt = ".d",
                FilterIndex = 4,
                InitialDirectory = GetInitialDirectory(),
                FileName = string.IsNullOrWhiteSpace(activeEditor.FilePath) ? "" : Path.GetFileName(activeEditor.FilePath)
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    WriteFileAutoEncoding(dlg.FileName, activeEditor.Text);
                    activeEditor.FilePath = dlg.FileName;
                    activeEditor.ResetModifiedFlag();
                    AddRecentFile(dlg.FileName);
                    lastUsedDirectory = Path.GetDirectoryName(dlg.FileName);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        public static bool SaveAllFiles(ScriptTabManager tabManager)
        {
            bool allSaved = true;

            foreach (var editor in tabManager.GetAllOpenEditors())
            {
                if (editor.IsModified && !SaveFile(editor))
                    allSaved = false;
            }

            return allSaved;
        }

        private static void AddRecentFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return;

            recentFiles.Remove(filePath);
            recentFiles.Insert(0, filePath);

            if (recentFiles.Count > MaxRecentFiles)
                recentFiles.RemoveAt(recentFiles.Count - 1);
        }

        public static List<string> GetRecentFiles() => recentFiles.ToList();

        public static void LoadRecentFiles()
        {
            const int MaxRecentFiles = 30; // Maximale Anzahl an zuletzt geöffneten Dateien

            if (File.Exists(RecentFilesPath))
            {
                try
                {
                    var content = ReadFileAutoEncoding(RecentFilesPath);
                    var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                    // Liste zurücksetzen und nur die ersten MaxRecentFiles gültigen Dateien hinzufügen
                    recentFiles.Clear();
                    recentFiles.AddRange(lines
                        .Where(File.Exists)
                        .Take(MaxRecentFiles));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fehler beim Laden der kürzlichen Dateien: {ex.Message}");
                }
            }
        }

        public static void SaveRecentFiles()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(RecentFilesPath)!);
                var content = string.Join(Environment.NewLine, recentFiles);
                WriteFileAutoEncoding(RecentFilesPath, content);
            }
            catch
            {
                // Ignorieren
            }
        }

        private static string GetInitialDirectory()
        {
            string basePath = Properties.Settings.Default.ScriptSearchPath;

            if (!string.IsNullOrWhiteSpace(lastUsedDirectory) && Directory.Exists(lastUsedDirectory))
            {
                try
                {
                    string fullLast = Path.GetFullPath(lastUsedDirectory);
                    string fullBase = Path.GetFullPath(basePath);

                    if (fullLast.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase))
                        return lastUsedDirectory;
                }
                catch { }
            }

            if (!string.IsNullOrWhiteSpace(basePath) && Directory.Exists(basePath))
                return basePath;

            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
    }
}