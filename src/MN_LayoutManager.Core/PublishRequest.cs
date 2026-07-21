using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MN_LayoutManager.Core
{
    /// <summary>Che cosa deve produrre la pubblicazione.</summary>
    public enum PublishOutputKind
    {
        /// <summary>
        /// "Stampa": manda ogni layout al plotter/stampante indicato nelle sue
        /// impostazioni di pagina. Se quel dispositivo stampa su file, i file finiscono
        /// nella cartella di destinazione indicata.
        /// </summary>
        PageSetupPlotter,

        /// <summary>Produce file PDF.</summary>
        Pdf,

        /// <summary>Produce file DWF.</summary>
        Dwf,

        /// <summary>Produce file DWFx.</summary>
        Dwfx,
    }

    /// <summary>
    /// Descrive una stampa o pubblicazione batch: quale disegno, quali layout,
    /// cosa produrre e in quale cartella.
    /// </summary>
    /// <remarks>
    /// Viene sempre prodotto <b>un file separato per ogni layout</b>, non un unico
    /// documento multipagina.
    /// </remarks>
    public sealed class PublishRequest
    {
        /// <summary>Crea la richiesta.</summary>
        /// <param name="drawingPath">Percorso completo del file DWG.</param>
        /// <param name="layoutNames">Layout da stampare/pubblicare, nell'ordine voluto.</param>
        /// <param name="outputKind">Cosa produrre.</param>
        /// <param name="outputFolder">Cartella in cui finiscono i file prodotti.</param>
        public PublishRequest(
            string drawingPath,
            IEnumerable<string> layoutNames,
            PublishOutputKind outputKind,
            string outputFolder)
        {
            DrawingPath = drawingPath;
            OutputKind = outputKind;
            OutputFolder = outputFolder;

            var names = new List<string>();
            if (layoutNames != null)
            {
                names.AddRange(layoutNames);
            }

            LayoutNames = new ReadOnlyCollection<string>(names);
        }

        /// <summary>Percorso completo del disegno.</summary>
        public string DrawingPath { get; }

        /// <summary>Layout coinvolti, nell'ordine in cui vanno pubblicati.</summary>
        public IReadOnlyList<string> LayoutNames { get; }

        /// <summary>Tipo di uscita.</summary>
        public PublishOutputKind OutputKind { get; }

        /// <summary>Cartella di destinazione dei file prodotti.</summary>
        public string OutputFolder { get; }

        /// <summary>Estensione dei file prodotti, punto incluso.</summary>
        public string OutputExtension
        {
            get
            {
                switch (OutputKind)
                {
                    case PublishOutputKind.Dwf:
                        return ".dwf";
                    case PublishOutputKind.Dwfx:
                        return ".dwfx";
                    default:
                        return ".pdf";
                }
            }
        }
    }
}
