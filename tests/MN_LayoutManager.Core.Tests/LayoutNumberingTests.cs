using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MN_LayoutManager.Core.Tests
{
    /// <summary>
    /// Verifica il riconoscimento delle progressioni numeriche nei nomi dei layout.
    /// In parole semplici: controlla che da "D_T_01" il plugin capisca che il prossimo
    /// si chiama "D_T_02", che non ripeta nomi gia' usati e che non si inventi numeri
    /// quando nel nome non ce n'e' nessuno.
    /// </summary>
    public class LayoutNumberingTests
    {
        // ==================== Lettura del nome ====================

        // Controlla che "D_T_01" venga letto come: testo "D_T_", numero 1, scritto con 2 cifre.
        [Fact]
        public void UnNomeConNumero_VieneScompostoCorrettamente()
        {
            Assert.True(LayoutNumbering.TryParse("D_T_01", out NumberedLayoutName parsed));

            Assert.Equal("D_T_", parsed.Prefix);
            Assert.Equal(1, parsed.Number);
            Assert.Equal(2, parsed.Digits);
            Assert.Equal(string.Empty, parsed.Suffix);
        }

        // Un nome senza cifre non e' una serie: il plugin non deve inventarsi niente.
        [Theory]
        [InlineData("Pianta")]
        [InlineData("Dettagli costruttivi")]
        [InlineData("")]
        [InlineData(null)]
        public void UnNomeSenzaNumero_NonSiScompone(string name)
        {
            Assert.False(LayoutNumbering.TryParse(name, out NumberedLayoutName parsed));
            Assert.Null(parsed);
        }

        // Se nel nome ci sono piu' numeri vale l'ULTIMO: e' quello che di solito
        // rappresenta il progressivo del foglio.
        [Fact]
        public void ConPiuNumeri_ValeLUltimo()
        {
            Assert.True(LayoutNumbering.TryParse("Lotto2_Tavola_07", out NumberedLayoutName parsed));

            Assert.Equal("Lotto2_Tavola_", parsed.Prefix);
            Assert.Equal(7, parsed.Number);
        }

        // Il testo dopo il numero viene conservato: "Tav_03_rev" resta "..._rev".
        [Fact]
        public void IlTestoDopoIlNumeroVieneConservato()
        {
            Assert.True(LayoutNumbering.TryParse("Tav_03_rev", out NumberedLayoutName parsed));

            Assert.Equal("Tav_", parsed.Prefix);
            Assert.Equal(3, parsed.Number);
            Assert.Equal("_rev", parsed.Suffix);
        }

        // Un numero enorme non e' un progressivo ma un codice: meglio non toccarlo.
        [Fact]
        public void UnNumeroTroppoGrande_NonVieneConsideratoUnProgressivo()
        {
            Assert.False(LayoutNumbering.TryParse("Codice_99999999999999999999", out _));
        }

        // Gli zeri davanti non si perdono, ma un numero grande non viene mai troncato.
        [Fact]
        public void RicostruendoIlNome_GliZeriDavantiRestano()
        {
            LayoutNumbering.TryParse("D_T_01", out NumberedLayoutName parsed);

            Assert.Equal("D_T_07", parsed.Format(7));
            Assert.Equal("D_T_123", parsed.Format(123));
        }

        // ==================== Nomi delle copie ====================

        // Il caso che conta: copiando "D_T_01" tre volte devono uscire 02, 03, 04.
        [Fact]
        public void CopiandoUnLayoutDiUnaSerie_LaNumerazioneProsegue()
        {
            var existing = new[] { "D_T_01" };

            IReadOnlyList<string> names = LayoutNumbering.BuildCopyNames(existing, "D_T_01", 3);

            Assert.Equal(new[] { "D_T_02", "D_T_03", "D_T_04" }, names);
        }

        // Si riparte sempre dal numero piu' alto gia' esistente, non da quello copiato:
        // copiando "D_T_01" quando c'e' gia' "D_T_05" il prossimo libero e' "D_T_06".
        [Fact]
        public void SiRipartAlDalNumeroPiuAltoDellaSerie()
        {
            var existing = new[] { "D_T_01", "D_T_05" };

            string name = LayoutNumbering.BuildCopyName(existing, "D_T_01");

            Assert.Equal("D_T_06", name);
        }

        // Le cifre si adeguano quando il numero cresce: dopo "D_T_09" viene "D_T_10".
        [Fact]
        public void QuandoIlNumeroCresce_LeCifreSiAdeguano()
        {
            Assert.Equal("D_T_10", LayoutNumbering.BuildCopyName(new[] { "D_T_09" }, "D_T_09"));
            Assert.Equal("D_T_100", LayoutNumbering.BuildCopyName(new[] { "D_T_99" }, "D_T_99"));
        }

        // Senza numero nel nome si torna al classico "Nome (2)", "Nome (3)".
        [Fact]
        public void SenzaProgressione_SiUsaIlClassicoNomeFraParentesi()
        {
            var existing = new[] { "Pianta" };

            IReadOnlyList<string> names = LayoutNumbering.BuildCopyNames(existing, "Pianta", 2);

            Assert.Equal(new[] { "Pianta (2)", "Pianta (3)" }, names);
        }

        // Nessun nome proposto deve essere gia' occupato, ne' ripetersi fra le copie:
        // e' la garanzia che AutoCAD non rifiutera' la creazione.
        [Fact]
        public void INomiProposti_SonoTuttiLiberiEDiversiFraLoro()
        {
            var existing = new[] { "T_01", "T_02", "T_04", "Pianta" };

            IReadOnlyList<string> names = LayoutNumbering.BuildCopyNames(existing, "T_01", 5);

            Assert.Equal(5, names.Count);
            Assert.Equal(5, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Empty(names.Intersect(existing, StringComparer.OrdinalIgnoreCase));
        }

        // Le serie si riconoscono ignorando maiuscole e minuscole, come fa AutoCAD.
        [Fact]
        public void LeSerieSiRiconosconoIgnorandoMaiuscoleEMinuscole()
        {
            string name = LayoutNumbering.BuildCopyName(new[] { "d_t_01", "D_T_02" }, "D_T_01");

            Assert.Equal("D_T_03", name);
        }

        // Chiedere zero copie o meno non deve produrre niente ne' dare errore.
        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public void ChiedendoZeroCopie_NonEsceNiente(int count)
        {
            Assert.Empty(LayoutNumbering.BuildCopyNames(new[] { "D_T_01" }, "D_T_01", count));
        }

        // Un nome di partenza vuoto e' un errore di programmazione, non un caso da gestire.
        [Fact]
        public void SenzaNomeDiPartenza_VieneSegnalatoUnErrore()
        {
            Assert.Throws<ArgumentException>(() => LayoutNumbering.BuildCopyNames(new string[0], " ", 1));
            Assert.Throws<ArgumentNullException>(() => LayoutNumbering.BuildCopyNames(null, "A_1", 1));
        }

        // Il nome proposto non deve mai essere un nome che AutoCAD rifiuterebbe.
        [Fact]
        public void INomiPropostiSonoSempreValidiPerAutoCad()
        {
            IReadOnlyList<string> names = LayoutNumbering.BuildCopyNames(new[] { "Tav_01" }, "Tav_01", 10);

            foreach (string name in names)
            {
                Assert.True(LayoutNameValidator.TryValidate(name, out string error), name + ": " + error);
            }
        }

        // ==================== Proposta per il layout nuovo ====================

        // Con una serie riconoscibile, creare un layout nuovo propone il numero successivo.
        [Fact]
        public void ConUnaSerieRiconoscibile_VienePropostoIlNumeroSuccessivo()
        {
            var existing = new[] { "D_T_01", "D_T_02", "Pianta" };

            Assert.True(LayoutNumbering.TryProposeNextInSeries(existing, out string proposed));
            Assert.Equal("D_T_03", proposed);
        }

        // Un solo layout numerato non basta per parlare di serie: potrebbe essere un caso.
        [Fact]
        public void ConUnSoloLayoutNumerato_NonSiProponeNiente()
        {
            Assert.False(LayoutNumbering.TryProposeNextInSeries(new[] { "D_T_01", "Pianta" }, out string proposed));
            Assert.Null(proposed);
        }

        // Senza nessun numero non c'e' niente da proporre.
        [Fact]
        public void SenzaNumeri_NonSiProponeNiente()
        {
            Assert.False(LayoutNumbering.TryProposeNextInSeries(new[] { "Pianta", "Sezione" }, out _));
        }

        // Con due serie diverse vince quella con piu' layout.
        [Fact]
        public void ConDueSerie_VinceQuellaConPiuLayout()
        {
            var existing = new[] { "A_1", "B_1", "B_2", "B_3" };

            Assert.True(LayoutNumbering.TryProposeNextInSeries(existing, out string proposed));
            Assert.Equal("B_4", proposed);
        }

        // A parita' di numerosita' vince la serie che compare piu' in basso nell'elenco:
        // e' quella su cui l'utente stava presumibilmente lavorando.
        [Fact]
        public void APartitaDiNumerosita_VinceLaSeriePiuInBasso()
        {
            var existing = new[] { "A_1", "A_2", "B_1", "B_2" };

            Assert.True(LayoutNumbering.TryProposeNextInSeries(existing, out string proposed));
            Assert.Equal("B_3", proposed);
        }

        // Il nome proposto non deve mai essere gia' occupato, anche se ci sono buchi.
        [Fact]
        public void IlNomePropostoNonEMaiGiaOccupato()
        {
            var existing = new[] { "T_01", "T_03", "T_04" };

            Assert.True(LayoutNumbering.TryProposeNextInSeries(existing, out string proposed));
            Assert.Equal("T_05", proposed);
            Assert.DoesNotContain(proposed, existing);
        }
    }
}
