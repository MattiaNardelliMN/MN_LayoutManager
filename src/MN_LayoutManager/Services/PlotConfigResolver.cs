using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using MN_LayoutManager.Core;
using MN_LayoutManager.Infrastructure;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;
using AcadLayoutManager = Autodesk.AutoCAD.DatabaseServices.LayoutManager;

namespace MN_LayoutManager.Services
{
    /// <summary>
    /// Sceglie la configurazione di stampa da passare alla pubblicazione di AutoCAD.
    /// </summary>
    /// <remarks>
    /// Perche' esiste questo modulo: <c>PlotConfigManager.CurrentConfig</c> - cioe'
    /// "dammi la configurazione di stampa corrente" - non e' una domanda che si possa
    /// fare sempre. Se in quella sessione di AutoCAD nessuno ha ancora scelto una
    /// stampante, non c'e' nessuna configurazione corrente e la proprieta' solleva
    /// <c>eNullPtr</c>. E' esattamente cio' che faceva fallire ogni pubblicazione
    /// avviata dalla palette: l'utente non aveva motivo di aprire prima la finestra di
    /// stampa, quindi la configurazione corrente non esisteva mai.
    /// <para>
    /// La soluzione non e' chiedere, e' <b>impostare</b>: qui si sceglie un dispositivo
    /// adatto al formato richiesto e lo si rende corrente con
    /// <c>PlotConfigManager.SetCurrentConfig</c>, che restituisce la configurazione da
    /// usare.
    /// </para>
    /// <para>
    /// Se non si trova nessun dispositivo utilizzabile, questo modulo NON solleva
    /// eccezioni: restituisce false e un messaggio in italiano che dice cosa e' stato
    /// provato e cosa AutoCAD ha invece disponibile. E' l'informazione che serve per
    /// capire il problema leggendo il log.
    /// </para>
    /// </remarks>
    internal static class PlotConfigResolver
    {
        private const string OperationName = "Stampa/Pubblica";

        /// <summary>
        /// Trova e rende corrente una configurazione di stampa adatta.
        /// </summary>
        /// <param name="document">Disegno attivo: serve a leggere il dispositivo del layout.</param>
        /// <param name="outputKind">Formato richiesto dall'utente.</param>
        /// <param name="config">La configurazione da passare alla pubblicazione.</param>
        /// <param name="error">Messaggio in italiano se non se ne e' trovata nessuna.</param>
        /// <returns>true se <paramref name="config"/> e' utilizzabile.</returns>
        public static bool TryResolve(
            Document document,
            PublishOutputKind outputKind,
            out PlotConfig config,
            out string error)
        {
            config = null;

            // Senza questo, l'elenco dei dispositivi puo' essere ancora vuoto: AutoCAD lo
            // costruisce alla prima occasione utile, che in una sessione dove non si e'
            // mai stampato non e' ancora arrivata.
            RefreshDeviceList();

            IReadOnlyList<string> candidates = BuildCandidates(document, outputKind);

            foreach (string deviceName in candidates)
            {
                if (TrySetCurrentConfig(deviceName, out config))
                {
                    PluginLog.Info(OperationName, "Dispositivo di stampa in uso: " + deviceName);
                    error = null;
                    return true;
                }
            }

            error = BuildNoDeviceMessage(outputKind, candidates);
            return false;
        }

        /// <summary>
        /// I dispositivi da provare, in ordine di preferenza.
        /// </summary>
        private static IReadOnlyList<string> BuildCandidates(
            Document document,
            PublishOutputKind outputKind)
        {
            IReadOnlyList<string> forFormat = PlotDeviceNames.ForOutputKind(outputKind);
            if (forFormat.Count > 0)
            {
                return forFormat;
            }

            // Caso "Stampa": il dispositivo non lo sceglie il plugin. Ogni foglio va al
            // proprio, indicato nelle sue impostazioni di pagina; quello che passiamo qui
            // e' solo il valore di partenza, e il piu' sensato e' il dispositivo del
            // layout corrente.
            var candidates = new List<string>();

            string layoutDevice = TryGetCurrentLayoutDevice(document);
            if (PlotDeviceNames.IsRealDevice(layoutDevice))
            {
                candidates.Add(layoutDevice);
            }

            candidates.Add(PlotDeviceNames.DefaultWindowsPrinter);
            return candidates;
        }

        /// <summary>
        /// Rende corrente il dispositivo indicato.
        /// </summary>
        /// <remarks>
        /// La configurazione restituita appartiene a <c>PlotConfigManager</c> (e' "quella
        /// corrente"), quindi NON va chiusa qui: chiuderla mentre la pubblicazione in
        /// secondo piano la sta ancora usando la farebbe fallire.
        /// </remarks>
        private static bool TrySetCurrentConfig(string deviceName, out PlotConfig config)
        {
            config = null;

            if (string.IsNullOrWhiteSpace(deviceName))
            {
                return false;
            }

            try
            {
                config = PlotConfigManager.SetCurrentConfig(deviceName);
                return config != null;
            }
            catch (AcadException ex)
            {
                // Un dispositivo non disponibile non e' un errore: si prova il prossimo.
                // Resta scritto nel log perche' e' l'indizio utile se falliscono tutti.
                PluginLog.Warn(
                    OperationName,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "Dispositivo '{0}' non utilizzabile: {1}",
                        deviceName,
                        ex.Message));
                return false;
            }
        }

        /// <summary>
        /// Il dispositivo di stampa dichiarato dalle impostazioni di pagina del layout
        /// attivo, oppure stringa vuota.
        /// </summary>
        private static string TryGetCurrentLayoutDevice(Document document)
        {
            if (document == null || document.Database == null)
            {
                return string.Empty;
            }

            try
            {
                AcadLayoutManager manager = AcadLayoutManager.Current;
                string layoutName = manager == null ? null : manager.CurrentLayout;
                if (string.IsNullOrEmpty(layoutName))
                {
                    return string.Empty;
                }

                ObjectId layoutId = manager.GetLayoutId(layoutName);
                if (layoutId.IsNull)
                {
                    return string.Empty;
                }

                using (Transaction tr = document.Database.TransactionManager.StartTransaction())
                {
                    var layout = tr.GetObject(layoutId, OpenMode.ForRead) as Layout;
                    string device = layout == null ? string.Empty : layout.PlotConfigurationName;
                    tr.Commit();
                    return device ?? string.Empty;
                }
            }
            catch (AcadException ex)
            {
                // Non riuscire a leggere il dispositivo del layout non deve impedire la
                // stampa: si prosegue con il ripiego.
                PluginLog.Warn(
                    OperationName,
                    "Non ho potuto leggere il dispositivo del layout corrente: " + ex.Message);
                return string.Empty;
            }
        }

        /// <summary>
        /// Messaggio di errore che dice cosa e' stato provato e cosa c'e' davvero.
        /// </summary>
        private static string BuildNoDeviceMessage(
            PublishOutputKind outputKind,
            IReadOnlyList<string> tried)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "Non ho trovato nessuna stampante utilizzabile per '{0}'. "
                + "Ho provato: {1}. Dispositivi disponibili in AutoCAD: {2}.",
                outputKind,
                tried.Count == 0 ? "(nessuno)" : string.Join(", ", tried),
                DescribeInstalledDevices());
        }

        /// <summary>
        /// Elenco dei dispositivi che AutoCAD dichiara di avere, per il log.
        /// </summary>
        private static string DescribeInstalledDevices()
        {
            try
            {
                var names = new List<string>();

                // La collezione appartiene a PlotConfigManager: si legge e basta.
                PlotConfigInfoCollection devices = PlotConfigManager.Devices;
                if (devices != null)
                {
                    foreach (PlotConfigInfo info in devices)
                    {
                        if (info != null && !string.IsNullOrEmpty(info.DeviceName))
                        {
                            names.Add(info.DeviceName);
                        }
                    }
                }

                return names.Count == 0 ? "(nessuno)" : string.Join(", ", names);
            }
            catch (AcadException ex)
            {
                return "(non leggibili: " + ex.Message + ")";
            }
        }

        private static void RefreshDeviceList()
        {
            try
            {
                PlotConfigManager.RefreshList(RefreshCode.All);
            }
            catch (AcadException ex)
            {
                // Se l'aggiornamento non riesce si prova comunque: l'elenco potrebbe
                // essere gia' popolato da prima.
                PluginLog.Warn(
                    OperationName,
                    "Non ho potuto aggiornare l'elenco delle stampanti: " + ex.Message);
            }
        }
    }
}
