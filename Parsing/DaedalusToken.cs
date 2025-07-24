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
        IntegerLiteral,
        FloatLiteral,
        StringLiteral,     // "…"
        BoolLiteral,       // TRUE, FALSE
        EnumLiteral,       // z.B. CRIME_MURDER, GIL_PAL, NPC_FLAG_IMMORTAL

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
