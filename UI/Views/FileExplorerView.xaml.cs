using System;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;

namespace Skriptorium.UI.Views
{
    public partial class FileExplorerView : UserControl
    {
        public ObservableCollection<FileNode> RootDirectories { get; } = new ObservableCollection<FileNode>();

        public FileExplorerView()
        {
            InitializeComponent();
            DataContext = this;
            LoadStartDirectory();

            // Auf Änderungen der Einstellungen reagieren
            Properties.Settings.Default.PropertyChanged += SettingsChanged;
        }

        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Properties.Settings.Default.ScriptSearchPath))
            {
                Dispatcher.Invoke(ReloadRootDirectory);
            }
        }

        private void LoadStartDirectory()
        {
            RootDirectories.Clear();

            // Startverzeichnis aus den App-Einstellungen holen
            string startPath = Properties.Settings.Default.ScriptSearchPath;

            // Fallback, falls der Pfad leer oder nicht vorhanden ist
            if (string.IsNullOrWhiteSpace(startPath) || !Directory.Exists(startPath))
            {
                startPath = @"C:\";
            }

            RootDirectories.Add(new FileNode(startPath, isRoot: true));
        }

        /// <summary>
        /// Diese Methode kann aufgerufen werden, um den Explorer sofort neu zu laden
        /// </summary>
        public void ReloadRootDirectory()
        {
            LoadStartDirectory();
        }

        private void FileTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FileTree.SelectedItem is FileNode node && !node.IsDirectory && !node.IsDummy)
            {
                try
                {
                    var mainWindow = Window.GetWindow(this) as MainWindow;
                    var content = File.ReadAllText(node.FullPath);
                    mainWindow?.OpenFileInNewTab(content, node.FullPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Öffnen der Datei:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void FileTree_Expanded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem item && item.DataContext is FileNode node)
            {
                node.LoadChildren();
            }
        }
    }

    public class FileNode
    {
        private bool _isLoaded;
        public bool IsDummy { get; } = false;
        public bool IsRoot { get; } = false;
        public string FullPath { get; }
        public string Name => string.IsNullOrEmpty(Path.GetFileName(FullPath)) ? FullPath : Path.GetFileName(FullPath);
        public bool IsDirectory { get; }
        public ImageSource Icon => IconCache.GetIcon(FullPath, IsDirectory, IsRoot);
        public ObservableCollection<FileNode> Children { get; } = new ObservableCollection<FileNode>();

        public FileNode(string path, bool isDummy = false, bool isRoot = false)
        {
            if (isDummy)
            {
                IsDummy = true;
                FullPath = string.Empty;
                IsDirectory = false;
                IsRoot = false;
                return;
            }

            FullPath = path ?? string.Empty;
            IsRoot = isRoot;

            if (!string.IsNullOrWhiteSpace(FullPath))
            {
                if (Directory.Exists(FullPath))
                    IsDirectory = true;
                else
                {
                    try
                    {
                        IsDirectory = File.GetAttributes(FullPath).HasFlag(FileAttributes.Directory);
                    }
                    catch { IsDirectory = false; }
                }
            }
            else
                IsDirectory = false;

            if (IsDirectory)
            {
                Children.Add(new FileNode(null, isDummy: true));
            }
        }

        public void LoadChildren()
        {
            if (_isLoaded || !IsDirectory || IsDummy) return;

            Children.Clear();

            try
            {
                foreach (var dir in Directory.GetDirectories(FullPath))
                    Children.Add(new FileNode(dir));

                var patterns = new[] { "*.d", "*.txt" };
                foreach (var pattern in patterns)
                {
                    foreach (var file in Directory.GetFiles(FullPath, pattern))
                        Children.Add(new FileNode(file));
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
            catch (IOException) { }

            _isLoaded = true;
        }
    }

    internal static class IconCache
    {
        private static readonly ConcurrentDictionary<string, ImageSource> _cache = new ConcurrentDictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);
        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

        public static ImageSource GetIcon(string path, bool isDirectory, bool isRoot = false)
        {
            if (string.IsNullOrEmpty(path))
                return GetFallbackIcon(isDirectory);

            if (isRoot)
                return GetShellIcon(path, true);

            return GetShellIcon(path, isDirectory);
        }

        private static ImageSource GetShellIcon(string path, bool isDirectory)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            uint flags = SHGFI_ICON | SHGFI_SMALLICON;
            uint attributes = isDirectory ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;
            string query = path;

            if (!isDirectory && !string.IsNullOrEmpty(path) && !File.Exists(path))
            {
                flags |= SHGFI_USEFILEATTRIBUTES;
                query = Path.GetExtension(path);
                if (string.IsNullOrEmpty(query))
                    query = ".";
            }

            IntPtr hRes = SHGetFileInfo(query, attributes, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
            if (hRes == IntPtr.Zero)
                return GetFallbackIcon(isDirectory);

            try
            {
                return Imaging.CreateBitmapSourceFromHIcon(
                    shinfo.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                if (shinfo.hIcon != IntPtr.Zero)
                    DestroyIcon(shinfo.hIcon);
            }
        }

        private static ImageSource GetFallbackIcon(bool isDirectory)
        {
            return new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);
        }

        #region PInvoke
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
        #endregion
    }
}
