using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skriptorium.Lexer
{
    using System.Text.RegularExpressions;

    public class DaedalusLexer
    {
        private static readonly string[] keywords = {
        "instance", "func", "class", "const", "var", "if", "else", "return",
        "prototype", "int", "float", "string", "void", "AI_", "NPC_", "ITEM_", "C_"
    };

        private static readonly Regex identifier = new(@"^[A-Za-z_][A-Za-z0-9_]*");
        private static readonly Regex number = new(@"^\d+(\.\d+)?");
        private static readonly Regex strLiteral = new("^\".*?\"");
        private static readonly Regex comment = new(@"^//.*");

        public List<Token> Tokenize(string[] lines)
        {
            var tokens = new List<Token>();

            for (int line = 0; line < lines.Length; line++)
            {
                var text = lines[line];
                int column = 0;

                while (column < text.Length)
                {
                    char current = text[column];

                    // Whitespace
                    if (char.IsWhiteSpace(current))
                    {
                        column++;
                        continue;
                    }

                    string remaining = text.Substring(column);

                    // Comment
                    if (comment.IsMatch(remaining))
                    {
                        tokens.Add(new Token { Type = TokenType.Comment, Value = remaining, Line = line + 1, Column = column + 1 });
                        break; // Rest der Zeile ignorieren
                    }

                    // String
                    if (strLiteral.IsMatch(remaining))
                    {
                        var match = strLiteral.Match(remaining);
                        tokens.Add(new Token { Type = TokenType.StringLiteral, Value = match.Value, Line = line + 1, Column = column + 1 });
                        column += match.Length;
                        continue;
                    }

                    // Number
                    if (number.IsMatch(remaining))
                    {
                        var match = number.Match(remaining);
                        tokens.Add(new Token { Type = TokenType.IntegerLiteral, Value = match.Value, Line = line + 1, Column = column + 1 });
                        column += match.Length;
                        continue;
                    }

                    // Identifier or Keyword
                    if (identifier.IsMatch(remaining))
                    {
                        var match = identifier.Match(remaining);
                        string val = match.Value;
                        var type = keywords.Contains(val.ToLower()) ? TokenType.Keyword : TokenType.Identifier;
                        tokens.Add(new Token { Type = type, Value = val, Line = line + 1, Column = column + 1 });
                        column += val.Length;
                        continue;
                    }

                    // Operator or separator (rudimentär)
                    if ("{}();=+-*/%<>!".Contains(current))
                    {
                        tokens.Add(new Token { Type = TokenType.Operator, Value = current.ToString(), Line = line + 1, Column = column + 1 });
                        column++;
                        continue;
                    }

                    // Unknown
                    tokens.Add(new Token { Type = TokenType.Unknown, Value = current.ToString(), Line = line + 1, Column = column + 1 });
                    column++;
                }
            }

            return tokens;
        }
    }
}
