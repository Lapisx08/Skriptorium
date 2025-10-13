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
            ["true"] = TokenType.BoolLiteral,
            ["false"] = TokenType.BoolLiteral,
        };

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
            ["MAX_"] = TokenType.MAXConstant,
            ["PROT_"] = TokenType.PROTConstant,
            ["DAM_"] = TokenType.DAMConstant,
            ["ITM_"] = TokenType.ITMConstant,
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

        private static readonly HashSet<char> singleCharOperators = new()
        {
            '+', '-', '*', '/', '%', '<', '>', '!'
        };

        private static readonly HashSet<string> multiCharOperators = new()
        {
            "==", "!=", "<=", ">=", "&&", "||"
        };

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

                    if (lineComment.IsMatch(remaining))
                    {
                        tokens.Add(new DaedalusToken(TokenType.Comment,
                            remaining, line + 1, column + 1));
                        break;
                    }

                    if (blockCommentStart.IsMatch(remaining))
                    {
                        int endIdx = text.IndexOf("*/", column, StringComparison.Ordinal);
                        if (endIdx >= 0)
                        {
                            tokens.Add(new DaedalusToken(TokenType.CommentBlock,
                                remaining.Substring(0, endIdx + 2 - column),
                                line + 1, column + 1));
                            column = endIdx + 2;
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

                    if (strLiteral.IsMatch(remaining))
                    {
                        var match = strLiteral.Match(remaining);
                        tokens.Add(new DaedalusToken(TokenType.StringLiteral,
                            match.Value, line + 1, column + 1));
                        column += match.Length;
                        continue;
                    }

                    if (floatLiteral.IsMatch(remaining))
                    {
                        var match = floatLiteral.Match(remaining);
                        tokens.Add(new DaedalusToken(TokenType.FloatLiteral,
                            match.Value, line + 1, column + 1));
                        column += match.Length;
                        continue;
                    }

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

                    if (current == '=')
                    {
                        tokens.Add(new DaedalusToken(TokenType.Assignment, "=", line + 1, column + 1));
                        column++;
                        continue;
                    }

                    if (singleCharOperators.Contains(current))
                    {
                        tokens.Add(new DaedalusToken(TokenType.Operator,
                            current.ToString(), line + 1, column + 1));
                        column++;
                        continue;
                    }

                    if (identifier.IsMatch(remaining))
                    {
                        var match = identifier.Match(remaining);
                        var val = match.Value;
                        TokenType type = TokenType.Unknown;

                        if (val.Equals("Npc_Default", StringComparison.OrdinalIgnoreCase))
                        {
                            type = TokenType.Identifier;
                        }
                        else
                        {
                            foreach (var prefix in prefixTokenTypes)
                            {
                                if (val.StartsWith(prefix.Key, StringComparison.Ordinal))
                                {
                                    type = prefix.Value;
                                    break;
                                }
                            }
                        }

                        if (specialKeywords.TryGetValue(val, out var keywordType))
                        {
                            type = keywordType;
                        }

                        if (type == TokenType.Unknown)
                        {
                            if (val.Equals("C_NPC", StringComparison.OrdinalIgnoreCase) &&
                                tokens.Count > 0 &&
                                (tokens[tokens.Count - 1].Value.Equals("var", StringComparison.OrdinalIgnoreCase) ||
                                 tokens[tokens.Count - 1].Value.Equals("int", StringComparison.OrdinalIgnoreCase)))
                            {
                                type = TokenType.TypeKeyword;
                            }
                            else if (val.Equals("aivar", StringComparison.OrdinalIgnoreCase))
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

                    if (singleCharTokenMap.TryGetValue(current, out var tokenType))
                    {
                        tokens.Add(new DaedalusToken(tokenType, current.ToString(), line + 1, column + 1));
                        column++;
                        continue;
                    }

                    tokens.Add(new DaedalusToken(TokenType.Unknown,
                        current.ToString(), line + 1, column + 1));
                    column++;
                }
            }

            tokens.Add(new DaedalusToken(TokenType.EOF, "", lines.Length, 0));

            // Nachbearbeitung: Korrigiere Token-Typen basierend auf Kontext
            for (int i = 1; i < tokens.Count - 1; i++)
            {
                if (tokens[i].Type == TokenType.FuncKeyword && tokens[i].Value.Equals("func", StringComparison.OrdinalIgnoreCase))
                {
                    var prev = tokens[i - 1];
                    var next = tokens[i + 1];
                    if (prev.Type == TokenType.VarKeyword)
                    {
                        // Nach 'VAR', z. B. 'VAR FUNC mission', wird 'func' zu TypeKeyword
                        tokens[i].Type = TokenType.TypeKeyword;
                        if (next.Type == TokenType.Identifier || next.Type == TokenType.BuiltInFunction)
                        {
                            next.Type = TokenType.Identifier; // Nächster Token ist Variablenname
                        }
                    }
                    else if (prev.Type == TokenType.FuncKeyword)
                    {
                        // Nach 'FUNC', z. B. 'FUNC FUNC myFunc', bleibt 'func' FuncKeyword
                        if (next.Type == TokenType.Identifier || next.Type == TokenType.BuiltInFunction)
                        {
                            next.Type = TokenType.FunctionName; // Nächster Token ist Funktionsname
                        }
                    }
                }
                else if (tokens[i].Type == TokenType.TypeKeyword)
                {
                    var prev = tokens[i - 1];
                    var next = tokens[i + 1];
                    if (prev.Type == TokenType.VarKeyword || prev.Type == TokenType.ConstKeyword)
                    {
                        // Nach 'VAR TypeKeyword' oder 'CONST TypeKeyword', z. B. 'VAR INT aivar' oder 'CONST INT aivar'
                        if (identifier.IsMatch(next.Value))
                        {
                            next.Type = TokenType.Identifier; // Nächster Token ist Variablenname, unabhängig vom ursprünglichen Typ
                        }
                    }
                    else if (prev.Type == TokenType.FuncKeyword)
                    {
                        if (next.Type == TokenType.Identifier || next.Type == TokenType.BuiltInFunction)
                        {
                            next.Type = TokenType.FunctionName; // Nächster Token ist Funktionsname
                        }
                    }
                }
            }
            return tokens;
        }
    }
}