using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using MN_LayoutManager.Infrastructure;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace MN_LayoutManager.UI
{
    /// <summary>
    /// Chiede quante copie fare di un layout, mostrando in anteprima i nomi che
    /// verranno creati.
    /// </summary>
    /// <remarks>
    /// La finestra non sa NIENTE di layout o di numerazione: riceve gia' pronta una
    /// funzione che, dato un numero di copie, restituisce la frase da mostrare. Cosi'
    /// la regola dei nomi resta una sola, in <see cref="Core.LayoutNumbering"/>.
    /// </remarks>
    public partial class DuplicateLayoutDialog : Window
    {
        private const string OperationName = "Duplica layout";
        private const int DefaultCount = 1;

        private readonly Func<int, string> _describeResult;
        private readonly int _maxCount;

        private DuplicateLayoutDialog(string sourceName, Func<int, string> describeResult, int maxCount)
        {
            InitializeComponent();

            _describeResult = describeResult;
            _maxCount = maxCount;

            QuestionText.Text = string.Format(
                CultureInfo.CurrentCulture,
                "Quante copie di \"{0}\" vuoi creare?",
                sourceName);

            CountBox.Text = DefaultCount.ToString(CultureInfo.CurrentCulture);
            CountBox.SelectAll();
            CountBox.Focus();
        }

        /// <summary>Numero di copie confermato dall'utente.</summary>
        private int Count { get; set; } = DefaultCount;

        /// <summary>
        /// Mostra la finestra e restituisce quante copie servono, oppure null se l'utente
        /// ha annullato. Non solleva mai eccezioni: un guasto qui non deve impedire il
        /// resto del lavoro, quindi viene registrato e vale come "annulla".
        /// </summary>
        /// <param name="sourceName">Layout da duplicare, mostrato nella domanda.</param>
        /// <param name="describeResult">Dato un numero di copie, la frase di anteprima da mostrare.</param>
        /// <param name="maxCount">Tetto massimo di copie ammesse.</param>
        /// <returns>Quante copie creare, o null se annullato.</returns>
        public static int? AskCount(string sourceName, Func<int, string> describeResult, int maxCount)
        {
            if (describeResult == null)
            {
                throw new ArgumentNullException(nameof(describeResult));
            }

            int? result = null;

            AcadContext.TryRun(
                OperationName,
                () =>
                {
                    var dialog = new DuplicateLayoutDialog(sourceName, describeResult, maxCount);

                    // Senza padrone la finestra puo' finire dietro ad AutoCAD e sembrare
                    // che il programma si sia bloccato: si aggancia alla finestra principale.
                    new WindowInteropHelper(dialog).Owner = AcadApp.MainWindow.Handle;

                    if (dialog.ShowDialog() == true)
                    {
                        result = dialog.Count;
                    }
                },
                out string error);

            if (error != null)
            {
                PluginLog.Warn(OperationName, "Finestra non aperta: " + error);
            }

            return result;
        }

        /// <summary>
        /// Legge il numero scritto dall'utente.
        /// Fuori intervallo o non numerico vale come "non valido": il bottone Crea si
        /// spegne e l'anteprima spiega cosa c'e' che non va.
        /// </summary>
        private bool TryReadCount(out int count, out string problem)
        {
            string text = (CountBox.Text ?? string.Empty).Trim();

            if (text.Length == 0)
            {
                count = 0;
                problem = "Scrivi quante copie servono.";
                return false;
            }

            if (!int.TryParse(text, NumberStyles.None, CultureInfo.CurrentCulture, out count) || count < 1)
            {
                count = 0;
                problem = "Scrivi un numero intero maggiore di zero.";
                return false;
            }

            if (count > _maxCount)
            {
                problem = string.Format(
                    CultureInfo.CurrentCulture,
                    "Al massimo {0} copie per volta.",
                    _maxCount);
                return false;
            }

            problem = null;
            return true;
        }

        private void OnCountChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (CreateButton == null || PreviewText == null)
            {
                // Succede durante la costruzione della finestra, quando i controlli non
                // sono ancora tutti creati: non c'e' niente da aggiornare.
                return;
            }

            bool valid = TryReadCount(out int count, out string problem);

            CreateButton.IsEnabled = valid;
            PreviewText.Text = valid ? Describe(count) : problem;
        }

        private string Describe(int count)
        {
            string description = null;

            AcadContext.TryRun(OperationName, () => description = _describeResult(count), out string error);

            return description ?? error ?? string.Empty;
        }

        private void OnCreateClick(object sender, RoutedEventArgs e)
        {
            if (!TryReadCount(out int count, out _))
            {
                return;
            }

            Count = count;
            DialogResult = true;
        }

        /// <summary>
        /// Permette di spostare la finestra trascinandone il titolo: senza la cornice di
        /// Windows non ci sarebbe nessun altro modo per muoverla.
        /// </summary>
        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }
    }
}
