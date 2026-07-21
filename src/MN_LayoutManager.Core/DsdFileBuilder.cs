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

            if (request.RequiresOutputFile && string.IsNullOrWhiteSpace(request.OutputFilePath))
            {
                error = "Manca il file di destinazione.";
                return false;
            }

            content = Build(request);
            error = null;
            return true;
        }

        private static string Build(PublishRequest request)
        {
            string dwgPath = request.DrawingPath;
            string dwgName = Path.GetFileNameWithoutExtension(dwgPath);

            var builder = new StringBuilder();
            builder.AppendLine("[DWF6Version]");
            builder.AppendLine("Ver=1");
            builder.AppendLine("[DWF6MinorVersion]");
            builder.AppendLine("MinorVer=1");

            var usedTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string layoutName in request.LayoutNames)
            {
                string title = BuildUniqueSheetTitle(dwgName, layoutName, usedTitles);

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
            builder.AppendLine(FormattableString.Invariant($"DWF={request.OutputFilePath ?? string.Empty}"));
            builder.AppendLine("PROMPT=FALSE");
            builder.AppendLine(FormattableString.Invariant($"MULTISHEET={(request.MultiSheet ? 1 : 0)}"));
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
        /// AutoCAD scarta i duplicati senza dire niente.
        /// </summary>
        private static string BuildUniqueSheetTitle(string dwgName, string layoutName, ISet<string> usedTitles)
        {
            string baseTitle = string.Format(CultureInfo.InvariantCulture, "{0}-{1}", dwgName, layoutName);
            string title = baseTitle;
            int counter = 2;

            while (!usedTitles.Add(title))
            {
                title = string.Format(CultureInfo.InvariantCulture, "{0} ({1})", baseTitle, counter);
                counter++;
            }

            return title;
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
