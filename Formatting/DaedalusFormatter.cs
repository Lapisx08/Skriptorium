using System.Text;
using Skriptorium.Parsing;

namespace Skriptorium.Formatting
{
    public class DaedalusFormatter
    {
        private const int IndentSize = 4;

        private string Indent(int level) => new string(' ', IndentSize * level);

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
                            sb.AppendLine(FormatStatement(stmt, 1));
                        }

                        sb.AppendLine("}");
                        sb.AppendLine();
                        break;

                    case InstanceDeclaration inst:
                        sb.Append($"instance {inst.Name}({inst.BaseClass}) ");
                        sb.AppendLine("{");

                        foreach (var stmt in inst.Body ?? new List<Statement>())
                        {
                            sb.AppendLine(FormatStatement(stmt, 1));
                        }

                        sb.AppendLine("}");
                        sb.AppendLine();
                        break;

                    default:
                        sb.AppendLine("// Unbekannter Deklarationstyp");
                        break;
                }
            }

            return sb.ToString().TrimEnd();
        }

        private string FormatStatement(Statement stmt, int indentLevel)
        {
            string indent = Indent(indentLevel);

            return stmt switch
            {
                Assignment a => indent + $"{FormatExpr(a.Left)} = {FormatExpr(a.Right)};",
                ReturnStatement r => indent + (r.ReturnValue != null ? $"return {FormatExpr(r.ReturnValue)};" : "return;"),
                ExpressionStatement e => indent + $"{FormatExpr(e.Expr)};",
                IfStatement ifStmt => FormatIfStatement(ifStmt, indentLevel),
                _ => indent + "// unbekannte Anweisung"
            };
        }

        private string FormatIfStatement(IfStatement ifStmt, int indentLevel)
        {
            var sb = new StringBuilder();
            string indent = Indent(indentLevel);

            sb.AppendLine($"{indent}if ({FormatExpr(ifStmt.Condition)}) {{");

            foreach (var stmt in ifStmt.ThenBranch ?? new List<Statement>())
            {
                sb.AppendLine(FormatStatement(stmt, indentLevel + 1));
            }

            sb.AppendLine($"{indent}}}");

            if (ifStmt.ElseBranch != null)
            {
                sb.AppendLine($"{indent}else {{");

                foreach (var stmt in ifStmt.ElseBranch)
                {
                    sb.AppendLine(FormatStatement(stmt, indentLevel + 1));
                }

                sb.AppendLine($"{indent}}}");
            }

            return sb.ToString();
        }

        private string FormatExpr(Expression expr)
        {
            return expr switch
            {
                LiteralExpression l => l.Value,
                VariableExpression v => v.Name,
                BinaryExpression b => $"{FormatExpr(b.Left)} {b.Operator} {FormatExpr(b.Right)}",
                FunctionCallExpression f => $"{f.FunctionName}({string.Join(", ", f.Arguments?.Select(FormatExpr) ?? new List<string>())})",
                _ => "<expr>"
            };
        }
    }
}
