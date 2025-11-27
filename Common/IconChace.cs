using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Skriptorium.Common
{
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
            if (string.IsNullOrEmpty(path) || path == "Aktives Skript" || path == "Unbenanntes Skript")
                return GetDefaultFileIcon();

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
                    query = ".txt"; // Standarderweiterung für Dateien ohne Pfad
            }

            IntPtr hRes = SHGetFileInfo(query, attributes, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
            if (hRes == IntPtr.Zero)
                return GetDefaultFileIcon();

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

        private static ImageSource GetDefaultFileIcon()
        {
            // Standard-Icon für Textdateien (.txt)
            SHFILEINFO shinfo = new SHFILEINFO();
            uint flags = SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES;
            uint attributes = FILE_ATTRIBUTE_NORMAL;

            IntPtr hRes = SHGetFileInfo(".txt", attributes, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
            if (hRes == IntPtr.Zero)
                return new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);

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
    }
}