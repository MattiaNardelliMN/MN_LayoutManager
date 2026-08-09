using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using MN_LayoutManager.Core;
using MN_LayoutManager.Infrastructure;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace MN_LayoutManager.Services
{
    /// <summary>
    /// Stampa e pubblicazione di piu' layout in un colpo solo, un file per layout.
    /// Prepara un file DSD temporaneo (l'elenco dei fogli) e lo affida a
    /// <see cref="AcadPublisher"/>, che chiama l'API di pubblicazione di AutoCAD.
    /// </summary>
    /// <remarks>
    /// Qui si prepara e si controlla; la chiamata ad AutoCAD sta tutta in
    /// <see cref="AcadPublisher"/>, cosi' la parte rischiosa resta confinata in un
    /// modulo solo.
    /// </remarks>
    public static class PublishService
    {
        private const string TempFolderName = "MN_LayoutManager";
        private const string OperationName = "Stampa/Pubblica";

        /// <summary>
        /// Prepara ed avvia la stampa/pubblicazione dei layout indicati.
        /// La pubblicazione parte in background: AutoCAD resta subito utilizzabile.
        /// </summary>
        /// <param name="document">Disegno da cui pubblicare.</param>
        /// <param name="layoutNames">Layout scelti, nell'ordine voluto.</param>
        /// <param name="outputKind">Stampa sul plotter delle impostazioni di pagina, o file PDF/DWF.</param>
        /// <param name="outputFolder">Cartella in cui devono finire i file prodotti.</param>
        /// <param name="error">Messaggio in italiano se non si e' potuto procedere.</param>
        /// <returns>true se la pubblicazione e' stata avviata.</returns>
        public static bool TryPublish(
            Document document,
            IReadOnlyList<string> layoutNames,
            PublishOutputKind outputKind,
            string outputFolder,
            out string error)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var request = new PublishRequest(
                GetSavedDrawingPath(document),
                layoutNames,
                outputKind,
                outputFolder);

            if (!DsdFileBuilder.TryBuild(request, out string dsdContent, out error))
            {
                return false;
            }

            if (!TryPrepareOutputFolder(request.OutputFolder, out error))
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
                PluginLog.Error(OperationName, error, ex);
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                error = "Non ho i permessi per creare il file temporaneo per la stampa.";
                PluginLog.Error(OperationName, error, ex);
                return false;
            }

            PluginLog.Info(
                OperationName,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} layout -> {1} in '{2}', elenco fogli in {3}",
                    request.LayoutNames.Count,
                    outputKind,
                    request.OutputFolder,
                    dsdPath));

            AcadPublisher.Publish(document, dsdPath, outputKind);
            error = null;
            return true;
        }

        /// <summary>
        /// Controlla che la cartella di destinazione sia utilizzabile, creandola se non esiste.
        /// </summary>
        /// <param name="folder">Cartella scelta dall'utente.</param>
        /// <param name="error">Messaggio in italiano se non e' utilizzabile.</param>
        /// <returns>true se si puo' scriverci dentro.</returns>
        public static bool TryPrepareOutputFolder(string folder, out string error)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                error = "Indica la cartella di destinazione delle stampe.";
                return false;
            }

            try
            {
                if (!Path.IsPathRooted(folder))
                {
                    error = "La cartella di destinazione deve essere un percorso completo, per esempio C:\\Stampe.";
                    return false;
                }

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                    PluginLog.Info(OperationName, "Creata la cartella di destinazione: " + folder);
                }

                error = null;
                return true;
            }
            catch (ArgumentException)
            {
                error = "Il percorso della cartella di destinazione non e' valido.";
                return false;
            }
            catch (NotSupportedException)
            {
                error = "Il percorso della cartella di destinazione non e' valido.";
                return false;
            }
            catch (PathTooLongException)
            {
                error = "Il percorso della cartella di destinazione e' troppo lungo.";
                return false;
            }
            catch (IOException ex)
            {
                error = "Non riesco ad usare la cartella di destinazione: " + ex.Message;
                PluginLog.Error(OperationName, error, ex);
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                error = "Non hai i permessi per scrivere nella cartella di destinazione.";
                return false;
            }
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
        /// Cartella proposta come destinazione: quella del disegno aperto.
        /// </summary>
        /// <param name="document">Disegno di partenza.</param>
        /// <returns>Percorso della cartella, o stringa vuota se il disegno non e' salvato.</returns>
        public static string SuggestOutputFolder(Document document)
        {
            string drawingPath = GetSavedDrawingPath(document);
            return string.IsNullOrEmpty(drawingPath) ? string.Empty : Path.GetDirectoryName(drawingPath);
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
            // NON usare Encoding.Default: su .NET 8/10 (AutoCAD 2025+) vuol dire UTF-8,
            // cioe' esattamente il contrario di quello che serve qui.
            File.WriteAllText(path, content, SystemEncoding.Ansi);
            return path;
        }
    }
}
