using System;
using System.Collections.Generic;
using MN_LayoutManager.Core;
using Xunit;

namespace MN_LayoutManager.Core.Tests
{
    /// <summary>
    /// Verifica l'elenco delle stampanti che il plugin propone ad AutoCAD.
    /// </summary>
    /// <remarks>
    /// In parole semplici: prima di pubblicare, il plugin deve dire ad AutoCAD SU QUALE
    /// stampante lavorare. Se non gliela dice, AutoCAD non produce niente - era la causa
    /// per cui i PDF non uscivano.
    ///
    /// Attenzione a cosa questi test possono e non possono garantire. NON possono
    /// verificare che "DWG To PDF.pc3" esista davvero sul computer: quello dipende da
    /// AutoCAD ed e' stato controllato a mano nella cartella Plotters. Quello che
    /// verificano e' che la REGOLA di scelta sia sana: che nessun formato resti scoperto,
    /// che i nomi abbiano la forma di un file di stampante, che non ci siano doppioni.
    /// </remarks>
    public class PlotDeviceNamesTests
    {
        /// <summary>
        /// Il test piu' importante del gruppo: se un domani si aggiunge un formato nuovo
        /// (per esempio PNG) e ci si dimentica di dire quale stampante usare, questo test
        /// diventa rosso invece di lasciare che la pubblicazione fallisca in silenzio
        /// dentro AutoCAD.
        /// </summary>
        [Fact]
        public void OgniFormatoPrevistoHaUnaRisposta_NessunoRestaScoperto()
        {
            foreach (PublishOutputKind kind in Enum.GetValues(typeof(PublishOutputKind)))
            {
                IReadOnlyList<string> devices = PlotDeviceNames.ForOutputKind(kind);
                Assert.NotNull(devices);
            }
        }

        [Fact]
        public void IFormatiSuFileIndicanoAlmenoUnaStampante()
        {
            // PDF, DWF e DWFx sono file: senza una stampante indicata non esce niente.
            Assert.NotEmpty(PlotDeviceNames.ForOutputKind(PublishOutputKind.Pdf));
            Assert.NotEmpty(PlotDeviceNames.ForOutputKind(PublishOutputKind.Dwf));
            Assert.NotEmpty(PlotDeviceNames.ForOutputKind(PublishOutputKind.Dwfx));
        }

        [Fact]
        public void LaStampaSuPlotterNonImponeNessunaStampante()
        {
            // "Stampa" deve mandare ogni foglio al SUO dispositivo, quello scritto nelle
            // sue impostazioni di pagina. Se qui comparisse un nome, il plugin
            // imporrebbe la stessa stampante a tutti i fogli.
            Assert.Empty(PlotDeviceNames.ForOutputKind(PublishOutputKind.PageSetupPlotter));
        }

        [Fact]
        public void TuttiINomiHannoLaFormaDiUnaStampanteDiAutoCad()
        {
            foreach (PublishOutputKind kind in Enum.GetValues(typeof(PublishOutputKind)))
            {
                foreach (string name in PlotDeviceNames.ForOutputKind(kind))
                {
                    Assert.False(string.IsNullOrWhiteSpace(name));

                    // Un nome senza ".pc3" non corrisponderebbe a nessun file di
                    // AutoCAD: la stampante non verrebbe mai trovata.
                    Assert.EndsWith(".pc3", name, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        [Fact]
        public void NessunNomeRipetutoNelloStessoElenco()
        {
            // Un doppione farebbe provare due volte la stessa stampante gia' fallita.
            foreach (PublishOutputKind kind in Enum.GetValues(typeof(PublishOutputKind)))
            {
                IReadOnlyList<string> devices = PlotDeviceNames.ForOutputKind(kind);
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (string name in devices)
                {
                    Assert.True(seen.Add(name), "Nome ripetuto: " + name);
                }
            }
        }

        [Fact]
        public void LaStampanteDiRipiegoEUnaStampanteValida()
        {
            Assert.False(string.IsNullOrWhiteSpace(PlotDeviceNames.DefaultWindowsPrinter));
            Assert.EndsWith(".pc3", PlotDeviceNames.DefaultWindowsPrinter, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void UnFormatoInesistenteVieneRifiutato_NonIgnorato()
        {
            // Un valore fuori elenco deve far rumore subito, non scivolare avanti e far
            // fallire la pubblicazione piu' tardi dentro AutoCAD.
            Assert.Throws<ArgumentOutOfRangeException>(
                () => PlotDeviceNames.ForOutputKind((PublishOutputKind)999));
        }

        [Theory]
        [InlineData("DWG To PDF.pc3", true)]
        [InlineData("Il mio plotter.pc3", true)]
        [InlineData("None", false)]
        [InlineData("none", false)]
        [InlineData("  None  ", false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData(null, false)]
        public void RiconosceQuandoIlLayoutNonHaNessunaStampante(string deviceName, bool atteso)
        {
            // AutoCAD scrive "None" nelle impostazioni di pagina quando non e' stato
            // scelto nessun dispositivo: e' un segnaposto, non il nome di una stampante.
            // Passarlo ad AutoCAD come se fosse un nome vero farebbe fallire la stampa.
            Assert.Equal(atteso, PlotDeviceNames.IsRealDevice(deviceName));
        }
    }
}
