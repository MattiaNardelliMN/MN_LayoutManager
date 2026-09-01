using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MN_LayoutManager.Core
{
    /// <summary>
    /// Nomi dei dispositivi di stampa standard di AutoCAD (i file <c>.pc3</c>), scelti in
    /// base al formato che si vuole ottenere.
    /// </summary>
    /// <remarks>
    /// A cosa servono: per pubblicare, AutoCAD pretende che una configurazione di stampa
    /// sia GIA' stata scelta nella sessione. Limitarsi a chiedergli "qual e' quella
    /// corrente" non basta: se nessuno ne ha mai scelta una - il caso normale quando si
    /// pubblica da una palette senza aver mai aperto la finestra di stampa - AutoCAD
    /// risponde con il vuoto e la pubblicazione fallisce prima ancora di iniziare.
    /// Il plugin deve quindi indicarne una lui, e questi sono i nomi fra cui sceglie.
    /// <para>
    /// Ogni elenco e' in ORDINE DI PREFERENZA: chi lo usa prova i nomi uno dopo l'altro e
    /// tiene il primo che AutoCAD accetta. Cosi' un computer su cui manca
    /// "DWG To PDF.pc3" usa comunque una delle altre stampanti PDF di AutoCAD, invece di
    /// arrendersi.
    /// </para>
    /// <para>
    /// Non sono nomi inventati: sono i file installati da AutoCAD stesso nella propria
    /// cartella <c>Plotters</c>, verificati su AutoCAD 2024.
    /// </para>
    /// </remarks>
    public static class PlotDeviceNames
    {
        /// <summary>
        /// Stampante di sistema di Windows. E' il ripiego per la stampa su plotter,
        /// quando il layout non dichiara nessun dispositivo suo.
        /// </summary>
        public const string DefaultWindowsPrinter = "Default Windows System Printer.pc3";

        /// <summary>
        /// Valore che AutoCAD scrive nelle impostazioni di pagina quando NON e' stato
        /// scelto nessun dispositivo. Va trattato come "nessuno", non come un nome.
        /// </summary>
        public const string NoDevice = "None";

        private static readonly ReadOnlyCollection<string> Pdf = AsReadOnly(
            "DWG To PDF.pc3",
            "AutoCAD PDF (General Documentation).pc3",
            "AutoCAD PDF (High Quality Print).pc3",
            "AutoCAD PDF (Smallest File).pc3",
            "AutoCAD PDF (Web and Mobile).pc3");

        private static readonly ReadOnlyCollection<string> Dwf = AsReadOnly(
            "DWF6 ePlot.pc3");

        private static readonly ReadOnlyCollection<string> Dwfx = AsReadOnly(
            "DWFx ePlot (XPS Compatible).pc3");

        private static readonly ReadOnlyCollection<string> None = AsReadOnly();

        /// <summary>
        /// I dispositivi da provare per ottenere il formato richiesto, in ordine di
        /// preferenza.
        /// </summary>
        /// <param name="kind">Cosa vuole ottenere l'utente.</param>
        /// <returns>
        /// I nomi da provare. Per <see cref="PublishOutputKind.PageSetupPlotter"/>
        /// l'elenco e' <b>vuoto</b> di proposito: in quel caso il dispositivo non lo
        /// sceglie il plugin, lo dice il layout nelle proprie impostazioni di pagina.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">Formato non riconosciuto.</exception>
        public static IReadOnlyList<string> ForOutputKind(PublishOutputKind kind)
        {
            switch (kind)
            {
                case PublishOutputKind.PageSetupPlotter:
                    return None;
                case PublishOutputKind.Pdf:
                    return Pdf;
                case PublishOutputKind.Dwf:
                    return Dwf;
                case PublishOutputKind.Dwfx:
                    return Dwfx;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(kind),
                        kind,
                        "Tipo di pubblicazione non riconosciuto.");
            }
        }

        /// <summary>
        /// true se il nome letto dalle impostazioni di pagina indica un dispositivo vero.
        /// </summary>
        /// <param name="deviceName">Nome letto dal layout.</param>
        /// <returns>false se e' vuoto oppure vale "None".</returns>
        public static bool IsRealDevice(string deviceName)
        {
            return !string.IsNullOrWhiteSpace(deviceName)
                && !string.Equals(deviceName.Trim(), NoDevice, StringComparison.OrdinalIgnoreCase);
        }

        private static ReadOnlyCollection<string> AsReadOnly(params string[] names)
        {
            return new ReadOnlyCollection<string>(new List<string>(names));
        }
    }
}
