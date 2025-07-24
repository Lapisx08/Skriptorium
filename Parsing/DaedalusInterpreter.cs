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

        public void LoadDeclarations(List<Declaration> decls)
        {
            foreach (var decl in decls)
            {
                switch (decl)
                {
                    case FunctionDeclaration func:
                        _functions[func.Name] = func;
                        break;
                    case VarDeclaration varDecl:
                        _globals[varDecl.Name] = GetDefaultValue(varDecl.TypeName);
                        break;
                }
            }
        }

        private object GetDefaultValue(string typeName) => typeName switch
        {
            "int" => 0,
            "float" => 0.0f,
            "string" => string.Empty,
            "bool" => false,
            _ => null,
        };

        /// <summary>
        /// Semantikprüfung der geladenen Funktionen und Variablen.
        /// </summary>
        /// <returns>Liste der gefundenen Fehler, leer wenn keine Fehler</returns>
        public List<string> SemanticCheck()
        {
            var errors = new List<string>();

            // Prüfe auf doppelte Funktionsnamen
            var duplicateFuncs = _functions
                .GroupBy(f => f.Key)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
            foreach (var d in duplicateFuncs)
                errors.Add($"Doppelte Funktionsdefinition: '{d}'");

            // Prüfe, ob jede Funktion einen Körper hat
            foreach (var func in _functions.Values)
            {
                if (func.Body == null || func.Body.Count == 0)
                    errors.Add($"Funktion '{func.Name}' hat keinen Körper.");
            }

            // Prüfe auf doppelte globale Variablen
            var globalVarNames = _globals.Keys.ToList();
            var duplicateGlobals = globalVarNames
                .GroupBy(n => n)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
            foreach (var d in duplicateGlobals)
                errors.Add($"Doppelte globale Variable: '{d}'");

            // Hier kannst du noch mehr Checks einbauen,
            // z.B. ob in Funktionen Variablen verwendet werden, die nicht deklariert sind.

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
            else if (assign.Left is IndexExpression idx)
            {
                // TODO: implement array/dictionary assignment
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
            IndexExpression idx => throw new NotImplementedException("Index evaluation not implemented."),
            BinaryExpression bin => EvaluateBinary(bin),
            FunctionCallExpression call => EvaluateCall(call),
            _ => throw new Exception("Unknown expression type.")
        };

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
            throw new Exception($"Variable '{name}' not defined.");
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
}
