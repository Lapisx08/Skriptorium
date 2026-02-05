using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Skriptorium.Parsing;

namespace Skriptorium.Managers
{
    public class ProjectManager
    {
        private static ProjectManager? _instance;
        public static ProjectManager Instance => _instance ??= new ProjectManager();

        public DaedalusCompiler Compiler { get; } = new DaedalusCompiler();
        public List<string> CurrentProjectFiles { get; set; } = new List<string>();

        private bool _isRecompiling = false;

        public ProjectManager()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public void LoadProject(string rootPath)
        {
            if (_isRecompiling) return;

            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath)) return;

            var allFiles = Directory.GetFiles(rootPath, "*.d", SearchOption.AllDirectories);

            CurrentProjectFiles = allFiles.ToList();

            Recompile();
        }

        public void Recompile()
        {
            if (_isRecompiling || CurrentProjectFiles == null || CurrentProjectFiles.Count == 0) return;

            try
            {
                _isRecompiling = true;
                Compiler.ClearSymbols();

                Compiler.CompileFiles(CurrentProjectFiles);
            }
            catch (Exception)
            {
            }
            finally
            {
                _isRecompiling = false;
            }
        }

        public void RefreshSingleFile(string filePath)
        {
            if (_isRecompiling || !File.Exists(filePath)) return;

            try
            {
                Compiler.CompileFiles(new List<string> { filePath });
            }
            catch (Exception)
            {
            }
        }
    }
}