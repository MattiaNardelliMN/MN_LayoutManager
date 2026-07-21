using System;
using System.Globalization;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;

namespace MN_LayoutManager.Services
{
    /// <summary>
    /// Manda comandi nativi ad AutoCAD facendoli passare da AutoLISP.
    /// </summary>
    /// <remarks>
    /// Perche' AutoLISP e non il testo del comando "a crudo": nella riga di comando lo
    /// spazio vale come Invio, quindi un layout chiamato "Tavola 1" verrebbe spezzato in
    /// due risposte. In AutoLISP il nome viaggia dentro virgolette e resta intero.
    /// <para>
    /// Attenzione: questi comandi sono ASINCRONI. AutoCAD li mette in coda e li esegue
    /// quando ha finito quello che sta facendo, quindi il risultato non e' disponibile
    /// subito dopo la chiamata: l'elenco si aggiorna poi grazie agli eventi.
    /// </para>
    /// </remarks>
    public static class AcadCommandRunner
    {
        /// <summary>
        /// Valore della variabile BACKGROUNDPLOT che fa pubblicare in background
        /// (stampa in primo piano, pubblicazione in secondo piano).
        /// </summary>
        private const int BackgroundPublishing = 2;

        /// <summary>Manda un'espressione AutoLISP al disegno indicato.</summary>
        /// <param name="document">Disegno destinatario.</param>
        /// <param name="lispExpression">Espressione AutoLISP completa, parentesi incluse.</param>
        public static void SendLisp(Document document, string lispExpression)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (string.IsNullOrWhiteSpace(lispExpression))
            {
                throw new ArgumentException("L'espressione AutoLISP non puo' essere vuota.", nameof(lispExpression));
            }

            // Lo spazio finale fa da Invio: senza, l'espressione resta li' in attesa.
            document.SendStringToExecute(lispExpression + " ", activate: true, wrapUpInactiveDoc: false, echoCommand: false);
        }

        /// <summary>
        /// Apre la finestra nativa "Gestione impostazioni di pagina".
        /// Non viene reimplementata: si richiama quella di AutoCAD, che e' completa e aggiornata.
        /// </summary>
        /// <param name="document">Disegno su cui agire.</param>
        public static void OpenPageSetupManager(Document document)
        {
            SendLisp(document, "(command \"_.PAGESETUP\")");
        }

        /// <summary>
        /// Esegue la pubblicazione batch leggendo un file DSD gia' scritto su disco.
        /// </summary>
        /// <param name="document">Disegno su cui agire.</param>
        /// <param name="dsdFilePath">Percorso del file DSD.</param>
        public static void PublishFromDsd(Document document, string dsdFilePath)
        {
            if (string.IsNullOrWhiteSpace(dsdFilePath))
            {
                throw new ArgumentException("Manca il percorso del file DSD.", nameof(dsdFilePath));
            }

            // FILEDIA=0 evita che AutoCAD apra la finestra di scelta file (bloccherebbe tutto).
            // BACKGROUNDPLOT=2 fa pubblicare IN BACKGROUND: AutoCAD torna subito utilizzabile
            // e la stampa prosegue per conto suo (l'icona della stampante appare in basso a destra).
            // I due valori originali vengono salvati e rimessi a posto alla fine, per non
            // cambiare le impostazioni personali dell'utente.
            string lisp = string.Format(
                CultureInfo.InvariantCulture,
                "(progn (setq *mnlm-filedia* (getvar \"FILEDIA\") *mnlm-bgplot* (getvar \"BACKGROUNDPLOT\"))" +
                " (setvar \"FILEDIA\" 0) (setvar \"BACKGROUNDPLOT\" {0})" +
                " (command \"_.-PUBLISH\" {1})" +
                " (setvar \"FILEDIA\" *mnlm-filedia*) (setvar \"BACKGROUNDPLOT\" *mnlm-bgplot*) (princ))",
                BackgroundPublishing,
                ToLispString(dsdFilePath));

            SendLisp(document, lisp);
        }

        /// <summary>
        /// Trasforma un testo in una stringa AutoLISP valida, proteggendo i caratteri
        /// che in AutoLISP hanno un significato speciale.
        /// </summary>
        /// <param name="value">Testo da racchiudere fra virgolette.</param>
        /// <returns>La stringa pronta da inserire nell'espressione.</returns>
        public static string ToLispString(string value)
        {
            var builder = new StringBuilder("\"");

            foreach (char character in value ?? string.Empty)
            {
                if (character == '\\' || character == '"')
                {
                    builder.Append('\\');
                }

                builder.Append(character);
            }

            builder.Append('"');
            return builder.ToString();
        }
    }
}
