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
            public TextBlock XPLabel;
            public TextBox ItemNameEntry;
            public TextBlock ItemNameLabel;
            public TextBox ItemQuantityEntry;
            public TextBlock ItemQuantityLabel;
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

        private ComboBox CreateComboBox(
            Dictionary<string, string> items,
            string defaultKey = null,
            double width = 100,
            Visibility visibility = Visibility.Visible)
        {
            var combo = new ComboBox
            {
                Width = width,
                Margin = new Thickness(5, 0, 5, 0),
                Visibility = visibility,
                ItemsSource = items,
                DisplayMemberPath = "Value",
                SelectedValuePath = "Key"
            };

            if (defaultKey != null && items.ContainsKey(defaultKey))
                combo.SelectedValue = defaultKey;
            else
                combo.SelectedIndex = 0;

            return combo;
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

            var typeDropdown = CreateComboBox(new Dictionary<string, string>
            {
                { "DIALOG", Application.Current.TryFindResource("Dialog") as string ?? "Dialog" },
                { "XP",     Application.Current.TryFindResource("GiveXP") as string ?? "XP Geben" },
                { "ITEM",   Application.Current.TryFindResource("GiveItem") as string ?? "Item Geben" },
                { "END",    Application.Current.TryFindResource("EndDialog") as string ?? "Ende-Dialog" }
            });

            var speakerDropdown = CreateComboBox(new Dictionary<string, string>
            {
                { "HERO", Application.Current.TryFindResource("Hero") as string ?? "Held" },
                { "NPC",  Application.Current.TryFindResource("NPC") as string ?? "NPC" }
            }, "HERO", width: 80);

            var textEntry = CreateTextBox(400);

            var xpLabel = new TextBlock
            {
                Text = Application.Current.TryFindResource("XP") as string ?? "XP:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Visibility = Visibility.Collapsed
            };

            var xpEntry = CreateTextBox(50, Visibility.Collapsed);

            var itemNameLabel = new TextBlock
            {
                Text = Application.Current.TryFindResource("ItemInstance") as string ?? "Iteminstanz:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Visibility = Visibility.Collapsed
            };

            var itemNameEntry = CreateTextBox(200, Visibility.Collapsed);

            var itemQuantityLabel = new TextBlock
            {
                Text = Application.Current.TryFindResource("Number") as string ?? "Anzahl:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Visibility = Visibility.Collapsed
            };

            var itemQuantityEntry = CreateTextBox(78, Visibility.Collapsed);

            var itemRecipientDropdown = CreateComboBox(new Dictionary<string, string>
            {
                { "HERO", "Held" },
                { "NPC",  "NPC" }
            }, "HERO", width: 70, visibility: Visibility.Collapsed);

            typeDropdown.SelectionChanged += (s, e) =>
            {
                string type = typeDropdown.SelectedValue as string;

                textEntry.Visibility = type == "DIALOG" ? Visibility.Visible : Visibility.Collapsed;
                xpLabel.Visibility = type == "XP" ? Visibility.Visible : Visibility.Collapsed;
                xpEntry.Visibility = type == "XP" ? Visibility.Visible : Visibility.Collapsed;

                bool itemVisible = type == "ITEM";
                itemNameLabel.Visibility = itemVisible ? Visibility.Visible : Visibility.Collapsed;
                itemNameEntry.Visibility = itemVisible ? Visibility.Visible : Visibility.Collapsed;
                itemQuantityLabel.Visibility = itemVisible ? Visibility.Visible : Visibility.Collapsed;
                itemQuantityEntry.Visibility = itemVisible ? Visibility.Visible : Visibility.Collapsed;

                speakerDropdown.IsEnabled = type == "DIALOG" || type == "ITEM";

                if (type == "XP" || type == "END")
                {
                    speakerDropdown.SelectedValue = "NPC";
                }
            };

            panel.Children.Add(typeDropdown);
            panel.Children.Add(speakerDropdown);
            panel.Children.Add(textEntry);
            panel.Children.Add(xpLabel);
            panel.Children.Add(xpEntry);
            panel.Children.Add(itemNameLabel);
            panel.Children.Add(itemNameEntry);
            panel.Children.Add(itemQuantityLabel);
            panel.Children.Add(itemQuantityEntry);

            var minusButton = new Button
            {
                Content = "-",
                Width = 24,
                Height = 24,
                Margin = new Thickness(5, 0, 0, 0)
            };

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
                XPLabel = xpLabel,
                ItemNameEntry = itemNameEntry,
                ItemNameLabel = itemNameLabel,
                ItemQuantityEntry = itemQuantityEntry,
                ItemQuantityLabel = itemQuantityLabel,
                ItemRecipientDropdown = itemRecipientDropdown,
                PanelContainer = panel
            });
        }

        public void UpdateDialogComboBoxes()
        {
            foreach (var line in dialogLines)
            {
                string prevTypeKey = line.TypeDropdown.SelectedValue as string;
                string prevSpeakerKey = line.SpeakerDropdown.SelectedValue as string;

                var typeDict = new Dictionary<string, string>
                {
                    { "DIALOG", Application.Current.TryFindResource("Dialog") as string ?? "Dialog" },
                    { "XP",     Application.Current.TryFindResource("GiveXP") as string ?? "XP Geben" },
                    { "ITEM",   Application.Current.TryFindResource("GiveItem") as string ?? "Item Geben" },
                    { "END",    Application.Current.TryFindResource("EndDialog") as string ?? "Ende-Dialog" }
                };

                var speakerDict = new Dictionary<string, string>
                {
                    { "HERO", Application.Current.TryFindResource("Hero") as string ?? "Held" },
                    { "NPC",  Application.Current.TryFindResource("NPC") as string ?? "NPC" }
                };

                line.TypeDropdown.ItemsSource = typeDict;
                line.SpeakerDropdown.ItemsSource = speakerDict;

                line.TypeDropdown.DisplayMemberPath = "Value";
                line.TypeDropdown.SelectedValuePath = "Key";

                line.SpeakerDropdown.DisplayMemberPath = "Value";
                line.SpeakerDropdown.SelectedValuePath = "Key";

                if (prevTypeKey != null && typeDict.ContainsKey(prevTypeKey))
                    line.TypeDropdown.SelectedValue = prevTypeKey;

                if (prevSpeakerKey != null && speakerDict.ContainsKey(prevSpeakerKey))
                    line.SpeakerDropdown.SelectedValue = prevSpeakerKey;
            }
        }

        // Neue Methode für Labels
        public void UpdateDialogLabels()
        {
            foreach (var line in dialogLines)
            {
                line.XPLabel.Text = Application.Current.TryFindResource("XP") as string ?? "XP:";
                line.ItemNameLabel.Text = Application.Current.TryFindResource("ItemInstance") as string ?? "Iteminstanz:";
                line.ItemQuantityLabel.Text = Application.Current.TryFindResource("Number") as string ?? "Anzahl:";
            }
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
                MessageBox.Show(
                    Application.Current.TryFindResource("MsgFillAllDialogFields") as string
                        ?? "Bitte Dialoginstanz, NPC-Instanz und Beschreibung ausfüllen!",
                    Application.Current.TryFindResource("CaptionError") as string
                        ?? "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            string dialogNumber = (dialogNumberDropdown.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "1";
            string important = importantDropdown.Text == Application.Current.TryFindResource("Yes") as string ? "TRUE" : "FALSE";
            string permanent = permanentDropdown.Text == Application.Current.TryFindResource("Yes") as string ? "TRUE" : "FALSE";

            bool hasDialogLine = false;
            bool hasNonEmptyDialogText = false;

            foreach (var line in dialogLines)
            {
                string type = line.TypeDropdown.SelectedValue as string;
                if (type == "DIALOG")
                {
                    hasDialogLine = true;

                    if (!string.IsNullOrWhiteSpace(line.TextEntry.Text))
                    {
                        hasNonEmptyDialogText = true;
                        break;
                    }
                }
            }

            if (hasDialogLine && !hasNonEmptyDialogText)
            {
                MessageBox.Show(
                    Application.Current.TryFindResource("MsgDialogLineTextRequired") as string
                        ?? "Mindestens eine Dialogzeile mit Text ist erforderlich!",
                    Application.Current.TryFindResource("CaptionError") as string
                        ?? "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var sb = new System.Text.StringBuilder();

            sb.AppendLine("// ************************************************************");
            sb.AppendLine("//                          Überschrift");
            sb.AppendLine("// ************************************************************");
            sb.AppendLine();

            sb.AppendLine($"instance DIA_{dialogInstance} (C_INFO) ");
            sb.AppendLine("{");
            sb.AppendLine($"    npc          =  {npcInstance};");
            sb.AppendLine($"    nr           =  {dialogNumber};");
            sb.AppendLine($"    condition    =  DIA_{dialogInstance}_Condition;");
            sb.AppendLine($"    information  =  DIA_{dialogInstance}_Info;");
            sb.AppendLine($"    important    =  {important};");
            sb.AppendLine($"    permanent    =  {permanent};");
            sb.AppendLine($"    Description  =  \"{description}\";");
            sb.AppendLine("};");
            sb.AppendLine();

            sb.AppendLine($"func int DIA_{dialogInstance}_Condition ()");
            sb.AppendLine("{");
            sb.AppendLine("    return TRUE;");
            sb.AppendLine("};");
            sb.AppendLine();

            sb.AppendLine($"func void DIA_{dialogInstance}_Info ()");
            sb.AppendLine("{");

            int dialogIndex = 0;
            foreach (var line in dialogLines)
            {
                string type = line.TypeDropdown.SelectedValue as string;

                if (type == "DIALOG")
                {
                    string dialogText = line.TextEntry.Text.Trim();
                    if (string.IsNullOrEmpty(dialogText))
                        continue;

                    string speakerKey = line.SpeakerDropdown.SelectedValue as string ?? "HERO";

                    string firstParam, secondParam;
                    if (speakerKey == "NPC")
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
                else if (type == "XP")
                {
                    string xpValue = line.XPEntry.Text.Trim();

                    if (int.TryParse(xpValue, out int xp) && xp >= 0)
                    {
                        sb.AppendLine($"    B_GivePlayerXP ({xp});");
                    }
                    else
                    {
                        MessageBox.Show(
                            Application.Current.TryFindResource("MsgInvalidXpValue") as string
                                ?? "Ungültiger XP-Wert in einer Zeile. Bitte gib eine ganze Zahl >= 0 ein.",
                            Application.Current.TryFindResource("CaptionError") as string
                                ?? "Fehler",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;

                    }
                }
                else if (type == "ITEM")
                {
                    string itemName = line.ItemNameEntry.Text.Trim();
                    string itemQuantityText = line.ItemQuantityEntry.Text.Trim();
                    string recipientKey = line.SpeakerDropdown.SelectedValue as string ?? "HERO";

                    if (string.IsNullOrEmpty(itemName))
                    {
                        MessageBox.Show(
                            Application.Current.TryFindResource("MsgItemInstanceRequired") as string
                                ?? "Bitte Iteminstanz angeben.",
                            Application.Current.TryFindResource("CaptionError") as string
                                ?? "Fehler",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;

                    }

                    if (!int.TryParse(itemQuantityText, out int itemQuantity) || itemQuantity <= 0)
                    {
                        MessageBox.Show(
                            Application.Current.TryFindResource("MsgInvalidItemAmount") as string
                                ?? "Bitte eine gültige Item-Anzahl (> 0) angeben.",
                            Application.Current.TryFindResource("CaptionError") as string
                                ?? "Fehler",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    string firstParam, secondParam;

                    if (recipientKey == "NPC")
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
                else if (type == "END")
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
