using System;
using System.Globalization;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;

namespace LayoutManagerPalette.Services
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
        /// Duplica un layout con tutto il suo contenuto usando l'opzione Copy del comando
        /// nativo LAYOUT: riusa la logica gia' collaudata di AutoCAD invece di ricopiare
        /// a mano finestre, entita' e impostazioni di stampa.
        /// </summary>
        /// <param name="document">Disegno su cui agire.</param>
        /// <param name="sourceLayoutName">Layout da copiare.</param>
        /// <param name="newLayoutName">Nome della copia.</param>
        public static void CopyLayout(Document document, string sourceLayoutName, string newLayoutName)
        {
            if (string.IsNullOrWhiteSpace(sourceLayoutName))
            {
                throw new ArgumentException("Manca il layout da copiare.", nameof(sourceLayoutName));
            }

            if (string.IsNullOrWhiteSpace(newLayoutName))
            {
                throw new ArgumentException("Manca il nome della copia.", nameof(newLayoutName));
            }

            string lisp = string.Format(
                CultureInfo.InvariantCulture,
                "(command \"_.LAYOUT\" \"_Copy\" {0} {1})",
                ToLispString(sourceLayoutName),
                ToLispString(newLayoutName));

            SendLisp(document, lisp);
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

            // FILEDIA=0 evita che AutoCAD apra la finestra di scelta file (bloccherebbe tutto);
            // BACKGROUNDPLOT=0 fa pubblicare in primo piano, cosi' eventuali errori si vedono.
            // I due valori originali vengono salvati e rimessi a posto alla fine.
            string lisp = string.Format(
                CultureInfo.InvariantCulture,
                "(progn (setq *lmp-filedia* (getvar \"FILEDIA\") *lmp-bgplot* (getvar \"BACKGROUNDPLOT\"))" +
                " (setvar \"FILEDIA\" 0) (setvar \"BACKGROUNDPLOT\" 0)" +
                " (command \"_.-PUBLISH\" {0})" +
                " (setvar \"FILEDIA\" *lmp-filedia*) (setvar \"BACKGROUNDPLOT\" *lmp-bgplot*) (princ))",
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
