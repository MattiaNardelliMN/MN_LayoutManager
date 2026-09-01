using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace MN_LayoutManager.Infrastructure
{
    /// <summary>
    /// Registro degli eventi del plugin, scritto su file di testo.
    /// Serve a capire cosa e' successo quando qualcosa non funziona, senza dover
    /// leggere il codice: un file al giorno, una riga per evento.
    /// </summary>
    /// <remarks>
    /// Percorso dei log: %AppData%\MN_LayoutManager\logs\
    /// </remarks>
    public static class PluginLog
    {
        private const string PluginFolderName = "MN_LayoutManager";
        private const int MaxLogFileAgeInDays = 30;

        private static readonly object WriteLock = new object();
        private static bool _cleanupDone;

        /// <summary>Cartella in cui vengono scritti i file di log.</summary>
        public static string LogDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            PluginFolderName,
            "logs");

        /// <summary>Registra un'operazione andata a buon fine o un'informazione utile.</summary>
        /// <param name="operation">Cosa stava facendo il plugin (es. "Rinomina layout").</param>
        /// <param name="message">Descrizione in italiano.</param>
        public static void Info(string operation, string message) => Write("INFO", operation, message, null);

        /// <summary>Registra una situazione anomala ma non bloccante.</summary>
        /// <param name="operation">Cosa stava facendo il plugin.</param>
        /// <param name="message">Descrizione in italiano.</param>
        public static void Warn(string operation, string message) => Write("WARN", operation, message, null);

        /// <summary>
        /// Registra un errore che il plugin ha riconosciuto da solo, senza che ci sia
        /// un'eccezione tecnica dietro (per esempio: manca una stampante utilizzabile).
        /// </summary>
        /// <param name="operation">Cosa stava facendo il plugin quando si e' fermato.</param>
        /// <param name="message">Spiegazione in italiano di cosa non ha funzionato.</param>
        public static void Error(string operation, string message) =>
            Write("ERROR", operation, message, null);

        /// <summary>Registra un errore, con i dettagli tecnici in coda.</summary>
        /// <param name="operation">Cosa stava facendo il plugin quando e' fallito.</param>
        /// <param name="message">Spiegazione in italiano di cosa non ha funzionato.</param>
        /// <param name="exception">Errore tecnico, se disponibile.</param>
        public static void Error(string operation, string message, Exception exception) =>
            Write("ERROR", operation, message, exception);

        /// <summary>Percorso del file di log di oggi.</summary>
        /// <returns>Percorso completo del file.</returns>
        public static string GetTodayLogFilePath()
        {
            string fileName = string.Format(
                CultureInfo.InvariantCulture,
                "MN_LayoutManager_{0:yyyy-MM-dd}.log",
                DateTime.Now);

            return Path.Combine(LogDirectory, fileName);
        }

        private static void Write(string level, string operation, string message, Exception exception)
        {
            // Il logger non deve MAI far fallire il plugin: se scrivere il log non
            // riesce (disco pieno, permessi, cartella bloccata) si prosegue in silenzio.
            // E' l'unico punto del progetto in cui ignorare un errore e' voluto.
            try
            {
                var line = new StringBuilder();
                line.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "[{0:yyyy-MM-dd HH:mm:ss}] [{1}] [{2}] {3}",
                    DateTime.Now,
                    level,
                    operation ?? "?",
                    message ?? string.Empty);

                if (exception != null)
                {
                    line.AppendLine();
                    line.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "    Dettaglio tecnico: {0}: {1}",
                        exception.GetType().Name,
                        exception.Message);
                    line.AppendLine();
                    line.Append("    ").Append(exception.StackTrace);
                }

                lock (WriteLock)
                {
                    Directory.CreateDirectory(LogDirectory);
                    RemoveOldLogFiles();
                    File.AppendAllText(GetTodayLogFilePath(), line.ToString() + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (IOException)
            {
                // Vedi commento sopra: il log non e' critico.
            }
            catch (UnauthorizedAccessException)
            {
                // Vedi commento sopra: il log non e' critico.
            }
        }

        /// <summary>Cancella i log piu' vecchi di un mese, una volta per sessione.</summary>
        private static void RemoveOldLogFiles()
        {
            if (_cleanupDone)
            {
                return;
            }

            _cleanupDone = true;

            try
            {
                DateTime limit = DateTime.Now.AddDays(-MaxLogFileAgeInDays);
                foreach (string file in Directory.GetFiles(LogDirectory, "MN_LayoutManager_*.log"))
                {
                    if (File.GetLastWriteTime(file) < limit)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (IOException)
            {
                // Se la pulizia non riesce non e' un problema: i log restano, nient'altro.
            }
            catch (UnauthorizedAccessException)
            {
                // Come sopra.
            }
        }
    }
}
