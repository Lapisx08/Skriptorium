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
            flagsEntry.SelectedIndex = 0; // 0 = 0

            // Standardauswahl für NPC Type = NPCTYPE_MAIN
            npcTypeDropdown.SelectedIndex = 0;

            // Standard für AIVars und Attribute = "Nein"
            aivarsDropdown.SelectedIndex = 1; // 0 = Ja, 1 = Nein
            attributesDropdown.SelectedIndex = 1;
            fightSkillsDropdown.SelectedIndex = 1;

            // Standard für Kommentare = "Nein"
            includeCommentsDropdown.SelectedIndex = 1; // 0 = "Ja", 1 = Nein
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

        public enum Gender { MALE, FEMALE }

        private readonly Random rng = new Random();

        private readonly List<string> maleHeads = new()
        {
            "Hum_Head_Psionic", "Hum_Head_Thief", "Hum_Head_Bald", "Hum_Head_Pony",
            "Hum_Head_Fighter", "Hum_Head_FatBald", "HUM_HEAD_BALD_BART", "HUM_HEAD_BEARD",
            "HUM_HEAD_BEARD2", "HUM_HEAD_BEARD4", "HUM_HEAD_BRODA2", "HUM_HEAD_IRO",
            "HUM_HEAD_KRUSSEL_BART", "HUM_HEAD_KRUSSEL_NOBART", "HUM_HEAD_LANGOFFEN_BART",
            "HUM_HEAD_LANGOFFEN_NOBART", "HUM_HEAD_LONG", "HUM_HEAD_LONGHAIR", "HUM_HEAD_MUSTACHE",
            "HUM_HEAD_NEMORA", "HUM_HEAD_PONYBEARD", "HUM_HEAD_PONYNEU", "HUM_HEAD_SABROSA",
            "HUM_HEAD_SIDEBURNS", "HUM_HEAD_ZOPFI_BART", "HUM_HEAD_ZOPFI_NOBART"
        };

        private readonly List<string> femaleHeads = new()
        {
            "Hum_Head_BabeHair", "Hum_Head_Babe8", "Hum_Head_Babe7", "Hum_Head_Babe6", "Hum_Head_Babe5",
            "Hum_Head_Babe4", "Hum_Head_Babe3", "Hum_Head_Babe2", "Hum_Head_Babe1", "Hum_Head_Babe",
            "HUM_HEAD_BABE9", "HUM_HEAD_BABE11", "HUM_HEAD_BABE12", "HUM_HEAD_BABE13", "HUM_HEAD_BABE14",
            "HUM_HEAD_GRELKA", "HUM_HEAD_REMI"
        };

        private readonly List<string> maleFaces = new()
        {
            "Face_P_ToughBald", "Face_P_Tough_Drago", "Face_P_Tough_Torrez", "Face_P_Tough_Rodriguez",
            "Face_P_ToughBald_Nek", "Face_P_NormalBald", "Face_P_Normal01", "Face_P_Normal02",
            "Face_P_Normal_Fletcher", "Face_P_Normal03", "Face_P_NormalBart01", "Face_P_NormalBart_Cronos",
            "Face_P_NormalBart_Nefarius", "Face_P_NormalBart_Riordian", "Face_P_OldMan_Gravo",
            "Face_P_Weak_Cutter", "Face_P_Weak_Ulf_Wohlers", "Face_N_Important_Arto", "Face_N_ImportantGrey",
            "Face_N_ImportantOld", "Face_N_Tough_Lee", "Face_N_Tough_Skip", "Face_N_ToughBart01",
            "Face_N_Tough_Okyl", "Face_N_Normal01", "Face_N_Normal_Cord", "Face_N_Normal_Olli_Kahn",
            "Face_N_Normal02", "Face_N_Normal_Spassvogel", "Face_N_Normal03", "Face_N_Normal04",
            "Face_N_Normal05", "Face_N_Normal_Stone", "Face_N_Normal06", "Face_N_Normal_Erpresser",
            "Face_N_Normal07", "Face_N_Normal_Blade", "Face_N_Normal08", "Face_N_Normal14",
            "Face_N_Normal_Sly", "Face_N_Normal16", "Face_N_Normal17", "Face_N_Normal18",
            "Face_N_Normal19", "Face_N_Normal20", "Face_N_NormalBart01", "Face_N_NormalBart02",
            "Face_N_NormalBart03", "Face_N_NormalBart04", "Face_N_NormalBart05", "Face_N_NormalBart06",
            "Face_N_NormalBart_Senyan", "Face_N_NormalBart08", "Face_N_NormalBart09", "Face_N_NormalBart10",
            "Face_N_NormalBart11", "Face_N_NormalBart12", "Face_N_NormalBart_Dexter", "Face_N_NormalBart_Graham",
            "Face_N_NormalBart_Dusty", "Face_N_NormalBart16", "Face_N_NormalBart17", "Face_N_NormalBart_Huno",
            "Face_N_NormalBart_Grim", "Face_N_NormalBart20", "Face_N_NormalBart21", "Face_N_NormalBart22",
            "Face_N_OldBald_Jeremiah", "Face_N_Weak_Ulbert", "Face_N_Weak_BaalNetbek", "Face_N_Weak_Herek",
            "Face_N_Weak04", "Face_N_Weak05", "Face_N_Weak_Orry", "Face_N_Weak_Asghan", "Face_N_Weak_Markus_Kark",
            "Face_N_Weak_Cipher_alt", "Face_N_NormalBart_Swiney", "Face_N_Weak12", "Face_L_ToughBald01",
            "Face_L_Tough01", "Face_L_Tough02", "Face_L_Tough_Santino", "Face_L_ToughBart_Quentin",
            "Face_L_Normal_GorNaBar", "Face_L_NormalBart01", "Face_L_NormalBart02", "Face_L_NormalBart_Rufus",
            "Face_B_ToughBald", "Face_B_Tough_Pacho", "Face_B_Tough_Silas", "Face_B_Normal01",
            "Face_B_Normal_Kirgo", "Face_B_Normal_Sharky", "Face_B_Normal_Orik", "Face_B_Normal_Kharim"
        };

        private readonly List<string> femaleFaces = new()
        {
            "FaceBabe_N_BlackHair", "FaceBabe_N_Blondie", "FaceBabe_N_BlondTattoo", "FaceBabe_N_PinkHair",
            "FaceBabe_L_Charlotte", "FaceBabe_B_RedLocks", "FaceBabe_N_HairAndCloth", "FaceBabe_N_WhiteCloth",
            "FaceBabe_N_GreyCloth", "FaceBabe_N_Brown", "FaceBabe_N_VlkBlonde", "FaceBabe_N_BauBlonde",
            "FaceBabe_N_YoungBlonde", "FaceBabe_N_OldBlonde", "FaceBabe_P_MidBlonde", "FaceBabe_N_MidBauBlonde",
            "FaceBabe_N_OldBrown", "FaceBabe_N_Lilo", "FaceBabe_N_Hure", "FaceBabe_N_Anne",
            "FaceBabe_B_RedLocks2", "FaceBabe_L_Charlotte2"
        };

        private readonly List<string> maleBodyTexes = new() { "BodyTex_P", "BodyTex_N", "BodyTex_L", "BodyTex_B", "BodyTex_T" };
        private readonly List<string> femaleBodyTexes = new() { "BodyTexBabe_P", "BodyTexBabe_N", "BodyTexBabe_L", "BodyTexBabe_B", "BodyTexBabe_F", "BodyTexBabe_S" };
       
        private string GenerateNpcVisual()
        {
            string selectedGender = ((ComboBoxItem)genderDropdown.SelectedItem)?.Content.ToString();
            Gender gender = selectedGender == Application.Current.TryFindResource("Female") as string ? Gender.FEMALE : Gender.MALE;
            
            string head = gender == Gender.MALE
                ? maleHeads[rng.Next(maleHeads.Count)]
                : femaleHeads[rng.Next(femaleHeads.Count)];
            
            string bodyTex = gender == Gender.MALE
                ? maleBodyTexes[rng.Next(maleBodyTexes.Count)]
                : femaleBodyTexes[rng.Next(femaleBodyTexes.Count)];
            
            string face = gender == Gender.MALE
                ? maleFaces[rng.Next(maleFaces.Count)]
                : femaleFaces[rng.Next(femaleFaces.Count)];
            return $"B_SetNpcVisual (self, {gender}, \"{head}\", {face}, {bodyTex}, NO_ARMOR)";
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

            bool includeComments = ((ComboBoxItem)includeCommentsDropdown.SelectedItem)?.Content.ToString() ==
                                   (Application.Current.TryFindResource("Yes") as string);

            var sb = new StringBuilder();

            // NSC
            string guildText;

            // Wenn kein "GIL_" vorhanden ist, automatisch hinzufügen
            if (guildEntry.Text.StartsWith("GIL_", StringComparison.OrdinalIgnoreCase))
            {
                guildText = guildEntry.Text;
            }
            else
            {
                guildText = $"GIL_{guildEntry.Text}";
            }

            string guildShort = guildText.Length > 4 ? guildText.Substring(4) : guildText;

            sb.AppendLine($"instance {guildShort}_{idEntry.Text}_{nameEntry.Text.Replace(" ", "_")} (Npc_Default)");
            sb.AppendLine("{");
            sb.AppendLine("    // ------ NSC ------");
            sb.AppendLine($"    name     =  \"{nameEntry.Text}\";");
            sb.AppendLine($"    guild    =  {guildText};");
            sb.AppendLine($"    id       =  {idEntry.Text};");
            sb.AppendLine($"    voice    =  {voiceEntry.Text};");
            sb.AppendLine($"    flags    =  {flagsEntry.Text};");
            sb.AppendLine($"    npctype  =  {((ComboBoxItem)npcTypeDropdown.SelectedItem)?.Content};");

            if (((ComboBoxItem)attributesDropdown.SelectedItem)?.Content.ToString() == Application.Current.TryFindResource("Yes") as string)
            {
                sb.AppendLine("    level    =  1;");
            }
            sb.AppendLine();

            if (((ComboBoxItem)aivarsDropdown.SelectedItem)?.Content.ToString() == Application.Current.TryFindResource("Yes") as string)
            {
                // AIVARS
                sb.AppendLine("    // ------ AIVARS ------");
                sb.AppendLine($"    aivar[AIV_ToughGuy]              =  TRUE;{(includeComments ? " // Jubelt beim Kampf, hat keine Neuigkeiten bei Attack (kann über AIV_LastFightAgainstPlayer reagieren)" : "")}");
                sb.AppendLine($"    aivar[AIV_ToughGuyNewsOverride]  =  TRUE;{(includeComments ? "" : "")}");
                sb.AppendLine($"    aivar[AIV_IGNORE_Murder]         =  TRUE;{(includeComments ? "" : "")}");
                sb.AppendLine($"    aivar[AIV_IGNORE_Theft]          =  TRUE;{(includeComments ? "" : "")}");
                sb.AppendLine($"    aivar[AIV_IGNORE_Sheepkiller]    =  TRUE;{(includeComments ? "" : "")}");
                sb.AppendLine($"    aivar[AIV_IgnoresFakeGuild]      =  TRUE;{(includeComments ? " // Ignoriert die falsche Gilde, die durch die Rüstung erzeugt wird" : "")}");
                sb.AppendLine($"    aivar[AIV_IgnoresArmor]          =  TRUE;{(includeComments ? " // Keine Reaktion oder Konsequenzen auf die Rüstung des Helden" : "")}");
                sb.AppendLine($"    aivar[AIV_NPCIsRanger]           =  TRUE;{(includeComments ? " // NPC gehört zum Ring des Wassers" : "")}");
                sb.AppendLine($"    aivar[AIV_NoFightParker]         =  TRUE;{(includeComments ? " // NPC wird weder angegriffen, noch greift er selbst welche an" : "")}");
                sb.AppendLine($"    aivar[AIV_EnemyOverride]         =  TRUE;{(includeComments ? "" : "")}");
                sb.AppendLine($"    aivar[AIV_MagicUser]             =  MAGIC_ALWAYS;{(includeComments ? " // Setzt immer Magie beim Kämpfen ein" : "")}");
                sb.AppendLine($"    // Lösche die AIV, die nicht benötigt werden{(includeComments ? "" : "")}");
                sb.AppendLine();
            }

            // Attribute
            sb.AppendLine("    // ------ Attribute ------");

            // Nur hinzufügen, wenn keine individuellen Attribute gesetzt werden
            if (((ComboBoxItem)attributesDropdown.SelectedItem)?.Content.ToString() !=
                (Application.Current.TryFindResource("Yes") as string))
            {
                sb.AppendLine($"    B_SetAttributesToChapter (self, 1);{(includeComments ? " // Setzt Attribute und Level entsprechend des angegebenen Kapitels (1-6)" : "")}");
                sb.AppendLine();
            }

            if (((ComboBoxItem)attributesDropdown.SelectedItem)?.Content.ToString() == Application.Current.TryFindResource("Yes") as string)
            {
                sb.AppendLine("    attribute[ATR_STRENGTH]       =  10;");
                sb.AppendLine("    attribute[ATR_DEXTERITY]      =  10;");
                sb.AppendLine("    attribute[ATR_HITPOINTS_MAX]  =  40;");
                sb.AppendLine("    attribute[ATR_HITPOINTS]      =  40;");
                sb.AppendLine("    attribute[ATR_MANA_MAX]       =  10;");
                sb.AppendLine("    attribute[ATR_MANA]           =  10;");
                sb.AppendLine();
            }

            // Kampf-Taktik
            sb.AppendLine("    // ------ Kampf-Taktik ------");
            sb.AppendLine($"    fight_tactic  =  FAI_HUMAN_COWARD;{(includeComments ? " // COWARD / NORMAL / STRONG / MASTER" : "")}");
            sb.AppendLine();

            // Ausgerüstete Waffen
            sb.AppendLine("    // ------ Ausgerüstete Waffen ------");
            sb.AppendLine($"    EquipItem (self, ItMw_1h_Bau_Mace);{(includeComments ? " // Ersetze die Iteminstanz durch die gewünschte Waffe / Munition wird automatisch generiert, darf aber angegeben werden" : "")}");
            sb.AppendLine();

            // Inventar
            sb.AppendLine("    // ------ Inventar ------");
            sb.AppendLine($"    B_CreateAmbientInv (self);{(includeComments ? " // Stattet NPC mit entsprechendem Standardinventar aus" : "")}");
            sb.AppendLine();

            // Aussehen
            sb.AppendLine("    // ------ Aussehen ------");
            sb.AppendLine($"    {GenerateNpcVisual()};{(includeComments ? " // Ersetze NO_ARMOR durch die gewünschte Rüstungsinstanz / Muss nach Attributen kommen, weil in B_SetNpcVisual die Breite abh. v. d. Stärke skaliert wird" : "")}");
            sb.AppendLine($"    Mdl_SetModelFatness (self, 0);{(includeComments ? " // -1 / 0 / 1 / 2" : "")}");
            sb.AppendLine($"    Mdl_ApplyOverlayMds (self, \"Humans_Tired.mds\");{(includeComments ? " // Tired / Militia / Mage / Arrogance / Relaxed" : "")}");
            sb.AppendLine();

            // NSC-relevante Talente
            sb.AppendLine("    // ------ NSC-relevante Talente ------");
            sb.AppendLine("    B_GiveNpcTalents (self);");
            sb.AppendLine();

            // Kampf-Talente
            sb.AppendLine("    // ------ Kampf-Talente ------");

            // Nur hinzufügen, wenn keine individuellen Kampf-Talente gesetzt werden
            if (((ComboBoxItem)fightSkillsDropdown.SelectedItem)?.Content.ToString() !=
                (Application.Current.TryFindResource("Yes") as string))
            {
                sb.AppendLine($"    B_SetFightSkills (self, 10);{(includeComments ? " // Grenzen für Talent-Level liegen bei 30 und 60 / Der enthaltene B_AddFightSkill setzt alle Kampftalente gleichhoch" : "")}");
                sb.AppendLine();
            }

            if (((ComboBoxItem)fightSkillsDropdown.SelectedItem)?.Content.ToString() == Application.Current.TryFindResource("Yes") as string)
            {
                sb.AppendLine("    B_AddFightSkill (self, NPC_TALENT_1H, 10);");
                sb.AppendLine("    B_AddFightSkill (self, NPC_TALENT_2H, 10);");
                sb.AppendLine("    B_AddFightSkill (self, NPC_TALENT_BOW, 10);");
                sb.AppendLine("    B_AddFightSkill (self, NPC_TALENT_CROSSBOW, 10);");
                sb.AppendLine();
            }

            // TA anmelden
            sb.AppendLine("    // ------ TA anmelden ------");
            sb.AppendLine($"    daily_routine  =  Rtn_Start_{idEntry.Text};");
            sb.AppendLine("};");
            sb.AppendLine();

            // Tagesroutine-Funktion
            sb.AppendLine($"func void Rtn_Start_{idEntry.Text} (){(includeComments ? " // Tages-Routine muss insgesamt immer 24 h ergeben und sie muss mindestens zwei Tagesabläufe umfassen" : "")}");
            sb.AppendLine("{");
            sb.AppendLine("    TA_Stand_ArmsCrossed (08,00,20,00,\"WP_Platzhalter\");");
            sb.AppendLine("    TA_Stand_ArmsCrossed (20,00,08,00,\"WP_Platzhalter\");");
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
            flagsEntry.SelectedIndex = 0; // "0"
            npcTypeDropdown.SelectedIndex = 0; // "NPCTYPE_MAIN"
            aivarsDropdown.SelectedIndex = 1; // "Nein"
            attributesDropdown.SelectedIndex = 1; // "Nein"
            fightSkillsDropdown.SelectedIndex = 1; // "Nein"
            genderDropdown.SelectedIndex = 0; // "Männlich"
            includeCommentsDropdown.SelectedIndex = 1; // "Nein"

            // Dynamischen Inhalt leeren
            detailsPanel.Children.Clear();
        }
    }
}