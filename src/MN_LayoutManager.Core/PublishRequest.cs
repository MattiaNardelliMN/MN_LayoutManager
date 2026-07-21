using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MN_LayoutManager.Core
{
    /// <summary>Che cosa deve produrre la pubblicazione.</summary>
    public enum PublishOutputKind
    {
        /// <summary>
        /// "Stampa": manda ogni layout al plotter/stampante indicato nelle sue
        /// impostazioni di pagina. Non produce un file.
        /// </summary>
        PageSetupPlotter,

        /// <summary>Produce un file PDF.</summary>
        Pdf,

        /// <summary>Produce un file DWF.</summary>
        Dwf,

        /// <summary>Produce un file DWFx.</summary>
        Dwfx,
    }

    /// <summary>
    /// Descrive una stampa o pubblicazione batch: quale disegno, quali layout, cosa produrre.
    /// Oggetto di soli dati.
    /// </summary>
    public sealed class PublishRequest
    {
        /// <summary>Crea la richiesta.</summary>
        /// <param name="drawingPath">Percorso completo del file DWG.</param>
        /// <param name="layoutNames">Layout da stampare/pubblicare, nell'ordine voluto.</param>
        /// <param name="outputKind">Cosa produrre.</param>
        /// <param name="outputFilePath">File di destinazione (ignorato per la stampa su plotter).</param>
        /// <param name="multiSheet">true = un unico file con tutte le pagine; false = un file per layout.</param>
        public PublishRequest(
            string drawingPath,
            IEnumerable<string> layoutNames,
            PublishOutputKind outputKind,
            string outputFilePath,
            bool multiSheet)
        {
            DrawingPath = drawingPath;
            OutputKind = outputKind;
            OutputFilePath = outputFilePath;
            MultiSheet = multiSheet;

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

        /// <summary>File prodotto (non usato quando si stampa sul plotter delle impostazioni di pagina).</summary>
        public string OutputFilePath { get; }

        /// <summary>true = un solo file multipagina.</summary>
        public bool MultiSheet { get; }

        /// <summary>true quando l'uscita e' un file e quindi serve un percorso di destinazione.</summary>
        public bool RequiresOutputFile => OutputKind != PublishOutputKind.PageSetupPlotter;
    }
}
