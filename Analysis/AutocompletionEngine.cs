using System;
using System.Collections.Generic;
using System.Linq;
using Skriptorium.Parsing;

namespace TestProgram.Analysis
{
    public class AutocompletionEngine
    {
        // Symboltabelle (Name -> Typ), ähnlich wie im SemanticAnalyzer
        private readonly Dictionary<string, string> symbolTable = new();

        // Liste der verfügbaren Keywords, Funktionen etc.
        private static readonly List<string> keywords = new()
        {
            "instance", "func", "var", "const", "if", "else", "return",
            "class", "prototype", "int", "float", "void", "string",
            "true", "false"
        };

        // Hier könnten auch Funktionen mit Signaturen gepflegt werden:
        private readonly List<string> functions = new()
        {
            "Wild_InsertNpc",
            // weitere Funktionsnamen hier ergänzen
        };

        public AutocompletionEngine(List<Declaration> declarations)
        {
            // Symboltabelle aus den Declarations füllen
            foreach (var decl in declarations)
            {
                switch (decl)
                {
                    case VarDeclaration varDecl:
                        if (!symbolTable.ContainsKey(varDecl.Name))
                            symbolTable.Add(varDecl.Name, varDecl.TypeName);
                        break;

                    case FunctionDeclaration funcDecl:
                        if (!symbolTable.ContainsKey(funcDecl.Name))
                            symbolTable.Add(funcDecl.Name, funcDecl.ReturnType);
                        break;
                }
            }
        }

        // Methode, um Vorschläge basierend auf aktuellem Eingabetext zu bekommen
        public List<string> GetSuggestions(string prefix)
        {
            var suggestions = new List<string>();

            // Keywords vorschlagen
            suggestions.AddRange(keywords.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));

            // Variablen vorschlagen
            suggestions.AddRange(symbolTable.Keys.Where(v => v.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));

            // Funktionen vorschlagen
            suggestions.AddRange(functions.Where(f => f.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));

            // Dopplungen entfernen und sortieren
            return suggestions.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();
        }
    }
}
