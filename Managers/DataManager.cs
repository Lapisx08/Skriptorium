using Microsoft.Win32;
using Skriptorium.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;

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

        // Liest Dateien: immer zuerst Latin1, Fallback UTF-8
        private static string ReadFileAutoEncoding(string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);

            // Versuch Latin1
            try
            {
                var text = Encoding.Latin1.GetString(bytes);
                if (!text.Contains('\uFFFD')) // Keine ungültigen Zeichen
                    return text;
            }
            catch
            {
                // Ignorieren, Fallback auf UTF-8
            }

            // Fallback UTF-8
            return Encoding.UTF8.GetString(bytes);
        }

        // Speichert immer Latin1
        private static void WriteFileLatin1(string filePath, string content)
        {
            File.WriteAllText(filePath, content, Encoding.Latin1);
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
                WriteFileLatin1(TabStateFilePath, json);
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
            var dlg = new OpenFileDialog
            {
                Filter = "Textdateien (*.txt)|*.txt|Daedalus-Skripte (*.d)|*.d|Source-Skripte (*.src)|*.src|Alle unterstützten Dateien (*.d;*.txt;*.src)|*.d;*.txt;*.src",
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
                WriteFileLatin1(activeEditor.FilePath, activeEditor.Text);
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
                Filter = "Textdateien (*.txt)|*.txt|Daedalus-Skripte (*.d)|*.d|Source-Skripte (*.src)|*.src|Alle unterstützten Dateien (*.d;*.txt;*.src)|*.d;*.txt;*.src",
                DefaultExt = ".d",
                FilterIndex = 4,
                InitialDirectory = GetInitialDirectory(),
                FileName = string.IsNullOrWhiteSpace(activeEditor.FilePath) ? "" : Path.GetFileName(activeEditor.FilePath)
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    WriteFileLatin1(dlg.FileName, activeEditor.Text);
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
            if (File.Exists(RecentFilesPath))
            {
                try
                {
                    var lines = File.ReadAllLines(RecentFilesPath, Encoding.Latin1);
                    recentFiles.Clear();
                    recentFiles.AddRange(lines.Where(File.Exists));
                }
                catch
                {
                    var linesUtf8 = File.ReadAllLines(RecentFilesPath, Encoding.UTF8);
                    recentFiles.Clear();
                    recentFiles.AddRange(linesUtf8.Where(File.Exists));
                }
            }
        }

        public static void SaveRecentFiles()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(RecentFilesPath)!);
                File.WriteAllLines(RecentFilesPath, recentFiles, Encoding.Latin1);
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
