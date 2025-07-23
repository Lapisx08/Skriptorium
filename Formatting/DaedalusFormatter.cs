using System.Text;
using Skriptorium.Parsing;

namespace Skriptorium.Formatting
{
    public class DaedalusFormatter
    {
        private const int IndentSize = 4;  // Einstellbare Einrückungsgröße (Standard: 4 Leerzeichen)
        private readonly string indentString;

        public DaedalusFormatter()
        {
            indentString = new string(' ', IndentSize);
        }

        public string Format(List<Declaration> declarations)
        {
            var sb = new StringBuilder();

            foreach (var decl in declarations)
            {
                switch (decl)
                {
                    case FunctionDeclaration func:
                        sb.Append($"func {func.ReturnType} {func.Name}(");
                        sb.Append(string.Join(", ", func.Parameters ?? new List<string>()));
                        sb.AppendLine(") {");

                        foreach (var stmt in func.Body ?? new List<Statement>())
                        {
                            sb.AppendLine(indentString + FormatStatement(stmt));
                        }

                        sb.AppendLine("}");
                        sb.AppendLine();  // Leerzeile nach der Deklaration
                        break;

                    case InstanceDeclaration inst:
                        sb.Append($"instance {inst.Name}({inst.BaseClass}) ");
                        sb.AppendLine("{");

                        foreach (var stmt in inst.Body ?? new List<Statement>())
                        {
                            sb.AppendLine(indentString + FormatStatement(stmt));
                        }

                        sb.AppendLine("}");
                        sb.AppendLine();  // Leerzeile nach der Deklaration
                        break;

                    default:
                        sb.AppendLine("// Unbekannter Deklarationstyp");
                        break;

                        // Weitere Deklarationstypen können hier hinzugefügt werden
                }
            }

            return sb.ToString().TrimEnd();  // Entfernt nachfolgende Leerzeichen
        }

        private string FormatStatement(Statement stmt)
        {
            return stmt switch
            {
                Assignment a => $"{FormatExpr(a.Left)} = {FormatExpr(a.Right)};",  // Zuweisung formatieren
                ReturnStatement r => r.ReturnValue != null ? $"return {FormatExpr(r.ReturnValue)};" : "return;",  // Rückgabe formatieren
                ExpressionStatement e => $"{FormatExpr(e.Expr)};",  // Ausdrucks-Anweisung formatieren
                IfStatement ifStmt => FormatIfStatement(ifStmt),  // If-Anweisung formatieren

                // Weitere Anweisungstypen können hier hinzugefügt werden
                _ => "// unbekannte Anweisung"
            };
        }

        private string FormatIfStatement(IfStatement ifStmt)
        {
            var sb = new StringBuilder();
            sb.Append($"if ({FormatExpr(ifStmt.Condition)}) ");
            sb.AppendLine("{");

            foreach (var stmt in ifStmt.ThenBranch ?? new List<Statement>())
            {
                sb.AppendLine(new string(' ', IndentSize * 2) + FormatStatement(stmt));  // Dann-Zweig mit doppelter Einrückung
            }

            sb.AppendLine(new string(' ', IndentSize) + "}");

            if (ifStmt.ElseBranch != null)
            {
                sb.AppendLine(new string(' ', IndentSize) + "else {");

                foreach (var stmt in ifStmt.ElseBranch)
                {
                    sb.AppendLine(new string(' ', IndentSize * 2) + FormatStatement(stmt));  // Sonst-Zweig mit doppelter Einrückung
                }

                sb.AppendLine(new string(' ', IndentSize) + "}");
            }

            return sb.ToString();
        }

        private string FormatExpr(Expression expr)
        {
            return expr switch
            {
                LiteralExpression l => l.Value,  // Literale direkt ausgeben
                VariableExpression v => v.Name,  // Variablennamen ausgeben
                BinaryExpression b => $"{FormatExpr(b.Left)} {b.Operator} {FormatExpr(b.Right)}",  // Binäre Ausdrücke formatieren
                FunctionCallExpression f => $"{f.FunctionName}({string.Join(", ", f.Arguments?.Select(FormatExpr) ?? new List<string>())})",  // Funktionsaufruf formatieren
                _ => "<expr>"  // Unbekannter Ausdruck
            };
        }
    }
}
