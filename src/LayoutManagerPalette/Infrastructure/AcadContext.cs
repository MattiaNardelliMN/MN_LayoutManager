using System;
using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using AcadErrorStatus = Autodesk.AutoCAD.Runtime.ErrorStatus;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace LayoutManagerPalette.Infrastructure
{
    /// <summary>
    /// Punto unico di accesso al documento AutoCAD attivo e di gestione degli errori.
    /// Tutto cio' che puo' andare storto passa da qui: cosi' un guasto resta confinato,
    /// viene scritto nel log e mostrato all'utente, e non fa crashare AutoCAD.
    /// </summary>
    public static class AcadContext
    {
        /// <summary>
        /// Recupera il disegno attivo IN QUESTO MOMENTO.
        /// Va richiamata a ogni azione: l'utente puo' cambiare disegno mentre la palette
        /// e' aperta, quindi tenere il documento in memoria porterebbe ad agire su quello sbagliato.
        /// </summary>
        /// <param name="document">Il disegno attivo, se ce n'e' uno.</param>
        /// <param name="error">Messaggio in italiano se non e' utilizzabile.</param>
        /// <returns>true se c'e' un disegno su cui lavorare.</returns>
        public static bool TryGetActiveDocument(out Document document, out string error)
        {
            document = AcadApp.DocumentManager.MdiActiveDocument;

            if (document == null)
            {
                error = "Nessun disegno aperto.";
                return false;
            }

            if (document.Database == null)
            {
                error = "Il disegno attivo non e' ancora pronto.";
                document = null;
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Come <see cref="TryGetActiveDocument"/>, ma verifica anche che AutoCAD non
        /// stia gia' eseguendo un comando: modificare i layout mentre un comando e' in
        /// corso provoca errori di blocco del documento.
        /// </summary>
        /// <param name="document">Il disegno attivo, se utilizzabile.</param>
        /// <param name="error">Messaggio in italiano se non e' utilizzabile.</param>
        /// <returns>true se si puo' modificare il disegno adesso.</returns>
        public static bool TryGetEditableDocument(out Document document, out string error)
        {
            if (!TryGetActiveDocument(out document, out error))
            {
                return false;
            }

            if (document.Editor != null && !document.Editor.IsQuiescent)
            {
                document = null;
                error = "AutoCAD sta eseguendo un altro comando: concludilo o annullalo, poi riprova.";
                return false;
            }

            return true;
        }

        /// <summary>Scrive un messaggio nella riga di comando di AutoCAD.</summary>
        /// <param name="message">Testo da mostrare.</param>
        public static void WriteMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc?.Editor == null)
            {
                return;
            }

            doc.Editor.WriteMessage(
                string.Format(CultureInfo.CurrentCulture, "\nGestione Layout: {0}\n", message));
        }

        /// <summary>
        /// Esegue un'operazione proteggendo AutoCAD: qualunque errore viene registrato
        /// nel log e riportato all'utente, invece di propagarsi e chiudere il programma.
        /// </summary>
        /// <param name="operation">Nome dell'operazione, per il log (es. "Rinomina layout").</param>
        /// <param name="action">Cosa fare.</param>
        /// <param name="error">Messaggio da mostrare all'utente se e' fallita.</param>
        /// <returns>true se l'operazione e' andata a buon fine.</returns>
        public static bool TryRun(string operation, Action action, out string error)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            try
            {
                action();
                error = null;
                return true;
            }
            catch (AcadException ex)
            {
                error = DescribeAcadError(ex);
                PluginLog.Error(operation, error, ex);
                return false;
            }
#pragma warning disable CA1031 // Qui la cattura generica e' voluta: e' il confine del
            // plugin. Un'eccezione non gestita che risale fino ad AutoCAD chiude l'intero
            // programma e l'utente perde il lavoro. Registriamo tutto nel log.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                error = "Errore imprevisto: " + ex.Message;
                PluginLog.Error(operation, error, ex);
                return false;
            }
        }

        /// <summary>Traduce gli errori AutoCAD piu' comuni in frasi comprensibili.</summary>
        private static string DescribeAcadError(AcadException ex)
        {
            switch (ex.ErrorStatus)
            {
                case AcadErrorStatus.LockViolation:
                    return "Il disegno e' occupato da un'altra operazione: riprova fra un istante.";
                case AcadErrorStatus.DuplicateRecordName:
                    return "Esiste gia' un layout con quel nome.";
                case AcadErrorStatus.KeyNotFound:
                    return "Il layout non esiste piu': aggiorno l'elenco.";
                case AcadErrorStatus.NotApplicable:
                    return "Operazione non consentita su questo layout.";
                default:
                    return string.Format(
                        CultureInfo.CurrentCulture,
                        "Errore di AutoCAD ({0}): {1}",
                        ex.ErrorStatus,
                        ex.Message);
            }
        }
    }
}
