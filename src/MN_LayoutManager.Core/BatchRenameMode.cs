namespace MN_LayoutManager.Core
{
    /// <summary>Modalita' della rinomina multipla.</summary>
    public enum BatchRenameMode
    {
        /// <summary>Aggiunge un testo all'inizio del nome.</summary>
        AddPrefix,

        /// <summary>Aggiunge un testo alla fine del nome.</summary>
        AddSuffix,

        /// <summary>
        /// Toglie un testo dall'inizio del nome, ma solo se il nome inizia davvero
        /// con quel testo: gli altri layout restano intatti.
        /// </summary>
        RemovePrefix,

        /// <summary>
        /// Toglie un testo dalla fine del nome, ma solo se il nome finisce davvero
        /// con quel testo: gli altri layout restano intatti.
        /// </summary>
        RemoveSuffix,

        /// <summary>Sostituisce un testo con un altro dentro il nome.</summary>
        FindReplace,
    }
}
