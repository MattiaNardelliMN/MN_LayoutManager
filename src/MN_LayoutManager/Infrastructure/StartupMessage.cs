using System;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace MN_LayoutManager.Infrastructure
{
    /// <summary>
    /// Mostra nella riga di comando di AutoCAD un messaggio nato troppo presto per
    /// poter essere visto, riproponendolo appena AutoCAD e' pronto.
    /// </summary>
    /// <remarks>
    /// AutoCAD carica i plugin installati come bundle PRIMA di aprire il disegno
    /// iniziale. In quel momento non esiste nessuna riga di comando su cui scrivere,
    /// quindi il messaggio di benvenuto veniva buttato via proprio quando sarebbe
    /// servito: chi non trovava il comando non aveva nessun indizio.
    /// <para>
    /// Qui il messaggio viene messo da parte e riscritto al primo momento libero di
    /// AutoCAD (evento <c>Idle</c>), quando il disegno c'e'. E' un modulo a se' proprio
    /// perche' e' un rimedio a un problema di tempi: se un domani AutoCAD offrisse un
    /// aggancio migliore, si cambia solo questo file.
    /// </para>
    /// <para>
    /// ATTENZIONE, e' la lezione che e' costata la 2.0.3: <c>Idle</c> non vuol dire
    /// "AutoCAD non sta facendo niente". Scatta anche mentre un comando e' fermo ad
    /// aspettare una risposta dall'utente. Scrivere in quel momento copre la domanda
    /// del comando: il comando resta in attesa, l'utente non vede piu' che cosa gli era
    /// stato chiesto, e per uscirne deve premere ESC. Per questo non basta che ci sia un
    /// disegno: si aspetta che AutoCAD sia fermo al prompt "Comando:".
    /// </para>
    /// </remarks>
    internal static class StartupMessage
    {
        private const string OperationName = "Avvio";

        /// <summary>
        /// Per quanto si continua a riprovare prima di rinunciare.
        /// </summary>
        /// <remarks>
        /// L'evento Idle di AutoCAD scatta di continuo: restare agganciati per sempre,
        /// nel caso in cui un disegno non venga mai aperto, sarebbe uno spreco inutile.
        /// </remarks>
        private static readonly TimeSpan MaxWait = TimeSpan.FromMinutes(2);

        private static string _pendingMessage;
        private static DateTime _giveUpAtUtc;
        private static bool _waitingForIdle;

        /// <summary>
        /// Mostra il messaggio subito se si puo', altrimenti appena AutoCAD e' pronto.
        /// </summary>
        /// <param name="message">Testo da mostrare.</param>
        public static void Show(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (TryWriteWithoutDisturbing(message))
            {
                return;
            }

            _pendingMessage = message;
            _giveUpAtUtc = DateTime.UtcNow + MaxWait;

            if (_waitingForIdle)
            {
                return;
            }

            AcadApp.Idle += OnIdle;
            _waitingForIdle = true;
        }

        /// <summary>
        /// Rinuncia a un messaggio ancora in attesa e si sgancia dall'evento Idle.
        /// Va chiamata alla chiusura del plugin: un evento lasciato agganciato
        /// sopravvivrebbe al plugin che lo ha registrato.
        /// </summary>
        public static void Cancel()
        {
            StopWaiting();
        }

        private static void OnIdle(object sender, EventArgs e)
        {
            // Un errore dentro un gestore di eventi di AutoCAD, se non fermato qui,
            // chiude l'intero programma: passa da TryRun come tutto il resto.
            if (!AcadContext.TryRun(OperationName, ShowPendingOrGiveUp, out _))
            {
                // Se e' fallito una volta fallira' a ogni Idle, riempiendo il log
                // di righe identiche: meglio rinunciare al messaggio.
                StopWaiting();
            }
        }

        private static void ShowPendingOrGiveUp()
        {
            if (TryWriteWithoutDisturbing(_pendingMessage))
            {
                StopWaiting();
                return;
            }

            if (DateTime.UtcNow >= _giveUpAtUtc)
            {
                PluginLog.Warn(
                    OperationName,
                    "AutoCAD non e' mai stato fermo al prompt dei comandi con un disegno "
                    + "aperto: il messaggio di benvenuto non e' stato mostrato nella riga "
                    + "di comando. Il plugin e' comunque caricato.");
                StopWaiting();
            }
        }

        /// <summary>
        /// Scrive il messaggio solo se e' un momento in cui non da' fastidio a nessuno.
        /// </summary>
        /// <param name="message">Testo da mostrare.</param>
        /// <returns>true se il messaggio e' stato mostrato.</returns>
        private static bool TryWriteWithoutDisturbing(string message)
        {
            return AcadContext.IsAtCommandPrompt() && AcadContext.TryWriteMessage(message);
        }

        private static void StopWaiting()
        {
            if (_waitingForIdle)
            {
                AcadApp.Idle -= OnIdle;
                _waitingForIdle = false;
            }

            _pendingMessage = null;
        }
    }
}
