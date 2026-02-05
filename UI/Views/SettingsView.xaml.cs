using ControlzEx.Theming;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using Skriptorium.Managers; // LanguageManager

namespace Skriptorium.UI.Views
{
    public partial class SettingsView : MetroWindow
    {
        private bool _isInitializing = true;
        private readonly ProjectManager _projectManager;

        public SettingsView(ProjectManager projectManager)
        {
            _projectManager = projectManager; // Manager speichern
            InitializeComponent();

            // Theme-Voreinstellung
            string savedTheme = Properties.Settings.Default.Theme ?? "Light";
            string baseTheme = savedTheme.Split('.')[0];

            foreach (ComboBoxItem item in ComboTheme.Items)
            {
                if (item.Tag?.ToString() == baseTheme)
                {
                    ComboTheme.SelectedItem = item;
                    break;
                }
            }

            // Sprache-Voreinstellung
            string savedLang = Properties.Settings.Default.Language ?? "de";
            foreach (ComboBoxItem item in ComboLanguage.Items)
            {
                if (item.Tag?.ToString() == savedLang)
                {
                    ComboLanguage.SelectedItem = item;
                    break;
                }
            }

            _isInitializing = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Skript-Pfad laden
            TxtScriptPath.Text = Properties.Settings.Default.ScriptSearchPath;
        }

        // Theme wechseln
        private void ComboTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (ComboTheme.SelectedItem is ComboBoxItem item && item.Tag is string themeKey)
            {
                string accent = "Steel";
                string fullTheme = $"{themeKey}.{accent}";
                ThemeManager.Current.ChangeTheme(Application.Current, fullTheme);

                Properties.Settings.Default.Theme = fullTheme;
                Properties.Settings.Default.Save();
            }
        }

        // Sprache wechseln
        private void ComboLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (ComboLanguage.SelectedItem is ComboBoxItem item && item.Tag is string langCode)
            {
                LanguageManager.ChangeLanguage(langCode);

                Properties.Settings.Default.Language = langCode;
                Properties.Settings.Default.Save();
            }
        }

        // Skript-Pfad auswählen
        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = Application.Current.TryFindResource("SelectTheScriptFolder") as string ?? "Wähle den Skript-Ordner",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                TxtScriptPath.Text = dialog.SelectedPath;
                Properties.Settings.Default.ScriptSearchPath = dialog.SelectedPath;
                Properties.Settings.Default.Save();
            }
        }
    }
}
