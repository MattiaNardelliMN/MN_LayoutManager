namespace MN_LayoutManager.Core
{
    /// <summary>Modalita' della rinomina multipla.</summary>
    public enum BatchRenameMode
    {
        /// <summary>Aggiunge un testo all'inizio del nome.</summary>
        AddPrefix,

        /// <summary>Aggiunge un testo alla fine del nome.</summary>
        AddSuffix,

        /// <summary>Sostituisce un testo con un altro dentro il nome.</summary>
        FindReplace,
    }
}
