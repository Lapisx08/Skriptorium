using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace Skriptorium.UI.Views.Tools
{
    public partial class OrcGenerator : MetroWindow
    {
        public OrcGenerator()
        {
            InitializeComponent();

            // Eingabefelder einschränken
            RestrictToNoDigits(nameEntry);
            RestrictToNumbers(idEntry);
            RestrictToNumbers(voiceEntry);
            RestrictToNumbers(levelEntry);

            // Standardwerte passend für Orks
            guildEntry.SelectedIndex = 0; // Standard = GIL_Orc
            flagsEntry.SelectedIndex = 0; // 0
            aivRealIdEntry.SelectedIndex = 0; // ID_ORCWARRIOR
            includeCommentsDropdown.SelectedIndex = 1; // Nein
        }

        private void RestrictToNumbers(TextBox textBox)
        {
            textBox.PreviewTextInput += OnlyAllowNumbers;
            DataObject.AddPastingHandler(textBox, PreventNonNumericPaste);
        }

        private bool IsValidIntegerInput(string text) => int.TryParse(text, out _);

        // Nur Ganzzahlen bei der Eingabe erlauben
        private void OnlyAllowNumbers(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsValidIntegerInput(e.Text);
        }

        // Auch bei Einfügen nur Ganzzahlen zulassen
        private void PreventNonNumericPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (!IsValidIntegerInput(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void RestrictToNoDigits(TextBox textBox)
        {
            textBox.PreviewTextInput += DisallowDigits;
            DataObject.AddPastingHandler(textBox, PreventDigitPaste);
        }

        // Keine Zahlen bei der Eingabe erlauben
        private void DisallowDigits(object sender, TextCompositionEventArgs e)
        {
            e.Handled = e.Text.Any(char.IsDigit);
        }

        // Auch beim Einfügen keine Zahlen zulassen
        private void PreventDigitPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (text.Any(char.IsDigit))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        // Generiert das Orc-Skript und schreibt es in das Output-Textfeld
        private void GenerateCode_Click(object sender, RoutedEventArgs e)
        {
            // Pflichtfelder prüfen
            if (string.IsNullOrWhiteSpace(nameEntry.Text) ||
                string.IsNullOrWhiteSpace(idEntry.Text) ||
                string.IsNullOrWhiteSpace(voiceEntry.Text) ||
                string.IsNullOrWhiteSpace(levelEntry.Text) ||
                flagsEntry.SelectedItem == null ||
                guildEntry.SelectedItem == null ||
                aivRealIdEntry.SelectedItem == null)
            {
                MessageBox.Show(
                    Application.Current.TryFindResource("MsgFillAllFieldsBeforeGenerate") as string
                        ?? "Bitte fülle alle Felder aus, bevor du den Code generierst.",
                    Application.Current.TryFindResource("CaptionMissingInput") as string
                        ?? "Fehlende Eingaben",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            bool includeComments = ((ComboBoxItem)includeCommentsDropdown.SelectedItem)?.Content.ToString() == (Application.Current.TryFindResource("Yes") as string);

            var sb = new StringBuilder();

            // Kommentare Laden
            string cStrengthComment = TryFindResource("Comment_AttributeStrength") as string;

            // Instance-Name
            string nameSafe = nameEntry.Text.Replace(" ", "_");

            sb.AppendLine($"instance {idEntry.Text}_{nameSafe}_Ork (Npc_Default)");
            sb.AppendLine("{");
            sb.AppendLine("\t// ------ NSC ------\"");
            sb.AppendLine($"\tname\t\t\t\t  = \"{nameEntry.Text}\";");
            sb.AppendLine($"\tguild\t\t\t\t  = {((ComboBoxItem)guildEntry.SelectedItem)?.Content};");
            sb.AppendLine($"\taivar[AIV_MM_REAL_ID] = {((ComboBoxItem)aivRealIdEntry.SelectedItem)?.Content};");
            sb.AppendLine($"\tid\t\t\t\t\t  = {idEntry.Text};");
            sb.AppendLine($"\tvoice\t\t\t\t  = {voiceEntry.Text};");
            sb.AppendLine($"\tflags\t\t\t\t  = {((ComboBoxItem)flagsEntry.SelectedItem)?.Content};");
            sb.AppendLine("\tnpctype\t\t\t\t  = NPCTYPE_MAIN;");
            sb.AppendLine($"\tlevel\t\t\t\t  = {levelEntry.Text};");
            sb.AppendLine();

            sb.AppendLine("\t// ------ Attribute ------");
            sb.AppendLine($"\tattribute[ATR_STRENGTH]\t\t = 80;{(includeComments ? $" // {cStrengthComment}" : "")}");
            sb.AppendLine("\tattribute[ATR_DEXTERITY]\t = 80;");
            sb.AppendLine("\tattribute[ATR_HITPOINTS_MAX] = 225;");
            sb.AppendLine("\tattribute[ATR_HITPOINTS]\t = 225;");
            sb.AppendLine("\tattribute[ATR_MANA_MAX]\t\t = 0;");
            sb.AppendLine("\tattribute[ATR_MANA]\t\t\t = 0;");
            sb.AppendLine();

            sb.AppendLine("\t//----- Protections ----");
            sb.AppendLine("\tprotection[PROT_BLUNT] = 150;");
            sb.AppendLine("\tprotection[PROT_EDGE]  = 150;");
            sb.AppendLine("\tprotection[PROT_POINT] = 150;");
            sb.AppendLine("\tprotection[PROT_FIRE]  = 150;");
            sb.AppendLine("\tprotection[PROT_FLY]   = 150;");
            sb.AppendLine("\tprotection[PROT_MAGIC] = 20;");
            sb.AppendLine();

            sb.AppendLine("\t//----- HitChances -----");
            sb.AppendLine("\tHitChance[NPC_TALENT_1H]\t   = 0;");
            sb.AppendLine("\tHitChance[NPC_TALENT_2H]\t   = 0;");
            sb.AppendLine("\tHitChance[NPC_TALENT_BOW]\t   = 0;");
            sb.AppendLine("\tHitChance[NPC_TALENT_CROSSBOW] = 0;");
            sb.AppendLine();

            sb.AppendLine("\t// ------ Kampf-Taktik ------");
            sb.AppendLine("\tfight_tactic = FAI_ORC;");
            sb.AppendLine();

            sb.AppendLine("\t// ------ Ausgerüstete Waffen ------");
            sb.AppendLine("\tEquipItem (self, ItMw_2H_OrcAxe_01);");
            sb.AppendLine();

            sb.AppendLine("\t// ------ Inventar ------");
            sb.AppendLine();

            sb.AppendLine("\t// ------ Aussehen ------");
            sb.AppendLine("\tMdl_SetVisual (self, \"Orc.mds\");");
            sb.AppendLine($"\tMdl_SetVisualBody (self, \"{((ComboBoxItem)bodyMeshEntry.SelectedItem)?.Content}\", DEFAULT, DEFAULT, \"{((ComboBoxItem)headMeshEntry.SelectedItem)?.Content}\", DEFAULT, DEFAULT, NO_ARMOR);");
            sb.AppendLine("\tMdl_SetModelFatness (self, 0);");
            sb.AppendLine();

            sb.AppendLine("\t// ------ TA anmelden ------");
            sb.AppendLine($"\tdaily_routine = Rtn_Start_{idEntry.Text};");
            sb.AppendLine("};");
            sb.AppendLine();

            sb.AppendLine($"func void Rtn_Start_{idEntry.Text} ()");
            sb.AppendLine("{");
            sb.AppendLine("\tTA_Stand_ArmsCrossed (08,00, 20,00, \"WP_Platzhalter\");");
            sb.AppendLine("\tTA_Stand_ArmsCrossed (20,00, 08,00, \"WP_Platzhalter\");");
            sb.AppendLine("};");

            outputText.Text = sb.ToString();
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
                Clipboard.SetText(outputText.Text);
        }

        private void ResetFields_Click(object sender, RoutedEventArgs e)
        {
            nameEntry.Text = string.Empty;
            idEntry.Text = string.Empty;
            voiceEntry.Text = string.Empty;
            levelEntry.Text = string.Empty;
            outputText.Clear();

            guildEntry.SelectedIndex = 0; // GIL_Orc
            flagsEntry.SelectedIndex = 0; // 0
            bodyMeshEntry.SelectedIndex = 0; // ORC_BODYSLAVE
            headMeshEntry.SelectedIndex = 0; // Orc_HeadWarrior
            aivRealIdEntry.SelectedIndex = 0; // ID_ORCWARRIOR
            includeCommentsDropdown.SelectedIndex = 1; // Nein
        }
    }
}
