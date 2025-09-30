using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;

namespace Skriptorium.UI.Views.Tools
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

            var typeDropdown = CreateComboBox(new[] { "Dialog", "XP Geben", "Item Geben", "Ende-Dialog" });
            var speakerDropdown = CreateComboBox(new[] { "Held", "NPC" }, width: 80);
            var textEntry = CreateTextBox(400);

            var xpLabel = new TextBlock
            {
                Text = "XP:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Visibility = Visibility.Collapsed
            };

            var xpEntry = CreateTextBox(50, Visibility.Collapsed);

            // Neue Labels für Item Geben:
            var itemNameLabel = new TextBlock
            {
                Text = "Iteminstanz:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Visibility = Visibility.Collapsed
            };

            var itemNameEntry = CreateTextBox(200, Visibility.Collapsed);

            var itemQuantityLabel = new TextBlock
            {
                Text = "Anzahl:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Visibility = Visibility.Collapsed
            };

            var itemQuantityEntry = CreateTextBox(78, Visibility.Collapsed);

            var itemRecipientDropdown = CreateComboBox(new[] { "Held", "NPC" }, width: 70, visibility: Visibility.Collapsed);

            typeDropdown.SelectionChanged += (s, e) =>
            {
                string type = typeDropdown.SelectedItem as string;

                textEntry.Visibility = type == "Dialog" ? Visibility.Visible : Visibility.Collapsed;
                xpLabel.Visibility = type == "XP Geben" ? Visibility.Visible : Visibility.Collapsed;
                xpEntry.Visibility = type == "XP Geben" ? Visibility.Visible : Visibility.Collapsed;

                bool itemVisible = type == "Item Geben";
                itemNameLabel.Visibility = itemVisible ? Visibility.Visible : Visibility.Collapsed;
                itemNameEntry.Visibility = itemVisible ? Visibility.Visible : Visibility.Collapsed;
                itemQuantityLabel.Visibility = itemVisible ? Visibility.Visible : Visibility.Collapsed;
                itemQuantityEntry.Visibility = itemVisible ? Visibility.Visible : Visibility.Collapsed;

                // Speaker nur bei "Dialog" aktiv
                speakerDropdown.IsEnabled = type == "Dialog" || type == "Item Geben";

                if (type == "XP Geben" || type == "Ende-Dialog")
                {
                    speakerDropdown.SelectedItem = "NPC";
                }
            };

            panel.Children.Add(typeDropdown);
            panel.Children.Add(speakerDropdown);
            panel.Children.Add(textEntry);
            panel.Children.Add(xpLabel);
            panel.Children.Add(xpEntry);

            // Labels und Felder für Item Geben:
            panel.Children.Add(itemNameLabel);
            panel.Children.Add(itemNameEntry);
            panel.Children.Add(itemQuantityLabel);
            panel.Children.Add(itemQuantityEntry);

            var minusButton = new Button { Content = "-", Width = 20, Height = 20, Margin = new Thickness(5, 0, 0, 0) };
            minusButton.Click += (s, e) =>
            {
                if (dialogLines.Count <= 1)
                    return;

                var btn = s as Button;
                var panelToRemove = btn?.Parent as StackPanel;
                if (panelToRemove != null)
                {
                    dialogPanel.Children.Remove(panelToRemove);

                    var dialogLine = dialogLines.Find(dl => dl.PanelContainer == panelToRemove);
                    if (dialogLine != null)
                    {
                        dialogLines.Remove(dialogLine);
                    }
                }
            };

            panel.Children.Add(minusButton);

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

            string dialogNumber = (dialogNumberDropdown.SelectedIndex + 1).ToString();
            string important = importantDropdown.Text == "Ja" ? "TRUE" : "FALSE";
            string permanent = permanentDropdown.Text == "Ja" ? "TRUE" : "FALSE";

            // Mindestens eine Dialogzeile mit Text prüfen
            bool hasDialogLine = false;
            bool hasNonEmptyDialogText = false;

            foreach (var line in dialogLines)
            {
                string type = line.TypeDropdown.SelectedItem?.ToString();
                if (type == "Dialog")
                {
                    hasDialogLine = true;

                    if (!string.IsNullOrWhiteSpace(line.TextEntry.Text))
                    {
                        hasNonEmptyDialogText = true;
                        break;
                    }
                }
            }

            // Prüfe nur, wenn Dialogzeilen existieren
            if (hasDialogLine && !hasNonEmptyDialogText)
            {
                MessageBox.Show("Mindestens eine Dialogzeile mit Text ist erforderlich!", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var sb = new System.Text.StringBuilder();

            // Überschrift
            sb.AppendLine("// ************************************************************");
            sb.AppendLine("//                          Überschrift");
            sb.AppendLine("// ************************************************************");
            sb.AppendLine();

            // Instanzenkopf
            sb.AppendLine($"instance DIA_{dialogInstance} (C_INFO) ");
            sb.AppendLine("{");
            sb.AppendLine($"    npc          =  {npcInstance};");
            sb.AppendLine($"    nr           =  {dialogNumber};");
            sb.AppendLine($"    condition    =  DIA_{dialogInstance}_Condition;");
            sb.AppendLine($"    information  =  DIA_{dialogInstance}_Info;");
            sb.AppendLine($"    important    =  {important};");
            sb.AppendLine($"    permanent    =  {permanent};");
            sb.AppendLine($"    Description  =  \"{description}\";");
            sb.AppendLine("}");
            sb.AppendLine();

            // Kondition
            sb.AppendLine($"func int DIA_{dialogInstance}_Condition ()");
            sb.AppendLine("{");
            sb.AppendLine("    return TRUE; //Bedingungen manuell eingeben");
            sb.AppendLine("};");
            sb.AppendLine();

            // Information
            sb.AppendLine($"func void DIA_{dialogInstance}_Info ()");
            sb.AppendLine("{");

            int dialogIndex = 0;
            foreach (var line in dialogLines)
            {
                string type = line.TypeDropdown.SelectedItem?.ToString();

                if (type == "Dialog")
                {
                    string dialogText = line.TextEntry.Text.Trim();
                    if (string.IsNullOrEmpty(dialogText))
                        continue;

                    string speaker = line.SpeakerDropdown.SelectedItem?.ToString() ?? "Held";

                    string firstParam, secondParam;
                    if (speaker == "NPC")
                    {
                        firstParam = "self";
                        secondParam = "other";
                    }
                    else
                    {
                        firstParam = "other";
                        secondParam = "self";
                    }

                    sb.AppendLine($"    AI_Output ({firstParam}, {secondParam}, \"{dialogInstance}_{dialogIndex:00}\"); // {dialogText}");
                    dialogIndex++;
                }
                else if (type == "XP Geben")
                {
                    string xpValue = line.XPEntry.Text.Trim();

                    if (int.TryParse(xpValue, out int xp) && xp >= 0)
                    {
                        sb.AppendLine($"    B_GivePlayerXP ({xp});");
                    }
                    else
                    {
                        MessageBox.Show("Ungültiger XP-Wert in einer Zeile. Bitte gib eine ganze Zahl >= 0 ein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else if (type == "Item Geben")
                {
                    string itemName = line.ItemNameEntry.Text.Trim();
                    string itemQuantityText = line.ItemQuantityEntry.Text.Trim();
                    string recipient = line.SpeakerDropdown.SelectedItem?.ToString() ?? "Held";

                    if (string.IsNullOrEmpty(itemName))
                    {
                        MessageBox.Show("Bitte Iteminstanz angeben.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (!int.TryParse(itemQuantityText, out int itemQuantity) || itemQuantity <= 0)
                    {
                        MessageBox.Show("Bitte eine gültige Item-Anzahl (> 0) angeben.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string firstParam, secondParam;

                    if (recipient == "NPC")
                    {
                        firstParam = "self";
                        secondParam = "other";
                    }
                    else // Held
                    {
                        firstParam = "other";
                        secondParam = "self";
                    }

                    sb.AppendLine($"    B_GiveInvItems ({firstParam}, {secondParam}, {itemName}, {itemQuantity});");
                }
                else if (type == "Ende-Dialog")
                {
                    sb.AppendLine("    AI_StopProcessInfos (self);");
                }
            }

            sb.AppendLine("};");

            outputText.Text = sb.ToString();
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(outputText.Text))
            {
                Clipboard.SetText(outputText.Text);
                
            }
        }

        private void npcInstanceEntry_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
