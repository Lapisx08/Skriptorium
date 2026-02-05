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
            ["oth"] = TokenType.OthKeyword,
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
            '+', '-', '*', '/', '%', '<', '>', '!', '&', '|', '~'
        };

        private static readonly HashSet<string> multiCharOperators = new()
        {
            "==", "!=", "<=", ">=", "-=", "+=", "&&", "||", "<<", ">>"
        };

        private static readonly Regex identifier = new(@"^[\p{L}_][\p{L}\p{Nd}_]*", RegexOptions.Compiled);
        private static readonly Regex floatLiteral = new(@"^\d+\.\d+([eE][+-]?\d+)?", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex intLiteral = new(@"^\d+\.?", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex strLiteral = new(@"^""(?:\\.|[^""])*""", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex lineComment = new(@"^//.*", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex blockCommentStart = new(@"^/\*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly HashSet<string> OtherFunctions = new(StringComparer.OrdinalIgnoreCase)
        {
            "EquipItem",
            "PrintScreen"
        };

        private bool _expectInstanceName = false;
        private bool _inInstanceBaseContext = false;
        private bool _inFunctionParameters = false;

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
                        tokens.Add(new DaedalusToken(TokenType.Comment, remaining, line + 1, column + 1));
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
                        tokens.Add(new DaedalusToken(TokenType.StringLiteral, match.Value, line + 1, column + 1));
                        column += match.Length;
                        continue;
                    }

                    if (floatLiteral.IsMatch(remaining))
                    {
                        var match = floatLiteral.Match(remaining);
                        tokens.Add(new DaedalusToken(TokenType.FloatLiteral, match.Value, line + 1, column + 1));
                        column += match.Length;
                        continue;
                    }

                    if (intLiteral.IsMatch(remaining))
                    {
                        var match = intLiteral.Match(remaining);
                        tokens.Add(new DaedalusToken(TokenType.IntegerLiteral, match.Value, line + 1, column + 1));
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
                        tokens.Add(new DaedalusToken(TokenType.Operator, current.ToString(), line + 1, column + 1));
                        column++;
                        continue;
                    }

                    // =============================================
                    // Wichtigster Teil: Identifier
                    // =============================================
                    if (identifier.IsMatch(remaining))
                    {
                        var match = identifier.Match(remaining);
                        var val = match.Value;
                        TokenType type = TokenType.Identifier;

                        // 1. Höchste Priorität: Wir erwarten gerade einen Instanznamen
                        if (_expectInstanceName)
                        {
                            type = TokenType.InstanceName;
                            _expectInstanceName = false;

                            // ⚡ Prüfen, ob direkt nach dem Instanznamen '(' kommt → Base-Klasse folgt
                            int lookaheadIndex = column + match.Length;
                            string restOfLine = text.Substring(lookaheadIndex).TrimStart();
                            if (restOfLine.StartsWith("("))
                            {
                                _inInstanceBaseContext = true;
                            }
                        }
                        else
                        {
                            // Normale Erkennung nur, wenn kein Instanzname erwartet wird
                            // Spezielle Keywords (self, other, slf, oth...)
                            if (specialKeywords.TryGetValue(val, out var specialType))
                            {
                                type = specialType;
                            }
                            // Sprachelemente (func, var, instance, int, string, ...)
                            else if (keywordsMap.TryGetValue(val, out var keywordType))
                            {
                                // NEU: "instance" in Funktionsparametern als Identifier behandeln
                                if (_inFunctionParameters && val.Equals("instance", StringComparison.OrdinalIgnoreCase))
                                {
                                    type = TokenType.Identifier;
                                }
                                else
                                {
                                    type = keywordType;
                                    if (type == TokenType.InstanceKeyword)
                                    {
                                        _expectInstanceName = true;
                                    }
                                }
                            }
                            // Kontextabhängige Typen
                            else if (val.Equals("C_NPC", StringComparison.OrdinalIgnoreCase) &&
                                     tokens.Count > 0 &&
                                     (tokens[tokens.Count - 1].Value.Equals("var", StringComparison.OrdinalIgnoreCase) ||
                                      tokens[tokens.Count - 1].Value.Equals("instance", StringComparison.OrdinalIgnoreCase)))
                            {
                                type = TokenType.TypeKeyword;
                            }
                            else if (val.Equals("aivar", StringComparison.OrdinalIgnoreCase))
                            {
                                type = TokenType.AiVariable;
                            }
                            else if (val.Equals("MALE", StringComparison.Ordinal) || val.Equals("FEMALE", StringComparison.Ordinal))
                            {
                                type = TokenType.SexConstant;
                            }
                            else if (val.EndsWith("_ZEN", StringComparison.OrdinalIgnoreCase))
                            {
                                type = TokenType.ZENConstant;
                            }
                            else if (OtherFunctions.Contains(val))
                            {
                                type = TokenType.EquipFunction;
                            }
                            else
                            {
                                // Prefix-Prüfung (TA_, AI_, Npc_, etc.) nur wenn nichts anderes gepasst hat
                                bool isFunctionContext = false;
                                if (tokens.Count >= 2)
                                {
                                    var prev = tokens[tokens.Count - 1];
                                    var prevPrev = tokens.Count >= 2 ? tokens[tokens.Count - 2] : null;
                                    isFunctionContext = prevPrev != null &&
                                                       prevPrev.Type == TokenType.FuncKeyword &&
                                                       prev.Type == TokenType.TypeKeyword;
                                }
                                if (!isFunctionContext)
                                {
                                    foreach (var prefix in prefixTokenTypes)
                                    {
                                        // ⚡ Npc_ nur ignorieren, wenn wir gerade die Base-Klasse einer Instance parsen
                                        if (_inInstanceBaseContext && prefix.Key == "Npc_")
                                            continue;

                                        if (val.StartsWith(prefix.Key, StringComparison.Ordinal))
                                        {
                                            type = prefix.Value;
                                            break;
                                        }
                                    }
                                    // ⚡ Base-Klasse Flag direkt nach dem ersten Base-Token zurücksetzen
                                    if (_inInstanceBaseContext)
                                        _inInstanceBaseContext = false;
                                }
                                else
                                {
                                    // NEU: Wenn in Funktionskontext, als FunctionName setzen
                                    type = TokenType.FunctionName;
                                }
                            }
                        }

                        // Flag zurücksetzen, außer es war gerade das Keyword "instance"
                        if (type != TokenType.InstanceKeyword)
                        {
                            _expectInstanceName = false;
                        }

                        tokens.Add(new DaedalusToken(type, val, line + 1, column + 1));
                        column += match.Length;

                        // NEU: State-Management für Funktionsparameter
                        UpdateFunctionParameterState(tokens);

                        continue;
                    }

                    if (singleCharTokenMap.TryGetValue(current, out var tokenType))
                    {
                        tokens.Add(new DaedalusToken(tokenType, current.ToString(), line + 1, column + 1));
                        column++;

                        // NEU: State-Management auch bei Symbolen
                        UpdateFunctionParameterState(tokens);

                        continue;
                    }

                    tokens.Add(new DaedalusToken(TokenType.Unknown, current.ToString(), line + 1, column + 1));
                    column++;
                }
            }

            tokens.Add(new DaedalusToken(TokenType.EOF, "", lines.Length, 0));

            // ==================== POST-PROCESSING ====================
            // func → Rückgabetyp → Funktionsname
            for (int i = 0; i < tokens.Count - 1; i++)
            {
                if (tokens[i].Type == TokenType.FuncKeyword)
                {
                    if (i + 1 < tokens.Count && tokens[i + 1].Type == TokenType.Identifier)
                        tokens[i + 1].Type = TokenType.TypeKeyword;

                    if (i + 2 < tokens.Count)
                    {
                        var funcName = tokens[i + 2];
                        if (funcName.Type == TokenType.Identifier ||
                            funcName.Type == TokenType.BuiltInFunction ||
                            funcName.Type == TokenType.MdlFunction ||
                            funcName.Type == TokenType.AIFunction ||
                            funcName.Type == TokenType.NpcFunction ||
                            funcName.Type == TokenType.InfoFunction ||
                            funcName.Type == TokenType.CreateFunction ||
                            funcName.Type == TokenType.WldFunction ||
                            funcName.Type == TokenType.LogFunction ||
                            funcName.Type == TokenType.HlpFunction ||
                            funcName.Type == TokenType.SndFunction ||
                            funcName.Type == TokenType.TAFunction ||
                            funcName.Type == TokenType.EquipFunction)
                        {
                            funcName.Type = TokenType.FunctionName;
                        }
                    }
                }

                // var/const → Typ → Variablenname
                if (tokens[i].Type == TokenType.VarKeyword || tokens[i].Type == TokenType.ConstKeyword)
                {
                    if (i + 1 < tokens.Count && identifier.IsMatch(tokens[i + 1].Value))
                        tokens[i + 1].Type = TokenType.TypeKeyword;

                    if (i + 2 < tokens.Count && identifier.IsMatch(tokens[i + 2].Value))
                        tokens[i + 2].Type = TokenType.Identifier;
                }
            }

            // instance ... (BaseClass) → BaseClass zu Identifier machen, wenn es eine Konstante war
            for (int i = 0; i < tokens.Count - 2; i++)
            {
                if (tokens[i].Type == TokenType.InstanceKeyword)
                {
                    int j = i + 1;
                    while (j < tokens.Count && (tokens[j].Type == TokenType.InstanceName || tokens[j].Type == TokenType.Comma))
                        j++;

                    if (j < tokens.Count && tokens[j].Type == TokenType.OpenParenthesis)
                    {
                        int baseIdx = j + 1;
                        if (baseIdx < tokens.Count)
                        {
                            var baseToken = tokens[baseIdx];
                            if (baseToken.Type == TokenType.NPC_Constant ||
                                baseToken.Type == TokenType.AIVConstant ||
                                baseToken.Type == TokenType.ATRConstant ||
                                baseToken.Type == TokenType.PLAYERConstant ||
                                baseToken.Type == TokenType.ZENConstant ||
                                baseToken.Type == TokenType.SexConstant ||
                                baseToken.Type == TokenType.MAXConstant ||
                                baseToken.Type == TokenType.PROTConstant ||
                                baseToken.Type == TokenType.DAMConstant ||
                                baseToken.Type == TokenType.ITMConstant)
                            {
                                tokens[baseIdx] = new DaedalusToken(
                                    TokenType.Identifier,
                                    baseToken.Value,
                                    baseToken.Line,
                                    baseToken.Column
                                );
                            }
                        }
                    }
                }
            }

            return tokens;
        }

        private void UpdateFunctionParameterState(List<DaedalusToken> tokens)
        {
            if (tokens.Count < 2) return;

            var last = tokens[tokens.Count - 1];
            var prev = tokens[tokens.Count - 2];

            // Funktion beginnt: func/type FunctionName (
            if (prev.Type == TokenType.FunctionName && last.Type == TokenType.OpenParenthesis)
            {
                _inFunctionParameters = true;
            }

            // Funktion endet
            if (last.Type == TokenType.CloseParenthesis)
            {
                _inFunctionParameters = false;
                _expectInstanceName = false;
                _inInstanceBaseContext = false;
            }
        }
    }
}