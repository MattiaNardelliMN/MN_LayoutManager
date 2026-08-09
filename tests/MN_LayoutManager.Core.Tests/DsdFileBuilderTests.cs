using System;
using System.Linq;
using MN_LayoutManager.Core;
using Xunit;

namespace MN_LayoutManager.Core.Tests
{
    /// <summary>
    /// Verifica la generazione del file DSD, cioe' l'elenco di fogli che la pubblicazione
    /// di AutoCAD legge per stampare/pubblicare piu' layout in un colpo solo.
    /// In parole semplici: controlla che nel file finiscano tutti e soli i layout scelti,
    /// che sia chiesto UN FILE PER OGNI LAYOUT nella cartella indicata, e che il plugin si
    /// fermi con un messaggio chiaro quando manca qualcosa.
    /// </summary>
    public class DsdFileBuilderTests
    {
        private static PublishRequest Richiesta(
            string dwg = @"C:\Disegni\Progetto.dwg",
            string[] layouts = null,
            PublishOutputKind kind = PublishOutputKind.Pdf,
            string folder = @"C:\Stampe")
        {
            return new PublishRequest(dwg, layouts ?? new[] { "Tavola1", "Tavola2" }, kind, folder);
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

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CartellaDiDestinazioneMancante_VieneSegnalata(string folder)
        {
            Assert.False(DsdFileBuilder.TryBuild(Richiesta(folder: folder), out _, out string error));
            Assert.Contains("cartella", error, StringComparison.OrdinalIgnoreCase);
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
        public void VieneChiestoUnFileSeparatoPerOgniLayout()
        {
            Assert.True(DsdFileBuilder.TryBuild(Richiesta(), out string dsd, out _));

            // MULTISHEET=0 significa "un file per foglio"; con 1 sarebbe un unico PDF
            // multipagina, che non e' quello che vogliamo.
            Assert.Contains("MULTISHEET=0", dsd, StringComparison.Ordinal);
        }

        [Fact]
        public void IlNomeDelFileProdottoEIlNomeDelLayout()
        {
            // Il titolo del foglio nel DSD diventa il nome del file su disco.
            Assert.True(DsdFileBuilder.TryBuild(Richiesta(layouts: new[] { "Tavola 01" }), out string dsd, out _));

            Assert.Contains("[DWF6Sheet:Tavola 01]", dsd, StringComparison.Ordinal);
            Assert.Equal("Tavola 01.pdf", DsdFileBuilder.GetOutputFileName(Richiesta(), "Tavola 01"));
        }

        [Fact]
        public void CaratteriVietatiNeiNomiDiFileVengonoSostituiti()
        {
            // Un layout puo' chiamarsi in modi che Windows non accetta come nome di file:
            // senza questa pulizia la stampa fallirebbe senza spiegazioni.
            Assert.Equal("Tavola-01.pdf", DsdFileBuilder.GetOutputFileName(Richiesta(), "Tavola/01"));
            Assert.Equal("Sez-A-A.pdf", DsdFileBuilder.GetOutputFileName(Richiesta(), "Sez:A\\A"));
        }

        [Fact]
        public void LaCartellaDiDestinazioneFinisceNelDsd()
        {
            Assert.True(DsdFileBuilder.TryBuild(Richiesta(folder: @"C:\Stampe"), out string dsd, out _));

            Assert.Contains(@"OUT=C:\Stampe\", dsd, StringComparison.Ordinal);
            Assert.Contains(@"DWF=C:\Stampe\", dsd, StringComparison.Ordinal);
        }

        [Fact]
        public void UnaCartellaCheFinisceGiaConLaBarraNonNeRicevePiuDiUna()
        {
            Assert.True(DsdFileBuilder.TryBuild(Richiesta(folder: @"C:\Stampe\"), out string dsd, out _));

            Assert.Contains(@"OUT=C:\Stampe\", dsd, StringComparison.Ordinal);
            Assert.DoesNotContain(@"OUT=C:\Stampe\\", dsd, StringComparison.Ordinal);
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

            Assert.Contains("[DWF6Sheet:Tavola1]", dsd, StringComparison.Ordinal);
            Assert.Contains("[DWF6Sheet:Tavola1 (2)]", dsd, StringComparison.Ordinal);
        }

        // I numeri attesi sono quelli dell'enumerazione SheetType di AutoCAD
        // (AcCoreMgd.dll), letta per riflessione e identica su AutoCAD 2024, 2026 e 2027.
        // Attenzione: le varianti "Single" producono un file per foglio, le "Multi" un
        // unico documento multipagina. Il plugin promette un file per layout, quindi qui
        // deve comparire sempre la variante "Single".
        [Theory]
        [InlineData(PublishOutputKind.PageSetupPlotter, "Type=2")] // OriginalDevice
        [InlineData(PublishOutputKind.Dwf, "Type=0")]              // SingleDwf
        [InlineData(PublishOutputKind.Dwfx, "Type=3")]             // SingleDwfx
        [InlineData(PublishOutputKind.Pdf, "Type=5")]              // SinglePdf
        public void IlTipoDiUscitaFinisceNelCampoType(PublishOutputKind kind, string atteso)
        {
            Assert.True(DsdFileBuilder.TryBuild(Richiesta(kind: kind), out string dsd, out _));

            Assert.Contains(atteso, dsd, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(PublishOutputKind.Pdf)]
        [InlineData(PublishOutputKind.Dwf)]
        [InlineData(PublishOutputKind.Dwfx)]
        [InlineData(PublishOutputKind.PageSetupPlotter)]
        public void IlDsdNonChiedeMaiUnFileMultipagina(PublishOutputKind kind)
        {
            // Il difetto che ha reso la pubblicazione inutile per mesi: il campo Type
            // chiedeva un documento multipagina (o addirittura il formato sbagliato)
            // mentre MULTISHEET=0 ne chiedeva uno per foglio. Le due richieste in
            // contrasto facevano fallire il lavoro senza produrre nessun file.
            int type = PublishSheetType.ForOneFilePerLayout(kind);

            Assert.DoesNotContain(
                type,
                new[] { PublishSheetType.MultiDwf, PublishSheetType.MultiDwfx, PublishSheetType.MultiPdf });

            Assert.True(DsdFileBuilder.TryBuild(Richiesta(kind: kind), out string dsd, out _));
            Assert.Contains("MULTISHEET=0", dsd, StringComparison.Ordinal);
        }

        [Fact]
        public void OgniFormatoHaUnNumeroDiverso()
        {
            // Due formati che finissero sullo stesso numero vorrebbero dire che uno dei
            // due produce il file sbagliato: e' esattamente l'errore che c'era prima,
            // dove "stampa su plotter" e "DWF" si sovrapponevano.
            int[] numeri =
            {
                PublishSheetType.ForOneFilePerLayout(PublishOutputKind.PageSetupPlotter),
                PublishSheetType.ForOneFilePerLayout(PublishOutputKind.Pdf),
                PublishSheetType.ForOneFilePerLayout(PublishOutputKind.Dwf),
                PublishSheetType.ForOneFilePerLayout(PublishOutputKind.Dwfx),
            };

            Assert.Equal(numeri.Length, numeri.Distinct().Count());
        }

        [Theory]
        [InlineData(PublishOutputKind.Pdf, ".pdf")]
        [InlineData(PublishOutputKind.Dwf, ".dwf")]
        [InlineData(PublishOutputKind.Dwfx, ".dwfx")]
        public void LEstensioneDeiFileSegueIlFormatoScelto(PublishOutputKind kind, string attesa)
        {
            Assert.Equal(attesa, Richiesta(kind: kind).OutputExtension);
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
            Assert.Throws<ArgumentNullException>(() => DsdFileBuilder.GetOutputFileName(null, "Tavola1"));
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
