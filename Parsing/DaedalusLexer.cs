using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Skriptorium.Parsing
{
    public class DaedalusLexer
    {
        private static readonly Dictionary<string, TokenType> keywordsMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["func"] = TokenType.FuncKeyword,
            ["var"] = TokenType.VarKeyword,
            ["const"] = TokenType.ConstKeyword,
            ["return"] = TokenType.ReturnKeyword,
            ["if"] = TokenType.IfKeyword,
            ["else"] = TokenType.ElseKeyword,
            ["instance"] = TokenType.InstanceKeyword,
            ["class"] = TokenType.ClassKeyword,
            ["prototype"] = TokenType.PrototypeKeyword,
            ["int"] = TokenType.TypeKeyword,
            ["float"] = TokenType.TypeKeyword,
            ["void"] = TokenType.TypeKeyword,
            ["string"] = TokenType.TypeKeyword,
            ["c_npc"] = TokenType.TypeKeyword,
            ["true"] = TokenType.BoolLiteral,
            ["false"] = TokenType.BoolLiteral,
        };

        // Präfixe für spezielle Konstanten und Funktionen
        private static readonly Dictionary<string, TokenType> prefixTokenTypes = new()
        {
            ["GIL_"] = TokenType.GuildConstant,
            ["NPC"] = TokenType.NPC_Constant,
            ["AIV_"] = TokenType.AIVConstant,
            ["FAI_"] = TokenType.FAIConstant,
            ["CRIME_"] = TokenType.CRIMEConstant,
            ["LOC_"] = TokenType.LOCConstant,
            ["PETZCOUNTER_"] = TokenType.PETZCOUNTERConstant,
            ["LOG_"] = TokenType.LOGConstant,
            ["FONT_"] = TokenType.FONTConstant,
            ["REAL_"] = TokenType.REALConstant,
            ["ATR_"] = TokenType.ATRConstant,
            ["AR_"] = TokenType.ARConstant,
            ["PLAYER_"] = TokenType.PLAYERConstant,
            ["B_"] = TokenType.BuiltInFunction,
            ["Mdl_"] = TokenType.MdlFunction,
            ["AI_"] = TokenType.AIFunction,
            ["Npc_"] = TokenType.NpcFunction,
            ["Info_"] = TokenType.InfoFunction,
            ["Create"] = TokenType.CreateFunction,
            ["Wld_"] = TokenType.WldFunction,
            ["Log_"] = TokenType.LogFunction,
            ["Hlp_"] = TokenType.HlpFunction,
            ["Snd_"] = TokenType.SndFunction,
            ["TA_"] = TokenType.TAFunction,
        };

        private static readonly Dictionary<string, TokenType> specialKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            ["self"] = TokenType.SelfKeyword,
            ["other"] = TokenType.OtherKeyword,
            ["hero"] = TokenType.OtherKeyword,
            ["slf"] = TokenType.SlfKeyword,
        };

        // Trennzeichen
        private static readonly Dictionary<char, TokenType> singleCharTokenMap = new()
        {
            { '{', TokenType.OpenBracket },
            { '}', TokenType.CloseBracket },
            { '(', TokenType.OpenParenthesis },
            { ')', TokenType.CloseParenthesis },
            { '[', TokenType.OpenSquareBracket },
            { ']', TokenType.CloseSquareBracket },
            { '.', TokenType.Dot },
            { ',', TokenType.Comma },
            { ';', TokenType.Semicolon },
        };

        // Single‑Char Operatoren (ohne '=')
        private static readonly HashSet<char> singleCharOperators = new()
        {
            '+', '-', '*', '/', '%', '<', '>', '!'
        };

        // Multi‑Char Operatoren
        private static readonly HashSet<string> multiCharOperators = new()
        {
            "==", "!=", "<=", ">=", "&&", "||"
        };

        // Regex für verschiedene Token‑Klassen
        private static readonly Regex identifier = new(@"^[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex floatLiteral = new(@"^\d+\.\d+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex intLiteral = new(@"^\d+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex strLiteral = new(@"^""(?:\\.|[^""])*""", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex lineComment = new(@"^//.*", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex blockCommentStart = new(@"^/\*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly HashSet<string> OtherFunctions = new(StringComparer.OrdinalIgnoreCase)
        {
            "EquipItem",
            "PrintScreen"
        };

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

                    char current = text[column];
                    if (char.IsWhiteSpace(current))
                    {
                        column++;
                        continue;
                    }

                    // Multi-Char Operator (2 Zeichen)
                    if (column + 1 < text.Length)
                    {
                        string twoChars = text.Substring(column, 2);
                        if (multiCharOperators.Contains(twoChars))
                        {
                            tokens.Add(new DaedalusToken(TokenType.Operator, twoChars, line + 1, column + 1));
                            column += 2;
                            continue;
                        }
                    }

                    // Einzelnes '=' als Assignment-Operator
                    if (current == '=')
                    {
                        tokens.Add(new DaedalusToken(TokenType.Assignment, "=", line + 1, column + 1));
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

                        // Präfix-Check
                        foreach (var prefix in prefixTokenTypes)
                        {
                            if (val.StartsWith(prefix.Key, StringComparison.Ordinal))
                            {
                                type = prefix.Value;
                                break;
                            }
                        }

                        if (specialKeywords.TryGetValue(val, out var keywordType))
                        {
                            type = keywordType;
                        }


                        if (type == TokenType.Unknown)
                        {
                            // Weitere Checks nur, wenn nicht schon erkannt
                            if (val.Equals("aivar", StringComparison.OrdinalIgnoreCase))
                                type = TokenType.AiVariable;
                            else if (val.Equals("MALE", StringComparison.Ordinal) || val.Equals("FEMALE", StringComparison.Ordinal))
                                type = TokenType.SexConstant;
                            else if (val.EndsWith("_ZEN", StringComparison.OrdinalIgnoreCase))
                                type = TokenType.ZENConstant;
                            else if (val.Equals("ZS_Talk", StringComparison.OrdinalIgnoreCase))
                                type = TokenType.REALConstant;
                            else if (OtherFunctions.Contains(val))
                                type = TokenType.EquipFunction;
                            else if (keywordsMap.TryGetValue(val, out var kwType))
                            {
                                type = kwType;
                                if (type == TokenType.InstanceKeyword)
                                    _expectInstanceName = true;
                            }
                            else if (_expectInstanceName)
                            {
                                type = TokenType.InstanceName;
                                _expectInstanceName = false;
                            }
                            else
                            {
                                type = TokenType.Identifier;
                            }
                        }

                        if (type != TokenType.InstanceKeyword)
                        {
                            _expectInstanceName = false;
                        }

                        tokens.Add(new DaedalusToken(type, val, line + 1, column + 1));
                        column += match.Length;
                        continue;
                    }

                    // Trennzeichen
                    if (singleCharTokenMap.TryGetValue(current, out var tokenType))
                    {
                        tokens.Add(new DaedalusToken(tokenType, current.ToString(), line + 1, column + 1));
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

            for (int i = 0; i < tokens.Count - 1; i++)
            {
                if (tokens[i].Type == TokenType.TypeKeyword &&
                    (string.Equals(tokens[i].Value, "void", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(tokens[i].Value, "int", StringComparison.OrdinalIgnoreCase))) // auch "int"
                {
                    var next = tokens[i + 1];
                    if (next.Type == TokenType.Identifier)
                    {
                        next.Type = TokenType.FunctionName;
                    }
                }
            }
            return tokens;
        }
    }
}
