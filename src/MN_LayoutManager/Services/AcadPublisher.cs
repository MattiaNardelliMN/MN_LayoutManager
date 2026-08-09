using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Publishing;
using MN_LayoutManager.Core;
using MN_LayoutManager.Infrastructure;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace MN_LayoutManager.Services
{
    /// <summary>
    /// Esegue la pubblicazione batch chiamando direttamente l'API di stampa di AutoCAD
    /// (<c>Publisher.PublishExecute</c>), invece di pilotare il comando <c>-PUBLISH</c>
    /// dalla riga di comando.
    /// </summary>
    /// <remarks>
    /// Perche' non si usa piu' la riga di comando: quella strada non restituisce nessun
    /// esito. Se la pubblicazione falliva, il plugin scriveva comunque "avviata" e non
    /// c'era modo di sapere che non era stato prodotto alcun file. Con l'API si intercetta
    /// l'errore, si registra nel log e si avvisa l'utente.
    /// <para>
    /// Tutto quello che riguarda la pubblicazione vera e propria sta qui dentro: se un
    /// domani serve cambiare strada, si tocca solo questo file.
    /// </para>
    /// </remarks>
    public static class AcadPublisher
    {
        private const string OperationName = "Stampa/Pubblica";

        /// <summary>
        /// Valore di BACKGROUNDPLOT che fa pubblicare in secondo piano, lasciando
        /// AutoCAD subito utilizzabile.
        /// </summary>
        private const int BackgroundPublishing = 2;

        private const string BackgroundPlotVariable = "BACKGROUNDPLOT";

        /// <summary>
        /// Avvia la pubblicazione dei fogli descritti da un file DSD gia' scritto su disco.
        /// </summary>
        /// <param name="document">Disegno su cui agire.</param>
        /// <param name="dsdFilePath">Percorso del file DSD.</param>
        /// <param name="outputKind">Cosa si vuole ottenere: e' la fonte di verita' del formato.</param>
        /// <exception cref="ArgumentNullException">Documento mancante.</exception>
        /// <exception cref="ArgumentException">Percorso del DSD mancante.</exception>
        public static void Publish(Document document, string dsdFilePath, PublishOutputKind outputKind)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (string.IsNullOrWhiteSpace(dsdFilePath))
            {
                throw new ArgumentException("Manca il percorso del file DSD.", nameof(dsdFilePath));
            }

            VerifySheetTypeNumbersStillMatchAutoCad();

            // La pubblicazione va eseguita nel "contesto comando" di AutoCAD: chiamata
            // direttamente da un clic sulla palette, AutoCAD la rifiuterebbe perche' in
            // quel momento non e' in esecuzione nessun comando.
            AcadApp.DocumentManager.ExecuteInCommandContextAsync(
                _ =>
                {
                    // Questo blocco viene eseguito PIU' TARDI, quando il clic dell'utente
                    // e' gia' finito: un errore che uscisse da qui non avrebbe piu' nessuno
                    // a raccoglierlo e chiuderebbe AutoCAD. Va quindi fermato qui dentro,
                    // registrato nel log e riportato nella riga di comando.
                    if (!AcadContext.TryRun(
                            OperationName,
                            () => RunPublish(dsdFilePath, outputKind),
                            out string error))
                    {
                        AcadContext.WriteMessage(error);
                    }

                    return Task.CompletedTask;
                },
                null);
        }

        /// <summary>
        /// Esegue la pubblicazione vera e propria. Gia' dentro il contesto comando.
        /// </summary>
        private static void RunPublish(string dsdFilePath, PublishOutputKind outputKind)
        {
            object previousBackgroundPlot = AcadApp.GetSystemVariable(BackgroundPlotVariable);
            var listener = new PublishOutcomeListener();

            try
            {
                AcadApp.SetSystemVariable(BackgroundPlotVariable, BackgroundPublishing);

                using (var dsd = new DsdData())
                {
                    dsd.ReadDsd(dsdFilePath);

                    // Il formato viene RIMESSO qui dall'enumerazione di AutoCAD, anche se
                    // il file DSD lo contiene gia': cosi' il tipo di uscita non dipende da
                    // come e' stato scritto il testo del file. E' il campo che, sbagliato,
                    // faceva fallire la pubblicazione senza dire niente.
                    dsd.SheetType = ToSheetType(outputKind);

                    // Nessuna finestra deve comparire: la pubblicazione si bloccherebbe in
                    // attesa di un clic che, in background, nessuno vedrebbe mai.
                    dsd.PromptForDwfName = false;
                    dsd.NoOfCopies = 1;

                    TrySetPublishLogPath(dsd);

                    listener.Attach();

                    PlotConfig config = PlotConfigManager.CurrentConfig;
                    AcadApp.Publisher.PublishExecute(dsd, config);
                }

                listener.LogSummary(outputKind);
            }
            finally
            {
                listener.Detach();
                RestoreBackgroundPlot(previousBackgroundPlot);
            }
        }

        /// <summary>
        /// Traduce la scelta dell'utente nel tipo di uscita di AutoCAD, sempre nella
        /// variante "un file per foglio".
        /// </summary>
        private static SheetType ToSheetType(PublishOutputKind kind) =>
            (SheetType)PublishSheetType.ForOneFilePerLayout(kind);

        /// <summary>
        /// Controlla che i numeri usati per scrivere il DSD corrispondano ancora
        /// all'enumerazione vera di AutoCAD.
        /// </summary>
        /// <remarks>
        /// I numeri sono stati verificati su AutoCAD 2024, 2026 e 2027, ma sono comunque
        /// una convenzione di Autodesk: se cambiassero, senza questo controllo il plugin
        /// tornerebbe a produrre il formato sbagliato in silenzio. Qui la differenza
        /// finisce nel log.
        /// </remarks>
        private static void VerifySheetTypeNumbersStillMatchAutoCad()
        {
            CheckOne(PublishSheetType.OriginalDevice, SheetType.OriginalDevice);
            CheckOne(PublishSheetType.SingleDwf, SheetType.SingleDwf);
            CheckOne(PublishSheetType.SingleDwfx, SheetType.SingleDwfx);
            CheckOne(PublishSheetType.SinglePdf, SheetType.SinglePdf);

            void CheckOne(int ours, SheetType autocad)
            {
                if (ours == (int)autocad)
                {
                    return;
                }

                PluginLog.Warn(
                    OperationName,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "ATTENZIONE: il valore di {0} e' cambiato in AutoCAD (plugin: {1}, AutoCAD: {2}). "
                        + "I file prodotti potrebbero essere nel formato sbagliato.",
                        autocad,
                        ours,
                        (int)autocad));
            }
        }

        /// <summary>
        /// Chiede ad AutoCAD di scrivere il proprio registro di pubblicazione accanto ai
        /// log del plugin: quando un foglio non viene prodotto, il motivo preciso e' li'.
        /// </summary>
        private static void TrySetPublishLogPath(DsdData dsd)
        {
            try
            {
                Directory.CreateDirectory(PluginLog.LogDirectory);
                dsd.LogFilePath = Path.Combine(
                    PluginLog.LogDirectory,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "pubblicazione_{0:yyyy-MM-dd}.csv",
                        DateTime.Now));
            }
            catch (IOException ex)
            {
                // Il registro di AutoCAD e' un di piu': se non si puo' scrivere, la
                // pubblicazione deve comunque partire.
                PluginLog.Warn(OperationName, "Registro di pubblicazione non attivato: " + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                PluginLog.Warn(OperationName, "Registro di pubblicazione non attivato: " + ex.Message);
            }
        }

        private static void RestoreBackgroundPlot(object previousValue)
        {
            if (previousValue == null)
            {
                return;
            }

            try
            {
                AcadApp.SetSystemVariable(BackgroundPlotVariable, previousValue);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                // Non rimettere a posto una variabile di sistema e' fastidioso, non grave:
                // non deve trasformarsi in un errore di pubblicazione.
                PluginLog.Warn(
                    OperationName,
                    "Non sono riuscito a ripristinare BACKGROUNDPLOT: " + ex.Message);
            }
        }
    }
}
