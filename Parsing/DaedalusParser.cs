using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Skriptorium.Parsing
{
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
            : base($"Syntax-Fehler: Zeile {token.Line}, Spalte {token.Column}. Erwartet: {expected ?? "<beliebig>"}, Gefunden: '{token?.Value}'")
        {
            Line = token?.Line ?? -1;
            Column = token?.Line ?? -1;
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
                {
                    if (decl is InstanceDeclaration)
                    {
                        var instanceDecls = ParseInstanceDeclaration();
                        declarations.AddRange(instanceDecls);
                    }
                    else
                    {
                        declarations.Add(decl);
                    }
                }
                else
                    Advance();
            }
            return declarations;
        }

        private Declaration ParseDeclaration()
        {
            if (Match(TokenType.InstanceKeyword)) return new InstanceDeclaration();
            if (Match(TokenType.FuncKeyword)) return ParseFunction();
            if (Match(TokenType.VarKeyword)) return ParseVarDeclaration();
            if (Match(TokenType.ConstKeyword)) return ParseConstDeclaration();
            if (Match(TokenType.ClassKeyword) && Peek(1).Type == TokenType.Identifier) return ParseClass();
            if (Match(TokenType.PrototypeKeyword)) return ParsePrototype();
            return null;
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
                      Match(TokenType.OtherKeyword) || Match(TokenType.SlfKeyword)))
                {
                    throw new ParseException("Instanzname oder spezielles Schlüsselwort (self, other, hero, slf) erwartet", nameToken, "instance name");
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
            Consume(TokenType.OpenBracket, "'{' vor Funktionsrumpf erwartet");
            while (!Check(TokenType.CloseBracket) && !IsAtEnd())
            {
                var stmt = ParseStatement();
                if (stmt != null) func.Body.Add(stmt);
                else Advance();
            }
            Consume(TokenType.CloseBracket, "'}' nach Funktionsrumpf erwartet");
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

                string value = null;
                if (Match(TokenType.Assignment))
                {
                    Advance();
                    if (Match(TokenType.OpenBracket))
                    {
                        var initializer = new List<string>();
                        Advance(); // consume {
                        while (!Check(TokenType.CloseBracket) && !IsAtEnd())
                        {
                            var expr = ParseExpression();
                            if (expr is LiteralExpression literal)
                            {
                                initializer.Add(literal.Value);
                            }
                            else
                            {
                                throw new ParseException("Array-Initialisierer muss Literale enthalten", Current());
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
                        if (expr is LiteralExpression literal)
                        {
                            value = literal.Value;
                        }
                        else if (expr is VariableExpression variable)
                        {
                            value = variable.Name;
                        }
                        else if (expr is BinaryExpression binary &&
                                 (binary.Operator == "<<" || binary.Operator == ">>" || binary.Operator == "+" || binary.Operator == "/") &&
                                 binary.Left is LiteralExpression left && binary.Right is LiteralExpression right)
                        {
                            value = $"{left.Value} {binary.Operator} {right.Value}";
                        }
                        else
                        {
                            throw new ParseException("Konstantenwert (Literal, Identifier oder binärer Ausdruck mit <<, >>, + oder /) erwartet", Current());
                        }
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
            {
                return declarations[0];
            }

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

                if (Match(TokenType.Assignment))
                {
                    var assignToken = Current();
                    Advance();
                    var rhs = ParseExpression();
                    Consume(TokenType.Semicolon, "';' nach Zuweisung erwartet");

                    var assignment = new Assignment
                    {
                        Left = lhs,
                        Right = rhs,
                        Line = assignToken.Line,
                        Column = assignToken.Line
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

                if (Match(TokenType.Assignment))
                {
                    var assignToken = Current();
                    Advance();
                    var rhs = ParseExpression();
                    Consume(TokenType.Semicolon, "';' nach Zuweisung erwartet");

                    multiVarStmt.Declarations.Add(new VarDeclarationStatement
                    {
                        Declaration = varDeclaration,
                        Line = startToken.Line,
                        Column = startToken.Column
                    });

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
                    Consume(TokenType.OpenParenthesis, "'(' nach logischem Operator erwartet");
                    var rightCondition = ParseExpression();
                    Consume(TokenType.CloseParenthesis, "')' nach Bedingung erwartet");

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

            if (Match(TokenType.Operator) && (Current().Value == "!" || Current().Value == "-"))
            {
                Advance();
                if (tok.Value == "-" && (Match(TokenType.IntegerLiteral) || Match(TokenType.FloatLiteral)))
                {
                    var literalToken = Current();
                    Advance();
                    return new LiteralExpression
                    {
                        Value = "-" + literalToken.Value,
                        Line = tok.Line,
                        Column = tok.Column
                    };
                }
                else if (tok.Value == "!")
                {
                    var operand = ParsePrimary();
                    return new UnaryExpression
                    {
                        Operator = tok.Value,
                        Operand = operand,
                        Line = tok.Line,
                        Column = tok.Column
                    };
                }
                throw new ParseException($"Unerwarteter Operanden nach unärem Operator '{tok.Value}'", Current());
            }

            if (Match(TokenType.OpenParenthesis))
            {
                Advance();
                var expr = ParseExpression();
                Consume(TokenType.CloseParenthesis, "')' nach geklammertem Ausdruck erwartet");
                return expr;
            }

            if (Match(TokenType.IntegerLiteral) || Match(TokenType.FloatLiteral) ||
                Match(TokenType.StringLiteral) || Match(TokenType.BoolLiteral) ||
                Match(TokenType.GuildConstant) || Match(TokenType.NPC_Constant) ||
                Match(TokenType.AIVConstant) || Match(TokenType.FAIConstant) ||
                Match(TokenType.CRIMEConstant) || Match(TokenType.LOCConstant) ||
                Match(TokenType.PETZCOUNTERConstant) || Match(TokenType.LOGConstant) ||
                Match(TokenType.FONTConstant) || Match(TokenType.REALConstant) ||
                Match(TokenType.ATRConstant) || Match(TokenType.ARConstant) ||
                Match(TokenType.PLAYERConstant) || Match(TokenType.ZENConstant) ||
                Match(TokenType.SexConstant) ||
                Match(TokenType.MAXConstant) ||
                Match(TokenType.PROTConstant) ||
                Match(TokenType.DAMConstant) ||
                Match(TokenType.ITMConstant))
            {
                Advance();
                return new LiteralExpression
                {
                    Value = tok.Value,
                    Line = tok.Line,
                    Column = tok.Column
                };
            }

            if (Match(TokenType.Identifier) || Match(TokenType.AiVariable) ||
                Match(TokenType.SelfKeyword) || Match(TokenType.OtherKeyword) ||
                Match(TokenType.SlfKeyword) || Match(TokenType.BuiltInFunction) ||
                Match(TokenType.MdlFunction) || Match(TokenType.AIFunction) ||
                Match(TokenType.NpcFunction) || Match(TokenType.InfoFunction) ||
                Match(TokenType.CreateFunction) || Match(TokenType.WldFunction) ||
                Match(TokenType.LogFunction) || Match(TokenType.HlpFunction) ||
                Match(TokenType.SndFunction) || Match(TokenType.TAFunction) ||
                Match(TokenType.EquipFunction))
            {
                Advance();
                Expression expr = new VariableExpression
                {
                    Name = tok.Value,
                    Line = tok.Line,
                    Column = tok.Column
                };

                while (Match(TokenType.Dot))
                {
                    Advance();
                    if (!(Match(TokenType.Identifier) || Match(TokenType.AiVariable)))
                    {
                        throw new ParseException("Membername (Identifier oder AiVariable) nach '.' erwartet", Current());
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

                if (Match(TokenType.OpenParenthesis))
                {
                    Advance();
                    var args = new List<Expression>();

                    if (!Check(TokenType.CloseParenthesis))
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

                    Consume(TokenType.CloseParenthesis, "')' nach Argumenten erwartet");
                    if (expr is MemberExpression memberExpr)
                    {
                        expr = new FunctionCallExpression
                        {
                            FunctionName = memberExpr.MemberName,
                            Arguments = args,
                            Line = tok.Line,
                            Column = tok.Column
                        };
                    }
                    else
                    {
                        expr = new FunctionCallExpression
                        {
                            FunctionName = ((VariableExpression)expr).Name,
                            Arguments = args,
                            Line = tok.Line,
                            Column = tok.Column
                        };
                    }
                }

                while (Match(TokenType.OpenSquareBracket))
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
                TokenType.Operator when token.Value == "<<" || token.Value == ">>" => 7,
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
    }
}