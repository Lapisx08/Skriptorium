using System;
using System.Collections.Generic;

namespace Skriptorium.Parsing
{
    public class SymbolTable
    {
        private readonly Dictionary<string, Declaration> _globalIndex = new(StringComparer.OrdinalIgnoreCase);
        private readonly Stack<Dictionary<string, Declaration>> _scopes = new();

        public SymbolTable()
        {
            ResetScopes();
        }

        public void Register(string name, Declaration decl)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            // In den aktuellen lokalen Scope schreiben
            if (_scopes.Count > 0)
            {
                _scopes.Peek()[name] = decl;
            }

            // In den globalen Index schreiben, wenn wir auf Root-Level sind oder es ein globaler Typ ist
            if (_scopes.Count == 1 || IsPermanentGlobal(decl))
            {
                _globalIndex[name] = decl;
            }
        }

        private bool IsPermanentGlobal(Declaration decl)
        {
            return decl is FunctionDeclaration
                || decl is InstanceDeclaration
                || decl is PrototypeDeclaration
                || decl is ClassDeclaration
                || decl is VarDeclaration
                || decl is ConstDeclaration;
        }

        public Declaration Resolve(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            // 1. Suche in Scopes (lokal -> global)
            foreach (var scope in _scopes)
            {
                if (scope.TryGetValue(name, out var decl)) return decl;
            }

            // 2. Suche im globalen Index
            _globalIndex.TryGetValue(name, out var globalDecl);
            return globalDecl;
        }

        public void EnterScope() => _scopes.Push(new Dictionary<string, Declaration>(StringComparer.OrdinalIgnoreCase));

        public void ExitScope()
        {
            if (_scopes.Count > 1) _scopes.Pop();
        }

        public void Clear()
        {
            _globalIndex.Clear();
            ResetScopes();
        }

        private void ResetScopes()
        {
            _scopes.Clear();
            _scopes.Push(new Dictionary<string, Declaration>(StringComparer.OrdinalIgnoreCase));
        }

        public int Count => _globalIndex.Count;
    }
}