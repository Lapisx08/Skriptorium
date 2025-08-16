using Skriptorium.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skriptorium.Interpreter
{
    public class DaedalusInterpreter
    {
        private readonly Dictionary<string, FunctionDeclaration> _functions = new();
        private readonly Dictionary<string, object> _globals = new();
        private readonly Stack<Dictionary<string, object>> _callStack = new();
        private readonly Dictionary<string, Dictionary<string, object>> _instances = new();

        public void LoadDeclarations(List<Declaration> decls)
        {
            var errors = new List<string>();

            foreach (var decl in decls)
            {
                switch (decl)
                {
                    case FunctionDeclaration func:
                        if (_functions.ContainsKey(func.Name))
                        {
                            var errorMsg = $"Semantischer Fehler: Zeile {func.Line}, Spalte {func.Column}. Doppelte Funktionsdefinition: '{func.Name}'";
                            errors.Add(errorMsg);
                        }
                        else
                        {
                            _functions[func.Name] = func;
                        }
                        break;

                    case VarDeclaration varDecl:
                        if (_globals.ContainsKey(varDecl.Name))
                        {
                            var errorMsg = $"Semantischer Fehler: Zeile {varDecl.Line}, Spalte {varDecl.Column}. Doppelte globale Variable: '{varDecl.Name}'";
                            errors.Add(errorMsg);
                        }
                        else
                        {
                            _globals[varDecl.Name] = GetDefaultValue(varDecl.TypeName);
                        }
                        break;

                    case InstanceDeclaration instance:
                        if (_instances.ContainsKey(instance.Name))
                        {
                            var errorMsg = $"Semantischer Fehler: Zeile {instance.Line}, Spalte {instance.Column}. Doppelte Instanzdefinition: '{instance.Name}'";
                            errors.Add(errorMsg);
                        }
                        else
                        {
                            _instances[instance.Name] = new Dictionary<string, object>();
                        }
                        break;
                }
            }

            // Zweiter Pass: Initialisiere Instanz-Assignments nach dem Laden aller Deklarationen
            foreach (var instanceDecl in decls.OfType<InstanceDeclaration>())
            {
                var instanceDict = _instances[instanceDecl.Name];
                _callStack.Push(instanceDict);
                foreach (var assign in instanceDecl.Assignments)
                {
                    HandleAssignment(assign);
                }
                _callStack.Pop();
            }

            if (errors.Any())
            {
                throw new Exception(string.Join("\n", errors));
            }
        }

        private object GetDefaultValue(string typeName) => typeName.ToLowerInvariant() switch
        {
            "int" => 0,
            "float" => 0.0f,
            "string" => string.Empty,
            "bool" => false,
            _ => null,
        };

        public List<string> SemanticCheck()
        {
            var errors = new List<string>();

            // Prüfe Funktionen
            foreach (var func in _functions.Values)
            {
                if (func.Body == null || func.Body.Count == 0)
                    errors.Add($"Semantischer Fehler: Zeile {func.Line}, Spalte {func.Column}. Funktion '{func.Name}' hat keinen Körper.");

                foreach (var stmt in func.Body)
                    errors.AddRange(CheckStatement(stmt));
            }

            // Prüfe doppelte globale Variablen (sollte durch LoadDeclarations verhindert sein, aber sicher ist sicher)
            var globalVarNames = _globals.Keys.ToList();
            var duplicateGlobals = globalVarNames
                .GroupBy(n => n)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
            foreach (var d in duplicateGlobals)
                errors.Add($"Semantischer Fehler: Doppelte globale Variable: '{d}'");

            return errors;
        }

        private List<string> CheckStatement(Statement stmt)
        {
            var errors = new List<string>();
            switch (stmt)
            {
                case ExpressionStatement exprStmt:
                    errors.AddRange(CheckExpression(exprStmt.Expr));
                    break;

                case IfStatement ifStmt:
                    errors.AddRange(CheckExpression(ifStmt.Condition));
                    foreach (var thenStmt in ifStmt.ThenBranch)
                        errors.AddRange(CheckStatement(thenStmt));
                    foreach (var elseStmt in ifStmt.ElseBranch)
                        errors.AddRange(CheckStatement(elseStmt));
                    break;

                case ReturnStatement retStmt:
                    if (retStmt.ReturnValue != null)
                        errors.AddRange(CheckExpression(retStmt.ReturnValue));
                    break;

                case Assignment assign:
                    errors.AddRange(CheckExpression(assign.Left));
                    errors.AddRange(CheckExpression(assign.Right));
                    break;
            }
            return errors;
        }

        private List<string> CheckExpression(Expression expr)
        {
            var errors = new List<string>();
            if (expr == null) return errors;

            switch (expr)
            {
                case FunctionCallExpression funcCall:
                    foreach (var arg in funcCall.Arguments)
                        errors.AddRange(CheckExpression(arg));
                    break;

                case BinaryExpression bin:
                    errors.AddRange(CheckExpression(bin.Left));
                    errors.AddRange(CheckExpression(bin.Right));
                    break;

                case UnaryExpression un:
                    errors.AddRange(CheckExpression(un.Operand));
                    break;

                case IndexExpression:
                    errors.Add($"Semantischer Fehler: Indexausdrücke werden nicht unterstützt.");
                    break;

                case VariableExpression varExpr:
                    if (!_globals.ContainsKey(varExpr.Name) && !_instances.ContainsKey(varExpr.Name) &&
                        !_callStack.Any(stack => stack.ContainsKey(varExpr.Name)) &&
                        !_functions.ContainsKey(varExpr.Name))
                    {
                        errors.Add($"Semantischer Fehler: Zeile {varExpr.Line}, Spalte {varExpr.Column}. Unbekannte Variable oder Funktion '{varExpr.Name}'.");
                    }
                    break;

                case MemberExpression mem:
                    errors.AddRange(CheckExpression(mem.Object));
                    break;
            }

            return errors;
        }

        public object CallFunction(string name, params object[] args)
        {
            if (!_functions.TryGetValue(name, out var func))
                throw new Exception($"Function '{name}' not found.");

            var locals = new Dictionary<string, object>();
            for (int i = 0; i < func.Parameters.Count; i++)
            {
                var parts = func.Parameters[i].Split(' ');
                var paramName = parts.Length > 1 ? parts[1] : parts[0];
                locals[paramName] = i < args.Length ? args[i] : GetDefaultValue(parts[0]);
            }

            _callStack.Push(locals);
            var result = ExecuteStatements(func.Body);
            _callStack.Pop();
            return result;
        }

        private object ExecuteStatements(List<Statement> statements)
        {
            foreach (var stmt in statements)
            {
                var ret = ExecuteStatement(stmt);
                if (ret is ReturnValue rv)
                    return rv.Value;
            }
            return null;
        }

        private object ExecuteStatement(Statement stmt) => stmt switch
        {
            Assignment assign => HandleAssignment(assign),
            ExpressionStatement exprStmt => EvaluateExpression(exprStmt.Expr),
            IfStatement ifStmt => HandleIf(ifStmt),
            ReturnStatement retStmt => new ReturnValue(EvaluateExpression(retStmt.ReturnValue)),
            _ => throw new Exception("Unknown statement type.")
        };

        private object HandleAssignment(Assignment assign)
        {
            var value = EvaluateExpression(assign.Right);
            if (assign.Left is VariableExpression varExpr)
            {
                if (_callStack.Count > 0 && _callStack.Peek().ContainsKey(varExpr.Name))
                    _callStack.Peek()[varExpr.Name] = value;
                else if (_globals.ContainsKey(varExpr.Name))
                    _globals[varExpr.Name] = value;
                else
                    throw new Exception($"Variable '{varExpr.Name}' not defined.");
            }
            else if (assign.Left is MemberExpression memLeft)
            {
                var obj = EvaluateExpression(memLeft.Object);
                if (obj is Dictionary<string, object> objDict)
                {
                    objDict[memLeft.MemberName] = value;
                }
                else
                {
                    throw new Exception($"Assignment to non-instance member at line {assign.Line}, column {assign.Column}.");
                }
            }
            else if (assign.Left is IndexExpression)
            {
                throw new NotImplementedException("Index assignment not implemented.");
            }
            else
            {
                throw new Exception("Unsupported assignment target.");
            }
            return null;
        }

        private object HandleIf(IfStatement ifStmt)
        {
            var cond = EvaluateExpression(ifStmt.Condition);
            var branch = IsTrue(cond) ? ifStmt.ThenBranch : ifStmt.ElseBranch;
            return ExecuteStatements(branch);
        }

        private object EvaluateExpression(Expression expr) => expr switch
        {
            LiteralExpression lit => ParseLiteral(lit.Value),
            VariableExpression varExpr => LookupVariable(varExpr.Name),
            IndexExpression => throw new NotImplementedException("Index evaluation not implemented."),
            BinaryExpression bin => EvaluateBinary(bin),
            FunctionCallExpression call => EvaluateCall(call),
            MemberExpression mem => EvaluateMember(mem),
            UnaryExpression un => EvaluateUnary(un),
            _ => throw new Exception("Unknown expression type.")
        };

        private object EvaluateMember(MemberExpression mem)
        {
            var obj = EvaluateExpression(mem.Object);
            if (obj is Dictionary<string, object> objDict && objDict.TryGetValue(mem.MemberName, out var value))
            {
                return value;
            }
            throw new Exception($"Member '{mem.MemberName}' not found or object is not an instance at line {mem.Line}, column {mem.Column}.");
        }

        private object EvaluateUnary(UnaryExpression un)
        {
            var val = EvaluateExpression(un.Operand);
            return un.Operator switch
            {
                "!" => !IsTrue(val),
                "-" => Negate(un, val),
                _ => throw new Exception($"Unknown unary operator '{un.Operator}' at line {un.Line}, column {un.Column}.")
            };
        }

        private object Negate(UnaryExpression un, object val)
        {
            return val switch
            {
                int i => -i,
                float f => -f,
                _ => throw new Exception($"Cannot negate value of type {val?.GetType().Name ?? "null"} at line {un.Line}, column {un.Column}.")
            };
        }

        private object ParseLiteral(string val)
        {
            if (int.TryParse(val, out var iv)) return iv;
            if (float.TryParse(val, out var fv)) return fv;
            if (bool.TryParse(val, out var bv)) return bv;
            return val.Trim('"');
        }

        private object LookupVariable(string name)
        {
            if (_callStack.Count > 0 && _callStack.Peek().TryGetValue(name, out var loc))
                return loc;
            if (_globals.TryGetValue(name, out var glo))
                return glo;
            if (_instances.TryGetValue(name, out var instanceDict))
                return instanceDict;
            throw new Exception($"Variable or instance '{name}' not defined.");
        }

        private object EvaluateBinary(BinaryExpression bin)
        {
            var left = EvaluateExpression(bin.Left);
            var right = EvaluateExpression(bin.Right);
            return bin.Operator switch
            {
                "+" => Add(left, right),
                "-" => Sub(left, right),
                "*" => Mul(left, right),
                "/" => Div(left, right),
                "==" => Equals(left, right),
                "!=" => !Equals(left, right),
                _ => throw new Exception($"Unknown operator '{bin.Operator}'")
            };
        }

        private object EvaluateCall(FunctionCallExpression call)
        {
            var args = new List<object>();
            foreach (var a in call.Arguments)
                args.Add(EvaluateExpression(a));
            return CallFunction(call.FunctionName, args.ToArray());
        }

        // Arithmetic helpers
        private static double ToDouble(object o) => Convert.ToDouble(o);
        private object Add(object l, object r)
            => l is string ls ? ls + r.ToString() : ToDouble(l) + ToDouble(r);
        private object Sub(object l, object r) => ToDouble(l) - ToDouble(r);
        private object Mul(object l, object r) => ToDouble(l) * ToDouble(r);
        private object Div(object l, object r) => ToDouble(r) != 0 ? ToDouble(l) / ToDouble(r) : throw new DivideByZeroException();

        private bool IsTrue(object v) => v switch
        {
            bool b => b,
            int i => i != 0,
            double d => d != 0.0,
            string s => !string.IsNullOrEmpty(s),
            _ => v != null
        };

        private class ReturnValue { public object Value { get; } public ReturnValue(object v) => Value = v; }
    }

    public abstract class Declaration
    {
        public int Line { get; }
        public int Column { get; }
        protected Declaration(int line, int column)
        {
            Line = line;
            Column = column;
        }
    }

    public class FunctionDeclaration : Declaration
    {
        public string Name { get; }
        public List<string> Parameters { get; }
        public List<Statement> Body { get; }
        public FunctionDeclaration(string name, List<string> parameters, List<Statement> body, int line, int column)
            : base(line, column)
        {
            Name = name;
            Parameters = parameters;
            Body = body;
        }
    }

    public class VarDeclaration : Declaration
    {
        public string Name { get; }
        public string TypeName { get; }
        public VarDeclaration(string name, string typeName, int line, int column)
            : base(line, column)
        {
            Name = name;
            TypeName = typeName;
        }
    }

    public class InstanceDeclaration : Declaration
    {
        public string Name { get; }
        public string BaseType { get; }
        public List<Assignment> Assignments { get; }
        public InstanceDeclaration(string name, string baseType, List<Assignment> assignments, int line, int column)
            : base(line, column)
        {
            Name = name;
            BaseType = baseType;
            Assignments = assignments;
        }
    }

    public abstract class Statement { }

    public class Assignment : Statement
    {
        public Expression Left { get; }
        public Expression Right { get; }
        public int Line { get; }
        public int Column { get; }
        public Assignment(Expression left, Expression right, int line, int column)
        {
            Left = left;
            Right = right;
            Line = line;
            Column = column;
        }
    }

    public class ExpressionStatement : Statement
    {
        public Expression Expr { get; }
        public ExpressionStatement(Expression expr) => Expr = expr;
    }

    public class IfStatement : Statement
    {
        public Expression Condition { get; }
        public List<Statement> ThenBranch { get; }
        public List<Statement> ElseBranch { get; }
        public IfStatement(Expression condition, List<Statement> thenBranch, List<Statement> elseBranch)
        {
            Condition = condition;
            ThenBranch = thenBranch;
            ElseBranch = elseBranch;
        }
    }

    public class ReturnStatement : Statement
    {
        public Expression ReturnValue { get; }
        public ReturnStatement(Expression returnValue) => ReturnValue = returnValue;
    }

    public abstract class Expression
    {
        public int Line { get; set; } = -1;
        public int Column { get; set; } = -1;
    }

    public class LiteralExpression : Expression
    {
        public string Value { get; }
        public LiteralExpression(string value) => Value = value;
    }

    public class VariableExpression : Expression
    {
        public string Name { get; }
        public string TypeName { get; }
        public VariableExpression(string name, string typeName, int line = -1, int column = -1)
        {
            Name = name;
            TypeName = typeName;
            Line = line;
            Column = column;
        }
    }

    public class IndexExpression : Expression
    {
        // Implementierung für Indexausdrücke
    }

    public class BinaryExpression : Expression
    {
        public Expression Left { get; }
        public Expression Right { get; }
        public string Operator { get; }
        public BinaryExpression(Expression left, string op, Expression right, int line = -1, int column = -1)
        {
            Left = left;
            Operator = op;
            Right = right;
            Line = line;
            Column = column;
        }
    }

    public class FunctionCallExpression : Expression
    {
        public string FunctionName { get; }
        public List<Expression> Arguments { get; }
        public FunctionCallExpression(string functionName, List<Expression> arguments, int line = -1, int column = -1)
        {
            FunctionName = functionName;
            Arguments = arguments;
            Line = line;
            Column = column;
        }
    }

    public class MemberExpression : Expression
    {
        public Expression Object { get; }
        public string MemberName { get; }
        public MemberExpression(Expression objectExpr, string memberName, int line = -1, int column = -1)
        {
            Object = objectExpr;
            MemberName = memberName;
            Line = line;
            Column = column;
        }
    }

    public class UnaryExpression : Expression
    {
        public string Operator { get; }
        public Expression Operand { get; }
        public UnaryExpression(string @operator, Expression operand, int line = -1, int column = -1)
        {
            Operator = @operator;
            Operand = operand;
            Line = line;
            Column = column;
        }
    }
}