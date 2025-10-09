using System;

namespace Skriptorium.Managers
{
    [Serializable]
    public class TabState
    {
        public string? FilePath { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}