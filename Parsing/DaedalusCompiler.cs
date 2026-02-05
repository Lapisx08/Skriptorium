using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Skriptorium.Parsing
{
    public class DaedalusCompiler
    {
        public SymbolTable GlobalSymbols { get; private set; } = new SymbolTable();
        public List<Declaration> FullAst { get; private set; } = new List<Declaration>();

        public void CompileFiles(List<string> filePaths)
        {
            ClearSymbols();

            foreach (string path in filePaths)
            {
                if (!File.Exists(path)) continue;

                try
                {
                    // Lösung A: Direkt zeilenweise einlesen (Besser für Line-Numbers im Go-To)
                    string[] lines = File.ReadAllLines(path, Encoding.GetEncoding(1252));

                    var lexer = new DaedalusLexer();
                    // Jetzt passt der Typ: lines ist string[], Tokenize erwartet string[]
                    var tokens = lexer.Tokenize(lines);

                    var parser = new DaedalusParser(tokens);
                    var fileAst = parser.ParseScript();

                    if (fileAst != null)
                    {
                        foreach (var decl in fileAst)
                        {
                            SetFilePathRecursive(decl, path);
                            FullAst.Add(decl);

                            // Registrierung im globalen Index
                            RegisterInTable(decl);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Fehler in {path}: {ex.Message}");
                }
            }
        }

        private void RegisterInTable(Declaration decl)
        {
            string name = GetDeclarationName(decl);
            if (!string.IsNullOrEmpty(name))
            {
                GlobalSymbols.Register(name, decl);
            }

            // Spezialfall für Sammel-Deklarationen (VAR INT A, B;)
            if (decl is MultiVarDeclaration mVar)
                mVar.Declarations?.ForEach(RegisterInTable);
            else if (decl is MultiConstDeclaration mConst)
                mConst.Declarations?.ForEach(RegisterInTable);
        }

        private string GetDeclarationName(Declaration decl) => decl switch
        {
            FunctionDeclaration f => f.Name,
            InstanceDeclaration i => i.Name,
            PrototypeDeclaration p => p.Name,
            ClassDeclaration c => c.Name,
            VarDeclaration v => v.Name,
            ConstDeclaration co => co.Name,
            _ => null
        };

        private void SetFilePathRecursive(Declaration decl, string path)
        {
            if (decl == null) return;
            decl.FilePath = path;

            if (decl is ClassDeclaration cls)
                cls.Declarations?.ForEach(d => SetFilePathRecursive(d, path));
            else if (decl is MultiVarDeclaration mVar)
                mVar.Declarations?.ForEach(d => SetFilePathRecursive(d, path));
            else if (decl is MultiConstDeclaration mConst)
                mConst.Declarations?.ForEach(d => SetFilePathRecursive(d, path));
            else if (decl is FunctionDeclaration func && func.Body != null)
            {
                foreach (var stmt in func.Body)
                    if ((object)stmt is Declaration sub) SetFilePathRecursive(sub, path);
            }
            // Analog für Instance/Prototype falls vorhanden
        }

        public void ClearSymbols()
        {
            GlobalSymbols.Clear();
            FullAst.Clear();
        }
    }
}