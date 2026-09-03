using System.Globalization;
using Autodesk.AutoCAD.Publishing;
using MN_LayoutManager.Core;
using MN_LayoutManager.Infrastructure;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace MN_LayoutManager.Services
{
    /// <summary>
    /// Ascolta gli eventi di pubblicazione di AutoCAD e ne scrive l'esito nel log.
    /// </summary>
    /// <remarks>
    /// E' la risposta al problema che ha reso invisibile per mesi un difetto della
    /// pubblicazione: prima il plugin scriveva "pubblicazione avviata" e finiva li',
    /// quindi un fallimento di AutoCAD non lasciava nessuna traccia. Ora il log dice
    /// quanti fogli sono stati prodotti, oppure che il lavoro e' fallito.
    /// <para>
    /// Se la pubblicazione va in secondo piano, AutoCAD la esegue in un processo a parte
    /// e gli eventi sui singoli fogli non arrivano qui: in quel caso resta comunque
    /// registrato che il lavoro e' stato consegnato al processo di sfondo; il dettaglio
    /// dei singoli fogli sta nel registro CSV di AutoCAD, che pero' esiste solo se
    /// l'utente ne ha attivato il salvataggio (vedi <see cref="PublishLogFile"/>).
    /// </para>
    /// </remarks>
    internal sealed class PublishOutcomeListener
    {
        private const string OperationName = "Stampa/Pubblica";

        private bool _attached;
        private bool _failed;
        private bool _inBackground;
        private int _sheetsCompleted;

        /// <summary>Comincia ad ascoltare gli eventi di pubblicazione.</summary>
        public void Attach()
        {
            if (_attached)
            {
                return;
            }

            Publisher publisher = AcadApp.Publisher;
            publisher.AboutToBeginBackgroundPublishing += OnAboutToBeginBackgroundPublishing;
            publisher.EndSheet += OnEndSheet;
            publisher.CancelledOrFailedPublishing += OnCancelledOrFailed;
            _attached = true;
        }

        /// <summary>
        /// Smette di ascoltare. Va chiamata sempre, anche se la pubblicazione fallisce:
        /// eventi lasciati agganciati si accumulerebbero a ogni stampa.
        /// </summary>
        public void Detach()
        {
            if (!_attached)
            {
                return;
            }

            Publisher publisher = AcadApp.Publisher;
            publisher.AboutToBeginBackgroundPublishing -= OnAboutToBeginBackgroundPublishing;
            publisher.EndSheet -= OnEndSheet;
            publisher.CancelledOrFailedPublishing -= OnCancelledOrFailed;
            _attached = false;
        }

        /// <summary>Scrive nel log com'e' andata.</summary>
        /// <param name="outputKind">Formato richiesto, per rendere il log leggibile.</param>
        public void LogSummary(PublishOutputKind outputKind)
        {
            if (_failed)
            {
                PluginLog.Error(
                    OperationName,
                    "AutoCAD ha annullato o fallito la pubblicazione: nessun file prodotto. "
                    + PublishLogFile.WhereToLookHint(),
                    null);
                return;
            }

            if (_inBackground)
            {
                PluginLog.Info(
                    OperationName,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Lavoro ({0}) consegnato alla pubblicazione in secondo piano di AutoCAD. {1}",
                        outputKind,
                        PublishLogFile.WhereToLookHint()));
                return;
            }

            PluginLog.Info(
                OperationName,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Pubblicazione ({0}) conclusa: {1} fogli prodotti.",
                    outputKind,
                    _sheetsCompleted));
        }

        private void OnAboutToBeginBackgroundPublishing(
            object sender,
            AboutToBeginBackgroundPublishingEventArgs e)
        {
            _inBackground = e.JobWillPublishInBackground;
        }

        private void OnEndSheet(object sender, PublishSheetEventArgs e)
        {
            if (e.IsPlotJobCancelled)
            {
                _failed = true;
                return;
            }

            _sheetsCompleted++;
        }

        private void OnCancelledOrFailed(object sender, PublishEventArgs e)
        {
            _failed = true;
        }
    }
}
