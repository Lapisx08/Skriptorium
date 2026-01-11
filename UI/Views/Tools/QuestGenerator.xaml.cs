using MahApps.Metro.Controls;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Skriptorium.UI.Views.Tools
{
    public partial class QuestGenerator : MetroWindow
    {
        // Zusätzliche freie Logeinträge
        private readonly List<TextBox> freeLogs = new();

        public QuestGenerator()
        {
            InitializeComponent();
        }

        // + Logeintrag hinzufügen
        private void AddFreeLog(object sender, RoutedEventArgs e)
        {
            // Container für TextBox + Minusbutton
            var rowPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var logBox = new TextBox
            {
                Height = 24,
                Width = 620,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = false,
                Margin = new Thickness(0, 0, 5, 0)
            };

            TextBoxHelper.SetWatermark(logBox, (Application.Current.TryFindResource("NewLogEntry") as string));
            
            var minusButton = new Button
            {
                Content = "–",
                Width = 24,
                Height = 24,
                VerticalAlignment = VerticalAlignment.Center
            };

            minusButton.Click += (s, _) =>
            {
                FreeLogContainer.Children.Remove(rowPanel);
                freeLogs.Remove(logBox);
            };

            rowPanel.Children.Add(logBox);
            rowPanel.Children.Add(minusButton);

            freeLogs.Add(logBox);
            FreeLogContainer.Children.Add(rowPanel);
        }

        // Generieren
        private void Generate(object sender, RoutedEventArgs e)
        {
            string questId = QuestIdBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(questId))
            {
                MessageBox.Show(
                    "Bitte einen internen Quest-Namen angeben.",
                    "Fehlende Eingabe",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            string topic = $"TOPIC_{questId}";
            string mis = $"MIS_{questId}";

            // Log_Constants.d
            OutConstantsBox.Text =
                $"const string {topic} = \"{Escape(QuestTitleBox.Text)}\";\n" +
                $"var int {mis};\n";

            // B_CloseTopics.d
            OutCloseBox.Text =
                $"B_CloseTopic ({topic}, {mis}, {StartChapterBox.Text}, {EndChapterBox.Text});\n";

            // Questdialog
            var sb = new StringBuilder();

            sb.AppendLine("// Queststart");
            sb.AppendLine($"Log_CreateTopic ({topic}, LOG_MISSION);");
            sb.AppendLine($"Log_SetTopicStatus ({topic}, LOG_RUNNING);");
            sb.AppendLine($"{mis} = LOG_RUNNING;");
            sb.AppendLine($"B_LogEntry ({topic}, \"{Escape(LogStartBox.Text)}\");");
            sb.AppendLine();

            sb.AppendLine("// Questerfolg");
            sb.AppendLine($"Log_SetTopicStatus ({topic}, LOG_SUCCESS);");
            sb.AppendLine($"{mis} = LOG_SUCCESS;");
            sb.AppendLine($"B_LogEntry ({topic}, \"{Escape(LogSuccessBox.Text)}\");");
            sb.AppendLine("B_CheckLog();");
            sb.AppendLine();

            sb.AppendLine("// Quest gescheitert");
            sb.AppendLine($"Log_SetTopicStatus ({topic}, LOG_FAILED);");
            sb.AppendLine($"{mis} = LOG_FAILED;");
            sb.AppendLine($"B_LogEntry ({topic}, \"{Escape(LogFailBox.Text)}\");");
            sb.AppendLine("B_CheckLog();");
            sb.AppendLine();

            foreach (var log in freeLogs)
            {
                if (string.IsNullOrWhiteSpace(log.Text))
                    continue;

                sb.AppendLine("// Neue Questinfos");
                sb.AppendLine($"B_LogEntry ({topic}, \"{Escape(log.Text)}\");");
            }

            OutDialogBox.Text = sb.ToString();
        }

        // Button zum Kopieren der Ergebnisse
        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button &&
                button.Tag is TextBox textBox &&
                !string.IsNullOrWhiteSpace(textBox.Text))
            {
                Clipboard.SetText(textBox.Text);
            }
        }

        // Zurücksetzen
        private void ResetFields(object sender, RoutedEventArgs e)
        {
            QuestIdBox.Clear();
            QuestTitleBox.Clear();

            StartChapterBox.Text = "1";
            EndChapterBox.Text = "6";

            LogStartBox.Clear();
            LogSuccessBox.Clear();
            LogFailBox.Clear();

            FreeLogContainer.Children.Clear();
            freeLogs.Clear();

            OutConstantsBox.Clear();
            OutCloseBox.Clear();
            OutDialogBox.Clear();
        }

        // Hilfsfunktion: Escape für Gothic-Skripte
        private static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "")
                .Replace("\n", " ");
        }
    }
}
