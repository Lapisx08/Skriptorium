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

        private void GenerateCode_Click(object sender, RoutedEventArgs e)
        {
            // Überprüfung: Nur ganze Zahlen für id und voice erlaubt
            if (!int.TryParse(idEntry.Text, out _) || !int.TryParse(voiceEntry.Text, out _))
            {
                MessageBox.Show("Bitte gib für ID und Voice nur ganze Zahlen ein.", "Ungültige Eingabe", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            sb.AppendLine($"    flags    =  {flagsEntry.Text}; //NPC_FLAG_IMMORTAL oder 0");
            sb.AppendLine($"    npctype  =  {((ComboBoxItem)npcTypeDropdown.SelectedItem)?.Content};");
            sb.AppendLine();

            // AIVARS
            // Optional

            // Attribute
            sb.AppendLine("    // ------ Attribute ------");
            sb.AppendLine($"    B_SetAttributesToChapter (self, 1); //Setzt Attribute und Level entsprechend des angegebenen Kapitels (1-6)");
            sb.AppendLine();

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
            sb.AppendLine("    // ------ Aussehen ------ // Muss nach Attributen kommen, weil in B_SetNpcVisual die Breite abh. v. d. Stärke skaliert wird");
            sb.AppendLine("    B_SetNpcVisual      (self, Geschlecht, \"Hum_Head_Fat_Platzhalter\", Face_N_Platzhalter, BodyTex_Platzhalter, ITAR_Platzhalter); ");
            sb.AppendLine("    Mdl_SetModelFatness (self, 0); // -1 / 0 / 1 / 2");
            sb.AppendLine("    Mdl_ApplyOverlayMds (self, \"Humans_Platzhalter.mds\"); // Tired / Militia / Mage / Arrogance / Relaxed");
            sb.AppendLine();

            // NSC-relevante Talente
            sb.AppendLine("    // ------ NSC-relevante Talente ------");
            sb.AppendLine("    B_GiveNpcTalents (self);");
            sb.AppendLine();

            // Kampf-Talente
            sb.AppendLine("    // ------ Kampf-Talente ------ // Der enthaltene B_AddFightSkill setzt Talent-Ani abhängig von TrefferChance% - alle Kampftalente werden gleichhoch gesetzt");
            sb.AppendLine("    B_SetFightSkills (self, Z); // Grenzen für Talent-Level liegen bei 30 und 60");
            sb.AppendLine();

            // TA anmelden
            sb.AppendLine("    // ------ TA anmelden ------");
            sb.AppendLine($"    daily_routine  =  Rtn_Start_{idEntry.Text};");
            sb.AppendLine();

            // Tagesroutine-Funktion
            sb.AppendLine($"func void Rtn_Start_{idEntry.Text} () // Tages-Routine muss gesamt immer 24 h ergeben");
            sb.AppendLine("{");
            sb.AppendLine("     TA_Platzhalter  (08,00,20,00,\"Waypoint-Platzhalter\");");
            sb.AppendLine("     TA_Platzhalter  (20,00,08,00,\"Waypoint-Platzhalter\");");
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
            guildEntry.Text = string.Empty;
            idEntry.Text = string.Empty;
            voiceEntry.Text = string.Empty;
            flagsEntry.Text = string.Empty;
            npcTypeDropdown.SelectedIndex = -1;
            detailsPanel.Children.Clear();
            outputText.Clear();
        }
    }
}
