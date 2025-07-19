using System.Windows.Controls;

namespace Skriptorium.UI
{
    public partial class ScriptEditor : UserControl
    {
        private string _originalText = "";
        private bool _suppressChangeTracking = false;

        public ScriptEditor()
        {
            InitializeComponent();
            myTextBox.TextChanged += MyTextBox_TextChanged;
        }

        private void MyTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_suppressChangeTracking)
                return;

            // Prüfen, ob aktueller Text vom Original abweicht
            IsModified = myTextBox.Text != _originalText;

            // Event nach außen feuern
            TextChanged?.Invoke(this, e);
        }

        // Text des Editors
        public string Text
        {
            get => myTextBox.Text;
            set
            {
                _suppressChangeTracking = true;
                myTextBox.Text = value;
                _originalText = value;
                _suppressChangeTracking = false;
                IsModified = false;
            }
        }

        // Gibt an, ob der Text geändert wurde im Vergleich zum Original
        public bool IsModified { get; private set; } = false;

        // Pfad der aktuell geöffneten Datei (kann leer sein)
        public string FilePath { get; set; } = "";

        // Referenz auf den TextBlock, der den Tab-Titel anzeigt, um Sternchen zu setzen/entfernen
        public TextBlock? TitleTextBlock { get; set; }

        // Event für Textänderungen
        public event TextChangedEventHandler? TextChanged;

        // Setzt das Änderungs-Flag zurück (z.B. nach Speichern)
        public void ResetModifiedFlag()
        {
            _originalText = myTextBox.Text;
            IsModified = false;

            // TextChanged-Event manuell auslösen, damit der Stern entfernt wird
            TextChanged?.Invoke(this, new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None));
        }

        // Zum temporären Deaktivieren der Änderungsverfolgung (z.B. beim Setzen von Text)
        public bool SuppressChangeTracking
        {
            get => _suppressChangeTracking;
            set => _suppressChangeTracking = value;
        }

        // Ermöglicht Zugriff auf das interne TextBox-Element von außen (für Undo/Redo etc.)
        public TextBox TextBox => myTextBox;
    }
}
