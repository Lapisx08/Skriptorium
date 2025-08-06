using System.Text;
using System.Text.RegularExpressions;

namespace Skriptorium.Formatting
{
    public class DaedalusFormatter
    {
        private const int IndentSize = 4;

        private string Indent(int level) => new string(' ', IndentSize * level);

        public string Format(string script)
        {
            var sb = new StringBuilder();
            int indentLevel = 0; // Aktuelle Verschachtelungsebene
            bool isInsideDeclaration = false; // Verfolgt, ob wir in einer Deklaration (func/instance) sind

            // Zeilenweise Verarbeitung
            string[] lines = script.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                string trimmedLine = line.TrimStart(); // Entferne führende Leerzeichen für die Analyse
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    // Leere Zeilen unverändert hinzufügen (optional mit Einrückung)
                    sb.AppendLine();
                    continue;
                }

                // Prüfe, ob die Zeile eine öffnende Klammer enthält
                bool hasOpeningBrace = trimmedLine.Contains("{");
                // Prüfe, ob die Zeile eine schließende Klammer enthält
                bool hasClosingBrace = trimmedLine.Contains("}");
                // Ignoriere Klammern in Kommentaren oder Strings
                if (IsInCommentOrString(trimmedLine))
                {
                    hasOpeningBrace = false;
                    hasClosingBrace = false;
                }

                // Wenn die Zeile eine schließende Klammer enthält, reduziere die Einrückungsebene *vor* der Zeile
                if (hasClosingBrace)
                {
                    indentLevel = Math.Max(0, indentLevel - 1);
                }

                // Füge die Zeile mit der aktuellen Einrückung hinzu
                sb.AppendLine(Indent(indentLevel) + trimmedLine);

                // Wenn die Zeile eine öffnende Klammer enthält, erhöhe die Einrückungsebene *nach* der Zeile
                if (hasOpeningBrace)
                {
                    indentLevel++;
                }

                // Prüfe, ob die Zeile eine func- oder instance-Deklaration beginnt
                if (Regex.IsMatch(trimmedLine, @"^(func|instance)\b"))
                {
                    isInsideDeclaration = true;
                }
                // Prüfe, ob die Deklaration endet (nach schließender Klammer)
                if (hasClosingBrace && indentLevel == 0)
                {
                    isInsideDeclaration = false;
                    sb.AppendLine(); // Füge eine leere Zeile nach der Deklaration hinzu
                }
            }

            return sb.ToString().TrimEnd();
        }

        // Hilfsmethode: Prüft, ob die Zeile ein Kommentar oder ein String ist (um Klammern zu ignorieren)
        private bool IsInCommentOrString(string line)
        {
            // Einfache Prüfung: Ignoriere Zeilen, die mit // beginnen oder in Anführungszeichen sind
            line = line.TrimStart();
            if (line.StartsWith("//"))
                return true;

            // Prüfe auf Strings (einfache Heuristik, kann erweitert werden)
            if (line.Contains("\""))
            {
                // Annahme: Wenn { oder } in einem String vorkommen, ignorieren wir sie
                int quoteCount = line.Count(c => c == '"');
                if (quoteCount % 2 == 0) // Nur geschlossene Strings
                {
                    // Prüfe, ob { oder } innerhalb von Anführungszeichen liegt
                    bool insideString = false;
                    foreach (char c in line)
                    {
                        if (c == '"')
                            insideString = !insideString;
                        if ((c == '{' || c == '}') && insideString)
                            return true;
                    }
                }
            }

            return false;
        }
    }
}