using Skriptorium.Parsing;
using System;
using System.Collections.Generic;
using System.Windows.Media;


public class DaedalusSyntaxHighlightingDarkmode
{
    private static readonly Dictionary<TokenType, (SyntaxColor Color, Color WpfColor)> tokenTypeToColor = new()
    {
            // Schlüsselwörter
        { TokenType.FuncKeyword,        (SyntaxColor.Keyword, Colors.LightSkyBlue) },
        { TokenType.VarKeyword,         (SyntaxColor.Keyword, Colors.LightSkyBlue) },
        { TokenType.ConstKeyword,       (SyntaxColor.Keyword, Colors.OrangeRed) },
        { TokenType.ReturnKeyword,      (SyntaxColor.Keyword, Colors.CornflowerBlue) },
        { TokenType.IfKeyword,          (SyntaxColor.Keyword, Colors.LightSkyBlue) },
        { TokenType.ElseKeyword,        (SyntaxColor.Keyword, Colors.LightSkyBlue) },
        { TokenType.InstanceKeyword,    (SyntaxColor.Keyword, Colors.LightSkyBlue) },
        { TokenType.ClassKeyword,       (SyntaxColor.Keyword, Colors.OrangeRed) },
        { TokenType.PrototypeKeyword,   (SyntaxColor.Keyword, Colors.OrangeRed) },
        { TokenType.BoolLiteral,        (SyntaxColor.Keyword, Colors.OrangeRed) },

        // Typen & Literale
        { TokenType.TypeKeyword,        (SyntaxColor.Type, Colors.OrangeRed) },
        { TokenType.Identifier,         (SyntaxColor.Identifier, Colors.WhiteSmoke) },
        { TokenType.InstanceName,       (SyntaxColor.InstanceName, Colors.CornflowerBlue) },
        { TokenType.FunctionName,       (SyntaxColor.FunctionName, Colors.CornflowerBlue) },
        { TokenType.StringLiteral,      (SyntaxColor.String, Colors.Violet) },
        { TokenType.FloatLiteral,       (SyntaxColor.Number, Colors.CornflowerBlue) },
        { TokenType.IntegerLiteral,     (SyntaxColor.Number, Colors.CornflowerBlue) },

        // Engine API & spezielle Bezeichner
        { TokenType.GuildConstant,          (SyntaxColor.GuildConstant, Colors.OrangeRed) },
        { TokenType.NPC_Constant,           (SyntaxColor.NPC_Constant, Colors.OrangeRed) },
        { TokenType.AiVariable,             (SyntaxColor.AiVariable, Colors.LightGreen) },
        { TokenType.AIVConstant,            (SyntaxColor.AIVConstant, Colors.OrangeRed) },
        { TokenType.FAIConstant,            (SyntaxColor.FAIConstant, Colors.OrangeRed) },
        { TokenType.SexConstant,            (SyntaxColor.SexConstant, Colors.OrangeRed) },
        { TokenType.CRIMEConstant,          (SyntaxColor.CRIMEConstant, Colors.OrangeRed) },
        { TokenType.LOCConstant,            (SyntaxColor.LOCConstant, Colors.OrangeRed) },
        { TokenType.PETZCOUNTERConstant,    (SyntaxColor.PETZCOUNTERConstant, Colors.OrangeRed) },
        { TokenType.LOGConstant,            (SyntaxColor.LOGConstant, Colors.OrangeRed) },
        { TokenType.FONTConstant,           (SyntaxColor.FONTConstant, Colors.OrangeRed) },
        { TokenType.ZENConstant,            (SyntaxColor.ZENConstant, Colors.OrangeRed) },
        { TokenType.REALConstant,           (SyntaxColor.REALConstant, Colors.OrangeRed) },
        { TokenType.ZS_TalkConstant,        (SyntaxColor.ZS_TalkConstant, Colors.OrangeRed) },
        { TokenType.ATRConstant,            (SyntaxColor.ATRConstant, Colors.OrangeRed) },
        { TokenType.ARConstant,             (SyntaxColor.ARConstant, Colors.OrangeRed) },
        { TokenType.PLAYERConstant,         (SyntaxColor.PLAYERConstant, Colors.OrangeRed) },
        { TokenType.BuiltInFunction,        (SyntaxColor.BuiltInFunction, Colors.SandyBrown) },
        { TokenType.MdlFunction,            (SyntaxColor.MdlFunction, Colors.SandyBrown) },
        { TokenType.AIFunction,             (SyntaxColor.AIFunction, Colors.SandyBrown) },
        { TokenType.NpcFunction,            (SyntaxColor.BuiltInFunction, Colors.SandyBrown) },
        { TokenType.InfoFunction,           (SyntaxColor.InfoFunction, Colors.SandyBrown) },
        { TokenType.CreateFunction,         (SyntaxColor.CreateFunction, Colors.SandyBrown) },
        { TokenType.WldFunction,            (SyntaxColor.WldFunction, Colors.SandyBrown) },
        { TokenType.LogFunction,            (SyntaxColor.LogFunction, Colors.SandyBrown) },
        { TokenType.HlpFunction,            (SyntaxColor.BuiltInFunction, Colors.SandyBrown) },
        { TokenType.SndFunction,            (SyntaxColor.SndFunction, Colors.SandyBrown) },
        { TokenType.TAFunction,             (SyntaxColor.TAFunction, Colors.SandyBrown) },
        { TokenType.EquipFunction,          (SyntaxColor.BuiltInFunction, Colors.SandyBrown) },
        { TokenType.PrintscreenFunction,    (SyntaxColor.BuiltInFunction, Colors.SandyBrown) },
        { TokenType.SelfKeyword,            (SyntaxColor.Identifier, Colors.LightGreen) },
        { TokenType.OtherKeyword,           (SyntaxColor.Identifier, Colors.LightGreen) },
        { TokenType.SlfKeyword,             (SyntaxColor.Identifier, Colors.LightGreen) },

            // Kommentare
        { TokenType.Comment,            (SyntaxColor.Comment, Colors.Gray) },
        { TokenType.CommentBlock,       (SyntaxColor.Comment, Colors.Gray) },

            // Operatoren & Symbole
        { TokenType.Operator,           (SyntaxColor.Operator, Colors.LightGreen) },
        { TokenType.Assignment,         (SyntaxColor.Assignment, Colors.LightGreen) },
        { TokenType.OpenBracket,        (SyntaxColor.Bracket, Colors.LightGreen) },
        { TokenType.CloseBracket,       (SyntaxColor.Bracket, Colors.LightGreen) },
        { TokenType.OpenParenthesis,    (SyntaxColor.Bracket, Colors.LightGreen) },
        { TokenType.CloseParenthesis,   (SyntaxColor.Bracket, Colors.LightGreen) },
        { TokenType.OpenSquareBracket,  (SyntaxColor.Bracket, Colors.LightGreen) },
        { TokenType.CloseSquareBracket, (SyntaxColor.Bracket, Colors.LightGreen) },
        { TokenType.Comma,              (SyntaxColor.Bracket, Colors.LightGreen) },
        { TokenType.Semicolon,          (SyntaxColor.Bracket, Colors.LightGreen) },

        // Sonstiges
        { TokenType.Whitespace,         (SyntaxColor.Unknown, Colors.WhiteSmoke) },
        { TokenType.EOF,                (SyntaxColor.Unknown, Colors.WhiteSmoke) },
        { TokenType.Unknown,            (SyntaxColor.Unknown, Colors.WhiteSmoke) }
    };

    public static (SyntaxColor Color, Color WpfColor) GetColorForToken(DaedalusToken token)
    {
        return tokenTypeToColor.TryGetValue(token.Type, out var colorInfo)
            ? colorInfo
            : (SyntaxColor.Unknown, Colors.WhiteSmoke);
    }

    public static IReadOnlyDictionary<TokenType, (SyntaxColor Color, Color WpfColor)> TokenColors => tokenTypeToColor;
}
