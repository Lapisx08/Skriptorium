using System;
using System.Collections.Generic;
using System.Linq;
using Skriptorium.Parsing;

namespace Skriptorium.Analysis
{
    public class LintResult
    {
        public string Message { get; set; }
        public string LocationHint { get; set; }
        public string Color { get; set; }
    }

    public class DaedalusLinter
    {
        private readonly List<LintResult> results = new();
        private readonly HashSet<string> knownFunctions;

        public DaedalusLinter(IEnumerable<string> additionalFunctions = null)
        {
            // Basis-Funktionen aus der AutocompletionEngine
            knownFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Wild_InsertNpc"
                // Weitere Standard-Funktionen hier
            };
            if (additionalFunctions != null)
                foreach (var fn in additionalFunctions)
                    knownFunctions.Add(fn);
        }

        public List<LintResult> Lint(List<Declaration> declarations)
        {
            results.Clear();

            foreach (var decl in declarations)
            {
                switch (decl)
                {
                    case VarDeclaration varDecl:
                        LintVar(varDecl);
                        break;
                    case FunctionDeclaration funcDecl:
                        LintFunction(funcDecl);
                        break;
                    case InstanceDeclaration instDecl:
                        // Instanzen können ggf. eigene Regeln erhalten
                        LintInstance(instDecl);
                        break;
                }
            }

            return results;
        }

        private void LintVar(VarDeclaration varDecl)
        {
            // Markiere Variablendeklarationen (grün)
            results.Add(new LintResult
            {
                Message = $"Variablendeklaration erkannt: '{varDecl.Name}'.",
                LocationHint = varDecl.Name,
                Color = "green"
            });
        }

        private void LintFunction(FunctionDeclaration func)
        {
            // Warnung bei mehrfacher Deklaration
            var declaredParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var param in func.Parameters)
            {
                var parts = param.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var name = parts[^1];
                if (!declaredParams.Add(name))
                {
                    results.Add(new LintResult
                    {
                        Message = $"Parameter '{name}' in Funktion '{func.Name}' mehrfach deklariert.",
                        LocationHint = name,
                        Color = "red"
                    });
                }
            }

            // Tracke lokale Variablenverwendung
            var usage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            // Initialisiere aus Parametern
            foreach (var param in declaredParams)
                usage[param] = 0;

            // Analysiere Statements
            foreach (var stmt in func.Body)
                LintStatement(stmt, usage, func);

            // Unbenutzte Variablen (grau)
            foreach (var kv in usage.Where(kv => kv.Value == 0))
            {
                results.Add(new LintResult
                {
                    Message = $"Variable '{kv.Key}' in Funktion '{func.Name}' wird nie verwendet.",
                    LocationHint = kv.Key,
                    Color = "gray"
                });
            }
        }

        private void LintInstance(InstanceDeclaration inst)
        {
            // Beispielregel: BaseClass prüfen
            if (string.IsNullOrWhiteSpace(inst.BaseClass))
            {
                results.Add(new LintResult
                {
                    Message = $"Instance '{inst.Name}' hat keine Basisklasse.",
                    LocationHint = inst.Name,
                    Color = "orange"
                });
            }
            // Analysiere den Body ohne Return-Kontext
            var usage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var stmt in inst.Body)
                LintStatement(stmt, usage, null);
        }

        private void LintStatement(Statement stmt, Dictionary<string, int> usage, FunctionDeclaration func)
        {
            switch (stmt)
            {
                case Assignment a:
                    if (a.Left is VariableExpression v)
                    {
                        results.Add(new LintResult
                        {
                            Message = $"Zuweisung erkannt: '{v.Name} = ...'.",
                            LocationHint = v.Name,
                            Color = "yellow"
                        });
                        usage.TryAdd(v.Name, 0);
                        usage[v.Name]++;
                    }
                    CountVariableUsage(a.Right, usage);
                    break;

                case ExpressionStatement e:
                    if (e.Expr is FunctionCallExpression fcall)
                    {
                        if (knownFunctions.Contains(fcall.FunctionName))
                        {
                            results.Add(new LintResult
                            {
                                Message = $"Funktionsaufruf erkannt: '{fcall.FunctionName}'.",
                                LocationHint = fcall.FunctionName,
                                Color = "blue"
                            });
                        }
                        else
                        {
                            results.Add(new LintResult
                            {
                                Message = $"Aufruf einer unbekannten Funktion: '{fcall.FunctionName}'.",
                                LocationHint = fcall.FunctionName,
                                Color = "orange"
                            });
                        }
                        foreach (var arg in fcall.Arguments)
                            CountVariableUsage(arg, usage);
                    }
                    else
                    {
                        CountVariableUsage(e.Expr, usage);
                    }
                    break;

                case ReturnStatement r:
                    // Redundanter return am Ende (blau)
                    if (func != null && r.ReturnValue == null && IsLastStatement(func, r))
                    {
                        results.Add(new LintResult
                        {
                            Message = "Redundanter 'return;'-Befehl am Ende der Funktion.",
                            LocationHint = "return",
                            Color = "blue"
                        });
                    }
                    if (r.ReturnValue != null)
                        CountVariableUsage(r.ReturnValue, usage);
                    break;

                case IfStatement ifs:
                    CountVariableUsage(ifs.Condition, usage);
                    foreach (var thenStmt in ifs.ThenBranch)
                        LintStatement(thenStmt, usage, func);
                    foreach (var elseStmt in ifs.ElseBranch)
                        LintStatement(elseStmt, usage, func);
                    break;
            }
        }

        private void CountVariableUsage(Expression expr, Dictionary<string, int> usage)
        {
            switch (expr)
            {
                case VariableExpression v:
                    usage.TryAdd(v.Name, 0);
                    usage[v.Name]++;
                    break;
                case BinaryExpression b:
                    CountVariableUsage(b.Left, usage);
                    CountVariableUsage(b.Right, usage);
                    break;
                case FunctionCallExpression f:
                    foreach (var arg in f.Arguments)
                        CountVariableUsage(arg, usage);
                    break;
                case IndexExpression i:
                    CountVariableUsage(i.Target, usage);
                    CountVariableUsage(i.Index, usage);
                    break;
            }
        }

        private bool IsLastStatement(FunctionDeclaration func, Statement stmt)
        {
            return func.Body.LastOrDefault() == stmt;
        }
    }
}
