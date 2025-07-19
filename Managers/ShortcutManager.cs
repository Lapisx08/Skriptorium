using System.Reflection;
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
            RegisterShortcuts();
        }

        private void RegisterShortcuts()
        {
            Register(Key.N, ModifierKeys.Control, "NewScript");        // Strg+N
            Register(Key.O, ModifierKeys.Control, "OpenScript");       // Strg+O
            Register(Key.S, ModifierKeys.Control, "SaveScript");       // Strg+S
            Register(Key.S, ModifierKeys.Control | ModifierKeys.Shift, "SaveScriptAs");   // Strg+Shift+S
            Register(Key.W, ModifierKeys.Control, "CloseActiveTab");   // Strg+W
            Register(Key.OemComma, ModifierKeys.Control, "OpenSettings");     // Strg+,
        }

        private void Register(Key key, ModifierKeys modifiers, string methodName)
        {
            var command = new RoutedCommand();

            // Executed-Handler
            var binding = new CommandBinding(command, (s, e) =>
            {
                var mi = _window.GetType()
                                .GetMethod(methodName,
                                           BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                mi?.Invoke(_window, null);
            });

            // CanExecute-Handler (erlaubt die Ausführung)
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
