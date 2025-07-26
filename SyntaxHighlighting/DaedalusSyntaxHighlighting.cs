using Skriptorium.Parsing;
using System;
using System.Collections.Generic;
using System.Windows.Media;

public enum SyntaxColor
{
    Keyword,
    Identifier,
    Type,
    String,
    Number,
    Comment,
    Operator,
    Assignment,
    Bracket,
    InstanceName,
    GuildConstant,
    NPC_Constant,
    AiVariable,
    AIV_Constant,
    FAI_Constant,
    BuiltInFunction,
    Unknown,
}

public class SyntaxHighlighting
{
    private static readonly Dictionary<TokenType, (SyntaxColor Color, Color WpfColor)> tokenTypeToColor = new()
        {
            // Schlüsselwörter
            { TokenType.FuncKeyword,        (SyntaxColor.Keyword, Colors.Blue) },
            { TokenType.VarKeyword,         (SyntaxColor.Keyword, Colors.Blue) },
            { TokenType.ConstKeyword,       (SyntaxColor.Keyword, Colors.Red) },
            { TokenType.ReturnKeyword,      (SyntaxColor.Keyword, Colors.DarkBlue) },
            { TokenType.IfKeyword,          (SyntaxColor.Keyword, Colors.Blue) },
            { TokenType.ElseKeyword,        (SyntaxColor.Keyword, Colors.Blue) },
            { TokenType.InstanceKeyword,    (SyntaxColor.Keyword, Colors.Blue) },
            { TokenType.ClassKeyword,       (SyntaxColor.Keyword, Colors.Red) },
            { TokenType.PrototypeKeyword,   (SyntaxColor.Keyword, Colors.Red) },
            { TokenType.BoolLiteral,        (SyntaxColor.Keyword, Colors.Red) },

            // Typen & Literale
            { TokenType.TypeKeyword,        (SyntaxColor.Type, Colors.Red) },
            { TokenType.Identifier,         (SyntaxColor.Identifier, Colors.Black) },
            { TokenType.InstanceName,       (SyntaxColor.InstanceName, Colors.DarkBlue) },
            { TokenType.StringLiteral,      (SyntaxColor.String, Colors.Magenta) },
            { TokenType.FloatLiteral,       (SyntaxColor.Number, Colors.DarkBlue) },
            { TokenType.IntegerLiteral,     (SyntaxColor.Number, Colors.DarkBlue) },


            // Engine API & spezielle Bezeichner
            { TokenType.GuildConstant,      (SyntaxColor.GuildConstant, Colors.Red) },
            { TokenType.NPC_Constant,       (SyntaxColor.NPC_Constant, Colors.Red) },
            { TokenType.AiVariable,         (SyntaxColor.AiVariable, Colors.Green) },
            { TokenType.AIV_Constant,       (SyntaxColor.AIV_Constant, Colors.Red) },
            { TokenType.FAI_Constant,       (SyntaxColor.FAI_Constant, Colors.Red) },
            { TokenType.BuiltInFunction,    (SyntaxColor.Identifier, Colors.Goldenrod) },
            { TokenType.SelfKeyword,        (SyntaxColor.Identifier, Colors.Green) },
            { TokenType.OtherKeyword,       (SyntaxColor.Identifier, Colors.Green) },


            // Kommentare
            { TokenType.Comment,            (SyntaxColor.Comment, Colors.Gray) },
            { TokenType.CommentBlock,       (SyntaxColor.Comment, Colors.Gray) },

            // Operatoren & Symbole
            { TokenType.Operator,           (SyntaxColor.Operator, Colors.Green) },
            { TokenType.Assignment,         (SyntaxColor.Assignment, Colors.Green) },
            { TokenType.OpenBracket,        (SyntaxColor.Bracket, Colors.Green) },
            { TokenType.CloseBracket,       (SyntaxColor.Bracket, Colors.Green) },
            { TokenType.OpenParenthesis,    (SyntaxColor.Bracket, Colors.Green) },
            { TokenType.CloseParenthesis,   (SyntaxColor.Bracket, Colors.Green) },
            { TokenType.OpenSquareBracket,  (SyntaxColor.Bracket, Colors.Green) },
            { TokenType.CloseSquareBracket, (SyntaxColor.Bracket, Colors.Green) },
            { TokenType.Comma,              (SyntaxColor.Bracket, Colors.Green) },
            { TokenType.Semicolon,          (SyntaxColor.Bracket, Colors.Green) },

            // Sonstiges
            { TokenType.Whitespace,         (SyntaxColor.Unknown, Colors.Violet) },
            { TokenType.EOF,                (SyntaxColor.Unknown, Colors.Violet) },
            { TokenType.Unknown,            (SyntaxColor.Unknown, Colors.Black) }
        };

    public static (SyntaxColor Color, Color WpfColor) GetColorForToken(DaedalusToken token)
    {
        return tokenTypeToColor.TryGetValue(token.Type, out var colorInfo)
            ? colorInfo
            : (SyntaxColor.Unknown, Colors.Red);
    }
}
