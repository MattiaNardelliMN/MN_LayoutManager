using System;

namespace MN_LayoutManager.Core
{
    /// <summary>
    /// I valori del campo <c>Type=</c> della sezione <c>[Target]</c> di un file DSD.
    /// </summary>
    /// <remarks>
    /// Questi numeri NON sono inventati e non vanno cambiati a intuito: sono i valori
    /// dell'enumerazione <c>Autodesk.AutoCAD.PlottingServices.SheetType</c>, letta per
    /// riflessione dalle DLL ufficiali di AutoCAD (<c>AcCoreMgd.dll</c>). Sono stati
    /// verificati identici su AutoCAD 2024 (24.3), 2025/2026 (25.0) e 2027 (26.0).
    /// <para>
    /// Il numero dice DA SOLO se si vuole un file unico multipagina ("Multi") oppure un
    /// file separato per ogni foglio ("Single"). Il plugin produce sempre un file per
    /// layout, quindi usa esclusivamente le varianti "Single".
    /// </para>
    /// <para>
    /// Il modulo <c>AcadPublisher</c>, lato AutoCAD, confronta questi numeri con
    /// l'enumerazione vera al momento della pubblicazione: se un giorno Autodesk li
    /// cambiasse, la differenza finirebbe nel log invece di produrre file sbagliati
    /// in silenzio.
    /// </para>
    /// </remarks>
    public static class PublishSheetType
    {
        /// <summary>Un file DWF separato per ogni foglio.</summary>
        public const int SingleDwf = 0;

        /// <summary>Un unico DWF multipagina con tutti i fogli.</summary>
        public const int MultiDwf = 1;

        /// <summary>
        /// Manda ogni foglio al dispositivo indicato nelle sue impostazioni di pagina:
        /// e' la "stampa" vera e propria, su plotter o stampante.
        /// </summary>
        public const int OriginalDevice = 2;

        /// <summary>Un file DWFx separato per ogni foglio.</summary>
        public const int SingleDwfx = 3;

        /// <summary>Un unico DWFx multipagina con tutti i fogli.</summary>
        public const int MultiDwfx = 4;

        /// <summary>Un file PDF separato per ogni foglio.</summary>
        public const int SinglePdf = 5;

        /// <summary>Un unico PDF multipagina con tutti i fogli.</summary>
        public const int MultiPdf = 6;

        /// <summary>
        /// Il valore di <c>Type=</c> che produce <b>un file separato per ogni layout</b>
        /// nel formato richiesto.
        /// </summary>
        /// <param name="kind">Cosa vuole ottenere l'utente.</param>
        /// <returns>Il numero da scrivere nel campo <c>Type=</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Formato non riconosciuto.</exception>
        public static int ForOneFilePerLayout(PublishOutputKind kind)
        {
            switch (kind)
            {
                case PublishOutputKind.PageSetupPlotter:
                    return OriginalDevice;
                case PublishOutputKind.Dwf:
                    return SingleDwf;
                case PublishOutputKind.Dwfx:
                    return SingleDwfx;
                case PublishOutputKind.Pdf:
                    return SinglePdf;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(kind),
                        kind,
                        "Tipo di pubblicazione non riconosciuto.");
            }
        }
    }
}
