using System.Globalization;
using System.IO;
using System.Text;
using MN_LayoutManager.Core;
using Xunit;

namespace MN_LayoutManager.Core.Tests
{
    /// <summary>
    /// Verifica la codifica usata per scrivere i file DSD che AutoCAD legge.
    /// </summary>
    /// <remarks>
    /// In parole semplici: questi test controllano che un nome di layout con lettere
    /// accentate (per esempio "Tavola Città") arrivi ad AutoCAD scritto esattamente
    /// come lo ha scritto l'utente, e che il file non cominci con caratteri invisibili
    /// che manderebbero in confusione AutoCAD.
    ///
    /// Sono importanti perche' vengono eseguiti su tutti e tre i motori .NET usati da
    /// AutoCAD (4.8 per il 2024, 8 per il 2025/2026, 10 per il 2027): e' proprio qui che
    /// i tre si comportavano in modo diverso.
    /// </remarks>
    public class SystemEncodingTests
    {
        [Fact]
        public void LaCodificaEsisteSempre()
        {
            Assert.NotNull(SystemEncoding.Ansi);
        }

        [Fact]
        public void UsaLaCodificaAnsiDiWindows_NonUtf8PerScelta()
        {
            // E' il cuore della correzione: su .NET 8/10 il valore predefinito del
            // linguaggio (Encoding.Default) e' UTF-8, che NON e' quello che AutoCAD
            // si aspetta. Qui si pretende la codepage ANSI vera del sistema.
            int codePageDiWindows = CultureInfo.CurrentCulture.TextInfo.ANSICodePage;
            Assert.Equal(codePageDiWindows, SystemEncoding.Ansi.CodePage);
        }

        [Fact]
        public void NonScriveIlBomInTestaAlFile()
        {
            // Il BOM sono 3 byte invisibili all'inizio del file. Il DSD e' un file INI e
            // la sua prima riga deve iniziare con '[': col BOM davanti AutoCAD non
            // riconoscerebbe la prima sezione e la pubblicazione fallirebbe.
            Assert.Empty(SystemEncoding.Ansi.GetPreamble());
        }

        [Fact]
        public void LeLettereAccentateSopravvivonoAlGiroDiScritturaELettura()
        {
            const string nomeConAccenti = "Tavola Città - Perù - è À";

            byte[] scritti = SystemEncoding.Ansi.GetBytes(nomeConAccenti);
            string riletto = SystemEncoding.Ansi.GetString(scritti);

            Assert.Equal(nomeConAccenti, riletto);
        }

        [Fact]
        public void UnFileScrittoCosiRicominciaDavveroConLaParentesiQuadra()
        {
            // Prova sul campo: si scrive un finto DSD su disco e si controlla che il
            // PRIMO byte del file sia proprio '[' e non un carattere invisibile.
            string percorso = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".dsd");

            try
            {
                File.WriteAllText(percorso, "[DWF6Version]\nTavola Città\n", SystemEncoding.Ansi);

                byte[] contenuto = File.ReadAllBytes(percorso);

                Assert.NotEmpty(contenuto);
                Assert.Equal((byte)'[', contenuto[0]);
            }
            finally
            {
                if (File.Exists(percorso))
                {
                    File.Delete(percorso);
                }
            }
        }

        [Fact]
        public void ChiamateRipetuteRestituisconoSempreLaStessaCodifica()
        {
            // La codifica viene calcolata una volta sola e riusata: se cambiasse fra una
            // pubblicazione e l'altra, due file identici uscirebbero diversi.
            Assert.Same(SystemEncoding.Ansi, SystemEncoding.Ansi);
        }

        [Fact]
        public void NonERimastaUnaUtf8ConBom()
        {
            // Test "di guardia": se qualcuno un domani rimettesse Encoding.UTF8 al posto
            // di questa codifica, il file tornerebbe a partire col BOM. Qui si controlla
            // esplicitamente che non sia quel caso.
            Assert.NotEqual(Encoding.UTF8.GetPreamble().Length, SystemEncoding.Ansi.GetPreamble().Length);
        }
    }
}
