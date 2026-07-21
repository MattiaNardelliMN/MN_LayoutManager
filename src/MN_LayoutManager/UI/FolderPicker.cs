using System;
using System.IO;
using WinForms = System.Windows.Forms;

namespace MN_LayoutManager.UI
{
    /// <summary>
    /// Finestra "Scegli cartella" di Windows.
    /// </summary>
    /// <remarks>
    /// WPF su .NET Framework 4.8 non ha una finestra per scegliere una cartella (ha solo
    /// quella per i file), quindi si usa quella di Windows Forms. E' l'unico punto del
    /// progetto che la usa: sta isolata qui dentro, cosi' se un domani si cambia
    /// approccio si tocca solo questo file.
    /// </remarks>
    public static class FolderPicker
    {
        /// <summary>
        /// Chiede all'utente di scegliere una cartella.
        /// </summary>
        /// <param name="description">Testo mostrato in cima alla finestra.</param>
        /// <param name="initialFolder">Cartella da cui partire; puo' essere vuota.</param>
        /// <param name="selectedFolder">Cartella scelta, se l'utente ha confermato.</param>
        /// <returns>true se l'utente ha scelto una cartella, false se ha annullato.</returns>
        public static bool TryPickFolder(string description, string initialFolder, out string selectedFolder)
        {
            selectedFolder = null;

            using (var dialog = new WinForms.FolderBrowserDialog())
            {
                dialog.Description = description;
                dialog.ShowNewFolderButton = true;

                if (!string.IsNullOrWhiteSpace(initialFolder) && Directory.Exists(initialFolder))
                {
                    dialog.SelectedPath = initialFolder;
                }

                if (dialog.ShowDialog() != WinForms.DialogResult.OK)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    return false;
                }

                selectedFolder = dialog.SelectedPath;
                return true;
            }
        }
    }
}
