using System;
using System.Windows;
using System.Windows.Input;

namespace Skriptorium.Managers
{
    public class ShortcutManager
    {
        private readonly Window _window;

        public ShortcutManager(Window window)
        {
            _window = window;
        }

        /// <summary>
        /// Registriert eine Tastenkombination, die beim Auslösen die Aktion action aufruft.
        /// </summary>
        public void Register(Key key, ModifierKeys modifiers, Action action)
        {
            var command = new RoutedCommand();

            // Executed-Handler ruft direkt die übergebene Action auf
            var binding = new CommandBinding(command, (s, e) =>
            {
                action();
                e.Handled = true;
            });

            // CanExecute immer true
            binding.CanExecute += (s, e) =>
            {
                e.CanExecute = true;
                e.Handled = true;
            };

            _window.CommandBindings.Add(binding);
            _window.InputBindings.Add(new KeyBinding(command, key, modifiers));
        }
    }
}
