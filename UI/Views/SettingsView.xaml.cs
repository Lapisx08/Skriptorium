using ControlzEx.Theming;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;

namespace Skriptorium.UI.Views
{
    public partial class SettingsView : MetroWindow
    {
        private bool _isInitializing = true;

        public SettingsView()
        {
            InitializeComponent();

            var theme = Properties.Settings.Default.Theme; // z. B. "Dark.Blue"
            string baseTheme = theme?.Split('.')[0] ?? "Light";

            foreach (ComboBoxItem item in ComboTheme.Items)
            {
                if (item.Tag?.ToString() == baseTheme)
                {
                    ComboTheme.SelectedItem = item;
                    break;
                }
            }

            _isInitializing = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Voreinstellung: Tagmodus
            string saved = Properties.Settings.Default.Theme ?? "Light";

            // Skript-Ordnerpfad laden
            TxtScriptPath.Text = Properties.Settings.Default.ScriptSearchPath;
        }

        // Sofortiger Theme-Wechsel bei Auswahl
        private void ComboTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (ComboTheme.SelectedItem is ComboBoxItem item && item.Tag is string themeKey)
            {
                OnThemeSelected(themeKey);
            }
        }

        private void OnThemeSelected(string baseTheme)
        {
            // Accent festlegen – könnte später auch auswählbar gemacht werden
            string accent = "Steel";
            string fullTheme = $"{baseTheme}.{accent}";

            // Theme global anwenden
            ThemeManager.Current.ChangeTheme(Application.Current, fullTheme);

            // Theme speichern
            Properties.Settings.Default.Theme = fullTheme;
            Properties.Settings.Default.Save();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Wähle den Skript-Ordner"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                TxtScriptPath.Text = dialog.FileName;
                Properties.Settings.Default.ScriptSearchPath = dialog.FileName;
                Properties.Settings.Default.Save();
            }
        }


        // Schließen-Button
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
