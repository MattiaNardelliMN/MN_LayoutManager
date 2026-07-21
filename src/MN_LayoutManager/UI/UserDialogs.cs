using System.Windows;

namespace MN_LayoutManager.UI
{
    /// <summary>
    /// Le finestrelle di conferma e avviso, raccolte in un punto solo.
    /// Cosi' hanno tutte lo stesso titolo e lo stesso comportamento, e il resto del
    /// codice non deve sapere come si mostra un messaggio.
    /// </summary>
    public static class UserDialogs
    {
        private const string Title = "Gestione Layout";

        /// <summary>Chiede una conferma prima di un'operazione che non si puo' annullare.</summary>
        /// <param name="message">Domanda da porre all'utente.</param>
        /// <returns>true se l'utente ha risposto Si'.</returns>
        public static bool Confirm(string message)
        {
            return MessageBox.Show(
                message,
                Title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No) == MessageBoxResult.Yes;
        }

        /// <summary>Mostra un avviso.</summary>
        /// <param name="message">Testo da mostrare.</param>
        public static void Warn(string message)
        {
            MessageBox.Show(message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
