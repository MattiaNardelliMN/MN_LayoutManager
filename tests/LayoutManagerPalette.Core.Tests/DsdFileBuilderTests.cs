using System;
using System.Linq;
using LayoutManagerPalette.Core;
using Xunit;

namespace LayoutManagerPalette.Core.Tests
{
    /// <summary>
    /// Verifica la generazione del file DSD, cioe' l'elenco di fogli che il comando
    /// nativo -PUBLISH di AutoCAD legge per stampare/pubblicare piu' layout in un colpo solo.
    /// In parole semplici: controlla che nel file finiscano tutti e soli i layout scelti,
    /// e che il plugin si fermi con un messaggio chiaro quando manca qualcosa.
    /// </summary>
    public class DsdFileBuilderTests
    {
        private static PublishRequest Richiesta(
            string dwg = @"C:\Disegni\Progetto.dwg",
            string[] layouts = null,
            PublishOutputKind kind = PublishOutputKind.Pdf,
            string output = @"C:\Disegni\Progetto.pdf",
            bool multiSheet = true)
        {
            return new PublishRequest(dwg, layouts ?? new[] { "Tavola1", "Tavola2" }, kind, output, multiSheet);
        }

        [Fact]
        public void DisegnoNonSalvato_VieneSegnalatoConUnMessaggio()
        {
            Assert.False(DsdFileBuilder.TryBuild(Richiesta(dwg: string.Empty), out _, out string error));
            Assert.Contains("salvato", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void NessunLayoutSelezionato_VieneSegnalatoConUnMessaggio()
        {
            Assert.False(DsdFileBuilder.TryBuild(Richiesta(layouts: Array.Empty<string>()), out _, out string error));
            Assert.Contains("Nessun layout", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FileDiDestinazioneMancante_VieneSegnalatoSoloQuandoServe()
        {
            Assert.False(DsdFileBuilder.TryBuild(Richiesta(output: null), out _, out string error));
            Assert.Contains("destinazione", error, StringComparison.OrdinalIgnoreCase);

            // Stampando sul plotter delle impostazioni di pagina non serve nessun file.
            Assert.True(DsdFileBuilder.TryBuild(
                Richiesta(kind: PublishOutputKind.PageSetupPlotter, output: null),
                out _,
                out _));
        }

        [Fact]
        public void OgniLayoutSceltoDiventaUnFoglioDelDsd()
        {
            Assert.True(DsdFileBuilder.TryBuild(Richiesta(), out string dsd, out _));

            Assert.Contains("Layout=Tavola1", dsd, StringComparison.Ordinal);
            Assert.Contains("Layout=Tavola2", dsd, StringComparison.Ordinal);
            Assert.Equal(2, ContaOccorrenze(dsd, "[DWF6Sheet:"));
        }

        [Fact]
        public void IlPercorsoDelDisegnoFinisceInOgniFoglio()
        {
            Assert.True(DsdFileBuilder.TryBuild(Richiesta(), out string dsd, out _));

            Assert.Equal(2, ContaOccorrenze(dsd, @"DWG=C:\Disegni\Progetto.dwg"));
            Assert.Equal(2, ContaOccorrenze(dsd, @"OriginalSheetPath=C:\Disegni\Progetto.dwg"));
        }

        [Fact]
        public void IlTitoloDeiFogliEUnivocoAncheConLayoutRipetuti()
        {
            Assert.True(DsdFileBuilder.TryBuild(
                Richiesta(layouts: new[] { "Tavola1", "Tavola1" }),
                out string dsd,
                out _));

            Assert.Contains("[DWF6Sheet:Progetto-Tavola1]", dsd, StringComparison.Ordinal);
            Assert.Contains("[DWF6Sheet:Progetto-Tavola1 (2)]", dsd, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(PublishOutputKind.PageSetupPlotter, "Type=0")]
        [InlineData(PublishOutputKind.Dwf, "Type=1")]
        [InlineData(PublishOutputKind.Dwfx, "Type=2")]
        [InlineData(PublishOutputKind.Pdf, "Type=6")]
        public void IlTipoDiUscitaFinisceNelCampoType(PublishOutputKind kind, string atteso)
        {
            Assert.True(DsdFileBuilder.TryBuild(
                Richiesta(kind: kind, output: @"C:\out.pdf"),
                out string dsd,
                out _));

            Assert.Contains(atteso, dsd, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(true, "MULTISHEET=1")]
        [InlineData(false, "MULTISHEET=0")]
        public void UnSoloFileOppureUnoPerLayout(bool multiSheet, string atteso)
        {
            Assert.True(DsdFileBuilder.TryBuild(Richiesta(multiSheet: multiSheet), out string dsd, out _));

            Assert.Contains(atteso, dsd, StringComparison.Ordinal);
        }

        [Fact]
        public void IlDsdNonChiedeConfermeAllUtente()
        {
            // PROMPT=FALSE evita che AutoCAD apra finestre durante la pubblicazione:
            // senza questo la stampa batch si bloccherebbe in attesa di un clic.
            Assert.True(DsdFileBuilder.TryBuild(Richiesta(), out string dsd, out _));

            Assert.Contains("PROMPT=FALSE", dsd, StringComparison.Ordinal);
            Assert.Contains("PromptForDwfName=FALSE", dsd, StringComparison.Ordinal);
        }

        [Fact]
        public void RichiestaNulla_SollevaEccezioneChiara()
        {
            Assert.Throws<ArgumentNullException>(() => DsdFileBuilder.TryBuild(null, out _, out _));
        }

        private static int ContaOccorrenze(string testo, string cercato)
        {
            int conteggio = 0;
            int posizione = 0;
            while ((posizione = testo.IndexOf(cercato, posizione, StringComparison.Ordinal)) >= 0)
            {
                conteggio++;
                posizione += cercato.Length;
            }

            return conteggio;
        }
    }
}
