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
        TypeKeyword,       // int, float, void, string

        // Literale
        Identifier,
        IntegerLiteral,    // Ganzzahlen
        FloatLiteral,      // z. B. 1.5
        StringLiteral,     // "…"
        BoolLiteral,       // TRUE, FALSE
        InstanceName,      // z. B. PAL_200_Hagen

        // Engine API
        GuildConstant,     // z.B. GIL_"Gilde"
        NPC_Constant,      //
        AiVariable,        // z. B. aivar
        AIV_Constant,      // z. B. AIV_TOUGHGUY usw.
        FAI_Constant,      // z. B. FAI_HUMAN_MASTER
        BuiltInFunction,   // z. B. B_SetNpcVisual, EquipItem usw.
        SelfKeyword,       // self
        OtherKeyword,      // other

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
        Comma,              // ,
        Semicolon,          // ;

        // Kommentare
        Comment,           // //
        CommentBlock,      // /* … */

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
