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
            int indentLevel = 0;

            string[] lines = script.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    sb.AppendLine(); // Leere Zeilen beibehalten
                    continue;
                }

                int netBraces = CountNetBraces(trimmedLine);

                // Wenn die Zeile schließende Klammern enthält, vor der Zeile die Einrückung reduzieren
                if (netBraces < 0)
                {
                    indentLevel = Math.Max(0, indentLevel + netBraces);
                }

                // Zeile mit aktueller Einrückung hinzufügen
                sb.AppendLine(Indent(indentLevel) + trimmedLine);

                // Wenn die Zeile öffnende Klammern enthält, nach der Zeile Einrückung erhöhen
                if (netBraces > 0)
                {
                    indentLevel += netBraces;
                }
            }

            return sb.ToString().TrimEnd();
        }

        // Zählt die Differenz von { und } in einer Zeile unter Berücksichtigung von Strings/Kommentare
        private int CountNetBraces(string line)
        {
            if (IsInCommentOrString(line))
                return 0;

            int count = 0;
            bool insideString = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    insideString = !insideString;
                    continue;
                }

                if (insideString)
                    continue;

                if (c == '{') count++;
                if (c == '}') count--;
            }

            return count;
        }

        // Prüft, ob die Zeile ein Kommentar oder String ist
        private bool IsInCommentOrString(string line)
        {
            line = line.TrimStart();

            if (line.StartsWith("//"))
                return true;

            int quoteCount = 0;
            foreach (char c in line)
            {
                if (c == '"')
                    quoteCount++;
            }

            // Ungeschlossene Strings oder leere Zeilen ignorieren
            if (quoteCount % 2 != 0)
                return true;

            return false;
        }
    }
}
