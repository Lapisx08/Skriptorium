using System;
using System.Collections.Generic;
using Skriptorium.Parsing;

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
    Enum,
    BuiltInFunction,
    Unknown,
}

public class SyntaxHighlighting
{
    private static readonly Dictionary<TokenType, SyntaxColor> tokenTypeToColor = new()
    {
        // Schlüsselwörter
        { TokenType.FuncKeyword, SyntaxColor.Keyword },
        { TokenType.VarKeyword, SyntaxColor.Keyword },
        { TokenType.ConstKeyword, SyntaxColor.Keyword },
        { TokenType.ReturnKeyword, SyntaxColor.Keyword },
        { TokenType.IfKeyword, SyntaxColor.Keyword },
        { TokenType.ElseKeyword, SyntaxColor.Keyword },
        { TokenType.InstanceKeyword, SyntaxColor.Keyword },
        { TokenType.ClassKeyword, SyntaxColor.Keyword },
        { TokenType.PrototypeKeyword, SyntaxColor.Keyword },
        { TokenType.TypeKeyword, SyntaxColor.Type },

        // Literale
        { TokenType.Identifier, SyntaxColor.Identifier },
        { TokenType.StringLiteral, SyntaxColor.String },
        { TokenType.FloatLiteral, SyntaxColor.Number },
        { TokenType.IntegerLiteral, SyntaxColor.Number },
        { TokenType.BoolLiteral, SyntaxColor.Keyword },
        { TokenType.EnumLiteral, SyntaxColor.Enum },

        // Kommentare
        { TokenType.Comment, SyntaxColor.Comment },
        { TokenType.CommentBlock, SyntaxColor.Comment },

        // Operatoren
        { TokenType.Operator, SyntaxColor.Operator },
        { TokenType.Assignment, SyntaxColor.Assignment },

        // Symbole
        { TokenType.OpenBracket, SyntaxColor.Bracket },
        { TokenType.CloseBracket, SyntaxColor.Bracket },
        { TokenType.OpenParenthesis, SyntaxColor.Bracket },
        { TokenType.CloseParenthesis, SyntaxColor.Bracket },
        { TokenType.OpenSquareBracket, SyntaxColor.Bracket },
        { TokenType.CloseSquareBracket, SyntaxColor.Bracket },
        { TokenType.Comma, SyntaxColor.Bracket },
        { TokenType.Semicolon, SyntaxColor.Bracket },

        // Sonstiges
        { TokenType.Whitespace, SyntaxColor.Unknown },
        { TokenType.EOF, SyntaxColor.Unknown },
        { TokenType.Unknown, SyntaxColor.Unknown }
    };

    public static SyntaxColor GetColorForToken(DaedalusToken token)
    {
        if (tokenTypeToColor.TryGetValue(token.Type, out var color))
            return color;
        return SyntaxColor.Unknown;
    }
}
