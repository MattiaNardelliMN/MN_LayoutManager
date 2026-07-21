using System;
using Autodesk.AutoCAD.ApplicationServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Autodesk.AutoCAD.DatabaseServices;
using LayoutManagerPalette.Infrastructure;
using AcadLayoutManager = Autodesk.AutoCAD.DatabaseServices.LayoutManager;

namespace LayoutManagerPalette.Services
{
    /// <summary>
    /// Avvisa la palette quando i layout cambiano, anche se il cambiamento arriva dai
    /// comandi nativi di AutoCAD e non dalla palette stessa.
    /// </summary>
    /// <remarks>
    /// Come funziona: gli eventi di AutoCAD arrivano a raffica (uno script che crea 20
    /// layout ne genera 20 di fila). Ricostruire l'elenco a ogni evento bloccherebbe
    /// AutoCAD. Quindi gli eventi alzano solo un "cartellino" (serve aggiornare) e
    /// l'aggiornamento vero avviene una sola volta quando AutoCAD e' fermo (evento Idle).
    /// </remarks>
    public static class LayoutChangeNotifier
    {
        private const string OperationName = "Sincronizzazione layout";

        private static bool _started;
        private static bool _refreshPending;

        /// <summary>Scatta quando l'elenco dei layout va ricostruito.</summary>
        public static event EventHandler LayoutsChanged;

        /// <summary>
        /// Inizia ad ascoltare i cambiamenti. Va chiamata una sola volta all'avvio del
        /// plugin: <c>LayoutManager.Current</c> e' unico per tutta la sessione di AutoCAD.
        /// </summary>
        public static void Start()
        {
            if (_started)
            {
                return;
            }

            AcadContext.TryRun(OperationName, () =>
            {
                AcadLayoutManager manager = AcadLayoutManager.Current;
                if (manager != null)
                {
                    manager.LayoutCreated += OnLayoutEvent;
                    manager.LayoutRemoved += OnLayoutEvent;
                    manager.LayoutCopied += OnLayoutCopied;
                    manager.LayoutRenamed += OnLayoutRenamed;
                    manager.LayoutSwitched += OnLayoutEvent;
                }

                DocumentCollection documents = AcadApp.DocumentManager;
                documents.DocumentActivated += OnDocumentEvent;
                documents.DocumentCreated += OnDocumentEvent;
                documents.DocumentDestroyed += OnDocumentDestroyed;

                AcadApp.Idle += OnIdle;
                _started = true;
            }, out string error);

            if (error != null)
            {
                AcadContext.WriteMessage(error);
            }
        }

        /// <summary>
        /// Smette di ascoltare. Va chiamata alla chiusura del plugin, altrimenti gli
        /// eventi resterebbero agganciati a oggetti non piu' validi.
        /// </summary>
        public static void Stop()
        {
            if (!_started)
            {
                return;
            }

            _started = false;

            AcadContext.TryRun(OperationName, () =>
            {
                AcadLayoutManager manager = AcadLayoutManager.Current;
                if (manager != null)
                {
                    manager.LayoutCreated -= OnLayoutEvent;
                    manager.LayoutRemoved -= OnLayoutEvent;
                    manager.LayoutCopied -= OnLayoutCopied;
                    manager.LayoutRenamed -= OnLayoutRenamed;
                    manager.LayoutSwitched -= OnLayoutEvent;
                }

                DocumentCollection documents = AcadApp.DocumentManager;
                documents.DocumentActivated -= OnDocumentEvent;
                documents.DocumentCreated -= OnDocumentEvent;
                documents.DocumentDestroyed -= OnDocumentDestroyed;

                AcadApp.Idle -= OnIdle;
            }, out _);
        }

        /// <summary>
        /// Chiede un aggiornamento dell'elenco al prossimo momento libero di AutoCAD.
        /// Usarla dopo le azioni della palette che non generano eventi propri.
        /// </summary>
        public static void RequestRefresh() => _refreshPending = true;

        private static void OnLayoutEvent(object sender, LayoutEventArgs e) => _refreshPending = true;

        private static void OnLayoutCopied(object sender, LayoutCopiedEventArgs e) => _refreshPending = true;

        private static void OnLayoutRenamed(object sender, LayoutRenamedEventArgs e) => _refreshPending = true;

        private static void OnDocumentEvent(object sender, DocumentCollectionEventArgs e) => _refreshPending = true;

        private static void OnDocumentDestroyed(object sender, DocumentDestroyedEventArgs e) => _refreshPending = true;

        private static void OnIdle(object sender, EventArgs e)
        {
            if (!_refreshPending)
            {
                return;
            }

            _refreshPending = false;

            EventHandler handler = LayoutsChanged;
            if (handler == null)
            {
                return;
            }

            // Se chi ascolta va in errore, l'errore non deve tornare dentro AutoCAD
            // dall'evento Idle: verrebbe fuori come crash senza spiegazione.
            AcadContext.TryRun(OperationName, () => handler(null, EventArgs.Empty), out string error);

            if (error != null)
            {
                PluginLog.Error(OperationName, "Aggiornamento dell'elenco non riuscito.", null);
            }
        }
    }
}
