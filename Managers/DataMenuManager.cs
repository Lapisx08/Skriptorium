using System;
using System.Windows;
using System.Windows.Controls;
using Skriptorium.UI;
using System.IO;

namespace Skriptorium.Managers
{
    public class DataMenuManager
    {
        private readonly ScriptTabManager _tabManager;
        private readonly MenuItem _menuZuletztGeoeffnet;

        public DataMenuManager(ScriptTabManager tabManager, MenuItem menuZuletztGeoeffnet)
        {
            _tabManager = tabManager;
            _menuZuletztGeoeffnet = menuZuletztGeoeffnet;

            // Sofort Menü mit zuletzt geöffneten Dateien füllen
            UpdateRecentFilesMenu();
        }

        public void UpdateRecentFilesMenu()
        {
            if (_menuZuletztGeoeffnet == null) return;

            _menuZuletztGeoeffnet.Items.Clear();

            var recent = DataManager.GetRecentFiles();
            if (recent.Count == 0)
            {
                _menuZuletztGeoeffnet.Items.Add(new MenuItem
                {
                    Header = "(Keine Dateien)",
                    IsEnabled = false
                });
                return;
            }

            foreach (var path in recent)
            {
                if (!File.Exists(path))
                    continue; // <--- Hier wird ungültiger Pfad übersprungen

                var item = new MenuItem
                {
                    Header = System.IO.Path.GetFileName(path),
                    ToolTip = path,
                    Tag = path
                };
                item.Click += OpenRecentFile_Click;
                _menuZuletztGeoeffnet.Items.Add(item);
            }

        }

        private void OpenRecentFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Tag is string path)
            {
                DataManager.OpenFile(path, (content, filePath) =>
                {
                    _tabManager.AddNewTab(
                        content,
                        System.IO.Path.GetFileName(filePath),
                        filePath);

                    UpdateRecentFilesMenu();
                }, _tabManager);
            }
        }
    }
}
