using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;

namespace Skriptorium.Tools
{
    public partial class DialogGenerator : MetroWindow
    {
        class DialogLine
        {
            public ComboBox TypeDropdown;
            public ComboBox SpeakerDropdown;
            public TextBox TextEntry;
            public TextBox XPEntry;
            public TextBox ItemNameEntry;
            public TextBox ItemQuantityEntry;
            public ComboBox ItemRecipientDropdown;
            public StackPanel PanelContainer;
        }

        private readonly List<DialogLine> dialogLines = new();
        private const int MaxDialogLines = 15;

        public DialogGenerator()
        {
            InitializeComponent();
            dialogNumberDropdown.SelectedIndex = 0;
            importantDropdown.SelectedIndex = 1;
            permanentDropdown.SelectedIndex = 1;
            AddDialogLine();
        }

        private ComboBox CreateComboBox(IEnumerable<string> items, int selectedIndex = 0, double width = 100, Visibility visibility = Visibility.Visible)
        {
            return new ComboBox
            {
                Width = width,
                ItemsSource = items,
                SelectedIndex = selectedIndex,
                Margin = new Thickness(5, 0, 5, 0),
                Visibility = visibility
            };
        }

        private TextBox CreateTextBox(double width, Visibility visibility = Visibility.Visible)
        {
            return new TextBox
            {
                Width = width,
                Margin = new Thickness(5, 0, 5, 0),
                Visibility = visibility
            };
        }

        private void AddDialogLine_Click(object sender, RoutedEventArgs e) => AddDialogLine();

        private void AddDialogLine()
        {
            if (dialogLines.Count >= MaxDialogLines)
                return;

            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };

            var typeDropdown = CreateComboBox(new[] { "Dialog", "XP Geben", "Item Geben" });
            var speakerDropdown = CreateComboBox(new[] { "Held", "NPC" }, width: 80);
            var textEntry = CreateTextBox(300);
            var xpEntry = CreateTextBox(50, Visibility.Collapsed);
            var itemNameEntry = CreateTextBox(100, Visibility.Collapsed);
            var itemQuantityEntry = CreateTextBox(40, Visibility.Collapsed);
            var itemRecipientDropdown = CreateComboBox(new[] { "Held", "NPC" }, width: 70, visibility: Visibility.Collapsed);

            typeDropdown.SelectionChanged += (s, e) =>
            {
                string type = typeDropdown.SelectedItem as string;
                textEntry.Visibility = type == "Dialog" ? Visibility.Visible : Visibility.Collapsed;
                xpEntry.Visibility = type == "XP Geben" ? Visibility.Visible : Visibility.Collapsed;
                bool itemVisible = type == "Item Geben";
                itemNameEntry.Visibility = itemVisible ? Visibility.Visible : Visibility.Collapsed;
                itemQuantityEntry.Visibility = itemVisible ? Visibility.Visible : Visibility.Collapsed;
                itemRecipientDropdown.Visibility = itemVisible ? Visibility.Visible : Visibility.Collapsed;
            };

            // Reihenfolge wichtig
            panel.Children.Add(typeDropdown);
            panel.Children.Add(speakerDropdown);
            panel.Children.Add(textEntry);
            panel.Children.Add(xpEntry);
            panel.Children.Add(itemNameEntry);
            panel.Children.Add(itemQuantityEntry);
            panel.Children.Add(itemRecipientDropdown);

            dialogPanel.Children.Add(panel);

            dialogLines.Add(new DialogLine
            {
                TypeDropdown = typeDropdown,
                SpeakerDropdown = speakerDropdown,
                TextEntry = textEntry,
                XPEntry = xpEntry,
                ItemNameEntry = itemNameEntry,
                ItemQuantityEntry = itemQuantityEntry,
                ItemRecipientDropdown = itemRecipientDropdown,
                PanelContainer = panel
            });
        }

        private void ResetDialog()
        {
            dialogPanel.Children.Clear();
            dialogLines.Clear();
            AddDialogLine();
        }

        private void ResetFields_Click(object sender, RoutedEventArgs e)
        {
            dialogInstanceEntry.Text = "";
            npcInstanceEntry.Text = "";
            descriptionEntry.Text = "";
            dialogNumberDropdown.SelectedIndex = 0;
            importantDropdown.SelectedIndex = 1;
            permanentDropdown.SelectedIndex = 1;
            outputText.Text = "";
            ResetDialog();
        }

        private void GenerateCode_Click(object sender, RoutedEventArgs e)
        {
            outputText.Text = "";

            string dialogInstance = dialogInstanceEntry.Text.Trim();
            string npcInstance = npcInstanceEntry.Text.Trim();
            string description = descriptionEntry.Text.Trim();

            if (string.IsNullOrEmpty(dialogInstance) || string.IsNullOrEmpty(npcInstance) || string.IsNullOrEmpty(description))
            {
                MessageBox.Show("Bitte Dialoginstanz, NPC-Instanz und Beschreibung ausfüllen!", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var lines = new List<string>
            {
                $"INSTANCE DIALOG {dialogInstance}",
                $"IMPORT NPC {npcInstance}",
                $"IMPORT DESCRIPTION \"{description}\""
            };

            lines.Add($"DIALOGNUMMER {dialogNumberDropdown.SelectedIndex + 1}");
            lines.Add($"WICHTIG {(importantDropdown.Text == "Ja" ? "1" : "0")}");
            lines.Add($"PERMANENT {(permanentDropdown.Text == "Ja" ? "1" : "0")}");
            lines.Add("");

            foreach (var line in dialogLines)
            {
                string type = line.TypeDropdown.SelectedItem?.ToString();
                string speaker = line.SpeakerDropdown.SelectedItem?.ToString();

                if (type == "Dialog")
                {
                    string text = line.TextEntry.Text.Trim();
                    if (string.IsNullOrEmpty(text))
                    {
                        MessageBox.Show("Dialogtext darf nicht leer sein!", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string prefix = speaker == "Held" ? "H" : "N";
                    lines.Add($"{prefix}: \"{text}\"");
                }
                else if (type == "XP Geben")
                {
                    if (!int.TryParse(line.XPEntry.Text.Trim(), out int xp))
                    {
                        MessageBox.Show("XP Wert ungültig!", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    lines.Add($"XP {xp}");
                }
                else if (type == "Item Geben")
                {
                    string item = line.ItemNameEntry.Text.Trim();
                    string qtyStr = line.ItemQuantityEntry.Text.Trim();
                    string recipient = line.ItemRecipientDropdown.SelectedItem?.ToString();

                    if (string.IsNullOrEmpty(item))
                    {
                        MessageBox.Show("Itemname darf nicht leer sein!", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if (!int.TryParse(qtyStr, out int qty))
                    {
                        MessageBox.Show("Anzahl ungültig!", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string prefix = recipient == "Held" ? "H" : "N";
                    lines.Add($"ITEM: {item} {qty} {prefix}");
                }
            }

            lines.Add("ENDDIALOG");

            outputText.Text = string.Join(Environment.NewLine, lines);
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(outputText.Text))
            {
                Clipboard.SetText(outputText.Text);
                MessageBox.Show("Script kopiert!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
