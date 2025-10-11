using System;
using System.Collections.Generic;
using Skriptorium.Parsing;

namespace Skriptorium.Analysis
{
    public class SemanticAnalyzer
    {
        private readonly Dictionary<string, string> symbolTable = new();  // Name → Typ
        private readonly List<string> errors = new();

        public List<string> Analyze(List<Declaration> declarations)
        {
            errors.Clear();
            symbolTable.Clear();

            foreach (var decl in declarations)
            {
                switch (decl)
                {
                    case VarDeclaration varDecl:
                        AnalyzeVarDeclaration(varDecl);
                        break;

                    case FunctionDeclaration funcDecl:
                        AnalyzeFunctionDeclaration(funcDecl);
                        break;

                    case InstanceDeclaration instDecl:
                        AnalyzeInstanceDeclaration(instDecl);
                        break;

                        // Andere Deklarationen ggf. auch prüfen
                }
            }

            return errors;
        }

        private void AnalyzeVarDeclaration(VarDeclaration varDecl)
        {
            if (symbolTable.ContainsKey(varDecl.Name))
            {
                errors.Add($"Variable '{varDecl.Name}' wurde mehrfach deklariert.");
            }
            else
            {
                symbolTable.Add(varDecl.Name, varDecl.TypeName);
            }
        }

        private void AnalyzeFunctionDeclaration(FunctionDeclaration funcDecl)
        {
            if (symbolTable.ContainsKey(funcDecl.Name))
            {
                errors.Add($"Funktion '{funcDecl.Name}' wurde mehrfach deklariert.");
                return;
            }

            symbolTable.Add(funcDecl.Name, funcDecl.ReturnType);

            // Lokaler Funktions-Scope
            var localSymbols = new Dictionary<string, string>();

            foreach (var param in funcDecl.Parameters)
            {
                // Annahme: "var int x" oder "int x"
                var parts = param.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string type = parts[^2];
                    string name = parts[^1];

                    if (localSymbols.ContainsKey(name))
                    {
                        errors.Add($"Parameter '{name}' in Funktion '{funcDecl.Name}' mehrfach deklariert.");
                    }
                    else
                    {
                        localSymbols[name] = type;
                    }
                }
            }

            foreach (var stmt in funcDecl.Body)
            {
                AnalyzeStatement(stmt, localSymbols, funcDecl.ReturnType);
            }
        }

        private void AnalyzeInstanceDeclaration(InstanceDeclaration instDecl)
        {
            // Optional: prüfen, ob BaseClass existiert (wenn du Klassensystem aufbauen willst)

            var localSymbols = new Dictionary<string, string>();
            foreach (var stmt in instDecl.Body)
            {
                AnalyzeStatement(stmt, localSymbols, null);  // Kein Rückgabetyp notwendig
            }
        }

        private void AnalyzeStatement(Statement stmt, Dictionary<string, string> localSymbols, string expectedReturnType)
        {
            switch (stmt)
            {
                case Assignment assignment:
                    var left = AnalyzeExpression(assignment.Left, localSymbols);
                    var right = AnalyzeExpression(assignment.Right, localSymbols);
                    if (left != null && right != null && left != right)
                    {
                        errors.Add($"Typkonflikt bei Zuweisung: '{left}' = '{right}'");
                    }
                    break;

                case ReturnStatement ret:
                    var returnType = AnalyzeExpression(ret.ReturnValue, localSymbols);
                    if (expectedReturnType != null && returnType != expectedReturnType)
                    {
                        errors.Add($"Falscher Rückgabetyp. Erwartet: '{expectedReturnType}', gefunden: '{returnType}'");
                    }
                    break;

                case ExpressionStatement exprStmt:
                    AnalyzeExpression(exprStmt.Expr, localSymbols);
                    break;

                case IfStatement ifStmt:
                    var condType = AnalyzeExpression(ifStmt.Condition, localSymbols);
                    if (condType != "int" && condType != "bool")  // Beispielannahme
                    {
                        errors.Add("Bedingung in if-Anweisung muss vom Typ 'int' oder 'bool' sein.");
                    }

                    foreach (var thenStmt in ifStmt.ThenBranch)
                        AnalyzeStatement(thenStmt, localSymbols, expectedReturnType);

                    foreach (var elseStmt in ifStmt.ElseBranch)
                        AnalyzeStatement(elseStmt, localSymbols, expectedReturnType);
                    break;
            }
        }

        private string AnalyzeExpression(Expression expr, Dictionary<string, string> localSymbols)
        {
            switch (expr)
            {
                case LiteralExpression lit:
                    if (int.TryParse(lit.Value, out _)) return "int";
                    if (float.TryParse(lit.Value, out _)) return "float";
                    if (lit.Value.StartsWith("\"")) return "string";
                    if (lit.Value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                        lit.Value.Equals("false", StringComparison.OrdinalIgnoreCase)) return "bool";
                    return "unknown";

                case VariableExpression varExpr:
                    if (localSymbols.TryGetValue(varExpr.Name, out var type) ||
                        symbolTable.TryGetValue(varExpr.Name, out type))
                        return type;
                    else
                        errors.Add($"Variable '{varExpr.Name}' ist nicht deklariert.");
                    return null;

                case BinaryExpression bin:
                    var leftType = AnalyzeExpression(bin.Left, localSymbols);
                    var rightType = AnalyzeExpression(bin.Right, localSymbols);
                    if (leftType != rightType)
                        errors.Add($"Typkonflikt in binärem Ausdruck: '{leftType}' {bin.Operator} '{rightType}'");
                    return leftType;  // Annahme: Rückgabetyp = linke Seite

                case FunctionCallExpression call:
                    if (!symbolTable.TryGetValue(call.FunctionName, out var returnType))
                    {
                        errors.Add($"Funktion '{call.FunctionName}' ist nicht deklariert.");
                        return null;
                    }
                    foreach (var arg in call.Arguments)
                        AnalyzeExpression(arg, localSymbols);
                    return returnType;

                default:
                    errors.Add("Unbekannter Ausdruckstyp.");
                    return null;
            }
        }
    }
}
