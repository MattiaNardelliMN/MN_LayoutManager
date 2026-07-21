using System;

namespace MN_LayoutManager.Core
{
    /// <summary>
    /// Cosa ha chiesto l'utente nel pannello "Rinomina multipla".
    /// Oggetto di soli dati: non fa nulla da solo, lo interpreta <see cref="BatchRenamePlanner"/>.
    /// </summary>
    public sealed class BatchRenameRequest
    {
        /// <summary>
        /// Filtro opzionale: agisce solo sui layout il cui nome contiene questo testo.
        /// Vuoto o null significa "tutti i layout".
        /// </summary>
        public string Filter { get; set; }

        /// <summary>Modalita' scelta.</summary>
        public BatchRenameMode Mode { get; set; }

        /// <summary>
        /// Prefisso, suffisso oppure testo da cercare, a seconda della modalita'.
        /// </summary>
        public string Value { get; set; }

        /// <summary>Testo sostitutivo: usato solo in modalita' Trova e sostituisci.</summary>
        public string ReplacementValue { get; set; }

        /// <summary>
        /// Se true, filtro e ricerca distinguono maiuscole e minuscole.
        /// Il default (false) e' il comportamento che si aspetta un utente normale.
        /// </summary>
        public bool CaseSensitive { get; set; }

        internal StringComparison Comparison =>
            CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
    }
}
