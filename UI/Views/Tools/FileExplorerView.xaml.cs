using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;
using Skriptorium.Common;

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

            string startPath = Properties.Settings.Default.ScriptSearchPath;

            if (string.IsNullOrWhiteSpace(startPath) || !Directory.Exists(startPath))
            {
                startPath = @"C:\";
            }

            RootDirectories.Add(new FileNode(startPath, isRoot: true));
        }

        public void ReloadRootDirectory()
        {
            foreach (var root in RootDirectories)
            {
                if (root.IsDirectory)
                    root.Refresh();
            }
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

        private FileNode GetTargetNode()
        {
            if (FileTree.SelectedItem is FileNode node && node.IsDirectory)
                return node;

            return RootDirectories.FirstOrDefault();
        }

        private void BtnCreateFile_Click(object sender, RoutedEventArgs e)
        {
            var node = GetTargetNode();
            if (node == null) return;

            string newPath = Path.Combine(node.FullPath, "Neue_Datei.d");
            int i = 1;
            while (File.Exists(newPath))
                newPath = Path.Combine(node.FullPath, $"Neue_Datei{i++}.d");

            File.WriteAllText(newPath, "");
            node.Refresh();
        }

        private void BtnCreateFolder_Click(object sender, RoutedEventArgs e)
        {
            var node = GetTargetNode();
            if (node == null) return;

            string newPath = Path.Combine(node.FullPath, "Neuer Ordner");
            int i = 1;
            while (Directory.Exists(newPath))
                newPath = Path.Combine(node.FullPath, $"Neuer Ordner {i++}");

            Directory.CreateDirectory(newPath);
            node.Refresh();
        }

        private void BtnCollapse_Click(object sender, RoutedEventArgs e)
        {
            CollapseAll(FileTree);
        }

        private void CollapseAll(ItemsControl parentContainer)
        {
            foreach (var item in parentContainer.Items)
            {
                if (parentContainer.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem treeItem)
                {
                    treeItem.IsExpanded = false;
                    CollapseAll(treeItem);
                }
            }
        }

        private void MenuRename_Click(object sender, RoutedEventArgs e)
        {
            if (FileTree.SelectedItem is FileNode node && !node.IsDummy)
            {
                string input = Microsoft.VisualBasic.Interaction.InputBox(
                    "Neuer Name:", "Umbenennen", node.Name);

                if (!string.IsNullOrWhiteSpace(input))
                {
                    string newFileName = input;
                    // Prüfen, ob die Datei eine .d-Datei ist und die Eingabe keine Erweiterung enthält
                    if (!node.IsDirectory && Path.GetExtension(node.FullPath).Equals(".d", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!Path.HasExtension(input))
                        {
                            newFileName = input + ".d"; // .d-Erweiterung hinzufügen
                        }
                    }

                    string newPath = Path.Combine(Path.GetDirectoryName(node.FullPath), newFileName);

                    try
                    {
                        if (node.IsDirectory)
                            Directory.Move(node.FullPath, newPath);
                        else
                            File.Move(node.FullPath, newPath);

                        if (node.Parent != null)
                            node.Parent.Refresh();
                        else
                            ReloadRootDirectory();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler beim Umbenennen:\n{ex.Message}");
                    }
                }
            }
        }

        private void MenuDelete_Click(object sender, RoutedEventArgs e)
        {
            if (FileTree.SelectedItem is FileNode node && !node.IsDummy)
            {
                if (MessageBox.Show($"Soll „{node.Name}“ wirklich gelöscht werden?",
                    "Löschen", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (node.IsDirectory)
                            Directory.Delete(node.FullPath, true);
                        else
                            File.Delete(node.FullPath);

                        if (node.Parent != null)
                            node.Parent.Refresh();
                        else
                            ReloadRootDirectory();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler beim Löschen:\n{ex.Message}");
                    }
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            ReloadRootDirectory();
        }

        // Drag-and-Drop-Handler
        private FileNode _dragSource;

        private void TreeViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem item && item.DataContext is FileNode node && !node.IsDummy)
            {
                _dragSource = node;
            }
        }

        private void TreeViewItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _dragSource != null)
            {
                var data = new DataObject(typeof(FileNode), _dragSource);
                DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move);
                _dragSource = null; // Reset nach Drag
            }
        }

        private void FileTree_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(FileNode)))
            {
                var sourceNode = e.Data.GetData(typeof(FileNode)) as FileNode;
                var target = GetDropTarget(e.OriginalSource);

                if (sourceNode != null && target != null && target.IsDirectory && !sourceNode.IsDummy)
                {
                    // Verhindern, dass ein Ordner in sich selbst oder einen Unterordner verschoben wird
                    if (sourceNode.IsDirectory && IsParentOrSelf(sourceNode, target))
                    {
                        e.Effects = DragDropEffects.None;
                    }
                    else
                    {
                        e.Effects = DragDropEffects.Move;
                    }
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void FileTree_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(FileNode)))
            {
                var sourceNode = e.Data.GetData(typeof(FileNode)) as FileNode;
                var targetNode = GetDropTarget(e.OriginalSource);

                if (sourceNode != null && targetNode != null && targetNode.IsDirectory && !sourceNode.IsDummy)
                {
                    // Verhindern, dass ein Ordner in sich selbst oder einen Unterordner verschoben wird
                    if (sourceNode.IsDirectory && IsParentOrSelf(sourceNode, targetNode))
                    {
                        return;
                    }

                    try
                    {
                        string newPath = Path.Combine(targetNode.FullPath, sourceNode.Name);

                        // Prüfen, ob das Ziel bereits existiert
                        int i = 1;
                        string baseName = Path.GetFileNameWithoutExtension(sourceNode.Name);
                        string extension = sourceNode.IsDirectory ? "" : Path.GetExtension(sourceNode.Name);
                        while (File.Exists(newPath) || Directory.Exists(newPath))
                        {
                            newPath = Path.Combine(targetNode.FullPath, $"{baseName} ({i++}){extension}");
                        }

                        // Verschieben der Datei oder des Ordners
                        if (sourceNode.IsDirectory)
                            Directory.Move(sourceNode.FullPath, newPath);
                        else
                            File.Move(sourceNode.FullPath, newPath);

                        // Aktualisieren der Baumansicht
                        if (sourceNode.Parent != null)
                            sourceNode.Parent.Refresh();
                        else
                            ReloadRootDirectory();

                        targetNode.Refresh();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler beim Verschieben:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            e.Handled = true;
        }

        private FileNode GetDropTarget(object originalSource)
        {
            if (originalSource is FrameworkElement element && element.DataContext is FileNode node)
            {
                return node;
            }
            return RootDirectories.FirstOrDefault();
        }

        private bool IsParentOrSelf(FileNode source, FileNode target)
        {
            if (source == target)
                return true;

            var current = target;
            while (current != null)
            {
                if (current == source)
                    return true;
                current = current.Parent;
            }
            return false;
        }

        public void SelectAndExpandToFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return;

            // Root-Node finden (angenommen, es gibt nur einen Root)
            var currentNode = RootDirectories.FirstOrDefault();
            if (currentNode == null) return;

            // Pfad in Segmente aufteilen (Ordner und Datei)
            var segments = filePath.Replace(currentNode.FullPath, "").TrimStart(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
            var treeItem = FileTree.ItemContainerGenerator.ContainerFromItem(currentNode) as TreeViewItem;

            foreach (var segment in segments)
            {
                if (currentNode == null) return;

                // Kinder laden und erweitern
                currentNode.LoadChildren();
                currentNode = currentNode.Children.FirstOrDefault(child => string.Equals(child.Name, segment, StringComparison.OrdinalIgnoreCase));

                if (currentNode == null) return; // Pfad nicht gefunden

                // TreeViewItem finden und erweitern
                if (treeItem != null)
                {
                    treeItem.IsExpanded = true;
                    treeItem.UpdateLayout(); // Layout aktualisieren, um Kinder zu generieren
                    treeItem = treeItem.ItemContainerGenerator.ContainerFromItem(currentNode) as TreeViewItem;
                }
            }

            // Datei-Node auswählen und scrollen
            if (treeItem != null)
            {
                treeItem.IsSelected = true;
                treeItem.BringIntoView();
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
        public FileNode Parent { get; }

        public FileNode(string path, bool isDummy = false, bool isRoot = false, FileNode parent = null)
        {
            Parent = parent;

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
                Children.Add(new FileNode(null, isDummy: true, parent: this));
            }
        }

        public void LoadChildren()
        {
            if (_isLoaded || !IsDirectory || IsDummy) return;

            Children.Clear();

            try
            {
                foreach (var dir in Directory.GetDirectories(FullPath))
                    Children.Add(new FileNode(dir, parent: this));

                var patterns = new[] { "*.d", "*.txt", "*.src" };
                foreach (var pattern in patterns)
                {
                    foreach (var file in Directory.GetFiles(FullPath, pattern))
                        Children.Add(new FileNode(file, parent: this));
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
            catch (IOException) { }

            _isLoaded = true;
        }

        public void Refresh()
        {
            _isLoaded = false;
            LoadChildren();
        }
    }
}