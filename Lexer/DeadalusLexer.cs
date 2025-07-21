using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Skriptorium.Lexer
{
    public class DaedalusLexer
    {
        private static readonly string[] keywords = {
            "instance", "func", "class", "const", "var", "if", "else", "return",
            "prototype", "int", "float", "string", "void", "TRUE", "FALSE",
            "AI_Output", "B_Say_Gold", "Info_AddChoice", "Info_ClearChoices"
        };

        private static readonly Regex identifier = new(@"^[A-Za-z_][A-Za-z0-9_]*");
        private static readonly Regex floatLiteral = new(@"^\d+\.\d+");
        private static readonly Regex intLiteral = new(@"^\d+");
        private static readonly Regex strLiteral = new("^\".*?\"");
        private static readonly Regex lineComment = new(@"^//.*");
        private static readonly Regex blockCommentStart = new(@"^/\*");
        private static readonly Regex blockCommentEnd = new(@"\*/");

        private static readonly string[] multiCharOperators = {
            "==", "!=", "<=", ">=", "&&", "||"
        };

        private static readonly char[] singleCharOperators = {
            '+', '-', '*', '/', '%', '<', '>', '!', '='
        };

        private static readonly char[] brackets = { '{', '}' };
        private static readonly char[] parentheses = { '(', ')' };
        private static readonly char[] commas = { ',' };
        private static readonly char[] semicolons = { ';' };
        private static readonly char[] bracketsSquare = { '[', ']' };

        public List<Token> Tokenize(string[] lines)
        {
            var tokens = new List<Token>();
            bool inBlockComment = false;

            for (int line = 0; line < lines.Length; line++)
            {
                string text = lines[line];
                int column = 0;

                while (column < text.Length)
                {
                    if (inBlockComment)
                    {
                        int endIdx = text.IndexOf("*/", column);
                        if (endIdx >= 0)
                        {
                            tokens.Add(new Token
                            {
                                Type = TokenType.CommentBlock,
                                Value = text.Substring(column, endIdx + 2 - column),
                                Line = line + 1,
                                Column = column + 1
                            });
                            column = endIdx + 2;
                            inBlockComment = false;
                        }
                        else
                        {
                            tokens.Add(new Token
                            {
                                Type = TokenType.CommentBlock,
                                Value = text.Substring(column),
                                Line = line + 1,
                                Column = column + 1
                            });
                            break;
                        }
                        continue;
                    }

                    char current = text[column];
                    if (char.IsWhiteSpace(current))
                    {
                        // Optional: Tokens für Whitespace ausgeben, wenn gewünscht
                        // tokens.Add(new Token { Type = TokenType.Whitespace, Value = current.ToString(), Line = line + 1, Column = column + 1 });
                        column++;
                        continue;
                    }

                    string remaining = text.Substring(column);

                    // Line comment
                    if (lineComment.IsMatch(remaining))
                    {
                        tokens.Add(new Token
                        {
                            Type = TokenType.Comment,
                            Value = remaining,
                            Line = line + 1,
                            Column = column + 1
                        });
                        break;
                    }

                    // Block comment start
                    if (blockCommentStart.IsMatch(remaining))
                    {
                        int endIdx = remaining.IndexOf("*/");
                        if (endIdx >= 0)
                        {
                            tokens.Add(new Token
                            {
                                Type = TokenType.CommentBlock,
                                Value = remaining.Substring(0, endIdx + 2),
                                Line = line + 1,
                                Column = column + 1
                            });
                            column += endIdx + 2;
                        }
                        else
                        {
                            tokens.Add(new Token
                            {
                                Type = TokenType.CommentBlock,
                                Value = remaining,
                                Line = line + 1,
                                Column = column + 1
                            });
                            inBlockComment = true;
                            break;
                        }
                        continue;
                    }

                    // String literal
                    if (strLiteral.IsMatch(remaining))
                    {
                        var match = strLiteral.Match(remaining);
                        tokens.Add(new Token
                        {
                            Type = TokenType.StringLiteral,
                            Value = match.Value,
                            Line = line + 1,
                            Column = column + 1
                        });
                        column += match.Length;
                        continue;
                    }

                    // Float literal
                    if (floatLiteral.IsMatch(remaining))
                    {
                        var match = floatLiteral.Match(remaining);
                        tokens.Add(new Token
                        {
                            Type = TokenType.FloatLiteral,
                            Value = match.Value,
                            Line = line + 1,
                            Column = column + 1
                        });
                        column += match.Length;
                        continue;
                    }

                    // Integer literal
                    if (intLiteral.IsMatch(remaining))
                    {
                        var match = intLiteral.Match(remaining);
                        tokens.Add(new Token
                        {
                            Type = TokenType.IntegerLiteral,
                            Value = match.Value,
                            Line = line + 1,
                            Column = column + 1
                        });
                        column += match.Length;
                        continue;
                    }

                    // Multi-char operators
                    var multiOp = multiCharOperators.FirstOrDefault(op => remaining.StartsWith(op));
                    if (multiOp != null)
                    {
                        tokens.Add(new Token
                        {
                            Type = TokenType.Operator,
                            Value = multiOp,
                            Line = line + 1,
                            Column = column + 1
                        });
                        column += multiOp.Length;
                        continue;
                    }

                    // Identifiers / Keywords
                    if (identifier.IsMatch(remaining))
                    {
                        var match = identifier.Match(remaining);
                        var val = match.Value;
                        var type = keywords.Contains(val) ? TokenType.Keyword : TokenType.Identifier;

                        tokens.Add(new Token
                        {
                            Type = type,
                            Value = val,
                            Line = line + 1,
                            Column = column + 1
                        });
                        column += match.Length;
                        continue;
                    }

                    // Single-char operators
                    if (singleCharOperators.Contains(current))
                    {
                        tokens.Add(new Token
                        {
                            Type = TokenType.Operator,
                            Value = current.ToString(),
                            Line = line + 1,
                            Column = column + 1
                        });
                        column++;
                        continue;
                    }

                    // Separator: Klammern, Kommas, Semikolons
                    if (brackets.Contains(current))
                    {
                        tokens.Add(new Token
                        {
                            Type = TokenType.Bracket,
                            Value = current.ToString(),
                            Line = line + 1,
                            Column = column + 1
                        });
                        column++;
                        continue;
                    }

                    if (parentheses.Contains(current))
                    {
                        tokens.Add(new Token
                        {
                            Type = TokenType.Parenthesis,
                            Value = current.ToString(),
                            Line = line + 1,
                            Column = column + 1
                        });
                        column++;
                        continue;
                    }

                    if (commas.Contains(current))
                    {
                        tokens.Add(new Token
                        {
                            Type = TokenType.Comma,
                            Value = current.ToString(),
                            Line = line + 1,
                            Column = column + 1
                        });
                        column++;
                        continue;
                    }

                    if (semicolons.Contains(current))
                    {
                        tokens.Add(new Token
                        {
                            Type = TokenType.Semicolon,
                            Value = current.ToString(),
                            Line = line + 1,
                            Column = column + 1
                        });
                        column++;
                        continue;
                    }

                    if (bracketsSquare.Contains(current))
                    {
                        tokens.Add(new Token
                        {
                            Type = TokenType.Separator,
                            Value = current.ToString(),
                            Line = line + 1,
                            Column = column + 1
                        });
                        column++;
                        continue;
                    }

                    // Unbekannte Zeichen
                    tokens.Add(new Token
                    {
                        Type = TokenType.Unknown,
                        Value = current.ToString(),
                        Line = line + 1,
                        Column = column + 1
                    });
                    column++;
                }
            }

            return tokens;
        }
    }
}
