using Skriptorium.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skriptorium.Parsing
{
    // AST Node Definitionen
    public abstract class Declaration
    {
        public int Line { get; set; }
        public int Column { get; set; }
    }

    public class InstanceDeclaration : Declaration
    {
        public string Name { get; set; }
        public string BaseClass { get; set; }
        public List<Statement> Body { get; set; } = new List<Statement>();
    }

    public class FunctionDeclaration : Declaration
    {
        public string ReturnType { get; set; }
        public string Name { get; set; }
        public List<string> Parameters { get; set; } = new List<string>();
        public List<Statement> Body { get; set; } = new List<Statement>();
    }

    public class VarDeclaration : Declaration
    {
        public string TypeName { get; set; }
        public string Name { get; set; }
    }

    public class ClassDeclaration : Declaration
    {
        public string Name { get; set; }
        public List<Statement> Body { get; set; } = new List<Statement>();
    }

    public class PrototypeDeclaration : Declaration
    {
        public string Signature { get; set; }
    }

    // Statements
    public abstract class Statement
    {
        public int Line { get; set; }
        public int Column { get; set; }
    }

    public class Assignment : Statement
    {
        public Expression Left { get; set; }
        public Expression Right { get; set; }
    }

    public class ExpressionStatement : Statement
    {
        public Expression Expr { get; set; }
    }

    public class IfStatement : Statement
    {
        public Expression Condition { get; set; }
        public List<Statement> ThenBranch { get; set; } = new List<Statement>();
        public List<Statement> ElseBranch { get; set; } = new List<Statement>();
    }

    public class ReturnStatement : Statement
    {
        public Expression ReturnValue { get; set; }
    }

    // Expressionen
    public abstract class Expression
    {
        public int Line { get; set; }
        public int Column { get; set; }
    }

    public class LiteralExpression : Expression
    {
        public string Value { get; set; }
    }

    public class VariableExpression : Expression
    {
        public string Name { get; set; }
    }

    public class IndexExpression : Expression
    {
        public Expression Target { get; set; }
        public Expression Index { get; set; }
    }

    public class BinaryExpression : Expression
    {
        public Expression Left { get; set; }
        public string Operator { get; set; }
        public Expression Right { get; set; }
    }

    public class FunctionCallExpression : Expression
    {
        public string FunctionName { get; set; }
        public List<Expression> Arguments { get; set; } = new List<Expression>();
    }

    public class ParseException : Exception
    {
        public int Line { get; }
        public int Column { get; }
        public string Expected { get; }
        public string Found { get; }

        public ParseException(string message, DaedalusToken token, string expected = null)
            : base($"Syntax-Fehler: Zeile {token.Line}, Spalte {token.Column}. Erwartet: {expected ?? "<beliebig>"}, Gefunden: '{token?.Value}'")
        {
            Line = token?.Line ?? -1;
            Column = token?.Column ?? -1;
            Expected = expected;
            Found = token?.Value ?? "<null>";
        }
    }

    public class DaedalusParser
    {
        private readonly List<DaedalusToken> _tokens;
        private int _position;

        public DaedalusParser(IEnumerable<DaedalusToken> tokens)
        {
            _tokens = tokens
                .Where(t => t.Type != TokenType.Comment && t.Type != TokenType.CommentBlock)
                .ToList();
        }

        public List<Declaration> ParseScript()
        {
            var declarations = new List<Declaration>();
            while (!IsAtEnd())
            {
                var decl = ParseDeclaration();
                if (decl != null)
                    declarations.Add(decl);
                else
                    Advance();
            }
            return declarations;
        }

        private Declaration ParseDeclaration()
        {
            if (Match(TokenType.InstanceKeyword)) return ParseInstance();
            if (Match(TokenType.FuncKeyword)) return ParseFunction();
            if (Match(TokenType.VarKeyword)) return ParseVarDeclaration();
            if (Match(TokenType.TypeKeyword) && Peek(1).Type == TokenType.Identifier) return ParseClass();
            if (Match(TokenType.PrototypeKeyword)) return ParsePrototype();
            return null;
        }

        private InstanceDeclaration ParseInstance()
        {
            var startToken = Current();
            Advance(); // 'instance'
            var nameToken = Consume(TokenType.Identifier, "Instanzname erwartet");

            var instance = new InstanceDeclaration
            {
                Name = nameToken.Value,
                Line = startToken.Line,
                Column = startToken.Column
            };

            Consume(TokenType.Parenthesis, "'(' erwartet");
            var baseToken = Consume(TokenType.Identifier, "Basisklasse erwartet");
            instance.BaseClass = baseToken.Value;
            Consume(TokenType.Parenthesis, "')' erwartet");
            Consume(TokenType.Bracket, "'{' erwartet");

            while (!Check(TokenType.Bracket) && !IsAtEnd())
            {
                try
                {
                    var stmt = ParseStatement();
                    if (stmt != null)
                        instance.Body.Add(stmt);
                    else
                        Advance();
                }
                catch (ParseException ex)
                {
                    if (Current().Type == TokenType.Semicolon)
                    {
                        throw new ParseException("'}' vor ';' erwartet", Current(), "'}'");
                    }
                    else if (Current().Type == TokenType.EOF)
                    {
                        throw new ParseException("Unerwartetes Dateiende. '}' zum Schließen des Instanzblocks erwartet.", Current(), "'}'");
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            Consume(TokenType.Bracket, "'}' erwartet");
            if (Match(TokenType.Semicolon)) Advance();

            return instance;
        }

        private ClassDeclaration ParseClass()
        {
            var startToken = Current();
            Consume(TokenType.TypeKeyword, "Typenschlüsselwort 'type' erwartet");
            var nameToken = Consume(TokenType.Identifier, "Klassenname erwartet");
            var cls = new ClassDeclaration
            {
                Name = nameToken.Value,
                Line = startToken.Line,
                Column = startToken.Column
            };
            Consume(TokenType.Bracket, "'{' nach Klassendefinition erwartet");

            while (!Check(TokenType.Bracket) && !IsAtEnd())
            {
                var stmt = ParseStatement();
                if (stmt != null) cls.Body.Add(stmt);
                else Advance();
            }

            Consume(TokenType.Bracket, "'}' nach Klassenrumpf erwartet");
            if (Match(TokenType.Semicolon)) Advance();
            return cls;
        }

        private PrototypeDeclaration ParsePrototype()
        {
            var startToken = Current();
            Advance(); // 'prototype'
            var signature = Consume(TokenType.Identifier, "Prototyp-Signatur erwartet").Value;
            Consume(TokenType.Semicolon, "';' nach Prototyp erwartet");
            return new PrototypeDeclaration
            {
                Signature = signature,
                Line = startToken.Line,
                Column = startToken.Column
            };
        }

        private FunctionDeclaration ParseFunction()
        {
            var startToken = Current();
            Advance(); // 'func'

            var returnToken = Current().Type == TokenType.TypeKeyword || Current().Type == TokenType.EnumLiteral || Current().Type == TokenType.Identifier
                ? AdvanceAndGet()
                : throw new ParseException("Rückgabetyp erwartet", Current());

            var func = new FunctionDeclaration
            {
                ReturnType = returnToken.Value,
                Line = startToken.Line,
                Column = startToken.Column
            };
            var nameToken = Consume(TokenType.Identifier, "Funktionsname erwartet");
            func.Name = nameToken.Value;

            Consume(TokenType.Parenthesis, "'(' erwartet");

            if (!Check(TokenType.Parenthesis))
            {
                while (true)
                {
                    bool isVar = false;
                    if (Match(TokenType.VarKeyword))
                    {
                        isVar = true;
                        Advance();
                    }

                    if (!(Current().Type == TokenType.TypeKeyword || Current().Type == TokenType.EnumLiteral || Current().Type == TokenType.Identifier))
                        throw new ParseException("Parametertyp erwartet", Current());

                    var typeToken = AdvanceAndGet();
                    var paramName = Consume(TokenType.Identifier, "Parametername erwartet").Value;
                    func.Parameters.Add($"{(isVar ? "var " : "")}{typeToken.Value} {paramName}");

                    if (Match(TokenType.Comma))
                        Advance();
                    else
                        break;
                }
            }

            Consume(TokenType.Parenthesis, "')' nach Parametern erwartet");
            Consume(TokenType.Bracket, "'{' vor Funktionsrumpf erwartet");
            while (!Check(TokenType.Bracket) && !IsAtEnd())
            {
                var stmt = ParseStatement();
                if (stmt != null) func.Body.Add(stmt);
                else Advance();
            }
            Consume(TokenType.Bracket, "'}' nach Funktionsrumpf erwartet");
            if (Match(TokenType.Semicolon)) Advance();

            return func;
        }

        private DaedalusToken AdvanceAndGet()
        {
            var tok = Current();
            Advance();
            return tok;
        }

        private VarDeclaration ParseVarDeclaration()
        {
            var startToken = Current();
            Advance(); // 'var'
            var typeToken = Consume(TokenType.TypeKeyword, "Typname erwartet");
            var nameToken = Consume(TokenType.Identifier, "Variablenname erwartet");
            Consume(TokenType.Semicolon, "';' nach Variablendeklaration erwartet");
            return new VarDeclaration
            {
                TypeName = typeToken.Value,
                Name = nameToken.Value,
                Line = startToken.Line,
                Column = startToken.Column
            };
        }

        private Statement ParseStatement()
        {
            if (Match(TokenType.IfKeyword)) return ParseIfStatement();
            if (Match(TokenType.ReturnKeyword)) return ParseReturnStatement();

            int startPos = _position;

            try
            {
                var lhs = ParseExpression();

                if (Match(TokenType.Assignment))
                {
                    var assignToken = Current();
                    Advance(); // '='
                    var rhs = ParseExpression();
                    Consume(TokenType.Semicolon, "';' nach Zuweisung erwartet");

                    var assignment = new Assignment
                    {
                        Left = lhs,
                        Right = rhs,
                        Line = assignToken.Line,
                        Column = assignToken.Column
                    };

                    return assignment;
                }

                _position = startPos;
            }
            catch
            {
                _position = startPos;
            }

            var expr = ParseExpression();
            var exprToken = Current() ?? new DaedalusToken(TokenType.EOF, "", -1, -1);
            Consume(TokenType.Semicolon, "';' nach Ausdruck erwartet");
            return new ExpressionStatement
            {
                Expr = expr,
                Line = expr.Line,
                Column = expr.Column
            };
        }

        private ReturnStatement ParseReturnStatement()
        {
            var startToken = Current();
            Advance(); // 'return'
            Expression expr = null;
            if (!Check(TokenType.Semicolon))
                expr = ParseExpression();
            Consume(TokenType.Semicolon, "';' nach return erwartet");
            return new ReturnStatement
            {
                ReturnValue = expr,
                Line = startToken.Line,
                Column = startToken.Column
            };
        }

        private IfStatement ParseIfStatement()
        {
            var startToken = Current();
            Advance(); // 'if'
            Consume(TokenType.Parenthesis, "'(' nach if erwartet");
            var condition = ParseExpression();
            Consume(TokenType.Parenthesis, "')' nach Bedingung erwartet");

            Consume(TokenType.Bracket, "'{' vor if-Block erwartet");
            var thenBranch = new List<Statement>();
            while (!Check(TokenType.Bracket) && !IsAtEnd())
            {
                var stmt = ParseStatement();
                if (stmt != null) thenBranch.Add(stmt); else Advance();
            }
            Consume(TokenType.Bracket, "'}' nach if-Block erwartet");
            if (Match(TokenType.Semicolon)) Advance();

            var elseBranch = new List<Statement>();
            if (Match(TokenType.ElseKeyword))
            {
                Advance();
                Consume(TokenType.Bracket, "'{' nach else erwartet");
                while (!Check(TokenType.Bracket) && !IsAtEnd())
                {
                    var stmt = ParseStatement();
                    if (stmt != null) elseBranch.Add(stmt); else Advance();
                }
                Consume(TokenType.Bracket, "'}' nach else-Block erwartet");
                if (Match(TokenType.Semicolon)) Advance();
            }

            return new IfStatement
            {
                Condition = condition,
                ThenBranch = thenBranch,
                ElseBranch = elseBranch,
                Line = startToken.Line,
                Column = startToken.Column
            };
        }

        private Expression ParseExpression() => ParseBinary();

        private Expression ParseBinary(int parentPrec = 0)
        {
            Expression left = ParsePrimary();
            while (true)
            {
                int prec = GetPrecedence(Current());
                if (prec == 0 || prec <= parentPrec) break;

                var opToken = Current();
                var op = opToken.Value;
                Advance();
                var right = ParseBinary(prec);
                left = new BinaryExpression
                {
                    Left = left,
                    Operator = op,
                    Right = right,
                    Line = opToken.Line,
                    Column = opToken.Column
                };
            }
            return left;
        }

        private Expression ParsePrimary()
        {
            var tok = Current();

            if (Match(TokenType.IntegerLiteral) || Match(TokenType.FloatLiteral) ||
                Match(TokenType.StringLiteral) || Match(TokenType.BoolLiteral) || Match(TokenType.EnumLiteral))
            {
                Advance();
                return new LiteralExpression
                {
                    Value = tok.Value,
                    Line = tok.Line,
                    Column = tok.Column
                };
            }

            if (Match(TokenType.Identifier))
            {
                Advance();
                Expression expr = new VariableExpression
                {
                    Name = tok.Value,
                    Line = tok.Line,
                    Column = tok.Column
                };

                if (Match(TokenType.Parenthesis) && Current().Value == "(")
                {
                    Advance(); // consume '('
                    var args = new List<Expression>();

                    if (!Check(TokenType.Parenthesis)) // not ')'
                    {
                        while (true)
                        {
                            args.Add(ParseExpression());

                            if (Match(TokenType.Comma))
                                Advance();
                            else
                                break;
                        }
                    }

                    Consume(TokenType.Parenthesis, "')' nach Argumenten erwartet");

                    expr = new FunctionCallExpression
                    {
                        FunctionName = tok.Value,
                        Arguments = args,
                        Line = tok.Line,
                        Column = tok.Column
                    };
                }


                while (Match(TokenType.Bracket) && Current().Value == "[")
                {
                    Advance();
                    var idx = ParseExpression();
                    Consume(TokenType.Bracket, "']' nach Index erwartet");
                    expr = new IndexExpression
                    {
                        Target = expr,
                        Index = idx,
                        Line = tok.Line,
                        Column = tok.Column
                    };
                }

                return expr;
            }

            throw new ParseException("Unerwartetes Token in Ausdruck", tok);
        }

        private int GetPrecedence(DaedalusToken token)
        {
            if (token == null) return 0;
            return token.Type switch
            {
                TokenType.Operator when token.Value == "||" => 1,
                TokenType.Operator when token.Value == "&&" => 2,
                TokenType.Operator when token.Value == "==" || token.Value == "!=" => 3,
                TokenType.Operator when token.Value == "<" || token.Value == ">" || token.Value == "<=" || token.Value == ">=" => 4,
                TokenType.Operator when token.Value == "+" || token.Value == "-" => 5,
                TokenType.Operator when token.Value == "*" || token.Value == "/" || token.Value == "%" => 6,
                _ => 0
            };
        }

        private bool Match(TokenType type)
        {
            var tok = Current();
            return tok != null && tok.Type == type;
        }

        private bool Check(TokenType type)
        {
            var tok = Current();
            return tok != null && tok.Type == type;
        }

        private DaedalusToken Current()
        {
            if (_position >= _tokens.Count) return new DaedalusToken(TokenType.EOF, "", -1, -1);
            return _tokens[_position];
        }

        private DaedalusToken Peek(int offset)
        {
            int pos = _position + offset;
            if (pos >= _tokens.Count) return new DaedalusToken(TokenType.EOF, "", -1, -1);
            return _tokens[pos];
        }

        private DaedalusToken Consume(TokenType type, string message)
        {
            var tok = Current();
            if (tok.Type != type)
            {
                string expectedSymbol = type switch
                {
                    TokenType.Semicolon => "';'",
                    TokenType.Bracket => Current().Value == "{" ? "'{'" : "'}'",
                    TokenType.Parenthesis => Current().Value == "(" ? "'('" : "')'",
                    TokenType.Comma => "','",
                    TokenType.Assignment => "'='",
                    TokenType.TypeKeyword => "type",
                    TokenType.FuncKeyword => "func",
                    TokenType.VarKeyword => "var",
                    TokenType.IfKeyword => "if",
                    TokenType.ElseKeyword => "else",
                    TokenType.ReturnKeyword => "return",
                    TokenType.PrototypeKeyword => "prototype",
                    TokenType.Identifier => "identifier",
                    // Weitere TokenTypes nach Bedarf
                    _ => type.ToString()
                };
                throw new ParseException(message, tok, expectedSymbol);
            }
            Advance();
            return tok;
        }

        private void Advance() => _position++;
        private bool IsAtEnd() => _position >= _tokens.Count;
    }
}