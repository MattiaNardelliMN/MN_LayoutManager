using System;
using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using AcadErrorStatus = Autodesk.AutoCAD.Runtime.ErrorStatus;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace MN_LayoutManager.Infrastructure
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

            // Volutamente NON si riusa IsAtCommandPrompt: qui la domanda e' "posso
            // modificare il disegno?", li' e' "posso scrivere senza disturbare?". Sono
            // due cose diverse e trattano il caso "nessun editor" in modo opposto: per
            // modificare il disegno l'editor non serve, per scriverci si'.
            if (document.Editor != null && !document.Editor.IsQuiescent)
            {
                document = null;
                error = "AutoCAD sta eseguendo un altro comando: concludilo o annullalo, poi riprova.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Dice se AutoCAD e' fermo al prompt "Comando:", cioe' se non sta eseguendo
        /// niente e non sta aspettando una risposta dall'utente.
        /// </summary>
        /// <remarks>
        /// Serve a decidere se e' un momento sicuro per scrivere nella riga di comando.
        /// Scrivere mentre un comando e' in attesa gli copre la domanda: il comando
        /// resta li' ad aspettare, ma l'utente non vede piu' che cosa gli era stato
        /// chiesto e si ritrova bloccato finche' non preme ESC.
        /// </remarks>
        /// <returns>true se si puo' scrivere senza disturbare nessuno.</returns>
        public static bool IsAtCommandPrompt()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            return doc?.Editor != null && doc.Editor.IsQuiescent;
        }

        /// <summary>Scrive un messaggio nella riga di comando di AutoCAD.</summary>
        /// <param name="message">Testo da mostrare.</param>
        public static void WriteMessage(string message)
        {
            TryWriteMessage(message);
        }

        /// <summary>
        /// Come <see cref="WriteMessage"/>, ma dice anche se il messaggio e' stato
        /// davvero mostrato.
        /// </summary>
        /// <remarks>
        /// Serve a chi deve riprovare piu' tardi: la riga di comando esiste solo se c'e'
        /// un disegno aperto, e quando AutoCAD carica il plugin dal bundle non c'e'
        /// ancora. Vedi <see cref="StartupMessage"/>.
        /// </remarks>
        /// <param name="message">Testo da mostrare.</param>
        /// <returns>true se il messaggio e' finito nella riga di comando.</returns>
        public static bool TryWriteMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }

            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc?.Editor == null)
            {
                return false;
            }

            // Niente a capo finale: lo lascerebbe la riga di comando su una riga vuota,
            // senza che AutoCAD ristampi "Comando:". A schermo sembra che il programma
            // stia aspettando qualcosa che non arrivera' mai.
            doc.Editor.WriteMessage(
                string.Format(CultureInfo.CurrentCulture, "\nGestione Layout: {0}", message));
            return true;
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
