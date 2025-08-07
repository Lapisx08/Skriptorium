using Microsoft.Win32;
using Skriptorium.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static void OpenFile(Action<string, string> onFileLoaded)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Textdateien (*.txt)|*.txt|Daedalus-Skripte (*.d)|*.d",
                DefaultExt = ".d",
                FilterIndex = 2,
                InitialDirectory = GetInitialDirectory()
            };

            if (dlg.ShowDialog() == true)
            {
                OpenFile(dlg.FileName, onFileLoaded);
            }
        }

        public static void OpenFile(string filePath, Action<string, string> onFileLoaded, Action<string>? onError = null)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                onFileLoaded(content, filePath);
                AddRecentFile(filePath);
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
                {
                    return;
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

        public static bool SaveFile(ScriptEditor activeEditor)
        {
            if (activeEditor == null)
            {
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
            catch (Exception)
            {
                return false;
            }
        }

        public static bool SaveFileAs(ScriptEditor activeEditor)
        {
            if (activeEditor == null)
            {
                return false;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "Textdateien (*.txt)|*.txt|Daedalus-Skripte (*.d)|*.d",
                DefaultExt = ".d",
                FilterIndex = 2,
                InitialDirectory = GetInitialDirectory(),
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
                catch (Exception)
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
                if (editor.IsModified)
                {
                    bool saved = SaveFile(editor);
                    if (!saved)
                    {
                        allSaved = false;
                    }
                }
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

        public static List<string> GetRecentFiles()
        {
            return recentFiles.ToList();
        }

        public static void LoadRecentFiles()
        {
            if (File.Exists(RecentFilesPath))
            {
                var lines = File.ReadAllLines(RecentFilesPath);
                recentFiles.Clear();
                recentFiles.AddRange(lines.Where(File.Exists));
            }
        }

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

        private static string GetInitialDirectory()
        {
            string path = Properties.Settings.Default.ScriptSearchPath;

            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                return path;

            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
    }
}