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
            "class", "const", "func", "if", "else", "return", "var", "void",
            
            // Datentypen
            "float", "int", "string",

            //Objektorientierte Begriffe
            "instance", "prototype",

            //Boolesche Werte
            "TRUE", "FALSE",

            // Bezeichner
            "self", "slf", "other", "hero", "c_npc",
    
            // Membervariablen
            "aivar", "attribute", "condition", "daily_routine", "description", "fight_tactic", "flags",
            "guild", "id", "important", "information", "level", "name", "npc",
            "npctype", "nr", "permanent", "voice",

            // B-Funktionen
            "B_AddFightSkill", "B_RaiseFightTalent", "B_SetFightSkills", "B_RaiseAttribute",
            "B_LogEntry", "B_GetDayPlus", "B_BlessAttribute", "B_StartOtherRoutine",
            "B_KillNpc", "B_RemoveNpc", "B_UseItem", "B_GivePlayerXP", "B_RufDiebesgilde",
            "B_GetLearnCostTalent", "B_GetLearnCostAttribute", "B_BuildLearnString",
            "B_TeachAttributePoints", "B_TeachFightTalentPercent", "B_TeachMagicCircle",
            "B_TeachPlayerTalentAlchemy", "B_TeachPlayerTalentForeignLanguage",
            "B_TeachPlayerTalentWispDetector", "B_TeachPlayerTalentRunes",
            "B_TeachPlayerTalentSmith", "B_TeachPlayerTalentTakeAnimalTrophy",
            "B_TeachThiefTalent", "B_UseFakeScroll", "B_InitGuildAttitudes",
            "B_NPC_IsAliveCheck", "B_ENTER_ADDONWORLD", "B_ENTER_MYRTANA",
            "B_KapitelWechsel", "B_ClearDeadTrader", "B_ENDPRODUCTIONDIALOG",
            "B_SCObsession", "B_Extro_Avi", "B_LieselMaeh", "B_PedroKill",
            "B_Beerdigungsvorbereitung", "B_FadeFunc", "B_AbteiKuecheFegen",
            "B_IrrlichtBeep", "B_Teleports", "B_Upgrade_Hero_HackChance",
            "B_ScHasBeliarsWeapon", "B_BeliarsWeaponSpecialDamage",
            "B_PlayerFindItem", "B_GiveThiefXP", "B_Beklauen", "B_GiveDeathInv",
            "B_Addon_Myxir_TeachRequest", "B_Addon_Riordian_TeachWisp",
            "B_Announce_Herold", "B_Preach_Vatras", "B_AssignAmbientNEWS",
            "B_AssignToughGuyNEWS", "B_AssignCityGuide", "B_AssignAmbientInfos",
            "B_Addon_GivePotion", "B_CloseTopic", "B_Checklog", "B_ClearRuneInv",
            "B_ClearJunkTradeInv", "B_GiveTradeInv", "B_CreateAmbientInv",
            "B_GiveNpcTalents", "B_SetAttributesToChapter", "B_SetNpcVisual","B_Say",
            "B_Say_Overlay", "B_LookAtNpc", "B_ResetAll", "B_ValidateOther",
            "B_Say_Gold", "B_CommentFakeGuild", "B_Say_AttackEnd", "B_Say_AttackReason",
            "B_Say_GuildGreetings", "B_Say_Smalltalk", "B_Say_FleeReason", "B_SetAttitude",
            "B_GetTotalPetzCounter", "B_GetCurrentAbsolutionLevel", "B_GetGreatestPetzCrime",
            "B_GrantAbsolution", "B_GetPlayerCrime", "B_DeletePetzCrime", "B_AddPetzCrime",
            "B_MemorizePlayerCrime", "B_Attack", "B_CallGuards", "B_CreateAmmo",
            "B_FinishingMove", "B_SelectWeapon", "B_AssessEnemy", "B_AssessDamage",
            "B_AssessDrawWeapon", "B_AssessEnterRoom", "B_AssessFightSound", "B_AssessMurder",
            "B_AssessObserveSuspect", "B_AssessQuietSound", "B_AssessTalk", "B_AssessTheft",
            "B_AssessThreat", "B_AssessUseMob", "B_AssessWarn", "B_AssessPlayer",
            "B_MoveMob", "B_RemoveWeapon", "B_SetMonsterAttitude", "B_InitMonsterAttitudes",
            "B_MM_AssessBody", "B_MM_AssessDamage", "B_MM_AssessEnemy", "B_MM_AssessOthersDamage",
            "B_MM_AssessPlayer", "B_MM_AssessWarn", "B_MM_WispDetect", "B_MM_DeSynchronize",
            "B_RefreshAtInsert", "B_AssessMagic", "B_ReadySpell", "B_SelectSpell", "B_GiveInvItems",
            "B_WISPDETECTOR_LearnEffect", "B_SetKDFRunes", "B_SetHeroExp", "B_SetHeroWeapon",
            "B_SetHeroEquipment",
    
            // Npc-Funktionen
            "Npc_AreWeStronger", "Npc_CanSeeItem", "Npc_CanSeeNpc", "Npc_CanSeeNpcFreeLOS",
            "Npc_CanSeeSource", "Npc_ChangeAttribute", "Npc_CheckAvailableMission",
            "Npc_CheckInfo", "Npc_CheckOfferMission", "Npc_CheckRunningMission",
            "Npc_ClearAIQueue", "Npc_ClearInventory", "Npc_CreateSpell", "Npc_DeleteNews",
            "Npc_ExchangeRoutine", "Npc_GetActiveSpell", "Npc_GetActiveSpellCat",
            "Npc_GetActiveSpellIsScroll", "Npc_GetActiveSpellLevel", "Npc_GetAttitude",
            "Npc_GetBodyState", "Npc_GetComrades", "Npc_GetDetectedMob", "Npc_GetDistToItem",
            "Npc_GetDistToNpc", "Npc_GetDistToPlayer", "Npc_GetDistToWP", "Npc_GetEquippedArmor",
            "Npc_GetEquippedMeleeWeapon", "Npc_GetEquippedRangedWeapon", "Npc_GetGuildAttitude",
            "Npc_GetHeightToItem", "Npc_GetHeightToNpc", "Npc_GetInvItem", "Npc_GetInvItemBySlot",
            "Npc_GetLastHitSpellCat", "Npc_GetLastHitSpellID", "Npc_GetLookAtTarget",
            "Npc_GetNearestWP", "Npc_GetNewsOffender", "Npc_GetNewsVictim", "Npc_GetNewsWitness",
            "Npc_GetNextTarget", "Npc_GetNextWP", "Npc_GetPermAttitude", "Npc_GetPortalGuild",
            "Npc_GetPortalOwner", "Npc_GetReadiedWeapon", "Npc_GetStateTime", "Npc_GetTalentSkill",
            "Npc_GetTalentValue", "Npc_GetTarget", "Npc_GetTrueGuild", "Npc_GiveInfo",
            "Npc_GiveItem", "Npc_HasBodyFlag", "Npc_HasDetectedNpc", "Npc_HasEquippedArmor",
            "Npc_HasEquippedMeleeWeapon", "Npc_HasEquippedRangedWeapon", "Npc_HasEquippedWeapon",
            "Npc_HasFightTalent", "Npc_HasItems", "Npc_HasNews", "Npc_HasOffered",
            "Npc_HasRangedWeaponWithAmmo", "Npc_HasReadiedMeleeWeapon", "Npc_HasReadiedRangedWeapon",
            "Npc_HasReadiedWeapon", "Npc_HasSpell", "Npc_HasTalent", "Npc_IsAiming", "Npc_IsDead",
            "Npc_IsDetectedMobOwnedByGuild", "Npc_IsDetectedMobOwnedByNpc", "Npc_IsDrawingSpell",
            "Npc_IsDrawingWeapon", "Npc_IsInCutscene", "Npc_IsInFightMode", "Npc_IsInPlayersRoom",
            "Npc_IsInRoutine", "Npc_IsInState", "Npc_IsNear", "Npc_IsNewsGossip",
            "Npc_IsNextTargetAvailable", "Npc_IsOnFP", "Npc_IsPlayer", "Npc_IsPlayerInMyRoom",
            "Npc_IsVoiceActive", "Npc_IsWayBlocked", "Npc_KnowsInfo", "Npc_KnowsPlayer",
            "Npc_LearnSpell", "Npc_MemoryEntry", "Npc_MemoryEntryGuild", "Npc_OwnedByGuild",
            "Npc_OwnedByNpc", "Npc_PercDisable", "Npc_PercEnable", "Npc_PerceiveAll", "Npc_PlayAni",
            "Npc_RefuseTalk", "Npc_RemoveInvItem", "Npc_RemoveInvItems", "Npc_SendPassivePerc",
            "Npc_SendSinglePerc", "Npc_SetActiveSpellInfo", "Npc_SetAttitude", "Npc_SetKnowsPlayer",
            "Npc_SetTalentValue", "Npc_SetTarget", "Npc_SetTeleportPos", "Npc_SetTempAttitude",
            "Npc_SetToFightMode", "Npc_SetToFistMode", "Npc_SetTrueGuild",

            // AI-Funktionen
            "AI_AimAt", "AI_Attack", "AI_AlignToFP", "AI_AlignToWP", "AI_Ask", "AI_AskText",
            "AI_CanSeeNpc", "AI_ContinueRoutine", "AI_Defend", "AI_Dodge", "AI_DrawWeapon",
            "AI_DropItem", "AI_DropMob", "AI_EquipArmor", "AI_EquipBestArmor",
            "AI_EquipBestMeleeWeapon", "AI_EquipBestRangedWeapon", "AI_FinishingMove",
            "AI_Flee", "AI_GotoFP", "AI_GotoItem", "AI_GotoNextFP", "AI_GotoNpc",
            "AI_GotoSound", "AI_GotoWP", "AI_LookAt", "AI_LookAtNpc", "AI_LookForItem",
            "AI_Output", "AI_OutputSVM", "AI_OutputSVM_Overlay", "AI_PlayAni", "AI_PlayAniBS",
            "AI_PlayCutscene", "AI_PlayFX", "AI_PointAt", "AI_PointAtNpc", "AI_PrintScreen",
            "AI_ProcessInfos", "AI_QuickLook", "AI_Quicklook", "AI_ReadyMeleeWeapon",
            "AI_ReadyRangedWeapon", "AI_ReadySpell", "AI_RemoveWeapon", "AI_SetNpcsToState",
            "AI_SetWalkMode", "AI_SetWalkmode", "AI_ShootAt", "AI_Snd_Play", "AI_Snd_Play3D",
            "AI_StandUp", "AI_StandUpQuick", "AI_StartState", "AI_StopAim", "AI_StopFX",
            "AI_StopLookAt", "AI_StopPointAt", "AI_StopProcessInfos", "AI_TakeItem",
            "AI_TakeMob", "AI_Teleport", "AI_TurnAway", "AI_TurnToNpc", "AI_TurnToSound",
            "AI_UnequipArmor", "AI_UnequipWeapons", "AI_UnreadySpell", "AI_UseItem",
            "AI_UseItemToState", "AI_UseMob", "AI_Wait", "AI_WaitForQuestion", "AI_WaitMS",
            "AI_WaitTillEnd", "AI_WhirlAround", "AI_WhirlAroundToSource",

            // TA_-Funktionen
            "TA_Announce_Herold", "TA_Cook_Cauldron", "TA_Cook_Pan", "TA_Dance", "TA_FleeToWp",
            "TA_Follow_Player", "TA_Guard_Passage", "TA_Guide_Player", "TA_Pee", "TA_Pick_FP",
            "TA_Pick_Ore", "TA_Play_Lute", "TA_Potion_Alchemy", "TA_Practice_Magic",
            "TA_Practice_Sword", "TA_Pray_Sleeper_FP", "TA_Pray_Sleeper", "TA_Pray_Innos_FP",
            "TA_Pray_Innos", "TA_Preach_Vatras", "TA_Read_Bookstand", "TA_Repair_Hut",
            "TA_Roast_Scavenger", "TA_RunToWP", "TA_Sit_Bench", "TA_Sit_Campfire", "TA_Sit_Chair",
            "TA_Sit_Throne", "TA_Sleep", "TA_Smalltalk", "TA_Smith_Anvil", "TA_Smith_Cool",
            "TA_Smith_Fire", "TA_Smith_Sharp", "TA_Smoke_Joint", "TA_Smoke_Waterpipe",
            "TA_Spit_Fire", "TA_Stand_ArmsCrossed", "TA_Stand_Drinking", "TA_Stand_Eating",
            "TA_Stand_Guarding", "TA_Stand_Sweeping", "TA_Stand_WP", "TA_Stomp_Herb",
            "TA_Sweep_FP", "TA_Wash_FP", "TA_Rake_FP", "TA_Cook_Stove", "TA_Saw", "TA_Circle",
            "TA_Stand_Dementor", "TA_Guard_Hammer", "TA_Study_WP", "TA_Concert", "TA_Sleep_Deep",
            "TA_RangerMeeting", "TA_Ghost", "TA_GhostWusel", "TA_BeginOverlay", "TA_CS",
            "TA_EndOverlay", "TA_Min", "TA_RemoveOverlay",

            // Wld-Funktionen
            "Wld_AssignRoomToGuild", "Wld_AssignRoomToNpc", "Wld_DetectItem", "Wld_DetectNpc",
            "Wld_DetectNpcEx", "Wld_DetectNpcExAtt", "Wld_DetectPlayer",
            "Wld_ExchangeGuildAttitudes", "Wld_GetDay", "Wld_GetFormerPlayerPortalGuild",
            "Wld_GetFormerPlayerPortalOwner", "Wld_GetGuildAttitude", "Wld_GetMobState",
            "Wld_GetPlayerPortalGuild", "Wld_GetPlayerPortalOwner", "Wld_InsertItem",
            "Wld_InsertNpc", "Wld_InsertNpcAndRespawn", "Wld_InsertObject", "Wld_IsFPAvailable",
            "Wld_IsFpAvailable", "Wld_IsMobAvailable", "Wld_IsNextFPAvailable",
            "Wld_IsNextFpAvailable", "Wld_IsRaining", "Wld_IsTime", "Wld_PlayEffect",
            "Wld_RemoveItem", "Wld_RemoveNpc", "Wld_SendTrigger", "Wld_SendUntrigger",
            "Wld_SetGuildAttitude", "Wld_SetMobRoutine", "Wld_SetObjectRoutine", "Wld_SetTime",
            "Wld_SpawnNpcRange",

            // Mdl-Funktionen
            "Mdl_ApplyOverlayMds", "Mdl_ApplyOverlayMdsTimed", "Mdl_ApplyRandomAni",
            "Mdl_ApplyRandomAniFreq", "Mdl_ApplyRandomFaceAni", "Mdl_RemoveOverlayMds",
            "Mdl_SetModelFatness", "Mdl_SetModelScale", "Mdl_SetVisual", "Mdl_SetVisualBody",
            "Mdl_StartFaceAni",

            // Hlp-Funktionen
            "Hlp_CutscenePlayed", "Hlp_GetInstanceID", "Hlp_GetNpc", "Hlp_IsItem",
            "Hlp_IsValidItem", "Hlp_IsValidNpc", "Hlp_Random", "Hlp_StrCmp",

            // Print-Funktionen
            "Print", "PrintDebug", "PrintDebugCh", "PrintDebugInst", "PrintDebugInstCh",
            "PrintDialog", "PrintMulti", "PrintScreen",

            // Snd_-Funktionen
            "Snd_GetDistToSource", "Snd_IsSourceItem", "Snd_IsSourceNpc", "Snd_Play",
            "Snd_Play3D",

            // Mis-Funktionen
            "Mis_AddMissionEntry", "Mis_GetStatus", "Mis_OnTime", "Mis_RemoveMission",
            "Mis_SetStatus",

            // Log-Funktionen
             "Log_AddEntry", "Log_CreateTopic", "Log_SetTopicStatus",

            // Info-Funktionen
            "Info_AddChoice", "Info_ClearChoices",

            // Create-Funktionen
            "CreateInvItem", "CreateInvItems",

            // Mob-Funktionen
            "Mob_CreateItems", "Mob_HasItems",

            // Sonstige Funktionen
            "EquipItem", "ExitGame", "ExitSession", "FloatToInt", "FloatToString",
            "IntToFloat", "IntToString", "Game_InitEnglish", "Game_InitGerman",
            "InfoManager_HasFinished", "IntroduceChapter", "Perc_SetRange",
            "PlayVideo", "PlayVideoEx", "Rtn_Exchange", "SetPercentDone",
            "Tal_Configure", "Update_ChoiceBox",

            // FLAG-Konsten
            "NPC_FLAG_FRIEND","NPC_FLAG_IMMORTAL", "NPC_FLAG_GHOST",

            // NPC_TALENT-Konstanten
            "NPC_TALENT_1H", "NPC_TALENT_2H", "NPC_TALENT_BOW", "NPC_TALENT_CROSSBOW",
            "NPC_TALENT_PICKLOCK", "NPC_TALENT_MAGE", "NPC_TALENT_SNEAK",
            "NPC_TALENT_ACROBAT", "NPC_TALENT_PICKPOCKET", "NPC_TALENT_SMITH",
            "NPC_TALENT_RUNES", "NPC_TALENT_ALCHEMY", "NPC_TALENT_TAKEANIMALTROPHY",
            "NPC_TALENT_FOREIGNLANGUAGE", "NPC_TALENT_WISPDETECTOR",

            // NPCTYPE-Konstanten
            "NPCTYPE_AMBIENT", "NPCTYPE_MAIN", "NPCTYPE_FRIEND", "NPCTYPE_OCAMBIENT",
            "NPCTYPE_OCMAIN", "NPCTYPE_BL_AMBIENT", "NPCTYPE_TAL_AMBIENT", "NPCTYPE_BL_MAIN",

            // AIV-Konstanten
            "AIV_LastFightAgainstPlayer", "AIV_NpcSawPlayerCommit",
            "AIV_NpcSawPlayerCommitDay", "AIV_NpcStartedTalk", "AIV_INVINCIBLE",
            "AIV_TalkedToPlayer", "AIV_PlayerHasPickedMyPocket", "AIV_LASTTARGET",
            "AIV_PursuitEnd", "AIV_ATTACKREASON", "AIV_RANSACKED", "AIV_DeathInvGiven",
            "AIV_Guardpassage_Status", "AIV_LastDistToWP", "AIV_PASSGATE", "AIV_PARTYMEMBER",
            "AIV_VictoryXPGiven", "AIV_Gender", "AIV_Food", "AIV_TAPOSITION",
            "AIV_SelectSpell", "AIV_SeenLeftRoom", "AIV_HitByOtherNpc", "AIV_WaitBeforeAttack",
            "AIV_LastAbsolutionLevel", "AIV_ToughGuyNewsOverride", "AIV_MM_ThreatenBeforeAttack",
            "AIV_MM_FollowTime", "AIV_MM_FollowInWater", "AIV_MM_PRIORITY", "AIV_MM_SleepStart",
            "AIV_MM_SleepEnd", "AIV_MM_RestStart", "AIV_MM_RestEnd", "AIV_MM_RoamStart",
            "AIV_MM_RoamEnd", "AIV_MM_EatGroundStart", "AIV_MM_EatGroundEnd", "AIV_MM_WuselStart",
            "AIV_MM_WuselEnd", "AIV_MM_OrcSitStart", "AIV_MM_OrcSitEnd", "AIV_MM_ShrinkState",
            "AIV_MM_REAL_ID", "AIV_LASTBODY", "AIV_ArenaFight", "AIV_CrimeAbsolutionLevel",
            "AIV_LastPlayerAR", "AIV_DuelLost", "AIV_ChapterInv", "AIV_MM_Packhunter",
            "AIV_MagicUser", "AIV_DropDeadAndKill", "AIV_FreezeStateTime", "AIV_IGNORE_Murder",
            "AIV_IGNORE_Theft", "AIV_IGNORE_Sheepkiller", "AIV_ToughGuy", "AIV_NewsOverride",
            "AIV_MaxDistToWp", "AIV_OriginalFightTactic", "AIV_EnemyOverride", "AIV_SummonTime",
            "AIV_FightDistCancel", "AIV_LastFightComment", "AIV_LOADGAME", "AIV_DefeatedByPlayer",
            "AIV_KilledByPlayer", "AIV_StateTime", "AIV_Dist", "AIV_IgnoresFakeGuild",
            "AIV_NoFightParker", "AIV_NPCIsRanger", "AIV_IgnoresArmor", "AIV_StoryBandit",
            "AIV_StoryBandit_Esteban", "AIV_WhirlwindStateTime", "AIV_InflateStateTime",
            "AIV_SwarmStateTime", "AIV_SuckEnergyStateTime", "AIV_FollowDist", "AIV_SpellLevel",

            // REAL-Konstanten
            "REAL_STRENGTH", "REAL_DEXTERITY", "REAL_MANA_MAX", "REAL_TALENT_1H", "REAL_TALENT_2H",
            "REAL_TALENT_BOW", "REAL_TALENT_CROSSBOW",
    
            // FAI-Konstanten
            "FAI_HUMAN_COWARD", "FAI_HUMAN_NORMAL", "FAI_HUMAN_STRONG", "FAI_HUMAN_MASTER",
            "FAI_MONSTER_COWARD", "FAI_NAILED", "FAI_GOBBO", "FAI_SCAVENGER",
            "FAI_GIANT_RAT", "FAI_GIANT_BUG", "FAI_BLOODFLY", "FAI_WARAN",
            "FAI_WOLF", "FAI_MINECRAWLER", "FAI_LURKER", "FAI_ZOMBIE",
            "FAI_SNAPPER", "FAI_SHADOWBEAST", "FAI_HARPY", "FAI_STONEGOLEM",
            "FAI_DEMON", "FAI_TROLL", "FAI_SWAMPSHARK", "FAI_DRAGON",
            "FAI_MOLERAT", "FAI_ORC", "FAI_DRACONIAN", "FAI_Alligator",
            "FAI_Gargoyle", "FAI_Bear", "FAI_Stoneguardian",

            // CRIME-Konstanten
            "CRIME_NONE", "CRIME_SHEEPKILLER", "CRIME_ATTACK", "CRIME_THEFT", "CRIME_MURDER",

            // GIL-Konstanten
            "GIL_NONE", "GIL_HUMAN", "GIL_PAL", "GIL_MIL", "GIL_VLK", "GIL_KDF", "GIL_NOV","GIL_DJG", "GIL_SLD",
            "GIL_BAU", "GIL_BDT", "GIL_STRF", "GIL_DMT", "GIL_OUT", "GIL_PIR", "GIL_KDW", "GIL_PUBLIC",
            
            "GIL_MEATBUG", "GIL_SHEEP", "GIL_GOBBO", "GIL_GOBBO_SKELETON", "GIL_SUMMONED_GOBBO_SKELETON",
            "GIL_SCAVENGER", "GIL_GIANT_RAT", "GIL_GIANT_BUG", "GIL_BLOODFLY", "GIL_WARAN", "GIL_WOLF",
            "GIL_SUMMONED_WOLF", "GIL_MINECRAWLER", "GIL_LURKER", "GIL_SKELETON", "GIL_SUMMONED_SKELETON",
            "GIL_SKELETON_MAGE", "GIL_ZOMBIE", "GIL_SNAPPER", "GIL_SHADOWBEAST", "GIL_SHADOWBEAST_SKELETON",
            "GIL_HARPY", "GIL_STONEGOLEM", "GIL_FIREGOLEM", "GIL_ICEGOLEM", "GIL_SUMMONED_GOLEM", "GIL_DEMON",
            "GIL_SUMMONED_DEMON", "GIL_TROLL", "GIL_SWAMPSHARK", "GIL_DRAGON", "GIL_MOLERAT", "GIL_ALLIGATOR",
            "GIL_SWAMPGOLEM", "GIL_Stoneguardian", "GIL_Gargoyle", "GIL_SummonedGuardian", "GIL_SummonedZombie",

            "GIL_ORC", "GIL_FRIENDLY_ORC", "GIL_UNDEADORC", "GIL_DRACONIAN",
    
            // ATR-Konstanten
            "ATR_HITPOINTS", "ATR_HITPOINTS_MAX", "ATR_MANA", "ATR_MANA_MAX", "ATR_STRENGTH", "ATR_DEXTERITY",
            "ATR_REGENERATEHP", "ATR_REGENERATEMANA",
            
            // PETZCOUNTER-Konstanten
            "PETZCOUNTER_OldCamp_Murder", "PETZCOUNTER_OldCamp_Theft", "PETZCOUNTER_OldCamp_Attack",
            "PETZCOUNTER_OldCamp_Sheepkiller", "PETZCOUNTER_City_Murder", "PETZCOUNTER_City_Theft",
            "PETZCOUNTER_City_Attack", "PETZCOUNTER_City_Sheepkiller", "PETZCOUNTER_Monastery_Murder",
            "PETZCOUNTER_Monastery_Theft", "PETZCOUNTER_Monastery_Attack", "PETZCOUNTER_Monastery_Sheepkiller",
            "PETZCOUNTER_Farm_Murder", "PETZCOUNTER_Farm_Theft", "PETZCOUNTER_Farm_Attack",
            "PETZCOUNTER_Farm_Sheepkiller", "PETZCOUNTER_BL_Murder", "PETZCOUNTER_BL_Theft",
            "PETZCOUNTER_BL_Attack",
    
            // PLAYER_TALENT-Konstanten
            "PLAYER_TALENT_ALCHEMY", "PLAYER_TALENT_TAKEANIMALTROPHY", "PLAYER_TALENT_RUNES",
            "PLAYER_TALENT_FOREIGNLANGUAGE", "PLAYER_TALENT_SMITH", "PLAYER_TALENT_WISPDETECTOR",
    
            // Gender-Konstanten
            "MALE", "FEMALE",
    
            // AR-Konstanten
            "AR_NONE", "AR_LeftPortalRoom", "AR_ClearRoom", "AR_GuardCalledToRoom",
            "AR_MonsterVsHuman", "AR_MonsterMurderedHuman", "AR_SheepKiller", "AR_Theft",
            "AR_UseMob", "AR_GuardCalledToThief", "AR_ReactToWeapon", "AR_ReactToDamage",
            "AR_GuardStopsFight", "AR_GuardCalledToKill", "AR_GuildEnemy", "AR_HumanMurderedHuman",
            "AR_MonsterCloseToGate", "AR_GuardStopsIntruder", "AR_SuddenEnemyInferno", "AR_KILL",

            // FIGHT-Konstante
            "FIGHT_NONE", "FIGHT_LOST", "FIGHT_WON", "FIGHT_CANCEL",

            // GP-Konstanten
            "GP_NONE", "GP_FirstWarnGiven", "GP_SecondWarnGiven",

            // FOOD-Konstanten
            "FOOD_Apple", "FOOD_Cheese", "FOOD_Bacon", "FOOD_Bread",

            // POS-Konstanten
            "ISINPOS", "NOTINPOS", "NOTINPOS_WALK",

            // FOLLOWTIME-Konstanten
            "FOLLOWTIME_SHORT", "FOLLOWTIME_MEDIUM", "FOLLOWTIME_LONG",

            // PRIO-Konstanten
            "PRIO_EAT", "PRIO_ATTACK",

            // ID-Konstanten
            "ID_MEATBUG", "ID_SHEEP", "ID_GOBBO_GREEN", "ID_GOBBO_BLACK",
            "ID_GOBBO_SKELETON", "ID_SUMMONED_GOBBO_SKELETON", "ID_SCAVENGER", "ID_SCAVENGER_DEMON",
            "ID_GIANT_RAT", "ID_GIANT_BUG", "ID_BLOODFLY", "ID_WARAN", "ID_FIREWARAN", "ID_WOLF",
            "ID_WARG", "ID_SUMMONED_WOLF", "ID_MINECRAWLER", "ID_MINECRAWLERWARRIOR", "ID_LURKER",
            "ID_SKELETON", "ID_SUMMONED_SKELETON", "ID_SKELETON_MAGE", "ID_ZOMBIE", "ID_SNAPPER",
            "ID_DRAGONSNAPPER", "ID_SHADOWBEAST", "ID_SHADOWBEAST_SKELETON", "ID_HARPY",
            "ID_STONEGOLEM", "ID_FIREGOLEM", "ID_ICEGOLEM", "ID_SUMMONED_GOLEM", "ID_DEMON",
            "ID_SUMMONED_DEMON", "ID_DEMON_LORD", "ID_TROLL", "ID_TROLL_BLACK", "ID_SWAMPSHARK",
            "ID_DRAGON_FIRE", "ID_DRAGON_ICE", "ID_DRAGON_ROCK", "ID_DRAGON_SWAMP", "ID_DRAGON_UNDEAD",
            "ID_MOLERAT", "ID_ORCWARRIOR", "ID_ORCSHAMAN", "ID_ORCELITE", "ID_UNDEADORCWARRIOR",
            "ID_DRACONIAN", "ID_WISP", "ID_Alligator", "ID_Swampgolem", "ID_Stoneguardian",
            "ID_Gargoyle", "ID_Bloodhound", "ID_Icewolf", "ID_OrcBiter", "ID_Razor", "ID_Swarm",
            "ID_Swamprat", "ID_BLATTCRAWLER", "ID_SummonedGuardian", "ID_SummonedZombie", "ID_Keiler",
            "ID_SWAMPDRONE", "ID_ZIEGE", "ID_HUHN", "ID_Turtle", "ID_TESTGOBO", "ID_OGER",

            // AF-Konstanten
            "AF_NONE", "AF_RUNNING", "AF_AFTER", "AF_AFTER_PLUS_DAMAGE",

            // MAGIC-Konstanten
            "MAGIC_NEVER", "MAGIC_ALWAYS",

            // LOC-Konstanten
            "LOC_NONE", "LOC_OLDCAMP", "LOC_CITY", "LOC_MONASTERY", "LOC_FARM", "LOC_BL",
            "LOC_ALL",

            // Q-Konstanten
            "Q_KASERNE", "Q_GALGEN", "Q_MARKT", "Q_TEMPEL", "Q_UNTERSTADT", "Q_HAFEN",
            "Q_OBERSTADT",

            // LOG-Konstanten
            "LOG_RUNNING", "LOG_SUCCESS", "LOG_FAILED", "LOG_OBSOLETE", "LOG_MISSION", "LOG_NOTE",

            // Distanz-Konstanten
            "PERC_DIST_SUMMONED_ACTIVE_MAX", "PERC_DIST_MONSTER_ACTIVE_MAX", "PERC_DIST_ORC_ACTIVE_MAX",
            "PERC_DIST_DRAGON_ACTIVE_MAX", "FIGHT_DIST_MONSTER_ATTACKRANGE", "FIGHT_DIST_MONSTER_FLEE",
            "FIGHT_DIST_DRAGON_MAGIC", "MONSTER_THREATEN_TIME", "MONSTER_SUMMON_TIME", "TA_DIST_SELFWP_MAX",
            "PERC_DIST_ACTIVE_MAX", "PERC_DIST_INTERMEDIAT", "PERC_DIST_DIALOG", "PERC_DIST_HEIGHT",
            "PERC_DIST_INDOOR_HEIGHT", "FIGHT_DIST_MELEE", "FIGHT_DIST_RANGED_INNER", "FIGHT_DIST_RANGED_OUTER",
            "FIGHT_DIST_CANCEL", "WATCHFIGHT_DIST_MIN", "WATCHFIGHT_DIST_MAX", "ZivilAnquatschDist",
            "RANGED_CHANCE_MINDIST", "RANGED_CHANCE_MAXDIST",

            // Zeit-Konstanten
            "NPC_ANGRY_TIME", "HAI_TIME_UNCONSCIOUS", "NPC_TIME_FOLLOW",

            // Mindestschaden-Konstanten
            "NPC_MINIMAL_DAMAGE", "NPC_MINIMAL_PERCENT",

            // Allgemeine-Konstanten
            "LOOP_CONTINUE", "LOOP_END", "DEFAULT",

            // Spieler-Konstanten
            "LP_PER_LEVEL", "HP_PER_LEVEL", "XP_PER_VICTORY",

            // MOBSI-Konstanten
            "MOBSI_NONE", "MOBSI_SmithWeapon", "MOBSI_SleepAbit", "MOBSI_MakeRune",
            "MOBSI_PotionAlchemy", "MOBSI_PRAYSHRINE", "MOBSI_GOLDHACKEN", "MOBSI_PRAYIDOL",
            "MOBSI_ERZSCHMELZEN", "MOBSI_FEGEN1", "MOBSI_FEGEN2", "MOBSI_FEGEN3",
            "MOBSI_JAEGERTISCH", "MOBSI_BAUMSAEGEN", "MOBSI_AUFHAENGENPROLOG",
            "MOBSI_ALCHEMIE", "MOBSI_ESSE",

            // Weitere Konstanten
            "Npc_Default", "ZS_Talk",

            // FIGHT MODES
            "FMODE_NONE", "FMODE_FIST", "FMODE_MELEE", "FMODE_FAR", "FMODE_MAGIC", 

            // WALK MODES
            "NPC_RUN", "NPC_WALK", "NPC_SNEAK", "NPC_RUN_WEAPON", "NPC_WALK_WEAPON", "NPC_SNEAK_WEAPON",

            // ARMOR FLAGS
            "WEAR_TORSO", "WEAR_HEAD", "WEAR_EFFECT",

            // INVENTORY CATEGORIES
            "INV_WEAPON", "INV_ARMOR", "INV_RUNE", "INV_MAGIC", "INV_FOOD", "INV_POTION", "INV_DOC",
            "INV_MISC", "INV_CAT_MAX",

            // INVENTORY CAPACITIES
            "INV_MAX_WEAPONS", "INV_MAX_ARMORS", "INV_MAX_RUNES", "INV_MAX_FOOD", "INV_MAX_DOCS",
            "INV_MAX_POTIONS", "INV_MAX_MAGIC", "INV_MAX_MISC",

            // ITEM FLAGS
            "ITEM_KAT_NONE", "ITEM_KAT_NF", "ITEM_KAT_FF", "ITEM_KAT_MUN", "ITEM_KAT_ARMOR",
            "ITEM_KAT_FOOD", "ITEM_KAT_DOCS", "ITEM_KAT_POTIONS", "ITEM_KAT_LIGHT", "ITEM_KAT_RUNE",
            "ITEM_KAT_MAGIC", "ITEM_KAT_KEYS", "ITEM_DAG", "ITEM_SWD", "ITEM_AXE", "ITEM_2HD_SWD",
            "ITEM_2HD_AXE", "ITEM_SHIELD", "ITEM_BOW", "ITEM_CROSSBOW", "ITEM_RING", "ITEM_AMULET",
            "ITEM_BELT", "ITEM_DROPPED", "ITEM_MISSION", "ITEM_MULTI", "ITEM_NFOCUS", "ITEM_CREATEAMMO",
            "ITEM_NSPLIT", "ITEM_DRINK", "ITEM_TORCH", "ITEM_THROW", "ITEM_ACTIVE",

            // DAMAGE TYPES
            "DAM_INVALID", "DAM_BARRIER", "DAM_BLUNT", "DAM_EDGE", "DAM_FIRE", "DAM_FLY", "DAM_MAGIC",
            "DAM_POINT", "DAM_FALL", "DAM_INDEX_BARRIER", "DAM_INDEX_BLUNT", "DAM_INDEX_EDGE",
            "DAM_INDEX_FIRE", "DAM_INDEX_FLY", "DAM_INDEX_MAGIC", "DAM_INDEX_POINT", "DAM_INDEX_FALL",
            "DAM_INDEX_MAX",

            // OTHER DAMAGE CONSTANTS
            "NPC_ATTACK_FINISH_DISTANCE", "NPC_BURN_TICKS_PER_DAMAGE_POINT", "NPC_BURN_DAMAGE_POINTS_PER_INTERVALL",
            "DAM_CRITICAL_MULTIPLIER", "BLOOD_SIZE_DIVISOR", "BLOOD_DAMAGE_MAX", "DAMAGE_FLY_CM_MAX",
            "DAMAGE_FLY_CM_MIN", "DAMAGE_FLY_CM_PER_POINT", "NPC_DAM_DIVE_TIME", "IMMUNE",

            // PROTECTION TYPES
            "PROT_BARRIER", "PROT_BLUNT", "PROT_EDGE", "PROT_FIRE", "PROT_FLY", "PROT_MAGIC", "PROT_POINT",
            "PROT_FALL", "PROT_INDEX_MAX",

            // SENSES
            "SENSE_SEE", "SENSE_HEAR", "SENSE_SMELL",

            // PERCEPTIONS
            "PERC_ASSESSPLAYER", "PERC_ASSESSENEMY", "PERC_ASSESSFIGHTER", "PERC_ASSESSBODY",
            "PERC_ASSESSITEM", "PERC_ASSESSMURDER", "PERC_ASSESSDEFEAT", "PERC_ASSESSDAMAGE",
            "PERC_ASSESSOTHERSDAMAGE", "PERC_ASSESSTHREAT", "PERC_ASSESSREMOVEWEAPON",
            "PERC_OBSERVEINTRUDER", "PERC_ASSESSFIGHTSOUND", "PERC_ASSESSQUIETSOUND", "PERC_ASSESSWARN",
            "PERC_CATCHTHIEF", "PERC_ASSESSTHEFT", "PERC_ASSESSCALL", "PERC_ASSESSTALK",
            "PERC_ASSESSGIVENITEM", "PERC_ASSESSFAKEGUILD", "PERC_MOVEMOB", "PERC_MOVENPC",
            "PERC_DRAWWEAPON", "PERC_OBSERVESUSPECT", "PERC_NPCCOMMAND", "PERC_ASSESSMAGIC",
            "PERC_ASSESSSTOPMAGIC", "PERC_ASSESSCASTER", "PERC_ASSESSSURPRISE", "PERC_ASSESSENTERROOM",
            "PERC_ASSESSUSEMOB",

            // NEWS SPREAD MODE
            "NEWS_DONT_SPREAD", "NEWS_SPREAD_NPC_FRIENDLY_TOWARDS_VICTIM", 
            "NEWS_SPREAD_NPC_FRIENDLY_TOWARDS_WITNESS", "NEWS_SPREAD_NPC_FRIENDLY_TOWARDS_OFFENDER",
            "NEWS_SPREAD_NPC_SAME_GUILD_VICTIM",

            // INFO STATUS
            "INF_TELL", "INF_UNKNOWN",

            // ATTITUDES
            "ATT_FRIENDLY", "ATT_NEUTRAL", "ATT_ANGRY", "ATT_HOSTILE",

            // BodyTex
            "BodyTex_P", "BodyTex_N", "BodyTex_L", "BodyTex_B", "BodyTexBabe_P", "BodyTexBabe_N",
            "BodyTexBabe_L", "BodyTexBabe_B", "BodyTex_Player", "BodyTex_T", "BodyTexBabe_F",
            "BodyTexBabe_S",

            // Face
            "Face_N_Gomez", "Face_N_Scar", "Face_N_Raven", "Face_N_Bullit",
            "Face_B_Thorus", "Face_N_Corristo", "Face_N_Milten", "Face_N_Bloodwyn",
            "Face_L_Scatty", "Face_N_YBerion", "Face_N_CoolPock", "Face_B_CorAngar",
            "Face_B_Saturas", "Face_N_Xardas", "Face_N_Lares", "Face_L_Ratford",
            "Face_N_Drax", "Face_B_Gorn", "Face_N_Player", "Face_P_Lester",
            "Face_N_Lee", "Face_N_Torlof", "Face_N_Mud", "Face_N_Ricelord",
            "Face_N_Horatio", "Face_N_Richter", "Face_N_Cipher_neu", "Face_N_Homer",
            "Face_B_Cavalorn", "Face_L_Ian", "Face_L_Diego", "Face_N_MadPsi",
            "Face_N_Bartholo", "Face_N_Snaf", "Face_N_Mordrag", "Face_N_Lefty",
            "Face_N_Wolf", "Face_N_Fingers", "Face_N_Whistler", "Face_P_Gilbert",
            "Face_L_Jackal", "Face_P_ToughBald", "Face_P_Tough_Drago", "Face_P_Tough_Torrez",
            "Face_P_Tough_Rodriguez", "Face_P_ToughBald_Nek", "Face_P_NormalBald", "Face_P_Normal01",
            "Face_P_Normal02", "Face_P_Normal_Fletcher", "Face_P_Normal03", "Face_P_NormalBart01",
            "Face_P_NormalBart_Cronos", "Face_P_NormalBart_Nefarius", "Face_P_NormalBart_Riordian",
            "Face_P_OldMan_Gravo", "Face_P_Weak_Cutter", "Face_P_Weak_Ulf_Wohlers",
            "Face_N_Important_Arto", "Face_N_ImportantGrey", "Face_N_ImportantOld",
            "Face_N_Tough_Lee_ähnlich", "Face_N_Tough_Skip", "Face_N_ToughBart01", "Face_N_Tough_Okyl",
            "Face_N_Normal01", "Face_N_Normal_Cord", "Face_N_Normal_Olli_Kahn", "Face_N_Normal02",
            "Face_N_Normal_Spassvogel", "Face_N_Normal03", "Face_N_Normal04", "Face_N_Normal05",
            "Face_N_Normal_Stone", "Face_N_Normal06", "Face_N_Normal_Erpresser", "Face_N_Normal07",
            "Face_N_Normal_Blade", "Face_N_Normal08", "Face_N_Normal14", "Face_N_Normal_Sly",
            "Face_N_Normal16", "Face_N_Normal17", "Face_N_Normal18", "Face_N_Normal19",
            "Face_N_Normal20", "Face_N_NormalBart01", "Face_N_NormalBart02", "Face_N_NormalBart03",
            "Face_N_NormalBart04", "Face_N_NormalBart05", "Face_N_NormalBart06", "Face_N_NormalBart_Senyan",
            "Face_N_NormalBart08", "Face_N_NormalBart09", "Face_N_NormalBart10", "Face_N_NormalBart11",
            "Face_N_NormalBart12", "Face_N_NormalBart_Dexter", "Face_N_NormalBart_Graham",
            "Face_N_NormalBart_Dusty", "Face_N_NormalBart16", "Face_N_NormalBart17",
            "Face_N_NormalBart_Huno", "Face_N_NormalBart_Grim", "Face_N_NormalBart20",
            "Face_N_NormalBart21", "Face_N_NormalBart22", "Face_N_OldBald_Jeremiah",
            "Face_N_Weak_Ulbert", "Face_N_Weak_BaalNetbek", "Face_N_Weak_Herek", "Face_N_Weak04",
            "Face_N_Weak05", "Face_N_Weak_Orry", "Face_N_Weak_Asghan", "Face_N_Weak_Markus_Kark",
            "Face_N_Weak_Cipher_alt", "Face_N_NormalBart_Swiney", "Face_N_Weak12",
            "Face_L_ToughBald01", "Face_L_Tough01", "Face_L_Tough02", "Face_L_Tough_Santino",
            "Face_L_ToughBart_Quentin", "Face_L_Normal_GorNaBar", "Face_L_NormalBart01",
            "Face_L_NormalBart02", "Face_L_NormalBart_Rufus", "Face_B_ToughBald",
            "Face_B_Tough_Pacho", "Face_B_Tough_Silas", "Face_B_Normal01", "Face_B_Normal_Kirgo",
            "Face_B_Normal_Sharky", "Face_B_Normal_Orik", "Face_B_Normal_Kharim",
            "FaceBabe_N_BlackHair", "FaceBabe_N_Blondie", "FaceBabe_N_BlondTattoo", "FaceBabe_N_PinkHair",
            "FaceBabe_L_Charlotte", "FaceBabe_B_RedLocks", "FaceBabe_N_HairAndCloth",
            "FaceBabe_N_WhiteCloth", "FaceBabe_N_GreyCloth", "FaceBabe_N_Brown", "FaceBabe_N_VlkBlonde",
            "FaceBabe_N_BauBlonde", "FaceBabe_N_YoungBlonde", "FaceBabe_N_OldBlonde",
            "FaceBabe_P_MidBlonde", "FaceBabe_N_MidBauBlonde", "FaceBabe_N_OldBrown",
            "FaceBabe_N_Lilo", "FaceBabe_N_Hure", "FaceBabe_N_Anne", "FaceBabe_B_RedLocks2",
            "FaceBabe_L_Charlotte2", "Face_N_Fortuno", "Face_P_Greg", "Face_N_Pirat01",
            "Face_N_ZombieMud"


            // Item-Instanzen
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