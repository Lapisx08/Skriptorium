using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace Skriptorium.UI.Views.Tools
{
    public partial class NPCGenerator : MetroWindow
    {
        public NPCGenerator()
        {
            InitializeComponent();

            // Nur Ganzzahlen in idEntry und voiceEntry erlauben
            RestrictToNumbers(idEntry);
            RestrictToNumbers(voiceEntry);

            // Keine Ziffern in nameEntry und guildEntry erlauben
            RestrictToNoDigits(nameEntry);
            RestrictToNoDigits(guildEntry);

            // Standardauswahl für Flags = "0"
            flagsEntry.SelectedIndex = 0; // 0 = 0, 1 = NPC_FLAG_IMMORTAL

            // Standardauswahl für NPC Type = NPCTYPE_MAIN
            npcTypeDropdown.SelectedIndex = 0;

            // Standard für AIVars und Attribute = "Nein"
            aivarsDropdown.SelectedIndex = 1; // 0 = Ja, 1 = Nein
            attributesDropdown.SelectedIndex = 1;
            fightSkillsDropdown.SelectedIndex = 1;

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

        private void GenerateCode_Click(object sender, RoutedEventArgs e)
        {
            // Pflichtfelder prüfen
            if (string.IsNullOrWhiteSpace(nameEntry.Text) ||
                string.IsNullOrWhiteSpace(guildEntry.Text) ||
                string.IsNullOrWhiteSpace(idEntry.Text) ||
                string.IsNullOrWhiteSpace(voiceEntry.Text) ||
                flagsEntry.SelectedItem == null ||
                npcTypeDropdown.SelectedItem == null ||
                aivarsDropdown.SelectedItem == null ||
                attributesDropdown.SelectedItem == null ||
                fightSkillsDropdown.SelectedItem == null)
            {
                MessageBox.Show("Bitte fülle alle Felder aus, bevor du den Code generierst.", "Fehlende Eingaben", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sb = new StringBuilder();

            // NSC
            string guildShort = guildEntry.Text.StartsWith("GIL_") ? guildEntry.Text.Substring(4) : guildEntry.Text;
            sb.AppendLine($"instance {guildShort}_{idEntry.Text}_{nameEntry.Text.Replace(" ", "_")} (Npc_Default)");
            sb.AppendLine("{");
            sb.AppendLine("    // ------ NSC ------");
            sb.AppendLine($"    name     =  \"{nameEntry.Text}\";");
            sb.AppendLine($"    guild    =  {guildEntry.Text};");
            sb.AppendLine($"    id       =  {idEntry.Text};");
            sb.AppendLine($"    voice    =  {voiceEntry.Text};");
            sb.AppendLine($"    flags    =  {flagsEntry.Text}; // NPC_FLAG_IMMORTAL oder 0");
            sb.AppendLine($"    npctype  =  {((ComboBoxItem)npcTypeDropdown.SelectedItem)?.Content};");
            sb.AppendLine();

            if (((ComboBoxItem)aivarsDropdown.SelectedItem)?.Content.ToString() == "Ja")
            {
                // AIVARS
                sb.AppendLine("    // ------ AIVARS ------");
                sb.AppendLine("    aivar[AIV_ToughGuy]              =  TRUE;");
                sb.AppendLine("    aivar[AIV_ToughGuyNewsOverride]  =  TRUE;");
                sb.AppendLine("    aivar[AIV_IGNORE_Murder]         =  TRUE;");
                sb.AppendLine("    aivar[AIV_IGNORE_Theft]          =  TRUE;");
                sb.AppendLine("    aivar[AIV_IGNORE_Sheepkiller]    =  TRUE;");
                sb.AppendLine("    aivar[AIV_IgnoresArmor]          =  TRUE;");
                sb.AppendLine("    aivar[AIV_EnemyOverride]         =  TRUE;");
                sb.AppendLine("    aivar[AIV_MagicUser]             =  MAGIC_ALWAYS; // Setzt immer Magie beim Kämpfen ein");
                sb.AppendLine("    // Lösche die AIV, die nicht benötigt werden");
                sb.AppendLine();
            }

            // Attribute
            sb.AppendLine("    // ------ Attribute ------");
            sb.AppendLine($"    B_SetAttributesToChapter (self, 1); // Setzt Attribute und Level entsprechend des angegebenen Kapitels (1-6)");
            sb.AppendLine();

            if (((ComboBoxItem)attributesDropdown.SelectedItem)?.Content.ToString() == "Ja")
            {
                sb.AppendLine("    // Ersetzte bei Nutzung individueller Attribute B_SetAttributesToChapter (self, 1);");
                sb.AppendLine("    attribute[ATR_STRENGTH        =  10;");
                sb.AppendLine("    attribute[ATR_DEXTERITY]      =  10;");
                sb.AppendLine("    attribute[ATR_HITPOINTS_MAX]  =  40;");
                sb.AppendLine("    attribute[ATR_HITPOINTS]      =  40;");
                sb.AppendLine("    attribute[ATR_MANA_MAX]       =  10;");
                sb.AppendLine("    attribute[ATR_MANA]           =  10;");
                sb.AppendLine();
            }

            // Kampf-Taktik
            sb.AppendLine("    // ------ Kampf-Taktik ------");
            sb.AppendLine($"    fight_tactic  =  (FAI_HUMAN_Platzhalter); // COWARD / STRONG / MASTER");
            sb.AppendLine();

            // Ausgerüstete Waffen
            sb.AppendLine("    // ------ Ausgerüstete Waffen ------");
            sb.AppendLine($"    EquipItem (self, Item-Instanz); // Munition wird automatisch generiert, darf aber angegeben werden"); 
            sb.AppendLine();

            // Inventar
            sb.AppendLine("    // ------ Inventar ------");
            sb.AppendLine($"    B_CreateAmbientInv (self); // Stattet NPC mit entsprechendem Standardinventar aus");
            sb.AppendLine();

            // Aussehen
            sb.AppendLine("    // ------ Aussehen ------");
            sb.AppendLine("    B_SetNpcVisual      (self, MALE, \"Hum_Head_Fat_Platzhalter\", Face_N_Platzhalter, BodyTex_Platzhalter, ITAR_Platzhalter); // Muss nach Attributen kommen, weil in B_SetNpcVisual die Breite abh. v. d. Stärke skaliert wird");
            sb.AppendLine("    Mdl_SetModelFatness (self, 0); // -1 / 0 / 1 / 2");
            sb.AppendLine("    Mdl_ApplyOverlayMds (self, \"Humans_Platzhalter.mds\"); // Tired / Militia / Mage / Arrogance / Relaxed");
            sb.AppendLine();

            // NSC-relevante Talente
            sb.AppendLine("    // ------ NSC-relevante Talente ------");
            sb.AppendLine("    B_GiveNpcTalents (self);");
            sb.AppendLine();

            // Kampf-Talente
            sb.AppendLine("    // ------ Kampf-Talente ------");
            sb.AppendLine("    B_SetFightSkills (self, 10); // Grenzen für Talent-Level liegen bei 30 und 60 / Der enthaltene B_AddFightSkill setzt alle Kampftalente gleichhoch");
            sb.AppendLine();

            if (((ComboBoxItem)fightSkillsDropdown.SelectedItem)?.Content.ToString() == "Ja")
            {
                sb.AppendLine("    // Ersetzte bei Nutzung individueller Kampf-Talente B_SetFightSkills (self, 10);");
                sb.AppendLine("    B_AddFightSkill (self, NPC_TALENT_1H, 10);");
                sb.AppendLine("    B_AddFightSkill (self, NPC_TALENT_2H, 10);");
                sb.AppendLine("    B_AddFightSkill (self, NPC_TALENT_BOW, 10);");
                sb.AppendLine("    B_AddFightSkill (self, NPC_TALENT_CROSSBOW, 10);");
                sb.AppendLine();
            }

            // TA anmelden
            sb.AppendLine("    // ------ TA anmelden ------");
            sb.AppendLine($"    daily_routine  =  Rtn_Start_{idEntry.Text};");
            sb.AppendLine();

            // Tagesroutine-Funktion
            sb.AppendLine($"func void Rtn_Start_{idEntry.Text} () // Tages-Routine muss insgesamt immer 24 h ergeben");
            sb.AppendLine("{");
            sb.AppendLine("    TA_Platzhalter  (08,00,20,00,\"WP_Platzhalter\");");
            sb.AppendLine("    TA_Platzhalter  (20,00,08,00,\"WP_Platzhalter\");");
            sb.AppendLine("};");

            outputText.Text = sb.ToString();
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(outputText.Text);
        }

        private void ResetFields_Click(object sender, RoutedEventArgs e)
        {
            // Textfelder zurücksetzen
            nameEntry.Text = string.Empty;
            guildEntry.Text = string.Empty;
            idEntry.Text = string.Empty;
            voiceEntry.Text = string.Empty;
            outputText.Clear();

            // ComboBoxen auf Standard zurücksetzen
            flagsEntry.SelectedIndex = 0;         // "0"
            npcTypeDropdown.SelectedIndex = 0;    // "NPCTYPE_MAIN"
            aivarsDropdown.SelectedIndex = 1;     // "Nein"
            attributesDropdown.SelectedIndex = 1; // "Nein"
            fightSkillsDropdown.SelectedIndex = 1; // "Nein"

            // Dynamischen Inhalt leeren (optional)
            detailsPanel.Children.Clear();
        }
    }
}
