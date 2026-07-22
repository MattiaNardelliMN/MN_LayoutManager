using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using Autodesk.AutoCAD.ApplicationServices;
using MN_LayoutManager.Infrastructure;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace MN_LayoutManager.Services
{
    /// <summary>
    /// Fa arrivare alla palette le scorciatoie con Ctrl (per esempio Ctrl+A) che
    /// altrimenti AutoCAD si prenderebbe per se'.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Il problema: Ctrl+A in AutoCAD significa "seleziona tutti gli oggetti del disegno".
    /// AutoCAD intercetta il tasto nel proprio ciclo dei messaggi di Windows, PRIMA che
    /// WPF possa vederlo. Risultato: anche con la palette in primo piano, Ctrl+A finiva
    /// nel disegno invece che nell'elenco dei layout.
    /// </para>
    /// <para>
    /// La soluzione: agganciarsi a <c>Application.PreTranslateMessage</c>, che e' il punto
    /// in cui AutoCAD offre ai plugin la possibilita' di vedere un messaggio prima di
    /// gestirlo. Se il tasto e' nostro lo eseguiamo e marchiamo il messaggio come gia'
    /// gestito, cosi' AutoCAD non lo vede nemmeno.
    /// </para>
    /// <para>
    /// Le scorciatoie NON sono scritte qui: si leggono dagli <c>InputBindings</c> della
    /// palette, che restano l'unico posto dove sono definite. Aggiungerne una nello XAML
    /// la fa funzionare anche da qui, senza toccare questo file.
    /// </para>
    /// </remarks>
    public sealed class PaletteShortcutInterceptor : IDisposable
    {
        private const string OperationName = "Scorciatoie palette";

        private const int WmKeyDown = 0x0100;
        private const int WmSysKeyDown = 0x0104;

        private readonly UIElement _palette;
        private bool _disposed;

        /// <summary>Inizia a intercettare le scorciatoie destinate alla palette indicata.</summary>
        /// <param name="palette">Il controllo che contiene gli <c>InputBindings</c> da rispettare.</param>
        public PaletteShortcutInterceptor(UIElement palette)
        {
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));

            AcadContext.TryRun(
                OperationName,
                () => AcadApp.PreTranslateMessage += OnPreTranslateMessage,
                out string error);

            if (error != null)
            {
                PluginLog.Warn(OperationName, "Ctrl+A potrebbe finire nel disegno invece che nella palette: " + error);
            }
        }

        /// <summary>Smette di intercettare. Va chiamata quando la palette viene chiusa.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            AcadContext.TryRun(OperationName, () => AcadApp.PreTranslateMessage -= OnPreTranslateMessage, out _);
        }

        private void OnPreTranslateMessage(object sender, PreTranslateMessageEventArgs e)
        {
            // Questo metodo viene chiamato per OGNI messaggio di Windows che passa da
            // AutoCAD: deve essere velocissimo e non deve mai sollevare eccezioni, o si
            // pianta l'intero programma. Per questo i controlli sono in ordine di costo
            // crescente, dal piu' economico al piu' costoso.
            if (e == null || _disposed)
            {
                return;
            }

            MSG message = e.Message;
            if (message.message != WmKeyDown && message.message != WmSysKeyDown)
            {
                return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
            {
                return;
            }

            AcadContext.TryRun(OperationName, () => TryHandleShortcut(message, e), out _);
        }

        private void TryHandleShortcut(MSG message, PreTranslateMessageEventArgs e)
        {
            if (!_palette.IsKeyboardFocusWithin)
            {
                return;
            }

            // Mentre si sta scrivendo un nome, Ctrl+A deve selezionare il testo e non i
            // layout: dentro una casella di testo le scorciatoie restano quelle di Windows.
            if (Keyboard.FocusedElement is TextBoxBase)
            {
                return;
            }

            Key key = KeyInterop.KeyFromVirtualKey(message.wParam.ToInt32());
            KeyBinding binding = FindBinding(key, Keyboard.Modifiers);

            if (binding?.Command == null)
            {
                return;
            }

            // Il messaggio viene marcato come gestito anche se il comando e' disabilitato:
            // altrimenti un Ctrl+A "a vuoto" nella palette finirebbe comunque nel disegno,
            // che e' proprio il comportamento da evitare.
            e.Handled = true;

            if (binding.Command.CanExecute(binding.CommandParameter))
            {
                binding.Command.Execute(binding.CommandParameter);
            }
        }

        private KeyBinding FindBinding(Key key, ModifierKeys modifiers)
        {
            foreach (InputBinding input in _palette.InputBindings)
            {
                if (input is KeyBinding candidate && candidate.Key == key && candidate.Modifiers == modifiers)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
