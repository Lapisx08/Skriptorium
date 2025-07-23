using Skriptorium.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skriptorium.Parsing
{
    // AST Node Definitionen
    public abstract class Declaration { }

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
    public abstract class Statement { }

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
    public abstract class Expression { }

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
            : base($"[Parse Error] {message} at {token.Line}:{token.Column}. Expected: {expected ?? "<any>"}, Found: '{token?.Value}'")
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
            Advance(); // 'instance'
            var nameToken = Consume(TokenType.Identifier, "Expect instance name");
            var instance = new InstanceDeclaration { Name = nameToken.Value };
            Consume(TokenType.Parenthesis, "Expect '('");
            var baseToken = Consume(TokenType.Identifier, "Expect base class");
            instance.BaseClass = baseToken.Value;
            Consume(TokenType.Parenthesis, "Expect ')'");
            Consume(TokenType.Bracket, "Expect '{'");

            while (!Check(TokenType.Bracket) && !IsAtEnd())
            {
                var stmt = ParseStatement();
                if (stmt != null) instance.Body.Add(stmt);
                else Advance();
            }

            Consume(TokenType.Bracket, "Expect '}'");
            if (Match(TokenType.Semicolon)) Advance(); // optional
            return instance;
        }

        private ClassDeclaration ParseClass()
        {
            Consume(TokenType.TypeKeyword, "Expect class keyword");
            var nameToken = Consume(TokenType.Identifier, "Expect class name");
            var cls = new ClassDeclaration { Name = nameToken.Value };
            Consume(TokenType.Bracket, "Expect '{' after class");

            while (!Check(TokenType.Bracket) && !IsAtEnd())
            {
                var stmt = ParseStatement();
                if (stmt != null) cls.Body.Add(stmt);
                else Advance();
            }

            Consume(TokenType.Bracket, "Expect '}' after class body");
            if (Match(TokenType.Semicolon)) Advance();
            return cls;
        }

        private PrototypeDeclaration ParsePrototype()
        {
            Advance(); // 'prototype'
            var signature = Consume(TokenType.Identifier, "Expect prototype signature").Value;
            Consume(TokenType.Semicolon, "Expect ';' after prototype");
            return new PrototypeDeclaration { Signature = signature };
        }

        private FunctionDeclaration ParseFunction()
        {
            Advance(); // 'func'

            var returnToken = Current().Type == TokenType.TypeKeyword || Current().Type == TokenType.EnumLiteral || Current().Type == TokenType.Identifier
                ? AdvanceAndGet()
                : throw new ParseException("Expect return type", Current());

            var func = new FunctionDeclaration { ReturnType = returnToken.Value };
            var nameToken = Consume(TokenType.Identifier, "Expect function name");
            func.Name = nameToken.Value;

            Consume(TokenType.Parenthesis, "Expect '('");

            if (!Check(TokenType.Parenthesis))
            {
                do
                {
                    bool isVar = false;
                    if (Match(TokenType.VarKeyword))
                    {
                        isVar = true;
                        Advance();
                    }

                    if (!(Current().Type == TokenType.TypeKeyword || Current().Type == TokenType.EnumLiteral || Current().Type == TokenType.Identifier))
                        throw new ParseException("Expect parameter type", Current());

                    var typeToken = AdvanceAndGet();
                    var paramName = Consume(TokenType.Identifier, "Expect parameter name").Value;
                    func.Parameters.Add($"{(isVar ? "var " : "")}{typeToken.Value} {paramName}");
                }
                while (Match(TokenType.Comma) && Advance() != null);
            }

            Consume(TokenType.Parenthesis, "Expect ')' after parameters");

            Consume(TokenType.Bracket, "Expect '{' before function body");
            while (!Check(TokenType.Bracket) && !IsAtEnd())
            {
                var stmt = ParseStatement();
                if (stmt != null) func.Body.Add(stmt);
                else Advance();
            }
            Consume(TokenType.Bracket, "Expect '}' after function body");
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
            Advance(); // 'var'
            var typeToken = Consume(TokenType.TypeKeyword, "Expect type name");
            var nameToken = Consume(TokenType.Identifier, "Expect variable name");
            Consume(TokenType.Semicolon, "Expect ';' after variable declaration");
            return new VarDeclaration { TypeName = typeToken.Value, Name = nameToken.Value };
        }

        private Statement ParseStatement()
        {
            if (Match(TokenType.IfKeyword)) return ParseIfStatement();
            if (Match(TokenType.ReturnKeyword)) return ParseReturnStatement();

            if (Check(TokenType.Identifier) && Peek(1).Type == TokenType.Assignment)
            {
                var nameToken = Consume(TokenType.Identifier, "Expect variable name");
                Consume(TokenType.Assignment, "Expect '=' after variable");
                var right = ParseExpression();
                Consume(TokenType.Semicolon, "Expect ';' after assignment");
                return new Assignment
                {
                    Left = new VariableExpression { Name = nameToken.Value },
                    Right = right
                };
            }

            var expr = ParseExpression();
            Consume(TokenType.Semicolon, "Expect ';' after expression");
            return new ExpressionStatement { Expr = expr };
        }

        private ReturnStatement ParseReturnStatement()
        {
            Advance(); // 'return'
            Expression expr = null;
            if (!Check(TokenType.Semicolon))
                expr = ParseExpression();
            Consume(TokenType.Semicolon, "Expect ';' after return");
            return new ReturnStatement { ReturnValue = expr };
        }

        private IfStatement ParseIfStatement()
        {
            Advance(); // 'if'
            Consume(TokenType.Parenthesis, "Expect '(' after if");
            var condition = ParseExpression();
            Consume(TokenType.Parenthesis, "Expect ')' after condition");

            Consume(TokenType.Bracket, "Expect '{' before if body");
            var thenBranch = new List<Statement>();
            while (!Check(TokenType.Bracket) && !IsAtEnd())
            {
                var stmt = ParseStatement();
                if (stmt != null) thenBranch.Add(stmt); else Advance();
            }
            Consume(TokenType.Bracket, "Expect '}' after if body");
            if (Match(TokenType.Semicolon)) Advance();

            var elseBranch = new List<Statement>();
            if (Match(TokenType.ElseKeyword))
            {
                Advance();
                Consume(TokenType.Bracket, "Expect '{' after else");
                while (!Check(TokenType.Bracket) && !IsAtEnd())
                {
                    var stmt = ParseStatement();
                    if (stmt != null) elseBranch.Add(stmt); else Advance();
                }
                Consume(TokenType.Bracket, "Expect '}' after else body");
                if (Match(TokenType.Semicolon)) Advance();
            }

            return new IfStatement { Condition = condition, ThenBranch = thenBranch, ElseBranch = elseBranch };
        }

        private Expression ParseExpression() => ParseBinary();

        private Expression ParseBinary(int parentPrec = 0)
        {
            Expression left = ParsePrimary();
            while (true)
            {
                int prec = GetPrecedence(Current());
                if (prec == 0 || prec <= parentPrec) break;

                var op = Current().Value;
                Advance();
                var right = ParseBinary(prec);
                left = new BinaryExpression { Left = left, Operator = op, Right = right };
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
                return new LiteralExpression { Value = tok.Value };
            }

            if (Match(TokenType.Identifier))
            {
                Advance();
                Expression expr = new VariableExpression { Name = tok.Value };

                if (Match(TokenType.Parenthesis) && Current().Value == "(")
                {
                    Advance();
                    var args = new List<Expression>();
                    if (!Check(TokenType.Parenthesis))
                    {
                        do { args.Add(ParseExpression()); }
                        while (Match(TokenType.Comma) && Advance() != null);
                    }
                    Consume(TokenType.Parenthesis, "Expect ')' after arguments");
                    expr = new FunctionCallExpression { FunctionName = tok.Value, Arguments = args };
                }

                while (Match(TokenType.Bracket) && Current().Value == "[")
                {
                    Advance();
                    var idx = ParseExpression();
                    Consume(TokenType.Bracket, "Expect ']' after index");
                    expr = new IndexExpression { Target = expr, Index = idx };
                }

                return expr;
            }

            if (Match(TokenType.Parenthesis) && tok.Value == "(")
            {
                Advance();
                var e = ParseExpression();
                Consume(TokenType.Parenthesis, "Expect ')' after grouped expression");
                return e;
            }

            throw new ParseException("Unexpected token", tok);
        }

        private int GetPrecedence(DaedalusToken tok)
        {
            if (tok?.Type != TokenType.Operator) return 0;
            return tok.Value switch
            {
                "*" or "/" => 3,
                "+" or "-" => 2,
                "==" or "!=" or "<" or ">" or "<=" or ">=" => 1,
                "&&" or "||" => 0,
                _ => 0,
            };
        }

        // Helpers
        private DaedalusToken Current() => _position < _tokens.Count ? _tokens[_position] : null;
        private DaedalusToken Advance() { if (_position < _tokens.Count) _position++; return Current(); }
        private DaedalusToken Peek(int offset) => _position + offset < _tokens.Count ? _tokens[_position + offset] : _tokens.Last();
        private bool Match(TokenType type) => Current()?.Type == type;
        private bool Check(TokenType type) => !IsAtEnd() && Current().Type == type;
        private DaedalusToken Consume(TokenType type, string message)
        {
            var tok = Current();
            if (tok == null || tok.Type != type)
                throw new ParseException(message, tok ?? new DaedalusToken(TokenType.EOF, "", -1, -1));
            Advance();
            return tok;
        }
        private bool IsAtEnd() => _position >= _tokens.Count;
    }
}
