using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Skriptorium.UI.Views
{
    public partial class SettingsView : Window
    {
        private bool _isInitializing = true;

        public SettingsView()
        {
            InitializeComponent();

            // Theme vorauswählen (z. B. Light oder Dark aus Settings)
            var theme = Properties.Settings.Default.Theme;
            foreach (ComboBoxItem item in ComboTheme.Items)
            {
                if (item.Tag?.ToString() == theme)
                {
                    ComboTheme.SelectedItem = item;
                    break;
                }
            }

            _isInitializing = false; // <- jetzt ist Initialisierung fertig
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

            var idx = ComboTheme.SelectedIndex;
            System.Diagnostics.Debug.WriteLine($"[SettingsView] SelectionChanged: Index={idx}");

            if (ComboTheme.SelectedItem is ComboBoxItem item
                && item.Tag is string themeKey)
            {
                Properties.Settings.Default.Theme = themeKey;
                Properties.Settings.Default.Save();

                if (Owner is MainWindow main)
                {
                    main.SetTheme(themeKey);
                }
            }
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
