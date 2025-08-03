using AvalonDock;
using AvalonDock.Layout;
using AvalonDock.Controls;
using System.Linq;

namespace Skriptorium.Managers
{
    public class CustomLayoutUpdateStrategy : ILayoutUpdateStrategy
    {
        public bool BeforeInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableToShow, ILayoutContainer destinationContainer)
        {
            return false; // Nicht relevant für Anchorables
        }

        public void AfterInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableShown)
        {
            // Nicht relevant für Anchorables
        }

        public bool BeforeInsertDocument(LayoutRoot layout, LayoutDocument documentToShow, ILayoutContainer destinationContainer)
        {
            return false; // Keine Änderung vor dem Einfügen eines Dokuments
        }

        public void AfterInsertDocument(LayoutRoot layout, LayoutDocument documentShown)
        {
            // Überprüfe, ob das Dokument in einem Floating-Fenster ist
            UpdateFloatingWindowTitles(layout);
        }

        private void UpdateFloatingWindowTitles(LayoutRoot layout)
        {
            foreach (var floatingWindow in layout.FloatingWindows.OfType<LayoutDocumentFloatingWindow>())
            {
                var floatingWindowControl = layout.Descendents()
                    .OfType<LayoutFloatingWindowControl>()
                    .FirstOrDefault(fwc => fwc.Model == floatingWindow);
                if (floatingWindowControl != null)
                {
                    floatingWindowControl.Title = "Skriptorium";
                }
            }
        }
    }
}