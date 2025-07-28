using Skriptorium.UI;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;

namespace Skriptorium.Managers
{
    public class ScriptTabManager
    {
        private readonly TabControl _tabControl;
        private int _newScriptCounter = 1;

        public ScriptTabManager(TabControl tabControl)
        {
            _tabControl = tabControl;
        }

        public void AddNewTab(string content = "", string? tabTitle = null, string? filePath = null)
        {
            // Wenn eine Datei bereits geöffnet ist, aktiviere den vorhandenen Tab und beende
            if (!string.IsNullOrWhiteSpace(filePath) && TryActivateTabByFilePath(filePath!))
                return;

            var scriptEditor = new ScriptEditor
            {
                FilePath = filePath ?? ""
            };
            scriptEditor.SetTextAndResetModified(content);

            string baseTitle = tabTitle ?? $"Neu{_newScriptCounter++}";
            var titleText = new TextBlock
            {
                Text = baseTitle,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["MahApps.Brushes.ThemeForeground"]
            };

            scriptEditor.TitleTextBlock = titleText;

            scriptEditor.TextChanged += (s, e) =>
            {
                if (scriptEditor.IsModified)
                {
                    if (!titleText.Text.EndsWith("*"))
                        titleText.Text += "*";
                }
                else
                {
                    RemoveTrailingAsterisk(titleText);
                }
            };

            var closeButton = new Button
            {
                Content = "x",
                Width = 20,
                Height = 20,
                Padding = new Thickness(0),
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Focusable = false,
                Cursor = Cursors.Hand,
                Foreground = (Brush)Application.Current.Resources["MahApps.Brushes.ThemeForeground"]
            };

            closeButton.Click += (s, e) =>
            {
                if (!ConfirmClose(scriptEditor))
                    return;

                if (closeButton.Parent is StackPanel sp && sp.Parent is TabItem tabItem)
                    _tabControl.Items.Remove(tabItem);
            };

            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            headerPanel.Background = (Brush)Application.Current.Resources["MahApps.Brushes.TabItem.Background"];
            headerPanel.Children.Add(titleText);
            headerPanel.Children.Add(closeButton);

            var newTab = new TabItem
            {
                Header = headerPanel,
                Content = scriptEditor,
                IsSelected = true
            };

            _tabControl.Items.Add(newTab);
        }

        public ScriptEditor? GetActiveScriptEditor()
        {
            if (_tabControl.SelectedItem is TabItem tabItem &&
                tabItem.Content is ScriptEditor editor)
                return editor;
            return null;
        }

        public void CloseActiveTab()
        {
            if (_tabControl.SelectedItem is TabItem tabItem &&
                tabItem.Content is ScriptEditor editor)
            {
                if (!ConfirmClose(editor))
                    return;
                _tabControl.Items.Remove(tabItem);
            }
        }

        public bool TryActivateTabByFilePath(string filePath)
        {
            foreach (TabItem item in _tabControl.Items)
            {
                if (item.Content is ScriptEditor editor &&
                    string.Equals(editor.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                {
                    item.IsSelected = true;
                    return true;
                }
            }
            return false;
        }

        public bool ConfirmCloseAllTabs()
        {
            foreach (var editor in GetAllOpenEditors())
            {
                if (!ConfirmClose(editor))
                    return false;
            }
            return true;
        }

        public bool ConfirmClose(ScriptEditor editor)
        {
            if (!editor.IsModified)
                return true;

            var result = MessageBox.Show(
                "Dieses Skript wurde geändert. Möchtest du es speichern?",
                "Änderungen speichern?",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning
            );
            if (result == MessageBoxResult.Cancel)
                return false;
            if (result == MessageBoxResult.Yes)
            {
                bool saved = DataManager.SaveFile(editor);
                if (!saved)
                    return false;
                editor.ResetModifiedFlag();
                if (editor.TitleTextBlock != null)
                    RemoveTrailingAsterisk(editor.TitleTextBlock);
            }
            return true;
        }

        private void RemoveTrailingAsterisk(TextBlock titleText)
        {
            if (titleText.Text.EndsWith("*"))
                titleText.Text = titleText.Text[..^1];
        }

        public IEnumerable<ScriptEditor> GetAllOpenEditors()
        {
            foreach (TabItem tabItem in _tabControl.Items)
            {
                if (tabItem.Content is ScriptEditor editor)
                    yield return editor;
            }
        }
    }
}
