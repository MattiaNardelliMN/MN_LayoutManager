using Autodesk.AutoCAD.Runtime;
using MN_LayoutManager.Infrastructure;
using MN_LayoutManager.Services;
using MN_LayoutManager.UI;

[assembly: CommandClass(typeof(MN_LayoutManager.Commands))]
[assembly: ExtensionApplication(typeof(MN_LayoutManager.Commands))]

namespace MN_LayoutManager
{
    /// <summary>
    /// Porta d'ingresso del plugin: e' la classe che AutoCAD carica e da cui parte tutto.
    /// </summary>
    public sealed class Commands : IExtensionApplication
    {
        /// <summary>Nome del comando da digitare in AutoCAD per aprire la palette.</summary>
        public const string PaletteCommandName = "GESTIONELAYOUT";

        /// <summary>
        /// Secondo nome dello STESSO comando. Esiste perche' chi conosce il plugin come
        /// "MN_LayoutManager" digita d'istinto questo, non il nome italiano, e si ritrova
        /// un "comando sconosciuto" senza capire perche'.
        /// Verificato che AutoCAD 2024 non abbia gia' un comando con questo nome.
        /// </summary>
        public const string PaletteCommandAlias = "LAYOUTMANAGER";

        /// <summary>
        /// Chiamata da AutoCAD una sola volta al caricamento del plugin.
        /// Qui ci si iscrive agli eventi dei layout, che valgono per tutta la sessione.
        /// </summary>
        public void Initialize()
        {
            // La riga di diagnostica va scritta PER PRIMA: se qualcosa piu' avanti
            // fallisce, nel log resta comunque scritto quale versione del plugin era
            // stata caricata e su quale AutoCAD. E' la prima cosa da guardare quando
            // il plugin "non si vede".
            PluginLog.Info("Avvio", BuildInfo.Describe());
            LayoutChangeNotifier.Start();

            // Non si usa AcadContext.WriteMessage: al caricamento dal bundle non c'e'
            // ancora nessun disegno, quindi il messaggio andrebbe perso. StartupMessage
            // lo ripropone appena AutoCAD e' pronto.
            StartupMessage.Show(
                "plugin caricato (" + BuildInfo.BuiltFor + "). Digita "
                + PaletteCommandName + " (oppure " + PaletteCommandAlias
                + ") per aprire la palette.");
        }

        /// <summary>
        /// Chiamata da AutoCAD alla chiusura. Serve a disiscriversi dagli eventi:
        /// lasciarli agganciati terrebbe in vita oggetti non piu' validi.
        /// </summary>
        public void Terminate()
        {
            LayoutPaletteHost.Shutdown();
            LayoutChangeNotifier.Stop();
            StartupMessage.Cancel();
            PluginLog.Info("Chiusura", "Plugin Gestione Layout scaricato.");
        }

        /// <summary>
        /// Apre o chiude la palette "Gestione Layout".
        /// </summary>
        /// <remarks>
        /// Ha due nomi (<see cref="PaletteCommandName"/> e <see cref="PaletteCommandAlias"/>):
        /// sono due porte sulla stessa stanza, non due comandi diversi.
        /// <c>CommandFlags.Session</c> permette di lanciare il comando anche quando non
        /// c'e' nessun disegno aperto: la palette mostra semplicemente l'elenco vuoto.
        /// </remarks>
        [CommandMethod(PaletteCommandName, CommandFlags.Session)]
        [CommandMethod(PaletteCommandAlias, CommandFlags.Session)]
        public void ShowLayoutPalette()
        {
            LayoutPaletteHost.Toggle();
        }
    }
}
