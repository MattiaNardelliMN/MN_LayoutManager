using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MN_LayoutManager.Core
{
    /// <summary>Una singola rinomina: da quale nome, a quale nome.</summary>
    public sealed class BatchRenameStep
    {
        /// <summary>Crea un passo di rinomina.</summary>
        /// <param name="originalName">Nome attuale del layout.</param>
        /// <param name="finalName">Nome che deve avere alla fine.</param>
        public BatchRenameStep(string originalName, string finalName)
        {
            OriginalName = originalName ?? throw new ArgumentNullException(nameof(originalName));
            FinalName = finalName ?? throw new ArgumentNullException(nameof(finalName));
        }

        /// <summary>Nome attuale del layout.</summary>
        public string OriginalName { get; }

        /// <summary>Nome finale desiderato.</summary>
        public string FinalName { get; }
    }

    /// <summary>
    /// Risultato del calcolo di una rinomina multipla: o e' valido e contiene i passi
    /// da eseguire, o contiene l'elenco dei problemi da mostrare all'utente.
    /// Il calcolo avviene tutto PRIMA di toccare il disegno: cosi' se qualcosa non
    /// torna non si lascia il disegno a meta'.
    /// </summary>
    public sealed class BatchRenamePlan
    {
        internal BatchRenamePlan(IList<BatchRenameStep> steps, IList<string> errors, bool requiresTemporaryNames)
        {
            Steps = new ReadOnlyCollection<BatchRenameStep>(steps);
            Errors = new ReadOnlyCollection<string>(errors);
            RequiresTemporaryNames = requiresTemporaryNames;
        }

        /// <summary>Rinomine da eseguire (solo i layout che cambiano davvero nome).</summary>
        public IReadOnlyList<BatchRenameStep> Steps { get; }

        /// <summary>Problemi trovati: se non e' vuoto, non si deve eseguire nulla.</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>
        /// true quando un nome finale coincide con il nome attuale di un altro layout
        /// coinvolto: in quel caso serve passare da nomi temporanei per non scontrarsi
        /// a meta' operazione (es. scambio di nomi fra due layout).
        /// </summary>
        public bool RequiresTemporaryNames { get; }

        /// <summary>true se il piano si puo' eseguire senza rischi.</summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>true se il piano e' valido ma non cambia niente (nessun layout coinvolto).</summary>
        public bool IsEmpty => IsValid && Steps.Count == 0;
    }
}
