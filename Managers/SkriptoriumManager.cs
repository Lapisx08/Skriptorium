using System;
using System.Windows;

namespace Skriptorium.Managers
{
    public static class SkriptoriumManager
    {
        // Programmname
        public static string ProgramName => "Skriptorium";

        // Programmversion (kann man auch aus Assembly auslesen)
        public static string Version => "1.0";

        // Zeigt eine Über-Dialog-MessageBox mit Programminfos an
        public static void ShowAboutDialog()
        {
            string datum = DateTime.Now.ToString("dd.MM.yyyy");

            string message =
                $"{ProgramName} ©\n" +
                $"Version: {Version}\n" +
                $"Entwickler: Lapis\n\n" +
                $"Datum: {datum}\n\n" +
                $"{ProgramName} ist eine kostenlose Software\n" +
                $"Lizensiert unter GNU General Public License";

            MessageBox.Show(message, $"Über {ProgramName}");
        }

        // Beispiel für Einstellungen (noch als Platzhalter)
        public static bool SomeSetting { get; set; } = true;

        // Öffnet (später) die Einstellungen — hier Platzhalter
        public static void ShowSettingsDialog()
        {
            MessageBox.Show("Einstellungen sind noch nicht implementiert.", "Einstellungen");
        }

        // Beendet die Anwendung (übergebenes Window schließen)
        public static void ExitApplication(Window mainWindow)
        {
            if (mainWindow != null)
            {
                mainWindow.Close();
            }
        }
    }
}
