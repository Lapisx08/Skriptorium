using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skriptorium.Lexer
{
    public enum TokenType
    {
        Keyword,
        Identifier,
        IntegerLiteral,
        FloatLiteral,
        StringLiteral,

        // Operatoren
        Operator,       // z.B. +, -, *, /, %, <, >, !
        Assignment,     // =

        // Symbole
        Separator,      // <<< HIER HINZUFÜGEN: für , ; ( ) { }

        // Symbole
        Bracket,        // { }
        Parenthesis,    // ( )
        Comma,          // ,
        Semicolon,      // ;

        // Kommentare
        Comment,        // Zeilenkommentar //
        CommentBlock,   // Blockkommentar /* */

        Whitespace,

        Unknown
    }

    public class Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }

        public override string ToString() => $"{Type}('{Value}') at {Line}:{Column}";
    }
}
