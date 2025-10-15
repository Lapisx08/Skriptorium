# Skriptorium
Das Skriptorium ist ein Skripteditor zur Erstellung und Bearbeitung von Daedalus-Skripten für das Modding von Gothic. Es können Daedalus-Skripte (.d), Source-Skripte (.src) und Textdateien (.txt) geöffnet und gespeichert werden. Inspiriert ist das Programm vom Skripteditor Stampfer.

Wenn das Programm Skriptorium als Standardprogramm für Daedalus-Skripte gesetzt ist, wird beim Öffnen eines solchen Skripts das Programm automatisch gestartet und die Datei geöffnet. Das Skriptorium lässt nur eine Instanz des Programms zu. Das heißt, beim Öffnen von Daedalus-Skripten im Explorer werden diese im laufenden Programm geöffnet. Ist die Tableiste überladen und schwebt man mit der Maus darüber, kann man mithilfe des Mausrads durch die Tabs scrollen. Alternativ lässt sich die Tableiste unterhalb des Dateinamens anklicken, um mit den linken und rechten Pfeiltasten durch die Tableiste zu navigieren.

## Funktionen
⦁ Speziell für Daedalus-Skripte optimiert
⦁ Moderne WPF-Oberfläche mit hellem und dunklem Anzeigemodus
⦁ Effiziente Tabverwaltung und Docking der Skripte
⦁ Daedalus-Syntax-Highlighting für bessere Lesbarkeit
⦁ NPC und Dialog Generator zur Zeitersparnis beim Skripten
⦁ Weitere gängige Funktionen

## Systemanforderungen
⦁ Zielbetriebssystem 10.0.26100.0 (Windows 11)
⦁ Unterstütztes Mindest-Betriebssystemversion 10.0.19041.0 (Windows 10)
⦁ .NET 8.0 Runtime (Windows-spezifische Variante)

## Installation
1. SkriptoriumSetup.exe starten
2. Folge dem Installationsassistenten

## Genutzte Pakete
 ⦁ MahApps.Metro (2.4.11)
 ⦁ AvalonEdit (6.3.1.120)
 ⦁ Dirkster.AvalonDock (4.72.1)
 ⦁ Dirkster.AvalonDock.Themes.VS2013 (4.72.1)
 ⦁ Ookii.Dialogs.Wpf (5.0.1)

## Entwicklung
Das Projekt wurde in C# mit .NET 8.0 WPF in Visual Studio 2022 entwickelt

## Entwickler
⦁ Lapis
⦁ Nemora26

## Lizenz
Lizenziert unter der GNU General Public License Version 3. Genaue Informationen siehe LICENSE.txt

## Kontaktinformationen
Kontaktmöglichkeit bei Fragen, Kritik oder Interesse an einer Mitwirkung des Projekts: lapisx08@protonmail.com

## Hinweise
⦁ Der Microsoft Defender (Windows Defender) kann anschlagen, weil das Programm weder mit einem Zertifikat versehen noch bei Microsoft gelistet ist
⦁ Die Autovervollständigung deckt die wichtigsten Begriffe der Original-Skripte von Gothic 2 ab, allerdings ist die Erfassung noch nicht vollständig
⦁ Der Parser ist noch unvollständig. Das Tool Codestruktur ist vom Perser abhängig. Deswegen stürzt das Programm ab, wenn das Tool Codestruktur die Struktur eins Skripts aufzuschlüsseln versucht, das nicht fehlerfrei geparst werden kann
⦁ Das Syntax-Highlighting kann geringfügige Unstimmigkeiten aufweisen


## Anleitung



### Über Skriptorium
- Beinhaltet Informationen über Version, Entwickler, Datum der Version und Lizenzierung

### Einstellungen
Allgemein:
- Änderung des Anzeigemodus (Tag- und Nachtmodus)

Pfade:
- Hier wird das Skriptverzeichnis des Gothic-Ordners gesetzt. Dieses dient als Stammverzeichnis. Datei Explorer, Explorer Suche, Suchen und Ersetzen beziehen sich bei der Suche auf dieses Stammverzeichnis

### Datei

Öffnen:
- Beim Öffnen einer Datei wird immer der letzte Pfad geöffnet, von dem aus eine Datei erfolgreich geöffnet wurde. Wird eine Datei außerhalb des Stammverzeichnisses geöffnet, springt das Programm beim nächsten Öffnen zum Stammverzeichnis

Zuletzt geöffnet: 
- Es werden die letzten 20 Dateien angezeigt, die man geöffnet hat

## Bearbeiten

Duplizieren:
- Um Text duplizieren zu können, muss der Text erst markiert und dann dupliziert werden

### Suchen
Suchen und Ersetzen:
- Für die Suche in einem Skript sollte dieses aktiv sein, bevor man Suchen und Ersetzen öffnet. Bei Eingabe ins Suchfeld werden die Treffer in Echtzeit gelb markiert
- Um im Skript zwischen den Treffen zu springen muss man den Button "Suchen" betätigen

Ersetzen:
- Ersetzt den nächsten Treffer mit dem Text, das in dem Feld "Ersetzen durch" steht. Wenn das Feld leer ist, werden die Treffer gelöscht

Alles Ersetzen:
- Ersetzt alle Treffer im Skript

Suchen in:
- Bei Aktivierung können alle offenen Skripte oder das Stammverzeichnis durchsucht und Text ersetzt werden. Für die erste Suche muss hier der Suche Button betätigt werden
- Änderungen im Suchfeld oder auch der Wechsel zwischen "In allen offenen Skripten" und "Im gesetzten Verzeichnis" werden dann zur Laufzeit aktualisiert
- Suche muss nur neu gestartet werden, wenn das Feld leer ist, dann schließt sich das Fenster "Suchergebnisse". Beim Schließen des "Suchen und Ersetzen" Fensters bleibt das Fenster Suchergebnisse geöffnet

### Lesezeichen
- Mit dieser Funktion kann man Lesezeichen in einem Skript setzen, um sich stellen zu markieren, die man im Laufe des Arbeitsvorgangs wieder auffinden möchte. Die Lesezeichen werden links vor dem eigentlichen Text in der Oberfläche erzeugt
- Die Lesezeichen werden nicht im Skript gespeichert. Das bedeutet beim Schließen und erneuten Öffnen verschwinden diese

### Tools

Syntax-Highlighting umschalten:
- Kann ein und ausgeschaltet werden

Autovervollständigung umschalten:
- Kann ein und ausgeschalten werden

Text einrücken:
- Text innerhalb einer geschweiften Klammer wird um 4 Leerzeichen eingerückt

Syntax prüfen:
- Prüft Syntax des Skripts. Dieses Tool funktioniert noch nicht einwandfrei, weshalb man sich darauf nicht verlassen sollte

Datei Explorer:
- Angezeigt wird das gesetzte Stammverzeichnis. Über dieses kann man neue Daedalus-Dateien (.d) und Ordner in der Struktur erstellen und umbenennen. Das Kontextmenü kann mit einem Rechtsklick im Datei Explorer geöffnet werden. Um Dateien löschen und umbenennen zu können, müssen diese durch Auswahl markiert sein
- Datei Explorer springt in dem View tree immer zum aktiven Skript

Explorer Suche:
- Bietet ein angenehmes Sucherlebnis zur Laufzeit im Stammverzeichnis. Treffer werden im Fenster gelb markiert
- Macht im Grunde dasselbe wie die Funktion "Suchen in" in Suchen und Ersetzen, aber die Explorer Suche ist für Suchen zur Laufzeit optimiert

Code Struktur:
- Der Parser erkennt Instanzen, Funktionen, Variablen und Konstanten in einem Skript. Die Struktur wird in dem Fenster übersichtlich aufgeschlüsselt (siehe Hinweise)


### NPC Generator

Name:
- Eingabe des NPC-Namens, z. B. "Gottfried" (Zahlen sind nicht zulässig)

Gilde:
- Eingabe der NPC-Gilde
- Es reicht die Gildenabkürzung einzugeben, z. B. "PAL", aber die Schreibweise "GIL_PAL" wird ebenfalls korrekt in der Generierung berücksichtigt (Zahlen sind nicht zulässig)

ID:
- Eingabe der NPC-ID (es sind nur Zahlen erlaubt)

Voice:
- Eingabe der NPC-Stimme (es sind nur Zahlen erlaubt)

Flags:
- Auswahl, ob NPC unsterblich sein soll
- 0 kann sterben, NPC_FLAG_IMMORTAL kann nicht sterben

NPC Type:
- Auswahl wie der NPC gegenüber dem Helden eingestellt ist
- NPCTYPE_MAIN neutrales Verhalten und NPCTYPE_FRIEND vertrautes Verhalten

AIVARs:
- Fügt bei "Ja", AIVARs hinzu
- Regelt wie sich der NPC in gewissen Situationen verhält; nicht benötigte AIVARs einfach löschen

Individuelle Attribute:
- Fügt bei "Ja" individuelle Attribute hinzu
- Individuelle Anpassung von Stärke, Gesichklichkeit usw. unabhängig vom Kapitel (bei Nutzung der individuellen Attribute "B_SetAttributesToChapter" löschen)

Individuelle Kampf-Talente:
- Fügt bei "Ja" individuelle Kampftalente hinzu (individuelle Anpassung von Einhand-, Zweihand-Talent usw; Bei Nutzung der individuellen Kampftalente "B_SetFightSkills" löschen)

Geschlecht:
- Auswahl des NPC-Geschlechts
- Generiert B_SetNpcVisual automatisch aus Original-Visuals von Gothic 2

### Dialog Generator 

Dialoginstantz:
- Eingabe der Dialoginstanz-Namens
- DIA_ wird standardmäßig als Präfix hinzugefügt

NPC-Instanz:
- Eingabe der dazugehörigen NPC-Instanz

Beschreibung:
- Eingabe der Beschreibung

Dialognummer:
- Eingabe der Dialognummer
- Die Dialognummer beeinflusst die Anzeige im Dialogfenster des Spiels. Niedrige erscheinen über höheren Nummern

Wichtig:
- Bei "Ja" spricht spricht der NPC den Held von sich aus an

Permanent:
- Bei "Ja" wird der Dialog immer angezeigt, auch wenn man diesen schon mal durchgegangen ist

Auswahlmöglichkeiten:
- Beim letzten Feld kann man zwischen Dialog, XP geben, Item geben und Ende-Dialog auswählen
- Bei der Auswahl Held oder NPC ist die Logik so aufgebaut, dass bei der Auswahl "NPC" der NPC etwas zum Held sagt beziehungsweise der NPC dem Held etwas gibt und umgekehrt.

Zeile hinzufügen:
- Über Zeile hinzufügen können Dialogzeilen hinzugefügt werden, über den Minus-Button können Dialogzeilen gelöscht werden
