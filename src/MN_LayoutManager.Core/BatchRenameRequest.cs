using System;
using System.Collections.Generic;

namespace MN_LayoutManager.Core
{
    /// <summary>
    /// Cosa ha chiesto l'utente nel pannello "Rinomina multipla".
    /// Oggetto di soli dati: non fa nulla da solo, lo interpreta <see cref="BatchRenamePlanner"/>.
    /// </summary>
    public sealed class BatchRenameRequest
    {
        /// <summary>
        /// I layout su cui agire: sono quelli che l'utente ha spuntato nell'elenco.
        /// null significa "tutti i layout del disegno".
        /// </summary>
        /// <remarks>
        /// Prima qui c'era un filtro testuale ("agisci sui nomi che contengono..."), poco
        /// pratico perche' obbligava a inventare un testo comune. Scegliere i layout con
        /// le caselle di spunta e' diretto e non lascia dubbi su cosa verra' toccato.
        /// </remarks>
        public IReadOnlyList<string> TargetNames { get; set; }

        /// <summary>Modalita' scelta.</summary>
        public BatchRenameMode Mode { get; set; }

        /// <summary>
        /// Prefisso, suffisso oppure testo da cercare, a seconda della modalita'.
        /// </summary>
        public string Value { get; set; }

        /// <summary>Testo sostitutivo: usato solo in modalita' Trova e sostituisci.</summary>
        public string ReplacementValue { get; set; }

        /// <summary>
        /// Se true, la ricerca distingue maiuscole e minuscole.
        /// Il default (false) e' il comportamento che si aspetta un utente normale.
        /// </summary>
        public bool CaseSensitive { get; set; }

        internal StringComparison Comparison =>
            CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
    }
}
