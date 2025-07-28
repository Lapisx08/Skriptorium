using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace Skriptorium.Formatting
{
    public class BraceFoldingStrategy
    {
        public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document)
        {
            var foldings = new List<NewFolding>();
            var startOffsets = new Stack<int>();
            var text = document.Text;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '{')
                {
                    startOffsets.Push(i);
                }
                else if (c == '}')
                {
                    if (startOffsets.Count > 0)
                    {
                        int startOffset = startOffsets.Pop();
                        foldings.Add(new NewFolding(startOffset, i + 1));
                    }
                }
            }

            foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
            return foldings;
        }
    }
}