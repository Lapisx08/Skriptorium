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

        private readonly SemaphoreSlim _compileLock = new SemaphoreSlim(1, 1);


        public ProjectManager()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            DataManager.FileSaved += async (path) =>
            {
                await RefreshSingleFileAsync(path);
            };
        }

        public async Task LoadProjectAsync(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                return;

            var allFiles = Directory.GetFiles(rootPath, "*.d", SearchOption.AllDirectories);
            CurrentProjectFiles = allFiles.ToList();

            await RecompileAsync();
        }

        public async Task RecompileAsync()
        {
            if (!await _compileLock.WaitAsync(0))
                return;

            try
            {
                Compiler.ClearSymbols();                 // Nur beim Full-Rebuild!
                Compiler.CompileFiles(CurrentProjectFiles);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Recompile] Fehler: {ex}");
            }
            finally
            {
                _compileLock.Release();
            }
        }

        public async Task RefreshSingleFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            if (!await _compileLock.WaitAsync(0))
                return;

            try
            {
                // Wichtig: KEIN ClearSymbols() hier!
                Compiler.CompileFiles(new List<string> { filePath });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RefreshSingleFile] Fehler: {ex}");
            }
            finally
            {
                _compileLock.Release();
            }
        }
    }
}