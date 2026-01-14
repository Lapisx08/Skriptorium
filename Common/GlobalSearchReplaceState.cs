using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skriptorium.Common
{
    public static class GlobalSearchReplaceState
    {
        // Zuletzt gesuchter Text
        public static string LastSearchText { get; set; } = "";

        // Zuletzt eingegebener Ersatztext
        public static string LastReplaceText { get; set; } = "";

        // Ob die Suche Groß-/Kleinschreibung beachten soll
        public static bool MatchCase { get; set; } = false;

        // Ob nur ganze Wörter gefunden werden sollen
        public static bool WholeWord { get; set; } = false;

        // Ob das Such-Panel aktuell offen ist
        public static bool IsSearchPanelOpen { get; set; } = false;

        // Ob der Ersetzen-Bereich aktuell offen ist
        public static bool IsReplacePanelOpen { get; set; } = false;
    }
}
