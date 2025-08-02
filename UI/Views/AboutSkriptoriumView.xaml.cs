using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using MahApps.Metro.Controls;

namespace Skriptorium.UI.Views
{
    public partial class AboutSkriptoriumView : MetroWindow
    {
        public AboutSkriptoriumView()
        {
            InitializeComponent();

            // Pfad zur aktuellen EXE
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string lastModified = File.GetLastWriteTime(exePath).ToString("dd.MM.yyyy");

            TextBlockDatum.Text = $"Datum: {lastModified}";
        }

        private void LicenseButton_Click(object sender, RoutedEventArgs e)
        {
            // Pfad zur Lizenzdatei im Programmordner (anpassen, falls nötig)
            string licensePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LICENSE.txt");

            if (File.Exists(licensePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(licensePath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lizenzdatei konnte nicht geöffnet werden:\n" + ex.Message,
                                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Lizenzdatei wurde nicht gefunden.", "Fehler",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
