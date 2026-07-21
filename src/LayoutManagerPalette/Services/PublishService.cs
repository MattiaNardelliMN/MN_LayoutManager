using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using LayoutManagerPalette.Core;
using LayoutManagerPalette.Infrastructure;

namespace LayoutManagerPalette.Services
{
    /// <summary>
    /// Stampa e pubblicazione di piu' layout in un colpo solo.
    /// Scrive un file DSD temporaneo (l'elenco dei fogli) e lo passa al comando nativo
    /// -PUBLISH di AutoCAD, che sa gia' gestire code di stampa, plotter e PDF.
    /// </summary>
    public static class PublishService
    {
        private const string TempFolderName = "LayoutManagerPalette";

        /// <summary>
        /// Prepara ed avvia la stampa/pubblicazione dei layout indicati.
        /// </summary>
        /// <param name="document">Disegno da cui pubblicare.</param>
        /// <param name="layoutNames">Layout scelti, nell'ordine voluto.</param>
        /// <param name="outputKind">Stampa sul plotter delle impostazioni di pagina, o file PDF/DWF.</param>
        /// <param name="outputFilePath">File di destinazione (solo per PDF/DWF).</param>
        /// <param name="error">Messaggio in italiano se non si e' potuto procedere.</param>
        /// <returns>true se la pubblicazione e' stata messa in coda ad AutoCAD.</returns>
        public static bool TryPublish(
            Document document,
            IReadOnlyList<string> layoutNames,
            PublishOutputKind outputKind,
            string outputFilePath,
            out string error)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            string drawingPath = GetSavedDrawingPath(document);
            var request = new PublishRequest(
                drawingPath,
                layoutNames,
                outputKind,
                outputFilePath,
                multiSheet: true);

            if (!DsdFileBuilder.TryBuild(request, out string dsdContent, out error))
            {
                return false;
            }

            string dsdPath;
            try
            {
                dsdPath = WriteTemporaryDsd(dsdContent);
            }
            catch (IOException ex)
            {
                error = "Non riesco a creare il file temporaneo per la stampa: " + ex.Message;
                PluginLog.Error("Stampa/Pubblica", error, ex);
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                error = "Non ho i permessi per creare il file temporaneo per la stampa.";
                PluginLog.Error("Stampa/Pubblica", error, ex);
                return false;
            }

            PluginLog.Info(
                "Stampa/Pubblica",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} layout, destinazione {1}, elenco fogli in {2}",
                    request.LayoutNames.Count,
                    outputKind,
                    dsdPath));

            AcadCommandRunner.PublishFromDsd(document, dsdPath);
            error = null;
            return true;
        }

        /// <summary>
        /// true se il disegno ha modifiche non ancora salvate.
        /// Conta perche' la pubblicazione legge il file su disco: le modifiche non
        /// salvate non finirebbero nella stampa.
        /// </summary>
        /// <returns>true se ci sono modifiche non salvate.</returns>
        public static bool HasUnsavedChanges()
        {
            object dbmod = AcadApp.GetSystemVariable("DBMOD");
            return dbmod != null && Convert.ToInt32(dbmod, CultureInfo.InvariantCulture) != 0;
        }

        /// <summary>
        /// Percorso su disco del disegno, oppure stringa vuota se non e' mai stato salvato.
        /// </summary>
        /// <param name="document">Disegno da controllare.</param>
        /// <returns>Percorso completo del DWG, o stringa vuota.</returns>
        public static string GetSavedDrawingPath(Document document)
        {
            if (document == null)
            {
                return string.Empty;
            }

            string name = document.Name;

            // Un disegno mai salvato si chiama "Drawing1.dwg" e non esiste su disco:
            // in quel caso non c'e' niente da pubblicare.
            if (string.IsNullOrWhiteSpace(name) || !Path.IsPathRooted(name) || !File.Exists(name))
            {
                return string.Empty;
            }

            return name;
        }

        /// <summary>
        /// Propone un nome di file di destinazione accanto al disegno
        /// (es. Progetto.dwg -> Progetto.pdf).
        /// </summary>
        /// <param name="document">Disegno di partenza.</param>
        /// <param name="extension">Estensione desiderata, punto incluso.</param>
        /// <returns>Percorso proposto, o stringa vuota se il disegno non e' salvato.</returns>
        public static string SuggestOutputPath(Document document, string extension)
        {
            string drawingPath = GetSavedDrawingPath(document);
            if (string.IsNullOrEmpty(drawingPath))
            {
                return string.Empty;
            }

            return Path.ChangeExtension(drawingPath, extension);
        }

        private static string WriteTemporaryDsd(string content)
        {
            string folder = Path.Combine(Path.GetTempPath(), TempFolderName);
            Directory.CreateDirectory(folder);

            string fileName = string.Format(
                CultureInfo.InvariantCulture,
                "publish_{0:yyyyMMdd_HHmmss}_{1}.dsd",
                DateTime.Now,
                Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture).Substring(0, 6));

            string path = Path.Combine(folder, fileName);

            // Il DSD e' un file INI che AutoCAD legge nella codifica di sistema:
            // usare UTF-8 romperebbe i nomi di layout con lettere accentate.
            File.WriteAllText(path, content, Encoding.Default);
            return path;
        }
    }
}
