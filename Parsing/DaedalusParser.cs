using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Skriptorium.Parsing
{
    public abstract class Declaration
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; }
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
        public string ArraySize { get; set; }
    }

    public class ConstDeclaration : Declaration
    {
        public string TypeName { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string ArraySize { get; set; }
    }

    public class MultiVarDeclaration : Declaration
    {
        public List<VarDeclaration> Declarations { get; set; } = new List<VarDeclaration>();
    }

    public class MultiConstDeclaration : Declaration
    {
        public List<ConstDeclaration> Declarations { get; set; } = new List<ConstDeclaration>();
    }

    public class ClassDeclaration : Declaration
    {
        public string Name { get; set; }
        public List<Statement> Body { get; set; } = new List<Statement>();
        public List<Declaration> Declarations { get; set; } = new List<Declaration>();
    }

    public class PrototypeDeclaration : Declaration
    {
        public string Signature { get; set; }

        // Optional: Body, falls Prototyp einen Block hat
        public List<Statement> Body { get; set; } = null;
    }

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

    public class VarDeclarationStatement : Statement
    {
        public VarDeclaration Declaration { get; set; }
    }

    public class MultiVarDeclarationStatement : Statement
    {
        public List<VarDeclarationStatement> Declarations { get; set; } = new List<VarDeclarationStatement>();
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

    public class MemberExpression : Expression
    {
        public Expression Object { get; set; }
        public string MemberName { get; set; }
    }

    public class UnaryExpression : Expression
    {
        public string Operator { get; set; }
        public Expression Operand { get; set; }
    }

    public class ParseException : Exception
    {
        public int Line { get; }
        public int Column { get; }
        public string Expected { get; }
        public string Found { get; }

        public ParseException(string message, DaedalusToken token, string expected = null)
            : base(string.Format(
                Application.Current.TryFindResource("ErrSyntax") as string
                    ?? "Syntax-Fehler: Zeile {0}, Spalte {1}. Erwartet: {2}, Gefunden: '{3}'",
                token?.Line ?? -1,
                token?.Column ?? -1,
                expected
                    ?? (Application.Current.TryFindResource("ErrExpectedAny") as string ?? "<beliebig>"),
                token?.Value
                    ?? (Application.Current.TryFindResource("ErrTokenNull") as string ?? "<null>")
            ))
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
        public List<ParseException> Errors = new List<ParseException>();

        public DaedalusParser(IEnumerable<DaedalusToken> tokens)
        {
            _tokens = tokens
                .Where(t => t.Type != TokenType.Comment && t.Type != TokenType.CommentBlock)
                .ToList();
        }

        public List<Declaration> ParseScript()
        {
            var declarations = new List<Declaration>();
            int lastPosition = -1;
            int recursionGuard = 0;

            while (!IsAtEnd())
            {
                // Sicherung: Wenn wir uns nicht vorwärts bewegen, erzwingen wir Fortschritt
                if (_position == lastPosition)
                {
                    recursionGuard++;
                    if (recursionGuard > 10)
                    {
                        Advance();
                        recursionGuard = 0;
                    }
                }
                else
                {
                    recursionGuard = 0;
                }
                lastPosition = _position;

                try
                {
                    declarations.Add(ParseDeclaration());
                }
                catch (ParseException ex)
                {
                    Errors.Add(ex);
                    Synchronize();
                }
                catch (Exception ex)
                {
                    // Fängt unerwartete Abstürze (z.B. NullReference) ab
                    System.Diagnostics.Debug.WriteLine($"Unerwarteter Fehler: {ex.Message}");
                    Synchronize();
                }
            }
            return declarations;
        }

        private void Synchronize()
        {
            // Falls wir schon am Ende sind, nichts tun
            if (IsAtEnd()) return;

            // Wir merken uns die Position vor dem Vorrücken
            int startPos = _position;
            Advance();

            while (!IsAtEnd())
            {
                // 1. Check: Haben wir ein Semikolon gerade hinter uns gelassen?
                if (_position > 0 && _tokens[_position - 1].Type == TokenType.Semicolon)
                    return;

                // 2. Check: Stehen wir vor einem neuen Block?
                switch (Peek(0).Type)
                {
                    case TokenType.FuncKeyword:
                    case TokenType.InstanceKeyword:
                    case TokenType.PrototypeKeyword:
                    case TokenType.ConstKeyword:
                    case TokenType.VarKeyword:
                        return;
                }

                Advance();

                // Sicherheits-Check: Falls Advance nicht vorwärts kommt (sollte nicht passieren)
                if (_position <= startPos) break;
            }
        }

        private Declaration ParseDeclaration()
        {
            // FEHLER WAR: if (Match(TokenType.InstanceKeyword)) return new InstanceDeclaration();
            // KORREKTUR: Die Methode aufrufen, die auch Namen und Basisklasse parst!
            if (Match(TokenType.InstanceKeyword))
            {
                var instances = ParseInstanceDeclaration();
                // Da ParseInstanceDeclaration eine Liste zurückgibt (wegen Komma-Trennung),
                // nehmen wir hier das erste Element oder passen die Logik an.
                return instances.FirstOrDefault();
            }

            if (Match(TokenType.FuncKeyword)) return ParseFunction();

            // NEU: Wenn eine "VAR" - Deklaration wie "VAR <Type> <Name> (" aussieht,
            // dann handelt es sich um eine Funktions-/Prototyp-Signatur mit führendem VAR.
            if (Match(TokenType.VarKeyword))
            {
                var t1 = Peek(1);
                var t2 = Peek(2);
                var t3 = Peek(3);
                if ((t1.Type == TokenType.TypeKeyword || t1.Type == TokenType.Identifier) &&
                    (t2.Type == TokenType.FunctionName || t2.Type == TokenType.Identifier) &&
                    t3.Type == TokenType.OpenParenthesis)
                {
                    return ParseFunctionWithVarPrefix();
                }

                return ParseVarDeclaration();
            }

            if (Match(TokenType.ConstKeyword)) return ParseConstDeclaration();
            if (Match(TokenType.ClassKeyword) && Peek(1).Type == TokenType.Identifier) return ParseClass();
            if (Match(TokenType.PrototypeKeyword)) return ParsePrototype();
            return null;
        }

        // NEU: Parst Funktionsdeklarationen, die mit "VAR <Type> <Name>(...)" beginnen.
        private FunctionDeclaration ParseFunctionWithVarPrefix()
        {
            var startToken = Current();
            Advance(); // consume VAR

            var returnToken = (Current().Type == TokenType.TypeKeyword || Current().Type == TokenType.Identifier)
                ? AdvanceAndGet()
                : throw new ParseException("Rückgabetyp erwartet", Current());

            var func = new FunctionDeclaration
            {
                ReturnType = returnToken.Value,
                Line = startToken.Line,
                Column = startToken.Column
            };

            // Funktionsname kann als FunctionName oder Identifier tokenisiert sein.
            DaedalusToken nameToken;
            if (Current().Type == TokenType.FunctionName || Current().Type == TokenType.Identifier)
                nameToken = AdvanceAndGet();
            else
                throw new ParseException("Funktionsname erwartet", Current());

            func.Name = nameToken.Value;

            Consume(TokenType.OpenParenthesis, "'(' erwartet");

            if (!Check(TokenType.CloseParenthesis))
            {
                while (true)
                {
                    bool isVar = false;
                    if (Match(TokenType.VarKeyword))
                    {
                        isVar = true;
                        Advance();
                    }

                    if (!(Current().Type == TokenType.TypeKeyword || Current().Type == TokenType.Identifier))
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

            Consume(TokenType.CloseParenthesis, "')' nach Parametern erwartet");

            // Funktionsrumpf optional (Prototypen)
            if (Check(TokenType.OpenBracket))
            {
                Consume(TokenType.OpenBracket, "'{' vor Funktionsrumpf erwartet");
                while (!Check(TokenType.CloseBracket) && !IsAtEnd())
                {
                    var stmt = ParseStatement();
                    if (stmt != null) func.Body.Add(stmt);
                    else Advance();
                }
                Consume(TokenType.CloseBracket, "'}' nach Funktionsrumpf erwartet");
            }

            // optionales Semikolon (Prototyp)
            if (Match(TokenType.Semicolon)) Advance();

            return func;
        }

        private List<InstanceDeclaration> ParseInstanceDeclaration()
        {
            var startToken = Current();
            Advance();

            var instanceDecls = new List<InstanceDeclaration>();
            var names = new List<(string Name, int Line, int Column)>();

            while (true)
            {
                var nameToken = Current();
                if (!(Match(TokenType.InstanceName) || Match(TokenType.SelfKeyword) ||
                      Match(TokenType.OtherKeyword) || Match(TokenType.SlfKeyword) || Match(TokenType.OthKeyword)))
                {
                    throw new ParseException("Instanzname oder spezielles Schlüsselwort (self, other, hero, slf, oth) erwartet", nameToken, "instance name");
                }
                names.Add((nameToken.Value, nameToken.Line, nameToken.Column));
                Advance();

                if (!Match(TokenType.Comma))
                    break;
                Advance();
            }

            Consume(TokenType.OpenParenthesis, "'(' erwartet");
            var baseToken = Consume(TokenType.Identifier, "Basisklasse erwartet");
            string baseClass = baseToken.Value;
            Consume(TokenType.CloseParenthesis, "')' erwartet");

            var body = new List<Statement>();
            if (Match(TokenType.OpenBracket))
            {
                Advance();
                while (!Check(TokenType.CloseBracket) && !IsAtEnd())
                {
                    try
                    {
                        var stmt = ParseStatement();
                        if (stmt != null)
                            body.Add(stmt);
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
                Consume(TokenType.CloseBracket, "'}' erwartet");
            }

            if (Match(TokenType.Semicolon)) Advance();

            foreach (var (name, line, column) in names)
            {
                instanceDecls.Add(new InstanceDeclaration
                {
                    Name = name,
                    BaseClass = baseClass,
                    Body = new List<Statement>(body),
                    Line = startToken.Line,
                    Column = startToken.Column
                });
            }

            return instanceDecls;
        }

        private ClassDeclaration ParseClass()
        {
            var startToken = Current();
            Consume(TokenType.ClassKeyword, "Typenschlüsselwort 'class' erwartet");
            var nameToken = Consume(TokenType.Identifier, "Klassenname erwartet");
            var cls = new ClassDeclaration
            {
                Name = nameToken.Value,
                Line = startToken.Line,
                Column = startToken.Column
            };
            Consume(TokenType.OpenBracket, "'{' nach Klassendefinition erwartet");

            while (!Check(TokenType.CloseBracket) && !IsAtEnd())
            {
                if (Match(TokenType.VarKeyword))
                {
                    var varDecl = ParseVarDeclaration();
                    cls.Declarations.Add(varDecl);
                }
                else if (Match(TokenType.ConstKeyword))
                {
                    var constDecl = ParseConstDeclaration();
                    cls.Declarations.Add(constDecl);
                }
                else
                {
                    var stmt = ParseStatement();
                    if (stmt != null) cls.Body.Add(stmt);
                    else Advance();
                }
            }

            Consume(TokenType.CloseBracket, "'}' nach Klassenrumpf erwartet");
            if (Match(TokenType.Semicolon)) Advance();
            return cls;
        }

        private PrototypeDeclaration ParsePrototype()
        {
            var startToken = Current();
            Advance();

            var signatureToken = Consume(TokenType.Identifier, "Prototyp-Signatur erwartet");
            string signature = signatureToken.Value;

            // Optional: Basisklasse in Klammern
            if (Match(TokenType.OpenParenthesis))
            {
                Advance();
                var baseToken = Consume(TokenType.Identifier, "Basisklasse erwartet");
                signature += $"({baseToken.Value})";
                Consume(TokenType.CloseParenthesis, "')' nach Basisklasse erwartet");
            }

            // Entweder Semikolon oder Block
            List<Statement> body = null;
            if (Match(TokenType.Semicolon))
            {
                Advance(); // Einfache Prototyp-Signatur
            }
            else if (Match(TokenType.OpenBracket))
            {
                Advance(); // '{'
                body = new List<Statement>();

                while (!Check(TokenType.CloseBracket) && !IsAtEnd())
                {
                    var stmt = ParseStatement();
                    if (stmt != null)
                        body.Add(stmt);
                    else
                        Advance();
                }

                Consume(TokenType.CloseBracket, "'}' nach Prototyp-Block erwartet");
            }
            else
            {
                throw new ParseException("';' oder '{' nach Prototyp-Signatur erwartet", Current());
            }

            return new PrototypeDeclaration
            {
                Signature = signature,
                Body = body,
                Line = startToken.Line,
                Column = startToken.Column
            };
        }


        private FunctionDeclaration ParseFunction()
        {
            var startToken = Current();
            Advance();

            var returnToken = Current().Type == TokenType.TypeKeyword || Current().Type == TokenType.Identifier
                ? AdvanceAndGet()
                : throw new ParseException("Rückgabetyp erwartet", Current());

            var func = new FunctionDeclaration
            {
                ReturnType = returnToken.Value,
                Line = startToken.Line,
                Column = startToken.Column
            };
            var nameToken = Consume(TokenType.FunctionName, "Funktionsname erwartet");
            func.Name = nameToken.Value;

            Consume(TokenType.OpenParenthesis, "'(' erwartet");

            if (!Check(TokenType.CloseParenthesis))
            {
                while (true)
                {
                    bool isVar = false;
                    if (Match(TokenType.VarKeyword))
                    {
                        isVar = true;
                        Advance();
                    }

                    if (!(Current().Type == TokenType.TypeKeyword || Current().Type == TokenType.Identifier))
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

            Consume(TokenType.CloseParenthesis, "')' nach Parametern erwartet");

            // Funktionsbody ist optional (für Prototypen)
            if (Check(TokenType.OpenBracket))
            {
                Consume(TokenType.OpenBracket, "'{' vor Funktionsrumpf erwartet");
                while (!Check(TokenType.CloseBracket) && !IsAtEnd())
                {
                    var stmt = ParseStatement();
                    if (stmt != null) func.Body.Add(stmt);
                    else Advance();
                }
                Consume(TokenType.CloseBracket, "'}' nach Funktionsrumpf erwartet");
            }

            // Optionales Semicolon (für Prototypen oder nach Body)
            if (Match(TokenType.Semicolon)) Advance();

            return func;
        }

        private Declaration ParseConstDeclaration()
        {
            var startToken = Current();
            Advance();
            var typeToken = Consume(TokenType.TypeKeyword, "Typname erwartet");

            var declarations = new List<ConstDeclaration>();

            while (true)
            {
                var nameToken = Consume(TokenType.Identifier, "Konstantenname erwartet");
                string arraySize = null;

                // Arraygröße optional
                if (Match(TokenType.OpenSquareBracket))
                {
                    Advance();
                    var sizeToken = Current();
                    if (sizeToken.Type == TokenType.IntegerLiteral ||
                        sizeToken.Type == TokenType.Identifier ||
                        sizeToken.Type == TokenType.ATRConstant ||
                        sizeToken.Type == TokenType.GuildConstant ||
                        sizeToken.Type == TokenType.NPC_Constant ||
                        sizeToken.Type == TokenType.AIVConstant ||
                        sizeToken.Type == TokenType.FAIConstant ||
                        sizeToken.Type == TokenType.CRIMEConstant ||
                        sizeToken.Type == TokenType.LOCConstant ||
                        sizeToken.Type == TokenType.PETZCOUNTERConstant ||
                        sizeToken.Type == TokenType.LOGConstant ||
                        sizeToken.Type == TokenType.FONTConstant ||
                        sizeToken.Type == TokenType.REALConstant ||
                        sizeToken.Type == TokenType.ARConstant ||
                        sizeToken.Type == TokenType.PLAYERConstant ||
                        sizeToken.Type == TokenType.ZENConstant ||
                        sizeToken.Type == TokenType.SexConstant ||
                        sizeToken.Type == TokenType.MAXConstant ||
                        sizeToken.Type == TokenType.PROTConstant ||
                        sizeToken.Type == TokenType.DAMConstant ||
                        sizeToken.Type == TokenType.ITMConstant ||
                        sizeToken.Type == TokenType.Identifier)
                    {
                        arraySize = sizeToken.Value;
                        Advance();
                    }
                    else
                    {
                        throw new ParseException("Array-Größe (Integer oder Konstante) erwartet", sizeToken);
                    }
                    Consume(TokenType.CloseSquareBracket, "']' nach Array-Größe erwartet");
                }

                string value = null;

                // Initialisierung optional
                if (Match(TokenType.Assignment))
                {
                    Advance();
                    if (Match(TokenType.OpenBracket))
                    {
                        var initializer = new List<string>();
                        Advance(); // consume '{'

                        while (!Check(TokenType.CloseBracket) && !IsAtEnd())
                        {
                            var expr = ParseExpression();

                            switch (expr)
                            {
                                case LiteralExpression lit:
                                    initializer.Add(lit.Value);
                                    break;

                                case VariableExpression varExpr:
                                    initializer.Add(varExpr.Name); // Konstanten wie ATT_NEUTRAL akzeptieren
                                    break;

                                default:
                                    throw new ParseException(
                                        "Array-Initialisierer muss Literale oder Konstanten enthalten",
                                        Current()
                                    );
                            }

                            if (Match(TokenType.Comma))
                            {
                                Advance();
                            }
                            else if (!Check(TokenType.CloseBracket))
                            {
                                throw new ParseException("Komma oder '}' erwartet", Current());
                            }
                        }

                        Consume(TokenType.CloseBracket, "'}' nach Array-Initialisierer erwartet");
                        value = "{" + string.Join(", ", initializer) + "}";
                    }
                    else
                    {
                        var expr = ParseExpression();
                        value = ExpressionToString(expr);
                    }
                }

                declarations.Add(new ConstDeclaration
                {
                    TypeName = typeToken.Value,
                    Name = nameToken.Value,
                    ArraySize = arraySize,
                    Value = value,
                    Line = nameToken.Line,
                    Column = nameToken.Column
                });

                if (Match(TokenType.Comma))
                {
                    Advance();
                    continue;
                }

                break;
            }

            Consume(TokenType.Semicolon, "';' nach Konstantendeklaration erwartet");

            if (declarations.Count == 1)
                return declarations[0];

            return new MultiConstDeclaration
            {
                Declarations = declarations,
                Line = startToken.Line,
                Column = startToken.Column
            };
        }

        private DaedalusToken AdvanceAndGet()
        {
            var tok = Current();
            Advance();
            return tok;
        }

        private Declaration ParseVarDeclaration()
        {
            var startToken = Current();
            Advance();
            var typeToken = Consume(TokenType.TypeKeyword, "Typname erwartet");

            var multiVar = new MultiVarDeclaration
            {
                Line = startToken.Line,
                Column = startToken.Column
            };

            while (true)
            {
                var nameToken = Consume(TokenType.Identifier, "Variablenname erwartet");
                string arraySize = null;

                if (Match(TokenType.OpenSquareBracket))
                {
                    Advance();
                    var sizeToken = Current();
                    if (sizeToken.Type == TokenType.IntegerLiteral ||
                        sizeToken.Type == TokenType.Identifier ||
                        sizeToken.Type == TokenType.ATRConstant ||
                        sizeToken.Type == TokenType.GuildConstant ||
                        sizeToken.Type == TokenType.NPC_Constant ||
                        sizeToken.Type == TokenType.AIVConstant ||
                        sizeToken.Type == TokenType.FAIConstant ||
                        sizeToken.Type == TokenType.CRIMEConstant ||
                        sizeToken.Type == TokenType.LOCConstant ||
                        sizeToken.Type == TokenType.PETZCOUNTERConstant ||
                        sizeToken.Type == TokenType.LOGConstant ||
                        sizeToken.Type == TokenType.FONTConstant ||
                        sizeToken.Type == TokenType.REALConstant ||
                        sizeToken.Type == TokenType.ARConstant ||
                        sizeToken.Type == TokenType.PLAYERConstant ||
                        sizeToken.Type == TokenType.ZENConstant ||
                        sizeToken.Type == TokenType.SexConstant ||
                        sizeToken.Type == TokenType.MAXConstant ||
                        sizeToken.Type == TokenType.PROTConstant ||
                        sizeToken.Type == TokenType.DAMConstant ||
                        sizeToken.Type == TokenType.ITMConstant)
                    {
                        arraySize = sizeToken.Value;
                        Advance();
                    }
                    else
                    {
                        throw new ParseException("Array-Größe (Integer oder Konstante) erwartet", sizeToken);
                    }
                    Consume(TokenType.CloseSquareBracket, "']' nach Array-Größe erwartet");
                }

                multiVar.Declarations.Add(new VarDeclaration
                {
                    TypeName = typeToken.Value,
                    Name = nameToken.Value,
                    ArraySize = arraySize,
                    Line = nameToken.Line,
                    Column = nameToken.Column
                });

                if (Match(TokenType.Comma))
                {
                    Advance();
                    continue;
                }
                break;
            }

            Consume(TokenType.Semicolon, "';' nach Variablendeklaration erwartet");
            return multiVar;
        }

        private Statement ParseStatement()
        {
            if (Match(TokenType.IfKeyword)) return ParseIfStatement();
            if (Match(TokenType.ReturnKeyword)) return ParseReturnStatement();
            if (Match(TokenType.VarKeyword)) return ParseVarStatement();

            int startPos = _position;

            try
            {
                var lhs = ParseExpression();

                if (Match(TokenType.Assignment) ||
                (Match(TokenType.Operator) && (Current().Value == "-=" || Current().Value == "+=")))

                {
                    var assignToken = Current();
                    bool isCompoundAssign = assignToken.Type == TokenType.Operator;
                    string compoundOp = assignToken.Value;

                    Advance();

                    var rhs = ParseExpression();
                    Consume(TokenType.Semicolon, "';' nach Zuweisung erwartet");

                    if (isCompoundAssign)
                    {
                        rhs = new BinaryExpression
                        {
                            Left = lhs,
                            Operator = compoundOp == "-=" ? "-" : "+",
                            Right = rhs,
                            Line = assignToken.Line,
                            Column = assignToken.Column
                        };
                    }

                    return new Assignment
                    {
                        Left = lhs,
                        Right = rhs,
                        Line = assignToken.Line,
                        Column = assignToken.Column
                    };
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

        private Statement ParseVarStatement()
        {
            var startToken = Current();
            Advance();
            var typeToken = Consume(TokenType.TypeKeyword, "Typname erwartet");

            var multiVarStmt = new MultiVarDeclarationStatement
            {
                Line = startToken.Line,
                Column = startToken.Column
            };

            while (true)
            {
                var nameToken = Consume(TokenType.Identifier, "Variablenname erwartet");
                string arraySize = null;

                if (Match(TokenType.OpenSquareBracket))
                {
                    Advance();
                    var sizeToken = Current();
                    if (sizeToken.Type == TokenType.IntegerLiteral ||
                        sizeToken.Type == TokenType.ATRConstant ||
                        sizeToken.Type == TokenType.GuildConstant ||
                        sizeToken.Type == TokenType.NPC_Constant ||
                        sizeToken.Type == TokenType.AIVConstant ||
                        sizeToken.Type == TokenType.FAIConstant ||
                        sizeToken.Type == TokenType.CRIMEConstant ||
                        sizeToken.Type == TokenType.LOCConstant ||
                        sizeToken.Type == TokenType.PETZCOUNTERConstant ||
                        sizeToken.Type == TokenType.LOGConstant ||
                        sizeToken.Type == TokenType.FONTConstant ||
                        sizeToken.Type == TokenType.REALConstant ||
                        sizeToken.Type == TokenType.ARConstant ||
                        sizeToken.Type == TokenType.PLAYERConstant ||
                        sizeToken.Type == TokenType.ZENConstant ||
                        sizeToken.Type == TokenType.SexConstant ||
                        sizeToken.Type == TokenType.MAXConstant ||
                        sizeToken.Type == TokenType.PROTConstant ||
                        sizeToken.Type == TokenType.DAMConstant ||
                        sizeToken.Type == TokenType.ITMConstant)
                    {
                        arraySize = sizeToken.Value;
                        Advance();
                    }
                    else
                    {
                        throw new ParseException("Array-Größe (Integer oder Konstante) erwartet", sizeToken);
                    }
                    Consume(TokenType.CloseSquareBracket, "']' nach Array-Größe erwartet");
                }

                var varDeclaration = new VarDeclaration
                {
                    TypeName = typeToken.Value,
                    Name = nameToken.Value,
                    ArraySize = arraySize,
                    Line = nameToken.Line,
                    Column = nameToken.Column
                };

                if (Match(TokenType.Assignment) ||
                    (Match(TokenType.Operator) && Current().Value == "-="))
                {
                    var assignToken = Current();
                    bool isMinusAssign = assignToken.Type == TokenType.Operator;

                    Advance();
                    var rhs = ParseExpression();
                    Consume(TokenType.Semicolon, "';' nach Zuweisung erwartet");

                    if (isMinusAssign)
                    {
                        rhs = new BinaryExpression
                        {
                            Left = new VariableExpression
                            {
                                Name = nameToken.Value,
                                Line = nameToken.Line,
                                Column = nameToken.Column
                            },
                            Operator = "-",
                            Right = rhs,
                            Line = assignToken.Line,
                            Column = assignToken.Column
                        };
                    }

                    return new Assignment
                    {
                        Left = new VariableExpression
                        {
                            Name = nameToken.Value,
                            Line = nameToken.Line,
                            Column = nameToken.Column
                        },
                        Right = rhs,
                        Line = assignToken.Line,
                        Column = assignToken.Column
                    };
                }

                multiVarStmt.Declarations.Add(new VarDeclarationStatement
                {
                    Declaration = varDeclaration,
                    Line = nameToken.Line,
                    Column = nameToken.Column
                });

                if (Match(TokenType.Comma))
                {
                    Advance();
                    continue;
                }
                break;
            }

            Consume(TokenType.Semicolon, "';' nach Variablendeklaration erwartet");
            return multiVarStmt;
        }

        private ReturnStatement ParseReturnStatement()
        {
            var startToken = Current();
            Advance();
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
            Advance();

            Expression condition;
            if (Match(TokenType.OpenParenthesis))
            {
                Advance();
                condition = ParseExpression();
                Consume(TokenType.CloseParenthesis, "')' nach Bedingung erwartet");

                while (Match(TokenType.Operator) && (Current().Value == "&&" || Current().Value == "||"))
                {
                    var opToken = Current();
                    Advance();
                    var rightCondition = ParseExpression();
                    condition = new BinaryExpression
                    {
                        Left = condition,
                        Operator = opToken.Value,
                        Right = rightCondition,
                        Line = opToken.Line,
                        Column = opToken.Column
                    };
                }
            }
            else
            {
                condition = ParseExpression();

                while (Match(TokenType.Operator) && (Current().Value == "&&" || Current().Value == "||"))
                {
                    var opToken = Current();
                    Advance();
                    var rightCondition = ParseExpression();
                    condition = new BinaryExpression
                    {
                        Left = condition,
                        Operator = opToken.Value,
                        Right = rightCondition,
                        Line = opToken.Line,
                        Column = opToken.Column
                    };
                }
            }

            Consume(TokenType.OpenBracket, "'{' vor if-Block erwartet");
            var thenBranch = new List<Statement>();
            while (!Check(TokenType.CloseBracket) && !IsAtEnd())
            {
                var stmt = ParseStatement();
                if (stmt != null) thenBranch.Add(stmt); else Advance();
            }
            Consume(TokenType.CloseBracket, "'}' nach if-Block erwartet");
            if (Match(TokenType.Semicolon)) Advance();

            var elseBranch = new List<Statement>();
            if (Match(TokenType.ElseKeyword))
            {
                Advance();
                if (Match(TokenType.IfKeyword))
                {
                    var elseIfStatement = ParseIfStatement();
                    elseBranch.Add(elseIfStatement);
                }
                else
                {
                    Consume(TokenType.OpenBracket, "'{' nach else erwartet");
                    while (!Check(TokenType.CloseBracket) && !IsAtEnd())
                    {
                        var stmt = ParseStatement();
                        if (stmt != null) elseBranch.Add(stmt); else Advance();
                    }
                    Consume(TokenType.CloseBracket, "'}' nach else-Block erwartet");
                    if (Match(TokenType.Semicolon)) Advance();
                }
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
            DaedalusToken tok = Current();
            Expression expr;

            // ---------- Unäre Operatoren
            if (Match(TokenType.Operator) && (tok.Value == "!" || tok.Value == "-" || tok.Value == "+" || tok.Value == "~"))
            {
                Advance();

                // -42 oder +42
                if ((tok.Value == "-" || tok.Value == "+") &&
                    (Match(TokenType.IntegerLiteral) || Match(TokenType.FloatLiteral)))
                {
                    var literalToken = Current();
                    Advance();
                    expr = new LiteralExpression
                    {
                        Value = tok.Value == "-" ? "-" + literalToken.Value : literalToken.Value,
                        Line = tok.Line,
                        Column = tok.Column
                    };
                    return expr;
                }

                // unäres +X → einfach X
                if (tok.Value == "+")
                    return ParsePrimary();

                // !X oder -X
                return new UnaryExpression
                {
                    Operator = tok.Value,
                    Operand = ParsePrimary(),
                    Line = tok.Line,
                    Column = tok.Column
                };

            }

            // ---------- Klammern
            if (Match(TokenType.OpenParenthesis))
            {
                Advance();
                expr = ParseExpression();
                Consume(TokenType.CloseParenthesis, "')' nach geklammertem Ausdruck erwartet");
                return expr;
            }

            // ---------- Literale, Konstanten, Variablen, Schlüsselwörter
            if (Match(TokenType.IntegerLiteral) || Match(TokenType.FloatLiteral) ||
                Match(TokenType.StringLiteral) || Match(TokenType.BoolLiteral) ||
                Match(TokenType.GuildConstant) || Match(TokenType.NPC_Constant) ||
                Match(TokenType.AIVConstant) || Match(TokenType.FAIConstant) ||
                Match(TokenType.CRIMEConstant) || Match(TokenType.LOCConstant) ||
                Match(TokenType.PETZCOUNTERConstant) || Match(TokenType.LOGConstant) ||
                Match(TokenType.FONTConstant) || Match(TokenType.REALConstant) ||
                Match(TokenType.ATRConstant) || Match(TokenType.ARConstant) ||
                Match(TokenType.PLAYERConstant) || Match(TokenType.ZENConstant) ||
                Match(TokenType.SexConstant) || Match(TokenType.MAXConstant) ||
                Match(TokenType.PROTConstant) || Match(TokenType.DAMConstant) ||
                Match(TokenType.ITMConstant) ||
                Match(TokenType.Identifier) || Match(TokenType.AiVariable) ||
                Match(TokenType.SelfKeyword) || Match(TokenType.OtherKeyword) ||
                Match(TokenType.SlfKeyword) || Match(TokenType.OthKeyword) ||
                Match(TokenType.BuiltInFunction) || Match(TokenType.MdlFunction) ||
                Match(TokenType.AIFunction) || Match(TokenType.NpcFunction) ||
                Match(TokenType.InfoFunction) || Match(TokenType.CreateFunction) ||
                Match(TokenType.WldFunction) || Match(TokenType.LogFunction) ||
                Match(TokenType.HlpFunction) || Match(TokenType.SndFunction) ||
                Match(TokenType.TAFunction) || Match(TokenType.EquipFunction))
            {
                Advance();
                expr = new VariableExpression
                {
                    Name = tok.Value,
                    Line = tok.Line,
                    Column = tok.Column
                };
            }
            else
            {
                throw new ParseException("Unerwartetes Token in Ausdruck", tok);
            }

            // ---------- POSTFIX-KETTE: Member, Indexer, Funktionsaufrufe
            while (true)
            {
                if (Match(TokenType.Dot))
                {
                    Advance();
                    // ALLE gültigen Token nach '.' erlauben (Identifier, AiVariable, Konstanten)
                    if (!(Match(TokenType.Identifier) || Match(TokenType.AiVariable) ||
                          Match(TokenType.PLAYERConstant) || Match(TokenType.AIVConstant) ||
                          Match(TokenType.ATRConstant) || Match(TokenType.NPC_Constant) ||
                          Match(TokenType.ZENConstant) || Match(TokenType.SexConstant) ||
                          Match(TokenType.MAXConstant) || Match(TokenType.DAMConstant) ||
                          Match(TokenType.ITMConstant)))
                    {
                        throw new ParseException("Membername nach '.' erwartet", Current());
                    }

                    var memberToken = AdvanceAndGet();
                    expr = new MemberExpression
                    {
                        Object = expr,
                        MemberName = memberToken.Value,
                        Line = memberToken.Line,
                        Column = memberToken.Column
                    };
                }
                else if (Match(TokenType.OpenSquareBracket))
                {
                    Advance();
                    var idx = ParseExpression();
                    Consume(TokenType.CloseSquareBracket, "']' nach Index erwartet");

                    expr = new IndexExpression
                    {
                        Target = expr,
                        Index = idx,
                        Line = tok.Line,
                        Column = tok.Column
                    };
                }
                else if (Match(TokenType.OpenParenthesis))
                {
                    Advance();
                    var args = new List<Expression>();

                    if (!Check(TokenType.CloseParenthesis))
                    {
                        while (true)
                        {
                            args.Add(ParseExpression());
                            if (Match(TokenType.Comma)) Advance();
                            else break;
                        }
                    }

                    Consume(TokenType.CloseParenthesis, "')' nach Argumenten erwartet");

                    expr = new FunctionCallExpression
                    {
                        FunctionName = expr is MemberExpression m
                            ? m.MemberName
                            : ((VariableExpression)expr).Name,
                        Arguments = args,
                        Line = tok.Line,
                        Column = tok.Column
                    };
                }
                else break;
            }

            return expr;
        }

        private int GetPrecedence(DaedalusToken token)
        {
            if (token?.Type != TokenType.Operator) return 0;
            switch (token.Value)
            {
                case "||": return 5;
                case "&&": return 6;
                case "|": return 7;
                case "^": return 8;
                case "&": return 9;
                case "==":
                case "!=": return 10;
                case "<":
                case ">":
                case "<=":
                case ">=": return 11;
                case "<<":
                case ">>": return 12;
                case "+":
                case "-": return 13;
                case "*":
                case "/":
                case "%": return 14;
                default: return 0;
            }
        }

        private string ExpressionToString(Expression expr)
        {
            if (expr is LiteralExpression lit)
                return lit.Value;
            if (expr is VariableExpression var)
                return var.Name;
            if (expr is BinaryExpression bin &&
                (bin.Operator == "<<" || bin.Operator == ">>" || bin.Operator == "+" ||
                 bin.Operator == "-" || bin.Operator == "/" || bin.Operator == "*" || bin.Operator == "|"))
            {
                return $"{ExpressionToString(bin.Left)} {bin.Operator} {ExpressionToString(bin.Right)}";
            }
            throw new ParseException("Ungültiger Ausdruck für Konstantenwert (nur Literale, Identifier oder binäre Ausdrücke mit <<, >>, +, /, | erlaubt)", new DaedalusToken(TokenType.EOF, "", expr.Line, expr.Column));
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
                    TokenType.OpenBracket => "'{'",
                    TokenType.CloseBracket => "'}'",
                    TokenType.OpenParenthesis => "'('",
                    TokenType.CloseParenthesis => "')'",
                    TokenType.OpenSquareBracket => "'['",
                    TokenType.CloseSquareBracket => "']'",
                    TokenType.Comma => "','",
                    TokenType.Assignment => "'='",
                    TokenType.TypeKeyword => "type",
                    TokenType.FuncKeyword => "func",
                    TokenType.VarKeyword => "var",
                    TokenType.ConstKeyword => "const",
                    TokenType.IfKeyword => "if",
                    TokenType.ElseKeyword => "else",
                    TokenType.ReturnKeyword => "return",
                    TokenType.PrototypeKeyword => "prototype",
                    TokenType.Identifier => "identifier",
                    _ => type.ToString()
                };
                throw new ParseException(message, tok, expectedSymbol);
            }
            Advance();
            return tok;
        }

        private void Advance() => _position++;
        private bool IsAtEnd() => _position >= _tokens.Count;

        public void CollectGlobals(List<Declaration> declarations, SymbolTable symbolTable)
        {
            foreach (var decl in declarations)
            {
                if (decl is ClassDeclaration cls)
                    symbolTable.Register(cls.Name, cls);

                else if (decl is FunctionDeclaration func)
                    symbolTable.Register(func.Name, func);

                else if (decl is InstanceDeclaration inst)
                    symbolTable.Register(inst.Name, inst);

                else if (decl is PrototypeDeclaration proto)
                {
                    // Daedalus Prototyp-Namen aus der Signatur extrahieren
                    // "C_NPC(C_NPC_Default)" -> "C_NPC"
                    string name = proto.Signature.Split('(')[0].Trim();
                    symbolTable.Register(name, proto);
                }
                else if (decl is MultiVarDeclaration mVar)
                {
                    foreach (var v in mVar.Declarations)
                        symbolTable.Register(v.Name, v);
                }
                else if (decl is MultiConstDeclaration mConst)
                {
                    foreach (var c in mConst.Declarations)
                        symbolTable.Register(c.Name, c);
                }
            }
        }
    }
}