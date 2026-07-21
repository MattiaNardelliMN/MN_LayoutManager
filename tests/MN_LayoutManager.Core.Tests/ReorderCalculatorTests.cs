using System;
using System.Collections.Generic;
using MN_LayoutManager.Core;
using Xunit;

namespace MN_LayoutManager.Core.Tests
{
    /// <summary>
    /// Verifica il calcolo del riordino dei layout col trascinamento.
    /// In parole semplici: controlla che trascinando un layout (o piu' d'uno) finisca
    /// esattamente dove l'utente ha rilasciato il mouse, senza scombinare gli altri.
    /// </summary>
    public class ReorderCalculatorTests
    {
        private static readonly string[] Quattro = { "A", "B", "C", "D" };

        [Fact]
        public void TrascinareIlPrimoDopoIlSecondo()
        {
            IReadOnlyList<string> risultato = ReorderCalculator.Move(Quattro, new[] { 0 }, 2);

            Assert.Equal(new[] { "B", "A", "C", "D" }, risultato);
        }

        [Fact]
        public void TrascinareLUltimoInSecondaPosizione()
        {
            IReadOnlyList<string> risultato = ReorderCalculator.Move(Quattro, new[] { 3 }, 1);

            Assert.Equal(new[] { "A", "D", "B", "C" }, risultato);
        }

        [Fact]
        public void TrascinareInCimaAllaLista()
        {
            IReadOnlyList<string> risultato = ReorderCalculator.Move(Quattro, new[] { 2 }, 0);

            Assert.Equal(new[] { "C", "A", "B", "D" }, risultato);
        }

        [Fact]
        public void TrascinareInFondoAllaLista()
        {
            IReadOnlyList<string> risultato = ReorderCalculator.Move(Quattro, new[] { 0 }, Quattro.Length);

            Assert.Equal(new[] { "B", "C", "D", "A" }, risultato);
        }

        [Fact]
        public void TrascinareNellaStessaPosizione_NonCambiaNulla()
        {
            IReadOnlyList<string> risultato = ReorderCalculator.Move(Quattro, new[] { 1 }, 1);

            Assert.Equal(Quattro, risultato);
        }

        [Fact]
        public void TrascinarePiuLayoutInsieme_NeMantieneLOrdineFraLoro()
        {
            // Sposto A e C (non adiacenti) in fondo: devono restare nell'ordine A poi C.
            IReadOnlyList<string> risultato = ReorderCalculator.Move(Quattro, new[] { 0, 2 }, 4);

            Assert.Equal(new[] { "B", "D", "A", "C" }, risultato);
        }

        [Fact]
        public void TrascinarePiuLayoutNonOrdinati_FunzionaUgualmente()
        {
            // Gli indici arrivano dalla UI e possono essere in qualsiasi ordine.
            IReadOnlyList<string> risultato = ReorderCalculator.Move(Quattro, new[] { 2, 0 }, 4);

            Assert.Equal(new[] { "B", "D", "A", "C" }, risultato);
        }

        [Fact]
        public void SelezioneVuota_RestituisceLaListaIdentica()
        {
            IReadOnlyList<string> risultato = ReorderCalculator.Move(Quattro, Array.Empty<int>(), 2);

            Assert.Equal(Quattro, risultato);
        }

        [Fact]
        public void LaListaDiPartenzaNonVieneModificata()
        {
            var originale = new List<string> { "A", "B", "C" };

            ReorderCalculator.Move(originale, new[] { 0 }, 3);

            Assert.Equal(new[] { "A", "B", "C" }, originale);
        }

        [Fact]
        public void PosizioneDiInserimentoFuoriDaiLimiti_SollevaEccezioneChiara()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ReorderCalculator.Move(Quattro, new[] { 0 }, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ReorderCalculator.Move(Quattro, new[] { 0 }, 5));
        }

        [Fact]
        public void IndiceSelezionatoFuoriDaiLimiti_SollevaEccezioneChiara()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ReorderCalculator.Move(Quattro, new[] { 9 }, 0));
        }

        [Fact]
        public void ParametriNulli_SollevanoEccezioniChiare()
        {
            Assert.Throws<ArgumentNullException>(() => ReorderCalculator.Move<string>(null, new[] { 0 }, 0));
            Assert.Throws<ArgumentNullException>(() => ReorderCalculator.Move(Quattro, null, 0));
        }
    }
}
