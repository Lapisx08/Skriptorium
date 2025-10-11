using Skriptorium.Analysis;
using Skriptorium.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skriptorium.Analysis
{
    public class AutocompletionEngine
    {
        private readonly DaedalusLexer lexer = new DaedalusLexer();
        private readonly Dictionary<string, string> symbolTable = new(); // Name -> Typ
        private readonly HashSet<string> suggestions = new(StringComparer.Ordinal); // Case-sensitive Speicherung

        public AutocompletionEngine()
        {
            // Vorschläge mit vordefinierten Schlüsselwörtern und Konstanten aus PredefinedSuggestions initialisieren
            suggestions.UnionWith(PredefinedSuggestions.GetSuggestions());
        }

        // Tokenisierung des Eingabecodes und Extraktion von Symbolen für die Autovervollständigung
        public void UpdateSymbolsFromCode(string[] lines)
        {
            var tokens = lexer.Tokenize(lines);
            symbolTable.Clear();
            suggestions.Clear();
            suggestions.UnionWith(PredefinedSuggestions.GetSuggestions());

            // Token verarbeiten, um Bezeichner, Funktionsnamen, Instanznamen und Konstanten/Funktionen zu extrahieren
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                Console.WriteLine($"Token: {token.Value}, Type: {token.Type}, Line: {token.Line}, Column: {token.Column}");

                switch (token.Type)
                {
                    case TokenType.Identifier:
                    case TokenType.FunctionName:
                    case TokenType.InstanceName:
                        if (!symbolTable.ContainsKey(token.Value) && token.Value.Length >= 3 && IsDeclaredIdentifier(tokens, i))
                        {
                            string inferredType = InferTypeFromContext(tokens, i);
                            symbolTable[token.Value] = inferredType;
                            suggestions.Add(token.Value);
                            Console.WriteLine($"Added to suggestions: {token.Value} (Type: {inferredType})");
                        }
                        break;

                    case TokenType.GuildConstant:
                    case TokenType.NPC_Constant:
                    case TokenType.AIVConstant:
                    case TokenType.FAIConstant:
                    case TokenType.CRIMEConstant:
                    case TokenType.LOCConstant:
                    case TokenType.PETZCOUNTERConstant:
                    case TokenType.LOGConstant:
                    case TokenType.FONTConstant:
                    case TokenType.REALConstant:
                    case TokenType.ATRConstant:
                    case TokenType.ARConstant:
                    case TokenType.PLAYERConstant:
                    case TokenType.BuiltInFunction:
                    case TokenType.MdlFunction:
                    case TokenType.AIFunction:
                    case TokenType.NpcFunction:
                    case TokenType.InfoFunction:
                    case TokenType.CreateFunction:
                    case TokenType.WldFunction:
                    case TokenType.LogFunction:
                    case TokenType.HlpFunction:
                    case TokenType.SndFunction:
                    case TokenType.TAFunction:
                    case TokenType.EquipFunction:
                    case TokenType.ZENConstant:
                    case TokenType.SexConstant:
                    case TokenType.AiVariable:
                        if (!symbolTable.ContainsKey(token.Value))
                        {
                            symbolTable[token.Value] = token.Type.ToString();
                            suggestions.Add(token.Value);
                            Console.WriteLine($"Added to suggestions: {token.Value} (Type: {token.Type})");
                        }
                        break;
                }
            }

            Console.WriteLine("Current suggestions: " + string.Join(", ", suggestions));
        }

        private bool IsDeclaredIdentifier(List<DaedalusToken> tokens, int currentIndex)
        {
            if (currentIndex <= 0)
                return false;

            var prevToken = tokens[currentIndex - 1];
            if (prevToken.Type == TokenType.VarKeyword ||
                prevToken.Type == TokenType.FuncKeyword ||
                prevToken.Type == TokenType.InstanceKeyword)
            {
                return true;
            }

            if (prevToken.Type == TokenType.TypeKeyword && currentIndex > 1)
            {
                var prevPrevToken = tokens[currentIndex - 2];
                if (prevPrevToken.Type == TokenType.FuncKeyword)
                {
                    return true;
                }
            }

            return false;
        }

        private string InferTypeFromContext(List<DaedalusToken> tokens, int currentIndex)
        {
            if (currentIndex > 0)
            {
                var prevToken = tokens[currentIndex - 1];
                if (prevToken.Type == TokenType.TypeKeyword)
                {
                    return prevToken.Value;
                }
                else if (prevToken.Type == TokenType.FuncKeyword)
                {
                    for (int i = currentIndex - 2; i >= 0; i--)
                    {
                        if (tokens[i].Type == TokenType.TypeKeyword)
                        {
                            return tokens[i].Value;
                        }
                    }
                    return "void";
                }
                else if (prevToken.Type == TokenType.InstanceKeyword)
                {
                    return "instance";
                }
            }
            return "unknown";
        }

        public List<string> GetSuggestions(string prefix)
        {
            return suggestions
                .Where(s => s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) // Case-insensitive Filterung
                .Distinct(StringComparer.Ordinal) // Case-sensitive Unterscheidung für Ergebnisse
                .OrderBy(s => s)
                .ToList();
        }
    }
}