using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace MN_LayoutManager.Core
{
    /// <summary>
    /// Costruisce il contenuto di un file DSD (Drawing Set Description): l'elenco di fogli
    /// che il comando nativo -PUBLISH di AutoCAD sa leggere.
    /// E' un file di testo in formato INI: generarlo e' logica pura, quindi testabile.
    /// </summary>
    public static class DsdFileBuilder
    {
        // I numeri del campo Type= del DSD sono definiti da AutoCAD: stanno qui come
        // costanti con un nome, invece che sparsi come numeri magici nel codice.
        private const int TargetTypePlotter = 0;
        private const int TargetTypeDwf = 1;
        private const int TargetTypeDwfx = 2;
        private const int TargetTypePdf = 6;

        /// <summary>
        /// Valore di MULTISHEET che chiede ad AutoCAD un file separato per ogni foglio,
        /// invece di un unico documento multipagina.
        /// </summary>
        private const int OneFilePerSheet = 0;

        /// <summary>
        /// Prova a costruire il testo del file DSD.
        /// </summary>
        /// <param name="request">Cosa pubblicare.</param>
        /// <param name="content">Testo del file DSD, se tutto e' a posto.</param>
        /// <param name="error">Messaggio in italiano che spiega cosa manca, altrimenti null.</param>
        /// <returns>true se il DSD e' stato costruito.</returns>
        public static bool TryBuild(PublishRequest request, out string content, out string error)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            content = null;

            if (string.IsNullOrWhiteSpace(request.DrawingPath))
            {
                error = "Il disegno non e' ancora stato salvato su disco: salvalo prima di stampare o pubblicare.";
                return false;
            }

            if (request.LayoutNames.Count == 0)
            {
                error = "Nessun layout da stampare o pubblicare.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.OutputFolder))
            {
                error = "Indica la cartella di destinazione delle stampe.";
                return false;
            }

            content = Build(request);
            error = null;
            return true;
        }

        /// <summary>
        /// Nome del file che verra' prodotto per un layout.
        /// Serve per poterlo mostrare all'utente prima di stampare.
        /// </summary>
        /// <param name="request">Richiesta di pubblicazione.</param>
        /// <param name="layoutName">Nome del layout.</param>
        /// <returns>Nome del file, estensione inclusa.</returns>
        public static string GetOutputFileName(PublishRequest request, string layoutName)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return MakeFileNameSafe(layoutName) + request.OutputExtension;
        }

        private static string Build(PublishRequest request)
        {
            string dwgPath = request.DrawingPath;
            string folder = EnsureTrailingSeparator(request.OutputFolder);

            var builder = new StringBuilder();
            builder.AppendLine("[DWF6Version]");
            builder.AppendLine("Ver=1");
            builder.AppendLine("[DWF6MinorVersion]");
            builder.AppendLine("MinorVer=1");

            var usedTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string layoutName in request.LayoutNames)
            {
                // Con un file per foglio, AutoCAD nomina il file come il titolo del foglio:
                // usando il nome del layout si ottiene "Tavola 01.pdf".
                string title = BuildUniqueSheetTitle(layoutName, usedTitles);

                builder.AppendLine(FormattableString.Invariant($"[DWF6Sheet:{title}]"));
                builder.AppendLine(FormattableString.Invariant($"DWG={dwgPath}"));
                builder.AppendLine(FormattableString.Invariant($"Layout={layoutName}"));
                builder.AppendLine("Setup=");
                builder.AppendLine(FormattableString.Invariant($"OriginalSheetPath={dwgPath}"));
                builder.AppendLine("Has Plot Port=0");
                builder.AppendLine("Has3DDWF=0");
            }

            builder.AppendLine("[Target]");
            builder.AppendLine(FormattableString.Invariant($"Type={GetTargetType(request.OutputKind)}"));

            // DWF vuole un percorso di file: con un file per foglio AutoCAD ne usa solo
            // la cartella, e il nome lo prende dal titolo di ciascun foglio.
            builder.AppendLine(FormattableString.Invariant(
                $"DWF={folder}{MakeFileNameSafe(Path.GetFileNameWithoutExtension(dwgPath))}{request.OutputExtension}"));
            builder.AppendLine(FormattableString.Invariant($"OUT={folder}"));
            builder.AppendLine("PROMPT=FALSE");
            builder.AppendLine(FormattableString.Invariant($"MULTISHEET={OneFilePerSheet}"));
            builder.AppendLine("PASSWORD=");
            builder.AppendLine("PWDPROTECTPUBLISHEDDWF=FALSE");

            builder.AppendLine("[AutoCAD Block Data]");
            builder.AppendLine("IncludeBlockInfo=0");
            builder.AppendLine("BlockTemplateFilePath=");

            builder.AppendLine("[SheetSet]");
            builder.AppendLine("Name=");
            builder.AppendLine("UseSetupPropertyOverride=FALSE");
            builder.AppendLine("PromptForDwfName=FALSE");

            builder.AppendLine("[SheetSetOM]");

            return builder.ToString();
        }

        /// <summary>
        /// Ogni foglio del DSD deve avere un titolo diverso dagli altri, altrimenti
        /// AutoCAD scarta i duplicati senza dire niente (e i file si sovrascriverebbero).
        /// </summary>
        private static string BuildUniqueSheetTitle(string layoutName, ISet<string> usedTitles)
        {
            string baseTitle = MakeFileNameSafe(layoutName);
            string title = baseTitle;
            int counter = 2;

            while (!usedTitles.Add(title))
            {
                title = string.Format(CultureInfo.InvariantCulture, "{0} ({1})", baseTitle, counter);
                counter++;
            }

            return title;
        }

        /// <summary>
        /// Il titolo del foglio diventa il nome di un file su disco: i caratteri che
        /// Windows non ammette nei nomi di file vengono sostituiti con un trattino.
        /// </summary>
        private static string MakeFileNameSafe(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "Layout";
            }

            var builder = new StringBuilder(value.Length);
            char[] invalid = Path.GetInvalidFileNameChars();

            foreach (char character in value)
            {
                builder.Append(Array.IndexOf(invalid, character) >= 0 ? '-' : character);
            }

            return builder.ToString().Trim();
        }

        private static string EnsureTrailingSeparator(string folder)
        {
            string trimmed = folder.TrimEnd();
            return trimmed.EndsWith("\\", StringComparison.Ordinal)
                ? trimmed
                : trimmed + "\\";
        }

        private static int GetTargetType(PublishOutputKind kind)
        {
            switch (kind)
            {
                case PublishOutputKind.PageSetupPlotter:
                    return TargetTypePlotter;
                case PublishOutputKind.Dwf:
                    return TargetTypeDwf;
                case PublishOutputKind.Dwfx:
                    return TargetTypeDwfx;
                case PublishOutputKind.Pdf:
                    return TargetTypePdf;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Tipo di pubblicazione non riconosciuto.");
            }
        }
    }
}
