using System;
using System.Collections.Generic;
using System.Linq;
using Skriptorium.Parsing;

namespace TestProgram.Analysis
{
    public class AutocompletionEngine
    {
        private readonly DaedalusLexer lexer = new DaedalusLexer();
        private readonly Dictionary<string, string> symbolTable = new(); // Name -> Typ
        private readonly HashSet<string> suggestions = new(StringComparer.OrdinalIgnoreCase);

        // Initialisierung mit den vordefinierten Schlüsselwörtern, speziellen Schlüsselwörtern und häufigen Konstanten/Funktionen
        private static readonly HashSet<string> predefinedSuggestions = new(StringComparer.OrdinalIgnoreCase)
        {
            // Schlüsselwörter
            "func", "var", "const", "return", "if", "else", "instance", "class", "prototype",
            "int", "float", "void", "string", "c_npc", "true", "false",
    
            // Spezielle Schlüsselwörter
            "self", "other", "hero", "slf",
    
            // Häufige Konstanten und Funktionen
            "Npc_Default", "aivar", "MALE", "FEMALE", "ZS_Talk", "EquipItem", "PrintScreen",
    
            // Npc_-Funktionen und -Konstanten
            "Npc_GetDistToNpc", "Npc_GetDistToWP", "Npc_SetTarget", "Npc_InsertItem",
            "Npc_HasItems", "Npc_GetStateTime", "Npc_IsInState", "Npc_KnowsInfo",
            "Npc_IsDead", "Npc_SetTrueGuild", "Npc_ExchangeRoutine",
    
            // NPC_-Konstanten
            "NPC_HUMAN", "NPC_ORC", "NPC_TROLL", "NPC_SKELETON",
            "NPCTYPE_MAIN", "NPC_FLAG_IMMORTAL",
            "NPC_TALENT_1H", "NPC_TALENT_2H", "NPC_TALENT_BOW", "NPC_TALENT_CROSSBOW",
    
            // AI_-Funktionen
            "AI_StandUp", "AI_GotoWP", "AI_AlignToWP", "AI_PlayAni", "AI_Wait",
            "AI_Queue", "AI_ContinueRoutine", "AI_EndProcessInfos", "AI_Output",
            "AI_UnequipArmor", "AI_EquipArmor",
    
            // AIV_-Konstanten
            "AIV_ToughGuy", "AIV_ToughGuyNewsOverride", "AIV_IGNORE_Murder",
            "AIV_IGNORE_Theft", "AIV_IGNORE_Sheepkiller", "AIV_TalkedToPlayer",
    
            // FAI_-Konstanten
            "FAI_HUMAN_MASTER", "FAI_HUMAN_NORMAL", "FAI_HUMAN_STRONG",
    
            // Wld_-Funktionen
            "Wld_InsertNpc", "Wld_SetTime", "Wld_GetDay", "Wld_IsRaining",
            "Wld_InsertItem", "Wld_PlayEffect",
    
            // Mdl_-Funktionen
            "Mdl_SetVisual", "Mdl_ApplyOverlayMds", "Mdl_SetModelScale", "Mdl_SetModelFatness",
    
            // Info_-Funktionen
            "Info_AddChoice", "Info_ClearChoices", "Info_Advance",
    
            // Create_-Funktionen
            "CreateInvItem", "CreateInvItems",
    
            // Log_-Funktionen
            "Log_CreateTopic", "Log_SetTopicStatus", "Log_AddEntry",
    
            // Hlp_-Funktionen
            "Hlp_GetNpc", "Hlp_IsValidNpc", "Hlp_GetInstanceID",
    
            // Snd_-Funktionen
            "Snd_Play", "Snd_Play3d",
    
            // TA_-Funktionen
            "TA_BeginOverlay", "TA_EndOverlay", "TA_Min",
            "TA_Sit_Bench", "TA_Stand_Drinking", "TA_Sleep", "TA_Bognern",
            "TA_Pfeileschnitzen", "TA_Guide_Player", "TA_Sit_Chair",
            "TA_Stand_Sweeping", "TA_Smalltalk", "TA_Campfire",
            "TA_Stand_WP", "TA_Follow_Player", "TA_Stand_ArmsCrossed", "TA_Stand_Guarding",
    
            // B_-Funktionen
            "B_SetAttributesToChapter", "B_SetNpcVisual", "B_CreateAmbientInv",
            "B_GiveNpcTalents", "B_SetFightSkills", "B_GiveInvItems", "B_Say_Gold",
            "B_GrantAbsolution", "B_LogEntry", "B_UseFakeScroll", "B_KillNpc",
            "B_TeachFightTalentPercent", "B_BuildLearnString", "B_GetLearnCostTalent",
            "B_GetTotalPetzCounter", "B_GetGreatestPetzCrime", "B_Kapitelwechsel",
            "B_StartOtherRoutine", "B_RaiseFightTalent", "B_GivePlayerXP",
    
            // GIL_-Konstanten
            "GIL_HUMAN", "GIL_ORC", "GIL_TROLL", "GIL_PAL", "GIL_MIL", "GIL_KDF",
            "GIL_NOV", "GIL_SLD", "GIL_OUT", "GIL_STRF",
    
            // Weitere Konstanten
            "ATR_STRENGTH", "ATR_DEXTERITY", "ATR_MANA", "AR_KILL",
            "LOC_TEMPLE", "LOC_CITY",
            "LOG_RUNNING", "LOG_SUCCESS", "LOG_MISSION", "FONT_SCREEN",
            "PETZCOUNTER_MURDER", "PETZCOUNTER_City_Theft", "PETZCOUNTER_City_Attack", "PETZCOUNTER_City_Sheepkiller",
    
            // REAL_-Konstanten
            "PRINT_Learn2h1", "PRINT_Learn2h5", "PRINT_Addon_GuildNeeded", "PRINT_Addon_GuildNeeded_NOV",
    
            // ZEN_-Konstanten
            "NEWWORLD_ZEN",
    
            // Sex_-Konstanten
            "MALE", "FEMALE",
    
            // Weitere Funktionen
            "EquipItem", "PrintScreen", "ZS_Talk"
        };

        public AutocompletionEngine()
        {
            // Vorschläge mit vordefinierten Schlüsselwörtern und Konstanten initialisieren
            suggestions.UnionWith(predefinedSuggestions);
        }

        // Tokenisierung des Eingabecodes und Extraktion von Symbolen für die Autovervollständigung
        public void UpdateSymbolsFromCode(string[] lines)
        {
            var tokens = lexer.Tokenize(lines);
            symbolTable.Clear();
            suggestions.Clear();
            suggestions.UnionWith(predefinedSuggestions);

            // Token verarbeiten, um Bezeichner, Funktionsnamen, Instanznamen und Konstanten/Funktionen zu extrahieren
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                // Debugging: Token-Werte und -Typen ausgeben
                Console.WriteLine($"Token: {token.Value}, Type: {token.Type}, Line: {token.Line}, Column: {token.Column}");

                switch (token.Type)
                {
                    case TokenType.Identifier:
                    case TokenType.FunctionName:
                    case TokenType.InstanceName:
                        // Nur Bezeichner mit Mindestlänge 3 und nach var, func oder instance aufnehmen
                        if (!symbolTable.ContainsKey(token.Value) && token.Value.Length >= 3 && IsDeclaredIdentifier(tokens, i))
                        {
                            // Typ basierend auf Kontext ableiten
                            string inferredType = InferTypeFromContext(tokens, i);
                            symbolTable[token.Value] = inferredType;
                            suggestions.Add(token.Value);
                            Console.WriteLine($"Added to suggestions: {token.Value} (Type: {inferredType})");
                        }
                        break;

                    case TokenType.GuildConstant:
                    case TokenType.NPC_Constant:
                    case TokenType.AIVConstant:
                    case TokenType.FAIConstant:
                    case TokenType.CRIMEConstant:
                    case TokenType.LOCConstant:
                    case TokenType.PETZCOUNTERConstant:
                    case TokenType.LOGConstant:
                    case TokenType.FONTConstant:
                    case TokenType.REALConstant:
                    case TokenType.ATRConstant:
                    case TokenType.ARConstant:
                    case TokenType.PLAYERConstant:
                    case TokenType.BuiltInFunction:
                    case TokenType.MdlFunction:
                    case TokenType.AIFunction:
                    case TokenType.NpcFunction:
                    case TokenType.InfoFunction:
                    case TokenType.CreateFunction:
                    case TokenType.WldFunction:
                    case TokenType.LogFunction:
                    case TokenType.HlpFunction:
                    case TokenType.SndFunction:
                    case TokenType.TAFunction:
                    case TokenType.EquipFunction:
                    case TokenType.ZENConstant:
                    case TokenType.SexConstant:
                    case TokenType.AiVariable:
                        if (!symbolTable.ContainsKey(token.Value))
                        {
                            symbolTable[token.Value] = token.Type.ToString();
                            suggestions.Add(token.Value);
                            Console.WriteLine($"Added to suggestions: {token.Value} (Type: {token.Type})");
                        }
                        break;
                }
            }

            // Debugging: Gesamte suggestions-Menge ausgeben
            Console.WriteLine("Current suggestions: " + string.Join(", ", suggestions));
        }

        // Prüfen, ob ein Bezeichner nach var, func oder instance deklariert ist
        private bool IsDeclaredIdentifier(List<DaedalusToken> tokens, int currentIndex)
        {
            if (currentIndex <= 0)
                return false;

            var prevToken = tokens[currentIndex - 1];
            // Prüfen, ob das vorherige Token var, func oder instance ist
            if (prevToken.Type == TokenType.VarKeyword ||
                prevToken.Type == TokenType.FuncKeyword ||
                prevToken.Type == TokenType.InstanceKeyword)
            {
                return true;
            }

            // Für Funktionen: Prüfen, ob ein TypeKeyword (z. B. void, int) vor dem func-Keyword steht
            if (prevToken.Type == TokenType.TypeKeyword && currentIndex > 1)
            {
                var prevPrevToken = tokens[currentIndex - 2];
                if (prevPrevToken.Type == TokenType.FuncKeyword)
                {
                    return true;
                }
            }

            return false;
        }

        // Typ aus Kontext ableiten (z. B. vorhergehende Schlüsselwörter)
        private string InferTypeFromContext(List<DaedalusToken> tokens, int currentIndex)
        {
            if (currentIndex > 0)
            {
                var prevToken = tokens[currentIndex - 1];
                if (prevToken.Type == TokenType.TypeKeyword)
                {
                    return prevToken.Value; // z. B. "int", "string", "c_npc"
                }
                else if (prevToken.Type == TokenType.FuncKeyword)
                {
                    // Nach Rückgabetyp suchen
                    for (int i = currentIndex - 2; i >= 0; i--)
                    {
                        if (tokens[i].Type == TokenType.TypeKeyword)
                        {
                            return tokens[i].Value; // z. B. "void", "int"
                        }
                    }
                    return "void"; // Standard für Funktionen
                }
                else if (prevToken.Type == TokenType.InstanceKeyword)
                {
                    return "instance";
                }
            }
            return "unknown";
        }

        // Autovervollständigungs-Vorschläge basierend auf Präfix abrufen
        public List<string> GetSuggestions(string prefix)
        {
            return suggestions
                .Where(s => s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();
        }
    }
}