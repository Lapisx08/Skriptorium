using Microsoft.Win32;
using Skriptorium.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Skriptorium.Managers
{
    public class DataManager
    {
        private const int MaxRecentFiles = 20;
        private static readonly string RecentFilesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Skriptorium", "recent_files.txt"
        );

        private static readonly List<string> recentFiles = new();

        // Öffnet eine Datei über Dialog
        public static void OpenFile(Action<string, string> onFileLoaded)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Textdateien (*.txt)|*.txt|Daedalus-Skripte (*.d)|*.d",
                DefaultExt = ".d",
                FilterIndex = 2
            };

            if (dlg.ShowDialog() == true)
            {
                OpenFile(dlg.FileName, onFileLoaded); // Ruft die neue Überladung auf
            }
        }

        // Neue Überladung: Öffnet eine Datei direkt über Pfad ohne TabManager
        public static void OpenFile(
            string filePath,
            Action<string, string> onFileLoaded,
            Action<string>? onError = null)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                onFileLoaded(content, filePath);
                AddRecentFile(filePath); // Jetzt wird es gespeichert
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
            }
        }

        // Öffnet eine Datei direkt über Pfad (für "Zuletzt geöffnet") mit TabManager
        public static void OpenFile(string filePath, Action<string, string> onFileLoaded, ScriptTabManager tabManager)
        {
            try
            {
                // Prüfen, ob die Datei bereits geöffnet ist (nur wenn TabManager vorhanden)
                if (tabManager != null && tabManager.TryActivateTabByFilePath(filePath))
                {
                    return; // Bereits geöffnet – Tab wurde aktiviert
                }

                string content = File.ReadAllText(filePath);
                onFileLoaded?.Invoke(content, filePath);
                AddRecentFile(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Laden der Datei:\n" + ex.Message);
            }
        }

        // Speichert das Skript, mit automatischer Entscheidung für "Speichern unter"
        public static bool SaveFile(ScriptEditor activeEditor)
        {
            if (activeEditor == null)
            {
                MessageBox.Show("Kein Skript zum Speichern ausgewählt.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(activeEditor.FilePath))
            {
                return SaveFileAs(activeEditor);
            }

            try
            {
                File.WriteAllText(activeEditor.FilePath, activeEditor.Text);
                activeEditor.ResetModifiedFlag();
                AddRecentFile(activeEditor.FilePath);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Speichern der Datei:\n" + ex.Message);
                return false;
            }
        }

        // Speichern unter – fragt Benutzer nach Speicherort
        public static bool SaveFileAs(ScriptEditor activeEditor)
        {
            if (activeEditor == null)
            {
                MessageBox.Show("Kein Skript zum Speichern ausgewählt.");
                return false;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "Textdateien (*.txt)|*.txt|Daedalus-Skripte (*.d)|*.d",
                DefaultExt = ".d",
                FilterIndex = 2,
                FileName = string.IsNullOrWhiteSpace(activeEditor.FilePath)
                    ? ""
                    : Path.GetFileName(activeEditor.FilePath)
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(dlg.FileName, activeEditor.Text);
                    activeEditor.FilePath = dlg.FileName;
                    activeEditor.ResetModifiedFlag();
                    AddRecentFile(dlg.FileName);
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fehler beim Speichern der Datei:\n" + ex.Message);
                    return false;
                }
            }

            return false; // Dialog abgebrochen
        }

        // Alles Speichern - speichert alle geöffneten Dateien
        public static bool SaveAllFiles(ScriptTabManager tabManager)
        {
            bool allSaved = true;

            foreach (var editor in tabManager.GetAllOpenEditors())
            {
                if (editor.IsModified)
                {
                    bool saved = SaveFile(editor);
                    if (!saved)
                    {
                        allSaved = false;
                        // Optional: Abbrechen, wenn ein Speichern fehlschlägt
                        // break;
                    }
                }
            }

            return allSaved;
        }

        // Fügt einen Pfad zu den "zuletzt geöffneten Dateien" hinzu
        private static void AddRecentFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return;

            recentFiles.Remove(filePath);
            recentFiles.Insert(0, filePath);

            if (recentFiles.Count > MaxRecentFiles)
                recentFiles.RemoveAt(recentFiles.Count - 1);
        }

        // Gibt aktuelle Liste zurück
        public static List<string> GetRecentFiles()
        {
            return recentFiles.ToList();
        }

        // Laden der Liste beim App-Start
        public static void LoadRecentFiles()
        {
            if (File.Exists(RecentFilesPath))
            {
                var lines = File.ReadAllLines(RecentFilesPath);
                recentFiles.Clear();
                recentFiles.AddRange(lines.Where(File.Exists));
            }
        }

        // Speichern der Liste beim App-Ende
        public static void SaveRecentFiles()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(RecentFilesPath)!);
                File.WriteAllLines(RecentFilesPath, recentFiles);
            }
            catch
            {
                // Ignorieren
            }
        }
    }
}
