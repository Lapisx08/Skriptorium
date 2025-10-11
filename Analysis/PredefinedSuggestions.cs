using System;
using System.Collections.Generic;

namespace Skriptorium.Analysis
{
    public static class PredefinedSuggestions
    {
        // Initialisierung der vordefinierten Schlüsselwörtern, Konstanten und Funktionen
        private static readonly HashSet<string> predefinedSuggestions = new(StringComparer.Ordinal)
        {
            // Klassen
            "C_NPC", "C_Mission", "C_Item", "C_Focus", "C_INFO", "C_ITEMREACT", "C_Spell",
            
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
            "Npc_Default", "ZS_Talk", "TIME_INFINITE", "NPC_VOICE_VARIATION_MAX",
            "TRADE_VALUE_MULTIPLIER", "RADE_CURRENCY_INSTANCE",

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

            // SOUND TYPES
            "NPC_SOUND_DROPTAKE", "NPC_SOUND_SPEAK", "NPC_SOUND_STEPS", "NPC_SOUND_THROWCOLL",
            "NPC_SOUND_DRAWWEAPON", "NPC_SOUND_SCREAM", "NPC_SOUND_FIGHT",

            // MATERIAL TYPES
            "MAT_WOOD", "MAT_STONE", "MAT_METAL", "MAT_LEATHER", "MAT_CLAY", "MAT_GLAS",

            // Kategorie-Konstanten
            "SPELL_GOOD", "SPELL_NEUTRAL", "SPELL_BAD",

            // Steuerungs-Konstanten
            "SPL_DONTINVEST", "SPL_RECEIVEINVEST", "SPL_SENDCAST", "SPL_SENDSTOP",
            "SPL_NEXTLEVEL", "SPL_STATUS_CANINVEST_NO_MANADEC", "SPL_FORCEINVEST",

            // Target-Konstanten
            "TARGET_COLLECT_NONE", "TARGET_COLLECT_CASTER", "TARGET_COLLECT_FOCUS",
            "TARGET_COLLECT_ALL", "TARGET_COLLECT_FOCUS_FALLBACK_NONE",
            "TARGET_COLLECT_FOCUS_FALLBACK_CASTER", "TARGET_COLLECT_ALL_FALLBACK_NONE",
            "TARGET_COLLECT_ALL_FALLBACK_CASTER", "TARGET_TYPE_ALL", "TARGET_TYPE_ITEMS",
            "TARGET_TYPE_NPCS", "TARGET_TYPE_ORCS", "TARGET_TYPE_HUMANS", "TARGET_TYPE_UNDEAD",

            // ID-Konstanten (Spells)
            "SPL_PalLight", "SPL_PalLightHeal", "SPL_PalHolyBolt", "SPL_PalMediumHeal", "SPL_PalRepelEvil",
            "SPL_PalFullHeal", "SPL_PalDestroyEvil", "SPL_PalTeleportSecret", "SPL_TeleportSeaport",
            "SPL_TeleportMonastery", "SPL_TeleportFarm", "SPL_TeleportXardas", "SPL_TeleportPassNW",
            "SPL_TeleportPassOW", "SPL_TeleportOC", "SPL_TeleportOWDemonTower", "SPL_TeleportTaverne",
            "SPL_Teleport_3", "SPL_Light", "SPL_Firebolt", "SPL_Icebolt", "SPL_LightHeal",
            "SPL_SummonGoblinSkeleton", "SPL_InstantFireball", "SPL_Zap", "SPL_SummonWolf", "SPL_WindFist",
            "SPL_Sleep", "SPL_MediumHeal", "SPL_LightningFlash", "SPL_ChargeFireball", "SPL_SummonSkeleton",
            "SPL_Fear", "SPL_IceCube", "SPL_ChargeZap", "SPL_SummonGolem", "SPL_DestroyUndead",
            "SPL_Pyrokinesis", "SPL_Firestorm", "SPL_IceWave", "SPL_SummonDemon", "SPL_FullHeal",
            "SPL_Firerain", "SPL_BreathOfDeath", "SPL_MassDeath", "SPL_ArmyOfDarkness", "SPL_Shrink",
            "SPL_TrfSheep", "SPL_TrfScavenger", "SPL_TrfGiantRat", "SPL_TrfGiantBug", "SPL_TrfWolf",
            "SPL_TrfWaran", "SPL_TrfSnapper", "SPL_TrfWarg", "SPL_TrfFireWaran", "SPL_TrfLurker",
            "SPL_TrfShadowbeast", "SPL_TrfDragonSnapper", "SPL_Charm", "SPL_MasterOfDisaster",
            "SPL_Deathbolt", "SPL_Deathball", "SPL_ConcussionBolt", "SPL_Thunderstorm", "SPL_Whirlwind",
            "SPL_WaterFist", "SPL_IceLance", "SPL_Inflate", "SPL_Geyser", "SPL_Waterwall", "SPL_Plague",
            "SPL_Swarm", "SPL_GreenTentacle", "SPL_Earthquake", "SPL_SummonGuardian", "SPL_Energyball",
            "SPL_SuckEnergy", "SPL_Skull", "SPL_SummonZombie", "SPL_SummonMud",

            // ForeignLanguage-TalentStufen
            "LANGUAGE_1", " LANGUAGE_2", "LANGUAGE_3", "MAX_LANGUAGE",

            // WispDetector-Talente
            "WISPSKILL_NF", "WISPSKILL_FF", "WISPSKILL_NONE", "WISPSKILL_RUNE", "WISPSKILL_MAGIC",
            "WISPSKILL_FOOD", "WISPSKILL_POTIONS", "MAX_WISPSKILL",

            "WispSearch_Follow", "WispSearch_ALL", "WispSearch_POTIONS", "WispSearch_MAGIC",
            "WispSearch_FOOD", "WispSearch_NF", "WispSearch_FF", "WispSearch_NONE", "WispSearch_RUNE",

            // Alchemie-Talente
            "POTION_Health_01", "POTION_Health_02", "POTION_Health_03", "POTION_Mana_01",
            "POTION_Mana_02", "POTION_Mana_03", "POTION_Speed", "POTION_Perm_STR", "POTION_Perm_DEX",
            "POTION_Perm_Mana", "POTION_Perm_Health", "POTION_MegaDrink", "POTION_Mana_04",
            "POTION_Health_04", "MAX_POTION",

            // Schmied-Talente
            "WEAPON_Common", "WEAPON_1H_Special_01", "WEAPON_2H_Special_01", "WEAPON_1H_Special_02",
            "WEAPON_2H_Special_02", "WEAPON_1H_Special_03", "WEAPON_2H_Special_03", "WEAPON_1H_Special_04",
            "WEAPON_2H_Special_04", "WEAPON_1H_Harad_01", "WEAPON_1H_Harad_02", "WEAPON_1H_Harad_03",
            "WEAPON_1H_Harad_04", "Recipe_1h_Shortsword", "MAX_WEAPONS",

            // AnimalTrophy-Talente
            "TROPHY_Teeth", "TROPHY_Claws", "TROPHY_Fur", "TROPHY_Heart", "TROPHY_ShadowHorn",
            "TROPHY_FireTongue", "TROPHY_BFWing", "TROPHY_BFSting", "TROPHY_Mandibles", "TROPHY_CrawlerPlate",
            "TROPHY_DrgSnapperHorn", "TROPHY_DragonScale", "TROPHY_DragonBlood", "TROPHY_ReptileSkin",
            "MAX_TROPHIES",

            // Font-Konstanten
            "TEXT_FONT_20", "TEXT_FONT_10", "TEXT_FONT_DEFAULT", "TEXT_FONT_Inventory",

            // Kamera für Inventory-Items
            "INVCAM_ENTF_RING_STANDARD", "INVCAM_ENTF_AMULETTE_STANDARD", "INVCAM_ENTF_MISC_STANDARD",
            "INVCAM_ENTF_MISC2_STANDARD", "INVCAM_ENTF_MISC3_STANDARD", "INVCAM_ENTF_MISC4_STANDARD",
            "INVCAM_ENTF_MISC5_STANDARD", "INVCAM_X_RING_STANDARD", "INVCAM_Z_RING_STANDARD",
            "INVCAM_X_Armor", "INVCAM_Z_Armor", "INVCAM_X_COIN_STANDARD", "INVCAM_Z_COIN_STANDARD",

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
            "Face_N_ZombieMud",

            // Amulette
            "ItAm_Prot_Fire_01", "ItAm_Prot_Edge_01", "ItAm_Prot_Point_01", "ItAm_Prot_Mage_01",
            "ItAm_Prot_Total_01", "ItAm_Dex_01", "ItAm_Strg_01", "ItAm_Hp_01", "ItAm_Mana_01", "ItAm_Dex_Strg_01",
            "ItAm_Hp_Mana_01",

            // Behälter
            "ItSe_ErzFisch", "ItSe_GoldFisch", "ItSe_Ringfisch", "ItSe_LockpickFisch", "ItSe_GoldPocket25",
            "ItSe_GoldPocket50", "ItSe_GoldPocket100", "ItSe_HannasBeutel",

            // Fackeln
            "ItLsTorch", "ItLsTorchburning", "ItLsTorchburned", "ItLsTorchFirespit",

            // Fernkampfwaffen
            "ItRw_Arrow", "ItRw_Bolt", "ItRw_Mil_Crossbow", "ItRw_Sld_Bow", "ItRw_Bow_L_01",
            "ItRw_Bow_L_02", "ItRw_Bow_L_03", "ItRw_Bow_L_04", "ItRw_Bow_M_01", "ItRw_Bow_M_02",
            "ItRw_Bow_M_03", "ItRw_Bow_M_04", "ItRw_Bow_H_01", "ItRw_Bow_H_02", "ItRw_Bow_H_03",
            "ItRw_Bow_H_04", "ItRw_Crossbow_L_01", "ItRw_Crossbow_L_02", "ItRw_Crossbow_M_01",
            "ItRw_Crossbow_M_02", "ItRw_Crossbow_H_01", "ItRw_Crossbow_H_02",

            // Missionitems Kapitel 1
            "ItKe_Xardas", "ItKe_Dexter", "ItKe_Storage", "ItKe_Hotel", "ItKe_ThiefGuildKey_MIS",
            "ItKe_ThiefGuildKey_Hotel_MIS", "ItKe_Innos_MIS", "ItKe_KlosterSchatz", "ItKe_KlosterStore",
            "ItKe_KDFPlayer", "ItKe_KlosterBibliothek", "ItKe_MagicChest", "ItKe_Bandit", "ItKe_EVT_CRYPT_01",
            "ItKe_EVT_CRYPT_02", "ItKe_EVT_CRYPT_03", "ItKe_Valentino", "ItKe_Buerger", "ItKe_Richter",
            "ItKe_Salandril", "ItKe_PaladinTruhe", "ItKe_ThiefTreasure", "ItKe_Fingers",
            "ItWr_Canthars_KomproBrief_MIS", "ItWr_Kraeuterliste", "ItWr_ManaRezept", "ItWr_Passierschein",
            "ItWr_HalvorMessage", "ItWr_VatrasMessage", "ItWr_VatrasMessage_Open", "ItWr_Passage_MIS",
            "ItWr_BanditLetter_MIS", "ItWr_Poster_MIS", "ItWr_Schuldenbuch", "ItMw_2h_Rod",
            "ItMw_AlriksSword_Mis", "ItMi_CoragonsSilber", "ItMi_TheklasPaket", "ItMi_MariasGoldPlate",
            "ItMi_HerbPaket", "ItMi_EddasStatue", "ItRi_ValentinosRing", "ItRi_Prot_Point_01_MIS",
            "ItFo_SmellyFish", "ItFo_HalvorFish_MIS", "ItFo_HalvorFish", "ItFo_Schafswurst",
            "ItPo_Perm_LittleMana", "ItPl_Sagitta_Herb_MIS", "ItRw_Bow_L_03_MIS", "ItRw_DragomirsArmbrust_MIS",
            "ITAR_PAL_SKEL", "ITKE_ORLAN_HOTELZIMMER",

            // Missionitems Kapitel 2
            "ItMi_StoneOfKnowlegde_MIS", "ItMi_GornsTreasure_MIS", "ItMi_GoldPlate_MIS", "ItWr_PaladinLetter_MIS",
            "ItWr_LetterForGorn_MIS", "ItWr_Silvestro_MIS", "ItWr_Bloody_MIS", "ItWr_Pfandbrief_MIS",
            "ItWr_Map_OldWorld_Oremines_MIS", "ItWr_Manowar", "ItWr_KDWLetter", "ItWr_GilbertLetter",
            "ItKe_PrisonKey_MIS", "ItKe_OC_Store", "ItKe_Pass_MIS", "ItKe_Bromor", "ItAt_ClawLeader",
            "ItSe_Olav", "ItRi_Tengron", "ITKE_ErzBaronFlur", "ITKE_ErzBaronRaum", "ITKE_RUNE_MIS",

            // Missionitems Kapitel 3
            "ItMi_InnosEye_MIS", "ItMi_InnosEye_Discharged_Mis", "ItMi_InnosEye_Broken_Mis",
            "ItMi_MalethsBanditGold", "ItMi_Moleratlubric_MIS", "ItMi_UltharsHolyWater_Mis",
            "ItMi_KarrasBlessedStone_Mis", "ItWr_PermissionToWearInnosEye_MIS",
            "ItWr_XardasBookForPyrokar_Mis", "ItWr_CorneliusTagebuch_Mis", "ItWr_PyrokarsObsessionList",
            "ItWr_BabosLetter_MIS", "ItWr_BabosPinUp_MIS", "ItWr_BabosDocs_MIS", "ItWr_Astronomy_Mis",
            "ItWr_ShatteredGolem_MIS", "ItWr_DiegosLetter_MIS", "ItWr_MinenAnteil_Mis",
            "ItWr_RichterKomproBrief_MIS", "ItWr_MorgahardTip", "ItWr_Map_Shrine_MIS",
            "ItWr_VinosKellergeister_Mis", "ItKe_CHEST_SEKOB_XARDASBOOK_MIS", "ItKe_IgarazChest_Mis",
            "ItPo_HealHilda_MIS", "ItPo_HealObsession_MIS", "ItSe_Golemchest_Mis", "ItSe_DiegosTreasure_Mis",
            "ItAm_Prot_BlackEye_Mis", "ItMw_MalethsGehstock_MIS", "ITWR_DementorObsessionBook_MIS",

            // Missionitems Kapitel 4
            "ItAm_Mana_Angar_MIS", "ItMW_1H_FerrosSword_Mis", "ItMi_KerolothsGeldbeutel_MIS",
            "ItMi_KerolothsGeldbeutelLeer_MIS", "ItRw_SengrathsArmbrust_MIS", "ItAt_TalbinsLurkerSkin",
            "ItAt_DragonEgg_MIS", "ItRi_OrcEliteRing", "ItPo_DragonEggDrinkNeoras_MIS", "ItWr_Map_Orcelite_MIS",
            "ItWr_Map_Caves_MIS",

            // Missionitems Kapitel 5
            "ItWr_XardasLetterToOpenBook_MIS", "ItWr_HallsofIrdorath_Mis", "ItWr_HallsofIrdorath_Open_Mis",
            "ItWr_XardasSeamapBook_Mis", "ItWr_UseLampIdiot_Mis", "ItWr_Seamap_Irdorath",
            "ITWr_ForgedShipLetter_MIS", "ItKe_MonastarySecretLibrary_Mis", "ITKE_OC_MAINGATE_MIS",
            "ITKE_SHIP_LEVELCHANGE_MIS", "ItPo_PotionOfDeath_01_Mis", "ItPo_PotionOfDeath_02_Mis",
            "ItAm_AmulettOfDeath_Mis", "ItPo_HealRandolph_MIS",

            // Missionitems Kapitel 6
            "ItSe_XardasNotfallBeutel_MIS", "ItWr_XardasErmahnungFuerIdioten_MIS", "ItWr_Krypta_Garon",
            "ItKe_OrkKnastDI_MIS", "ItKe_EVT_UNDEAD_01", "ItKe_EVT_UNDEAD_02", "ItKe_LastDoorToUndeadDrgDI_MIS",
            "ItWr_LastDoorToUndeadDrgDI_MIS", "ItKe_ChestMasterDementor_MIS", "ItWr_Rezept_MegaDrink_MIS",
            "ItWr_Diary_BlackNovice_MIS", "ItWr_ZugBruecke_MIS", "ItMi_PowerEye",

            // Nahkampfwaffen
            "ItMw_1h_Vlk_Dagger", "ItMw_1H_Mace_L_01", "ItMw_1h_Bau_Axe", "ItMw_1h_Vlk_Mace", "ItMw_1H_Mace_L_03",
            "ItMw_1h_Bau_Mace", "ItMw_1h_Vlk_Axe", "ItMw_1H_Mace_L_04", "ItMw_ShortSword1", "ItMw_Nagelknueppel",
            "ItMw_1H_Sword_L_03", "ItMw_ShortSword2", "ItMw_Sense", "ItMw_1h_Vlk_Sword", "ItMw_1h_Nov_Mace",
            "ItMw_2h_Bau_Axe", "ItMw_2H_Axe_L_01", "ItMw_1h_MISC_Sword", "ItMw_1h_Misc_Axe", "ItMw_2H_Sword_M_01",
            "ItMw_1h_Mil_Sword", "ItMw_1h_Sld_Axe", "ItMw_1h_Sld_Sword", "ItMw_2h_Sld_Axe", "ItMw_2h_Sld_Sword",
            "ItMw_1h_Pal_Sword", "ItMw_2h_Pal_Sword", "ItMw_2H_OrcAxe_01", "ItMw_2H_OrcAxe_02", "ItMw_2H_OrcAxe_03",
            "ItMw_2H_OrcAxe_04", "ItMw_2H_OrcSword_01", "ItMw_2H_OrcSword_02", "ItMw_ShortSword3",
            "ItMw_Nagelkeule", "ItMw_ShortSword4", "ItMw_Kriegskeule", "ItMw_Richtstab", "ItMw_ShortSword5",
            "ItMw_Kriegshammer1", "ItMw_Hellebarde", "ItMw_Nagelkeule2", "ItMw_Schiffsaxt", "ItMw_Piratensaebel",
            "ItMw_Schwert", "ItMw_1H_Common_01", "ItMw_Stabkeule", "ItMw_Zweihaender1", "ItMw_Steinbrecher",
            "ItMw_Spicker", "ItMw_Streitaxt1", "ItMw_Schwert1", "ItMw_Schwert2", "ItMw_Doppelaxt", "ItMw_Bartaxt",
            "ItMw_Morgenstern", "ItMw_Schwert3", "ItMw_Schwert4", "ItMw_1H_Special_01", "ItMw_2H_Special_01",
            "ItMw_Rapier", "ItMw_Rubinklinge", "ItMw_Streitkolben", "ItMw_Zweihaender2", "ItMw_Runenschwert",
            "ItMw_Rabenschnabel", "ItMw_Schwert5", "ItMw_Inquisitor", "ItMw_Streitaxt2", "ItMw_Zweihaender3",
            "ItMw_1H_Special_02", "ItMw_2H_Special_02", "ItMw_ElBastardo", "ItMw_Kriegshammer2", "ItMw_Meisterdegen",
            "ItMw_Folteraxt", "ItMw_Orkschlaechter", "ItMw_Zweihaender4", "ItMw_Schlachtaxt", "ItMw_Krummschwert",
            "ItMw_Barbarenstreitaxt", "ItMw_Sturmbringer", "ItMw_1H_Special_03", "ItMw_2H_Special_03",
            "ItMw_Berserkeraxt", "ItMw_Drachenschneide", "ItMw_1H_Special_04", "ItMw_2H_Special_04",
            "ItMw_1H_Blessed_01", "ItMw_1H_Blessed_02", "ItMw_1H_Blessed_03", "ItMw_2H_Blessed_01",
            "ItMw_2H_Blessed_02", "ItMw_2H_Blessed_03", "ItMw_1H_Sword_L_01", "ItMw_1H_Mace_L_02", "ItMw_1H_Axe_L_01",
            "ItMw_1H_Sword_L_02", "ItMw_1H_Axe_L_02", "ItMw_1H_Sword_L_04", "ItMw_1H_Axe_L_03", "ItMw_1H_Mace_L_05",
            "ItMw_1H_Sword_L_05", "ItMw_1H_Sword_L_06", "ItMw_1H_Axe_L_04", "ItMw_1H_Mace_L_06", "ItMw_1H_Sword_L_07",
            "ItMw_1H_Sword_L_08", "ItMw_1H_Axe_L_05", "ItMw_1H_Mace_L_07", "ItMw_1H_Sword_L_09", "ItMw_1H_Sword_L_10",
            "ItMw_1H_Axe_L_06", "ItMw_1H_Mace_L_08", "ItMw_1H_Mace_L_09", "ItMw_1H_Sword_M_01", "ItMw_1H_Axe_M_01",
            "ItMw_1H_Mace_M_01", "ItMw_1H_Mace_M_02", "ItMw_1H_Sword_M_02", "ItMw_1H_Axe_M_02", "ItMw_1H_Mace_M_03",
            "ItMw_1H_Mace_M_04", "ItMw_1H_Sword_M_03", "ItMw_1H_Axe_M_03", "ItMw_1H_Mace_M_05", "ItMw_1H_Mace_M_06",
            "ItMw_1H_Sword_M_04", "ItMw_1H_Axe_M_04", "ItMw_1H_Mace_M_07", "ItMw_1H_Sword_M_05", "ItMw_1H_Sword_H_01",
            "ItMw_1H_Axe_H_01", "ItMw_1H_Mace_H_01", "ItMw_1H_Sword_H_02", "ItMw_1H_Sword_H_03", "ItMw_1H_Axe_H_02",
            "ItMw_1H_Mace_H_02", "ItMw_1H_Sword_H_04", "ItMw_1H_Sword_H_05", "ItMw_1H_Axe_H_03", "ItMw_1H_Mace_H_03",
            "ItMw_2H_Axe_L_02", "ItMw_2H_Mace_L_01", "ItMw_2H_Sword_L_01", "ItMw_2H_Axe_L_03", "ItMw_2H_Mace_L_02",
            "ItMw_2H_Mace_L_03", "ItMw_2H_Sword_L_02", "ItMw_2H_Axe_L_04", "ItMw_2H_Mace_L_04", "ItMw_2H_Axe_M_01",
            "ItMw_2H_Mace_M_01", "ItMw_2H_Mace_M_02", "ItMw_2H_Sword_M_02", "ItMw_2H_Axe_M_02", "ItMw_2H_Mace_M_03",
            "ItMw_2H_Sword_M_03", "ItMw_2H_Axe_M_03", "ItMw_2H_Mace_M_04", "ItMw_2H_Sword_M_04", "ItMw_2H_Sword_M_05",
            "ItMw_2H_Axe_M_04", "ItMw_2H_Sword_M_06", "ItMw_2H_Sword_M_07", "ItMw_2H_Axe_M_05", "ItMw_2H_Mace_M_05",
            "ItMw_2H_Mace_M_06", "ItMw_2H_Sword_M_08", "ItMw_2H_Axe_M_06", "ItMw_2H_SWORD_M_09", "ItMw_2H_Sword_H_01",
            "ItMw_2H_Axe_H_01", "ItMw_2H_Sword_H_02", "ItMw_2H_Sword_H_03", "ItMw_2H_Axe_H_02", "ItMw_2H_Sword_H_04",
            "ItMw_2H_Sword_H_05", "ItMw_2H_Axe_H_03", "ItMw_2H_Sword_H_06", "ItMw_2H_Sword_H_07", "ItMw_2H_Axe_H_04",
            "ItMw_1H_Blessed_01", "ItMw_1H_Blessed_02", "ItMw_1H_Blessed_03", "ItMw_2H_Blessed_01", "ItMw_2H_Blessed_02",
            "ItMw_2H_Blessed_03",

            // Nahrung
            "ItFo_Apple", "ItFo_Cheese", "ItFo_Bacon", "ItFo_Bread", "ItFo_Fish", "ItFoMuttonRaw", "ItFoMutton",
            "ItFo_Stew", "ItFo_XPStew", "ItFo_CoragonsBeer", "ItFo_FishSoup", "ItFo_Sausage", "ItFo_Honey", "ItFo_Water",
            "ItFo_Beer", "ItFo_Booze", "ItFo_Wine", "ItFo_Milk",

            // Pflanzen
            "ItPl_Weed", "ItPl_Beet", "ItPl_SwampHerb", "ItPl_Mana_Herb_01", "ItPl_Mana_Herb_02", "ItPl_Mana_Herb_03",
            "ItPl_Health_Herb_01", "ItPl_Health_Herb_02", "ItPl_Health_Herb_03", "ItPl_Dex_Herb_01",
            "ItPl_Strength_Herb_01", "ItPl_Speed_Herb_01", "ItPl_Mushroom_01", "ItPl_Mushroom_02", "ItPl_Blueplant",
            "ItPl_Forestberry", "ItPl_Planeberry", "ItPl_Temp_Herb", "ItPl_Perm_Herb",

            // Ringe
            "ItRi_Prot_Fire_01", "ItRi_Prot_Fire_02", "ItRi_Prot_Point_01", "ItRi_Prot_Point_02", "ItRi_Prot_Edge_01",
            "ItRi_Prot_Edge_02", "ItRi_Prot_Mage_01", "ItRi_Prot_Mage_02", "ItRi_Prot_Total_01", "ItRi_Prot_Total_02",
            "ItRi_Dex_01", "ItRi_Dex_02", "ItRi_Hp_01", "ItRi_Hp_02", "ItRi_Str_01", "ItRi_Str_02", "ItRi_Mana_01",
            "ItRi_Mana_02", "ItRi_Hp_Mana_01", "ItRi_Dex_Strg_01",

            // Rüstungen
            "ITAR_Governor", "ITAR_JUDGE", "ITAR_SMITH", "ITAR_BARKEEPER", "ITAR_VLK_L", "ITAR_VLK_M", "ITAR_VLK_H",
            "ITAR_VlkBabe_L", "ITAR_VlkBabe_M", "ITAR_VlkBabe_H", "ITAR_MIL_L", "ITAR_MIL_M", "ITAR_PAL_M",
            "ITAR_PAL_H", "ITAR_BAU_L", "ITAR_BAU_M", "ITAR_BauBabe_L", "ITAR_BauBabe_M", "ITAR_SLD_L", "ITAR_SLD_M",
            "ITAR_SLD_H", "ITAR_DJG_Crawler", "ITAR_DJG_L", "ITAR_DJG_M", "ITAR_DJG_H", "ITAR_DJG_BABE", "ITAR_NOV_L",
            "ITAR_KDF_L", "ITAR_KDF_H", "ITAR_Leather_L", "ITAR_BDT_M", "ITAR_BDT_H", "ITAR_XARDAS", "ITAR_LESTER",
            "ITAR_Diego", "ITAR_CorAngar", "ITAR_Dementor", "ITAR_KDW_H", "ITAR_Prisoner",

            // Runen
            "ItRu_PalLight", "ItRu_PalLightHeal", "ItRu_PalMediumHeal", "ItRu_PalFullHeal", "ItRu_PalHolyBolt",
            "ItRu_PalRepelEvil", "ItRu_PalDestroyEvil", "ItRu_PalTeleportSecret", "ItRu_TeleportSeaport",
            "ItRu_TeleportMonastery", "ItRu_TeleportFarm", "ItRu_TeleportXardas", "ItRu_TeleportPassNW",
            "ItRu_TeleportPassOW", "ItRu_TeleportOC", "ItRu_TeleportOWDemonTower", "ItRu_TeleportTaverne", "ItRu_Light",
            "ItRu_FireBolt", "ItRu_Zap", "ItRu_LightHeal", "ItRu_SumGobSkel", "ItRu_InstantFireball", "ItRu_Icebolt",
            "ItRu_SumWolf", "ItRu_Windfist", "ItRu_Sleep", "ItRu_MediumHeal", "ItRu_LightningFlash",
            "ItRu_ChargeFireball", "ItRu_SumSkel", "ItRu_Fear", "ItRu_IceCube", "ItRu_ThunderBall", "ItRu_SumGol",
            "ItRu_HarmUndead", "ItRu_Pyrokinesis", "ItRu_Firestorm", "ItRu_IceWave", "ItRu_SumDemon", "ItRu_FullHeal",
            "ItRu_Firerain", "ItRu_BreathOfDeath", "ItRu_MassDeath", "ItRu_MasterOfDisaster", "ItRu_ArmyOfDarkness",
            "ItRu_Shrink", "ItRu_Deathbolt", "ItRu_Deathball", "ItRu_Concussionbolt",

            // Schlüssel
            "ItKe_Lockpick", "ItKe_Key_01", "ItKe_Key_02", "ItKe_Key_03", "ItKe_City_Tower_01", "ItKe_City_Tower_02",
            "ItKe_City_Tower_03", "ItKe_City_Tower_04", "ItKe_City_Tower_05", "ItKe_City_Tower_06",

            // Schriftrollen
            "ItSc_PalLight", "ItSc_PalLightHeal", "ItSc_PalHolyBolt", "ItSc_PalMediumHeal", "ItSc_PalRepelEvil",
            "ItSc_PalFullHeal", "ItSc_PalDestroyEvil", "ItSc_Light", "ItSc_Firebolt", "ItSc_Icebolt",
            "ItSc_LightHeal", "ItSc_SumGobSkel", "ItSc_InstantFireball", "ItSc_Zap", "ItSc_SumWolf", "ItSc_Windfist",
            "ItSc_Sleep", "ItSc_Charm", "ItSc_MediumHeal", "ItSc_LightningFlash", "ItSc_ChargeFireball",
            "ItSc_SumSkel", "ItSc_Fear", "ItSc_IceCube", "ItSc_ThunderBall", "ItSc_SumGol", "ItSc_HarmUndead",
            "ItSc_Pyrokinesis", "ItSc_Firestorm", "ItSc_IceWave", "ItSc_SumDemon", "ItSc_FullHeal", "ItSc_Firerain",
            "ItSc_BreathOfDeath", "ItSc_MassDeath", "ItSc_ArmyOfDarkness", "ItSc_Shrink", "ItSc_TrfSheep",
            "ItSc_TrfScavenger", "ItSc_TrfGiantRat", "ItSc_TrfGiantBug", "ItSc_TrfWolf", "ItSc_TrfWaran",
            "ItSc_TrfSnapper", "ItSc_TrfWarg", "ItSc_TrfFireWaran", "ItSc_TrfLurker", "ItSc_TrfShadowbeast",
            "ItSc_TrfDragonSnapper",

            // Schriftsachen
            "StandardBrief", "StandardBuch", "ItWr_Map_NewWorld", "ItWr_Map_NewWorld_City", "ItWr_Map_OldWorld",
            "ItWr_EinhandBuch", "ItWr_ZweihandBuch",

            // Tiertrophäen
            "ItAt_Addon_BCKopf", "ItAt_Meatbugflesh", "ItAt_SheepFur", "ItAt_WolfFur", "ItAt_BugMandibles",
            "ItAt_Claw", "ItAt_LurkerClaw", "ItAt_Teeth", "ItAt_CrawlerMandibles", "ItAt_Wing", "ItAt_Sting",
            "itat_LurkerSkin", "ItAt_WargFur", "ItAt_Addon_KeilerFur", "ItAt_DrgSnapperHorn", "ItAt_CrawlerPlate",
            "ItAt_ShadowFur", "ItAt_SharkSkin", "ItAt_TrollFur", "ItAt_TrollBlackFur", "ItAt_WaranFiretongue",
            "ItAt_ShadowHorn", "ItAt_SharkTeeth", "ItAt_TrollTooth", "ItAt_StoneGolemHeart", "ItAt_FireGolemHeart",
            "ItAt_IceGolemHeart", "ItAt_GoblinBone", "ItAt_SkeletonBone", "ItAt_DemonHeart", "ItAt_UndeadDragonSoulStone",
            "ItAt_IcedragonHeart", "ItAt_RockdragonHeart", "ItAt_SwampdragonHeart", "ItAt_FiredragonHeart",
            "ItAt_DragonBlood", "ItAt_DragonScale",

            // Tränke
            "ItPo_Mana_01", "ItPo_Mana_02", "ItPo_Mana_03", "ItPo_Health_01", "ItPo_Health_02", "ItPo_Health_03",
            "ItPo_Perm_STR", "ItPo_Perm_DEX", "ItPo_Perm_Health", "ItPo_Perm_Mana", "ItPo_Speed", "ItPo_MegaDrink",

            // Sonstiges
            "ItMi_Stomper", "ItMi_RuneBlank", "ItMi_Pliers", "ItMi_Flask", "ItMi_Hammer", "ItMi_Scoop", "ItMi_Pan",
            "ItMi_PanFull", "ItMi_Saw", "ItMiSwordraw", "ItMiSwordrawhot", "ItMiSwordbladehot", "ItMiSwordblade",
            "ItMi_Broom", "ItMi_Lute", "ItMi_Brush", "ItMi_Joint", "ItMi_Alarmhorn", "ItMi_Packet", "ItMi_Pocket",
            "ItMi_Nugget", "ItMi_Gold", "ItMi_OldCoin", "ItMi_GoldCandleHolder", "ItMi_GoldNecklace",
            "ItMi_SilverRing", "ItMi_SilverCup", "ItMi_SilverPlate", "ItMi_GoldPlate", "ItMi_GoldCup",
            "ItMi_BloodCup_MIS", "ItMi_GoldRing", "ItMi_SilverChalice", "ItMi_JeweleryChest", "ItMi_GoldChalice",
            "ItMi_GoldChest", "ItMi_InnosStatue", "ItMi_Sextant", "ItMi_SilverCandleHolder", "ItMi_SilverNecklace",
            "ItMi_Sulfur", "ItMi_Quartz", "ItMi_Pitch", "ItMi_Rockcrystal", "ItMi_Aquamarine", "ItMi_HolyWater",
            "ItMi_Coal", "ItMi_DarkPearl", "ItMi_ApfelTabak", "ItMi_PilzTabak", "ItMi_DoppelTabak",
            "ItMi_Honigtabak", "ItMi_SumpfTabak",

            // Amulette (Addon)
            "ItAm_Addon_Franco", "ItAm_Addon_Health", "ItAm_Addon_MANA", "ItAm_Addon_STR",
            "ItRi_Addon_Health_01", "ItRi_Addon_Health_02", "ItRi_Addon_MANA_01", "ItRi_Addon_MANA_02",
            "ItRi_Addon_STR_01", "ItRi_Addon_STR_02",

            // Bücher (Addon)
            "ItWr_Addon_BookXP250", "ItWr_Addon_BookXP500", "ItWr_Addon_BookXP1000", "ItWr_Addon_BookLP2",
            "ItWr_Addon_BookLP3", "ItWr_Addon_BookLP5", "ItWr_Addon_BookLP8",

            // Gürtel (Addon)
            "ItBE_Addon_Leather_01", "ItBE_Addon_SLD_01", "ItBE_Addon_NOV_01", "ItBE_Addon_MIL_01",
            "ItBE_Addon_KDF_01", "ItBE_Addon_MC", "ItBe_Addon_STR_5", "ItBe_Addon_STR_10", "ItBe_Addon_DEX_5",
            "ItBe_Addon_DEX_10", "ItBe_Addon_Prot_EDGE", "ItBe_Addon_Prot_Point", "ItBe_Addon_Prot_MAGIC",
            "ItBe_Addon_Prot_FIRE", "ItBe_Addon_Prot_EdgPoi", "ItBe_Addon_Prot_TOTAL",

            // Missionsitems (Addon)
            "ItWr_SaturasFirstMessage_Addon_Sealed", "ItWr_SaturasFirstMessage_Addon", "ItMi_Ornament_Addon",
            "ItMi_Ornament_Addon_Vatras", "ItWr_Map_NewWorld_Ornaments_Addon", "ItWr_Map_NewWorld_Dexter",
            "ItRi_Ranger_Lares_Addon", "ItRi_Ranger_Addon", "ItRi_LanceRing", "ItMi_PortalRing_Addon",
            "ItWr_Martin_MilizEmpfehlung_Addon", "ItWr_RavensKidnapperMission_Addon",
            "ItWr_Vatras_KDFEmpfehlung_Addon", "ItMi_LostInnosStatue_Daron", "ItWr_LuciasLoveLetter_Addon",
            "ItMi_Rake", "ItRi_Addon_BanditTrader", "ItWr_Addon_BanditTrader", "ItWr_Vatras2Saturas_FindRaven",
            "ItWr_Vatras2Saturas_FindRaven_opened", "ItAm_Addon_WispDetector", "ItFo_Addon_Krokofleisch_Mission",
            "ItRi_Addon_MorgansRing_Mission", "ItMi_Focus", "ItMi_Addon_Steel_Paket",
            "ItWr_StonePlateCommon_Addon", "ItMi_Addon_Stone_01", "ItMi_Addon_Stone_05", "ItMi_Addon_Stone_03",
            "ItMi_Addon_Stone_04", "ItMi_Addon_Stone_02", "ItMI_Addon_Kompass_Mis", "ItSE_Addon_FrancisChest",
            "ITWR_Addon_FrancisAbrechnung_Mis", "ITWR_Addon_GregsLogbuch_Mis", "ITKE_Addon_Bloodwyn_01",
            "ITKE_Addon_Heiler", "ItMi_TempelTorKey", "ItMi_Addon_Bloodwyn_Kopf", "ItWR_Addon_TreasureMap",
            "ItMi_Addon_GregsTreasureBottle_MIS", "itmi_erolskelch",

            // Muscheln (Addon)
            "ItMi_Addon_Shell_01", "ItMi_Addon_Shell_02",

            // Nahrung (Addon)
            "ItFo_Addon_Shellflesh", "ItFo_Addon_Rum", "ItFo_Addon_Grog", "ItFo_Addon_LousHammer",
            "ItFo_Addon_SchlafHammer", "ItFo_Addon_SchnellerHering", "ItFo_Addon_Pfeffer_01",
            "ItFo_Addon_FireStew", "ItFo_Addon_Meatsoup",

            // Rüstungen (Addon)
            "ITAR_PIR_L_Addon", "ITAR_PIR_M_Addon", "ITAR_PIR_H_Addon", "ITAR_Thorus_Addon", "ITAR_Raven_Addon",
            "ITAR_OreBaron_Addon", "ITAR_RANGER_Addon", "ITAR_Fake_RANGER", "ITAR_KDW_L_Addon",
            "ITAR_Bloodwyn_Addon", "ITAR_MayaZombie_Addon", "ItAr_FireArmor_Addon",

            // Runen (Addon)
            "ItRu_Thunderstorm", "ItRu_Whirlwind", "ItRu_Geyser", "ItRu_Waterfist", "ItRu_Icelance",
            "ItRu_BeliarsRage", "ItRu_SuckEnergy", "ItRu_GreenTentacle", "ItRu_Swarm", "ItRu_Skull",
            "ItRu_SummonZombie", "ItRu_SummonGuardian",

            // Runenbücher (Addon)
            "ITWR_Addon_Runemaking_KDW_CIRC1", "ITWR_Addon_Runemaking_KDW_CIRC2", "ITWR_Addon_Runemaking_KDW_CIRC3",
            "ITWR_Addon_Runemaking_KDW_CIRC4", "ITWR_Addon_Runemaking_KDW_CIRC5", "ITWR_Addon_Runemaking_KDW_CIRC6",
            "ITWR_Addon_Runemaking_KDF_CIRC1", "ITWR_Addon_Runemaking_KDF_CIRC2", "ITWR_Addon_Runemaking_KDF_CIRC3",
            "ITWR_Addon_Runemaking_KDF_CIRC4", "ITWR_Addon_Runemaking_KDF_CIRC5", "ITWR_Addon_Runemaking_KDF_CIRC6",

            // Schlüssel (Addon)
            "ITKE_PORTALTEMPELWALKTHROUGH_ADDON", "ITKE_Greg_ADDON_MIS", "ITKE_Addon_Tavern_01",
            "ITKE_Addon_Esteban_01", "ITKE_ORLAN_TELEPORTSTATION", "ITKE_CANYONLIBRARY_HIERARCHY_BOOKS_ADDON",
            "ITKE_ADDON_BUDDLER_01", "ITKE_ADDON_SKINNER", "ITKE_Addon_Thorus",

            // Schriftrollen (Addon)
            "ItSc_Geyser", "ItSc_Icelance", "ItSc_Waterfist", "ItSc_Thunderstorm", "ItSc_Whirlwind",

            // Schriftsachen (Addon)
            "ITWr_Addon_Hinweis_02", "ITWr_Addon_Health_04", "ITWr_Addon_Mana_04", "ITWr_Addon_Hinweis_01",
            "ITWr_Addon_William_01", "ITWr_Addon_MCELIXIER_01", "ITWr_Addon_Pirates_01", "ITWr_Addon_Joint_01",
            "ITWr_Addon_Lou_Rezept", "ITWr_Addon_Lou_Rezept2", "ITWr_Addon_Piratentod", "Fakescroll_Addon",
            "ItWr_Addon_AxtAnleitung", "ItWr_Addon_SUMMONANCIENTGHOST", "ItWr_Map_AddonWorld",

            // Steintafeln (Addon)
            "ItWr_StrStonePlate1_Addon", "ItWr_StrStonePlate2_Addon", "ItWr_StrStonePlate3_Addon",
            "ItWr_DexStonePlate1_Addon", "ItWr_DexStonePlate2_Addon", "ItWr_DexStonePlate3_Addon",
            "ItWr_HitPointStonePlate1_Addon", "ItWr_HitPointStonePlate2_Addon", "ItWr_HitPointStonePlate3_Addon",
            "ItWr_ManaStonePlate1_Addon", "ItWr_ManaStonePlate2_Addon", "ItWr_ManaStonePlate3_Addon",
            "ItWr_OneHStonePlate1_Addon", "ItWr_OneHStonePlate2_Addon", "ItWr_OneHStonePlate3_Addon",
            "ItWr_TwoHStonePlate1_Addon", "ItWr_TwoHStonePlate2_Addon", "ItWr_TwoHStonePlate3_Addon",
            "ItWr_BowStonePlate1_Addon", "ItWr_BowStonePlate2_Addon", "ItWr_BowStonePlate3_Addon",
            "ItWr_CrsBowStonePlate1_Addon", "ItWr_CrsBowStonePlate2_Addon", "ItWr_CrsBowStonePlate3_Addon",

            // Tränke (Addon)
            "ItPo_Addon_Geist_01", "ItPo_Addon_Geist_02", "ItPo_Health_Addon_04", "ItPo_Mana_Addon_04",

            // Waffen (Addon)
            "ItMW_Addon_Knife01", "ItMW_Addon_Stab01", "ItMW_Addon_Stab02", "ItMW_Addon_Stab03", "ItMW_Addon_Stab04",
            "ItMW_Addon_Stab05", "ItMW_Addon_Hacker_1h_01", "ItMW_Addon_Hacker_1h_02", "ItMW_Addon_Hacker_2h_01",
            "ItMW_Addon_Hacker_2h_02", "ItMW_Addon_Keule_1h_01", "ItMW_Addon_Keule_2h_01", "ItMw_FrancisDagger_Mis",
            "ItMw_RangerStaff_Addon", "ItMw_Addon_PIR2hAxe", "ItMw_Addon_PIR2hSword", "ItMw_Addon_PIR1hAxe",
            "ItMw_Addon_PIR1hSword", "ItMw_Addon_BanditTrader", "ItMw_Addon_Betty", "ItRw_Addon_MagicArrow",
            "ItRw_Addon_FireArrow", "ItRw_Addon_MagicBow", "ItRw_Addon_FireBow", "ItRw_Addon_MagicBolt",
            "ItRw_Addon_MagicCrossbow",

            // Sonstiges (Addon)
            "ItMi_GoldNugget_Addon", "ItMi_Addon_WhitePearl", "ItMi_Addon_Joint_01", "ItMi_BaltramPaket",
            "ItMi_Packet_Baltram4Skip_Addon", "ItMi_BromorsGeld_Addon", "ItSe_ADDON_CavalornsBeutel",
            "ItMi_Skull", "ItMi_IECello", "ItMi_IECelloBow", "ItMi_IEDrum", "ItMi_IEDrumScheit",
            "ItMi_IEDrumStick", "ItMi_IEDudelBlau", "ItMi_IEDudelGelb", "ItMi_IEHarfe", "ItMi_IELaute",
            "ItMi_Addon_Lennar_Paket", "ItMi_Zeitspalt_Addon"
        };

        public static HashSet<string> GetSuggestions()
        {
            return predefinedSuggestions;
        }
    }
}