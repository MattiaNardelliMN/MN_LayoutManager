using System;
using System.Windows.Input;

namespace LayoutManagerPalette.UI
{
    /// <summary>
    /// Collega un bottone o una voce di menu a un pezzo di codice, dicendo anche
    /// quando dev'essere attivo o grigio.
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>Crea il comando.</summary>
        /// <param name="execute">Cosa fare quando l'utente clicca.</param>
        /// <param name="canExecute">Quando il comando e' disponibile. null = sempre.</param>
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>Scatta quando cambia la disponibilita' del comando.</summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>Indica se il comando e' utilizzabile adesso.</summary>
        /// <param name="parameter">Non usato.</param>
        /// <returns>true se il comando e' attivo.</returns>
        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        /// <summary>Esegue il comando.</summary>
        /// <param name="parameter">Non usato.</param>
        public void Execute(object parameter) => _execute();

        /// <summary>Chiede all'interfaccia di ricontrollare se il comando e' attivo.</summary>
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
