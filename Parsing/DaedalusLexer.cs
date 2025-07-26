using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Skriptorium.Parsing
{
    public class DaedalusLexer
    {
        // Fall‑insensitive Keywords und Enums
        private static readonly HashSet<string> keywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "instance", "func", "var", "const", "if", "else", "return",
            "class", "prototype",
            // Datentypen als Keywords
            "int", "float", "void", "string",
            // Boolean Literale
            "true", "false",
        };

        // Built-in Funktionen
        private static readonly HashSet<string> BuiltInFunctions = new(StringComparer.OrdinalIgnoreCase)
        {
            "B_SetAttributesToChapter",
            "B_CreateAmbientInv",
            "B_SetNpcVisual",
            "B_GiveNpcTalents",
            "B_SetFightSkills",
            "EquipItem",
            "Mdl_SetModelFatness",
            "Mdl_ApplyOverlayMds",
            "TA_Stand_ArmsCrossed",
            "TA_Stand_Guarding"
        };

        // Regex für verschiedene Token‑Klassen
        private static readonly Regex identifier = new(@"^[A-Za-z_][A-Za-z0-9_]*");
        private static readonly Regex floatLiteral = new(@"^\d+\.\d+");
        private static readonly Regex intLiteral = new(@"^\d+");
        private static readonly Regex strLiteral = new Regex(@"^""(?:\\.|[^""])*""");
        private static readonly Regex lineComment = new(@"^//.*");
        private static readonly Regex blockCommentStart = new(@"^/\*");

        // Multi‑Char Operatoren
        private static readonly string[] multiCharOperators = { "==", "!=", "<=", ">=", "&&", "||" };

        // Single‑Char Operatoren (ohne '=')
        private static readonly char[] singleCharOperators = { '+', '-', '*', '/', '%', '<', '>', '!' };

        // Symbole
        private static readonly char[] brackets = { '{', '}' };
        private static readonly char[] parentheses = { '(', ')' };
        private static readonly char[] commas = { ',' };
        private static readonly char[] semicolons = { ';' };
        private static readonly char[] squareBrackets = { '[', ']' };

        private bool _expectInstanceName = false;

        public List<DaedalusToken> Tokenize(string[] lines)
        {
            var tokens = new List<DaedalusToken>();
            bool inBlockComment = false;

            for (int line = 0; line < lines.Length; line++)
            {
                string text = lines[line];
                int column = 0;

                while (column < text.Length)
                {
                    if (inBlockComment)
                    {
                        int endIdx = text.IndexOf("*/", column, StringComparison.Ordinal);
                        if (endIdx >= 0)
                        {
                            tokens.Add(new DaedalusToken(TokenType.CommentBlock,
                                text.Substring(column, endIdx + 2 - column),
                                line + 1, column + 1));
                            column = endIdx + 2;
                            inBlockComment = false;
                        }
                        else
                        {
                            tokens.Add(new DaedalusToken(TokenType.CommentBlock,
                                text.Substring(column),
                                line + 1, column + 1));
                            break;
                        }
                        continue;
                    }

                    char current = text[column];
                    if (char.IsWhiteSpace(current))
                    {
                        column++;
                        continue;
                    }

                    string remaining = text.Substring(column);

                    // Zeilenkommentar
                    if (lineComment.IsMatch(remaining))
                    {
                        tokens.Add(new DaedalusToken(TokenType.Comment,
                            remaining, line + 1, column + 1));
                        break;
                    }

                    // Blockkommentar
                    if (blockCommentStart.IsMatch(remaining))
                    {
                        int endIdx = remaining.IndexOf("*/", StringComparison.Ordinal);
                        if (endIdx >= 0)
                        {
                            tokens.Add(new DaedalusToken(TokenType.CommentBlock,
                                remaining.Substring(0, endIdx + 2),
                                line + 1, column + 1));
                            column += endIdx + 2;
                        }
                        else
                        {
                            tokens.Add(new DaedalusToken(TokenType.CommentBlock,
                                remaining, line + 1, column + 1));
                            inBlockComment = true;
                            break;
                        }
                        continue;
                    }

                    // String‑Literal
                    if (strLiteral.IsMatch(remaining))
                    {
                        var match = strLiteral.Match(remaining);
                        tokens.Add(new DaedalusToken(TokenType.StringLiteral,
                            match.Value, line + 1, column + 1));
                        column += match.Length;
                        continue;
                    }

                    // Float‑Literal
                    if (floatLiteral.IsMatch(remaining))
                    {
                        var match = floatLiteral.Match(remaining);
                        tokens.Add(new DaedalusToken(TokenType.FloatLiteral,
                            match.Value, line + 1, column + 1));
                        column += match.Length;
                        continue;
                    }

                    // Integer‑Literal
                    if (intLiteral.IsMatch(remaining))
                    {
                        var match = intLiteral.Match(remaining);
                        tokens.Add(new DaedalusToken(TokenType.IntegerLiteral,
                            match.Value, line + 1, column + 1));
                        column += match.Length;
                        continue;
                    }

                    // Multi‑Char Operator
                    var multiOp = multiCharOperators.FirstOrDefault(op => remaining.StartsWith(op, StringComparison.Ordinal));
                    if (multiOp != null)
                    {
                        tokens.Add(new DaedalusToken(TokenType.Operator,
                            multiOp, line + 1, column + 1));
                        column += multiOp.Length;
                        continue;
                    }

                    // Assignment‑Operator '='
                    if (current == '=')
                    {
                        tokens.Add(new DaedalusToken(TokenType.Assignment,
                            "=", line + 1, column + 1));
                        column++;
                        continue;
                    }

                    // Single‑Char Operator
                    if (singleCharOperators.Contains(current))
                    {
                        tokens.Add(new DaedalusToken(TokenType.Operator,
                            current.ToString(), line + 1, column + 1));
                        column++;
                        continue;
                    }

                    // Identifiers, Keywords, Bool- und AIV-Literals usw.
                    if (identifier.IsMatch(remaining))
                    {
                        var match = identifier.Match(remaining);
                        var val = match.Value;
                        TokenType type = TokenType.Unknown;

                        // Spezielle Keywords und Konstanten
                        if (val.StartsWith("GIL_", StringComparison.OrdinalIgnoreCase))
                        {
                            type = TokenType.GuildConstant;
                        }
                        else if (val.StartsWith("NPC"))
                        {
                            type = TokenType.NPC_Constant;
                        }
                        else if (val.Equals("aivar", StringComparison.OrdinalIgnoreCase))
                        {
                            type = TokenType.AiVariable;
                        }
                        else if (val.StartsWith("AIV_", StringComparison.OrdinalIgnoreCase))
                        {
                            type = TokenType.AIV_Constant;
                        }
                        else if (val.StartsWith("FAI_", StringComparison.OrdinalIgnoreCase))
                        {
                            type = TokenType.FAI_Constant;
                        }
                        else if (BuiltInFunctions.Contains(val))
                        {
                            type = TokenType.BuiltInFunction;
                        }
                        else if (val.Equals("self", StringComparison.OrdinalIgnoreCase))
                        {
                            type = TokenType.SelfKeyword;
                        }
                        else if (val.Equals("other", StringComparison.OrdinalIgnoreCase))
                        {
                            type = TokenType.OtherKeyword;
                        }
                        else if (keywords.Contains(val))
                        {
                            switch (val.ToLowerInvariant())
                            {
                                case "func": type = TokenType.FuncKeyword; break;
                                case "var": type = TokenType.VarKeyword; break;
                                case "const": type = TokenType.ConstKeyword; break;
                                case "return": type = TokenType.ReturnKeyword; break;
                                case "if": type = TokenType.IfKeyword; break;
                                case "else": type = TokenType.ElseKeyword; break;
                                case "instance":
                                    type = TokenType.InstanceKeyword;
                                    _expectInstanceName = true; // nächstes Identifier ist Instanzname
                                    break;
                                case "class": type = TokenType.ClassKeyword; break;
                                case "prototype": type = TokenType.PrototypeKeyword; break;
                                case "int":
                                case "float":
                                case "void":
                                case "string": type = TokenType.TypeKeyword; break;
                                case "true":
                                case "false": type = TokenType.BoolLiteral; break;
                                default: type = TokenType.Identifier; break;
                            }
                        }
                        else if (_expectInstanceName && type == TokenType.Unknown)
                        {
                            // Falls erwartet wird, dass dies ein Instanzname ist
                            type = TokenType.InstanceName;
                            _expectInstanceName = false;
                        }
                        else
                        {
                            type = TokenType.Identifier;
                        }

                        // Sicherheitshalber _expectInstanceName zurücksetzen, falls es nicht gesetzt wurde
                        if (type != TokenType.InstanceKeyword && !_expectInstanceName)
                        {
                            _expectInstanceName = false;
                        }

                        tokens.Add(new DaedalusToken(type, val, line + 1, column + 1));
                        column += match.Length;
                        continue;
                    }


                    // Symbole
                    if (current == '{')
                    {
                        tokens.Add(new DaedalusToken(TokenType.OpenBracket, "{", line + 1, column + 1));
                        column++;
                        continue;
                    }
                    else if (current == '}')
                    {
                        tokens.Add(new DaedalusToken(TokenType.CloseBracket, "}", line + 1, column + 1));
                        column++;
                        continue;
                    }
                    else if (current == '(')
                    {
                        tokens.Add(new DaedalusToken(TokenType.OpenParenthesis, "(", line + 1, column + 1));
                        column++;
                        continue;
                    }
                    else if (current == ')')
                    {
                        tokens.Add(new DaedalusToken(TokenType.CloseParenthesis, ")", line + 1, column + 1));
                        column++;
                        continue;
                    }
                    else if (current == '[')
                    {
                        tokens.Add(new DaedalusToken(TokenType.OpenSquareBracket, "[", line + 1, column + 1));
                        column++;
                        continue;
                    }
                    else if (current == ']')
                    {
                        tokens.Add(new DaedalusToken(TokenType.CloseSquareBracket, "]", line + 1, column + 1));
                        column++;
                        continue;
                    }
                    else if (commas.Contains(current))
                    {
                        tokens.Add(new DaedalusToken(TokenType.Comma, current.ToString(), line + 1, column + 1));
                        column++;
                        continue;
                    }
                    else if (semicolons.Contains(current))
                    {
                        tokens.Add(new DaedalusToken(TokenType.Semicolon, current.ToString(), line + 1, column + 1));
                        column++;
                        continue;
                    }

                    // Unbekanntes Zeichen
                    tokens.Add(new DaedalusToken(TokenType.Unknown,
                        current.ToString(), line + 1, column + 1));
                    column++;
                }
            }

            // EOF‑Markierung
            tokens.Add(new DaedalusToken(TokenType.EOF, "", lines.Length, 0));
            return tokens;
        }
    }
}
