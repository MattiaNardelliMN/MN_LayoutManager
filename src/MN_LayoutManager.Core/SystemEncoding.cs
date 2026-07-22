using System;
using System.Globalization;
using System.Text;

namespace MN_LayoutManager.Core
{
    /// <summary>
    /// Fornisce la codifica di testo "ANSI di sistema", quella che i comandi nativi di
    /// AutoCAD si aspettano nei file di supporto come il DSD.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Esiste per un motivo preciso. Su .NET Framework 4.8 (AutoCAD 2024)
    /// <c>Encoding.Default</c> significa "la codifica ANSI di Windows" (in Italia
    /// Windows-1252). Su .NET 8 e 10 (AutoCAD 2025 e successivi) la stessa proprieta'
    /// e' stata cambiata e significa UTF-8: lo stesso codice, senza dare nessun errore,
    /// scriverebbe file in una codifica diversa e i nomi di layout con lettere accentate
    /// ("Tavola Città") arriverebbero storpiati ad AutoCAD.
    /// </para>
    /// <para>
    /// Qui la codifica viene quindi scelta in modo esplicito invece di affidarsi a un
    /// valore predefinito che cambia sotto i piedi.
    /// </para>
    /// </remarks>
    public static class SystemEncoding
    {
        /// <summary>
        /// Codifica di ripiego se quella di sistema non e' disponibile: e' l'ANSI
        /// dell'Europa occidentale, la stessa usata da Windows in italiano.
        /// </summary>
        private const int WesternEuropeanCodePage = 1252;

        /// <summary>Numero della codifica UTF-8.</summary>
        private const int Utf8CodePage = 65001;

        private static readonly object Gate = new object();
        private static Encoding _ansi;

        /// <summary>
        /// Codifica ANSI di sistema, senza BOM.
        /// </summary>
        /// <remarks>
        /// Il BOM (tre byte invisibili in testa al file) va evitato: il DSD e' un file
        /// INI e la sua prima riga deve iniziare esattamente con la parentesi quadra,
        /// altrimenti AutoCAD non riconosce la prima sezione.
        /// </remarks>
        public static Encoding Ansi
        {
            get
            {
                // Calcolata una volta sola e poi tenuta da parte: puo' essere richiesta
                // a ogni pubblicazione e risolverla ogni volta sarebbe inutile.
                if (_ansi != null)
                {
                    return _ansi;
                }

                lock (Gate)
                {
                    if (_ansi == null)
                    {
                        _ansi = ResolveAnsi();
                    }
                }

                return _ansi;
            }
        }

        private static Encoding ResolveAnsi()
        {
#if !NETFRAMEWORK
            // Su .NET moderno le codifiche storiche non ci sono di serie: questa riga le
            // rende disponibili. Chiamarla piu' volte non fa danno.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif

            int codePage = CultureInfo.CurrentCulture.TextInfo.ANSICodePage;

            // Se Windows e' impostato in modalita' "UTF-8 universale" (l'opzione beta del
            // pannello di controllo) la codepage ANSI diventa UTF-8. In quel caso serve
            // comunque una UTF-8 SENZA BOM: quella predefinita di .NET il BOM lo scrive.
            if (codePage == Utf8CodePage)
            {
                return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            }

            return GetEncodingOrFallback(codePage);
        }

        private static Encoding GetEncodingOrFallback(int codePage)
        {
            try
            {
                return Encoding.GetEncoding(codePage);
            }
            catch (ArgumentException)
            {
                // Codepage sconosciuta al sistema.
                return Encoding.GetEncoding(WesternEuropeanCodePage);
            }
            catch (NotSupportedException)
            {
                // Codepage nota ma non installata.
                return Encoding.GetEncoding(WesternEuropeanCodePage);
            }
        }
    }
}
