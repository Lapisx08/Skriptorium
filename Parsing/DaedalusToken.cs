using System;

namespace Skriptorium.Parsing
{
    public enum TokenType
    {
        // Schlüsselwörter
        FuncKeyword,       // func
        VarKeyword,        // var
        ConstKeyword,      // const
        ReturnKeyword,     // return
        IfKeyword,         // if
        ElseKeyword,       // else
        InstanceKeyword,   // instance
        ClassKeyword,      // class
        PrototypeKeyword,  // prototype

        // Datentypen
        TypeKeyword,       // int, float, void, string, c_npc

        // Literale
        Identifier,
        FunctionName,      // Funktionsname nach "void"
        IntegerLiteral,    // Ganzzahlen
        FloatLiteral,      // z. B. 1.5
        StringLiteral,     // "…"
        BoolLiteral,       // TRUE, FALSE
        InstanceName,      // z. B. PAL_200_Hagen

        // Engine API
        GuildConstant,          // z.B. GIL_"Gilde"
        NPC_Constant,           //
        AiVariable,             // z. B. aivar
        AIVConstant,            // z. B. AIV_TOUGHGUY
        FAIConstant,            // z. B. FAI_HUMAN_MASTER
        SexConstant,            // MALE oder FEMALE
        CRIMEConstant,          // z. B. CRIME_Murder
        LOCConstant,            // z. B. LOC_CITY
        PETZCOUNTERConstant,    // z. B. PETZCOUNTER_City_Attack
        LOGConstant,            // z. B. LOG_Running
        FONTConstant,           // z. B. FONT_Screen
        ZENConstant,            // z. B. NEWWORLD_ZEN
        REALConstant,           // z. B. REAL_TALENT_2H
        ZS_TalkConstant,        // ZS_Talk
        ATRConstant,            // z. B. ATR_DEXTERITY
        MAXConstant,            // z. B. MAX_HITCHANCE
        PROTConstant,           // z. B. PROT_INDEX_MAX
        DAMConstant,            // z. B. DAM_INDEX_MAX
        ITMConstant,            // z. B. ITM_SOMEITEM
        ARConstant,             // z. B. AR_THEFT
        PLAYERConstant,         // z. B. PLAYER_TALENT_ALCHEMY
        BuiltInFunction,        // z. B. B_SetNpcVisual
        MdlFunction,            // z.  B. Mdl_SetModelFatness
        AIFunction,             // z. B. AI_StopProcessInfos
        NpcFunction,            // z. B. Npc_IsInState
        InfoFunction,           // z. B. Info_AddChoice
        CreateFunction,         // z. B. CreateInvItems
        WldFunction,            // z. B. Wld_InsertNpc
        LogFunction,            // z. B. Log_CreateTopic
        HlpFunction,            // z. B. Help_GetNpc
        SndFunction,            // z. B. Snd_Play
        TAFunction,             // z. B. TA_Stand_Armscrossed
        EquipFunction,          // EquipItem
        PrintscreenFunction,    // Printscreen
        SelfKeyword,            // self
        OtherKeyword,           // other
        SlfKeyword,             // slf

        // Operatoren
        Operator,          // +, -, *, /, %, <, >, !, &&, ||
        Assignment,        // =

        // Symbole
        OpenBracket,        // {
        CloseBracket,       // }
        OpenParenthesis,    // (
        CloseParenthesis,   // )
        OpenSquareBracket,  // [
        CloseSquareBracket, // ]
        Dot,                // .
        Comma,              // ,
        Semicolon,          // ;

        // Kommentare
        Comment,           // //
        CommentBlock,      // /* … */

        // Sonstige
        Whitespace,
        EOF,
        Unknown
    }

    public class DaedalusToken
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }

        public DaedalusToken()
        {
            Type = TokenType.Unknown;
            Value = "";
            Line = -1;
            Column = -1;
        }

        public DaedalusToken(TokenType type, string value, int line, int column)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
        }

        public override string ToString()
            => $"{Type}('{Value}') at {Line}:{Column}";
    }
}
