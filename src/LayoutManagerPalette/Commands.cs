using Autodesk.AutoCAD.Runtime;
using LayoutManagerPalette.Infrastructure;
using LayoutManagerPalette.Services;
using LayoutManagerPalette.UI;

[assembly: CommandClass(typeof(LayoutManagerPalette.Commands))]
[assembly: ExtensionApplication(typeof(LayoutManagerPalette.Commands))]

namespace LayoutManagerPalette
{
    /// <summary>
    /// Porta d'ingresso del plugin: e' la classe che AutoCAD carica e da cui parte tutto.
    /// </summary>
    public sealed class Commands : IExtensionApplication
    {
        /// <summary>Nome del comando da digitare in AutoCAD per aprire la palette.</summary>
        public const string PaletteCommandName = "GESTIONELAYOUT";

        /// <summary>
        /// Chiamata da AutoCAD una sola volta al caricamento del plugin.
        /// Qui ci si iscrive agli eventi dei layout, che valgono per tutta la sessione.
        /// </summary>
        public void Initialize()
        {
            PluginLog.Info("Avvio", "Plugin Gestione Layout caricato.");
            LayoutChangeNotifier.Start();

            AcadContext.WriteMessage(
                "plugin caricato. Digita " + PaletteCommandName + " per aprire la palette.");
        }

        /// <summary>
        /// Chiamata da AutoCAD alla chiusura. Serve a disiscriversi dagli eventi:
        /// lasciarli agganciati terrebbe in vita oggetti non piu' validi.
        /// </summary>
        public void Terminate()
        {
            LayoutPaletteHost.Shutdown();
            LayoutChangeNotifier.Stop();
            PluginLog.Info("Chiusura", "Plugin Gestione Layout scaricato.");
        }

        /// <summary>
        /// Apre o chiude la palette "Gestione Layout".
        /// </summary>
        /// <remarks>
        /// <c>CommandFlags.Session</c> permette di lanciare il comando anche quando non
        /// c'e' nessun disegno aperto: la palette mostra semplicemente l'elenco vuoto.
        /// </remarks>
        [CommandMethod(PaletteCommandName, CommandFlags.Session)]
        public void ShowLayoutPalette()
        {
            LayoutPaletteHost.Toggle();
        }
    }
}
