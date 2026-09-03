using System;
using System.Globalization;
using System.IO;

namespace MN_LayoutManager.Infrastructure
{
    /// <summary>
    /// Il registro CSV che AutoCAD puo' scrivere sulla pubblicazione: dove va a finire
    /// e come lo si racconta all'utente.
    /// </summary>
    /// <remarks>
    /// Il plugin puo' solo DIRE ad AutoCAD dove scriverlo (<c>DsdData.LogFilePath</c>).
    /// Se il file venga poi creato oppure no dipende da un'opzione di AutoCAD
    /// ("Salva automaticamente il registro di stampa e pubblicazione", in
    /// Opzioni &gt; Stampa e pubblicazione) che le API .NET non permettono di attivare:
    /// verificato per riflessione, su <c>DsdData</c> esiste solo <c>LogFilePath</c> e
    /// nessun interruttore.
    /// <para>
    /// Per questo il messaggio nel log e' condizionale. Prima affermava che il dettaglio
    /// "e' nel registro CSV di pubblicazione", e quel file quasi sempre non esisteva:
    /// mandava l'utente a cercare qualcosa che non c'era.
    /// </para>
    /// </remarks>
    internal static class PublishLogFile
    {
        /// <summary>
        /// Percorso del registro di oggi, accanto ai log del plugin: uno al giorno,
        /// come per il log normale.
        /// </summary>
        /// <returns>Percorso completo del file CSV.</returns>
        public static string PathForToday()
        {
            return Path.Combine(
                PluginLog.LogDirectory,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "pubblicazione_{0:yyyy-MM-dd}.csv",
                    DateTime.Now));
        }

        /// <summary>
        /// Frase da mettere nel log per spiegare dove cercare il dettaglio dei singoli
        /// fogli, senza dare per scontato che il file esista.
        /// </summary>
        /// <returns>Testo gia' pronto da concatenare a un messaggio di log.</returns>
        public static string WhereToLookHint()
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "Il dettaglio dei singoli fogli lo scrive AutoCAD in \"{0}\", ma solo se in "
                + "Opzioni > Stampa e pubblicazione e' attivo il salvataggio automatico del "
                + "registro; se quel file non compare, l'opzione e' spenta.",
                PathForToday());
        }
    }
}
