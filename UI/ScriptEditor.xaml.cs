using System;
using System.Windows.Controls;
using Skriptorium.UI.Views;

namespace Skriptorium.UI
{
    public partial class ScriptEditor : UserControl
    {
        private string _originalText = "";              // Originaltext zur Prüfung der Änderungen
        private bool _suppressChangeTracking = false;   // Verhindert die Änderungstracking bei bestimmten Aktionen
        private string _filePath = "";                  // Dateipfad zum Skript

        public ScriptEditor()
        {
            InitializeComponent();
            myTextBox.TextChanged += MyTextBox_TextChanged;  // Event abonnieren, um Textänderungen zu erkennen
        }

        // Methode, die Textänderungen verfolgt
        private void MyTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_suppressChangeTracking)
                return;

            // Prüfen, ob der Text geändert wurde
            IsModified = myTextBox.Text != _originalText;

            // Event nach außen auslösen
            TextChanged?.Invoke(this, e);
        }

        // Property, die angibt, ob der Text geändert wurde
        public bool IsModified { get; private set; } = false;

        // Nur Leserechte auf den Text – zum Setzen die Methoden verwenden
        public string Text => myTextBox.Text;

        // Den Pfad der Datei setzen
        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                _originalText = myTextBox.Text;  // Text mit dem Originaltext abgleichen
            }
        }

        // Text setzen und als unverändert markieren (z. B. nach Laden einer Datei)
        public void SetTextAndResetModified(string text)
        {
            _suppressChangeTracking = true;
            myTextBox.Text = text;
            _originalText = text;
            _suppressChangeTracking = false;
            IsModified = false;
        }

        // Text setzen und als verändert markieren (z. B. nach ReplaceAll)
        public void SetTextAndMarkAsModified(string text)
        {
            _suppressChangeTracking = true;
            myTextBox.Text = text;
            _suppressChangeTracking = false;
            IsModified = true;

            // TextChanged-Event manuell auslösen
            TextChanged?.Invoke(this, new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None));
        }

        // Methode zum Zurücksetzen des Änderungs-Flags (z. B. nach dem Speichern)
        public void ResetModifiedFlag()
        {
            _originalText = myTextBox.Text;
            IsModified = false;

            if (TitleTextBlock != null && TitleTextBlock.Text.EndsWith("*"))
            {
                TitleTextBlock.Text = TitleTextBlock.Text[..^1];
            }
        }

        // Event für Textänderungen
        public event TextChangedEventHandler? TextChanged;

        // Zugriff auf internes TextBox-Element
        public TextBox TextBox => myTextBox;

        // Für den Tab-Header
        public TextBlock? TitleTextBlock { get; set; }
    }
}
