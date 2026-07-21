using System;
using System.Collections.Generic;
using System.Linq;
using MN_LayoutManager.Core;
using Xunit;

namespace MN_LayoutManager.Core.Tests
{
    /// <summary>
    /// Verifica il calcolo della rinomina multipla.
    /// In parole semplici: controlla che, PRIMA di toccare il disegno, il plugin capisca
    /// esattamente quali layout cambiano nome e si accorga dei casi che darebbero errore.
    /// </summary>
    public class BatchRenamePlannerTests
    {
        private static readonly string[] TreLayout = { "Tavola1", "Tavola2", "Pianta" };

        [Fact]
        public void Prefisso_VieneAggiuntoATuttiILayout()
        {
            var request = new BatchRenameRequest { Mode = BatchRenameMode.AddPrefix, Value = "A_" };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(TreLayout, request);

            Assert.True(plan.IsValid);
            Assert.Equal(
                new[] { "A_Tavola1", "A_Tavola2", "A_Pianta" },
                plan.Steps.Select(s => s.FinalName).ToArray());
        }

        [Fact]
        public void Suffisso_VieneAggiuntoATuttiILayout()
        {
            var request = new BatchRenameRequest { Mode = BatchRenameMode.AddSuffix, Value = "_rev1" };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(TreLayout, request);

            Assert.True(plan.IsValid);
            Assert.Equal("Tavola1_rev1", plan.Steps[0].FinalName);
            Assert.Equal("Pianta_rev1", plan.Steps[2].FinalName);
        }

        [Fact]
        public void RimuoviPrefisso_ToglieIlTestoSoloAChiIniziaDavveroCosi()
        {
            var request = new BatchRenameRequest { Mode = BatchRenameMode.RemovePrefix, Value = "A_" };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(new[] { "A_Tavola1", "Tavola2", "XA_Tavola3" }, request);

            Assert.True(plan.IsValid);
            // Solo il primo inizia con "A_": gli altri due non vengono toccati.
            Assert.Single(plan.Steps);
            Assert.Equal("A_Tavola1", plan.Steps[0].OriginalName);
            Assert.Equal("Tavola1", plan.Steps[0].FinalName);
        }

        [Fact]
        public void RimuoviSuffisso_ToglieIlTestoSoloAChiFinisceDavveroCosi()
        {
            var request = new BatchRenameRequest { Mode = BatchRenameMode.RemoveSuffix, Value = "_rev1" };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(new[] { "Tavola1_rev1", "Tavola2", "Tavola3_rev1_old" }, request);

            Assert.True(plan.IsValid);
            Assert.Single(plan.Steps);
            Assert.Equal("Tavola1", plan.Steps[0].FinalName);
        }

        [Fact]
        public void RimuoviPrefisso_IgnoraMaiuscoleEMinuscolePerDefault()
        {
            var request = new BatchRenameRequest { Mode = BatchRenameMode.RemovePrefix, Value = "a_" };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(new[] { "A_Tavola1" }, request);

            Assert.Equal("Tavola1", plan.Steps.Single().FinalName);
        }

        [Fact]
        public void RimuoviPrefisso_ConDistinzioneMaiuscole_NonTocca()
        {
            var request = new BatchRenameRequest
            {
                Mode = BatchRenameMode.RemovePrefix,
                Value = "a_",
                CaseSensitive = true,
            };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(new[] { "A_Tavola1" }, request);

            Assert.True(plan.IsEmpty);
        }

        [Fact]
        public void RimuovereTuttoIlNome_EBloccatoConUnMessaggio()
        {
            // Togliendo "Tavola" a un layout che si chiama esattamente "Tavola" resterebbe
            // un nome vuoto: non deve passare, e non deve toccare il disegno.
            var request = new BatchRenameRequest { Mode = BatchRenameMode.RemovePrefix, Value = "Tavola" };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(new[] { "Tavola" }, request);

            Assert.False(plan.IsValid);
            Assert.Empty(plan.Steps);
        }

        [Fact]
        public void RimuoviPrefisso_SenzaTesto_ChiedeIlTestoDaTogliere()
        {
            var request = new BatchRenameRequest { Mode = BatchRenameMode.RemovePrefix, Value = string.Empty };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(TreLayout, request);

            Assert.False(plan.IsValid);
            Assert.Contains(plan.Errors, e => e.IndexOf("togliere", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        [Fact]
        public void RimuoviPrefisso_CheGenerebbeDueNomiUguali_EUnErrore()
        {
            var request = new BatchRenameRequest { Mode = BatchRenameMode.RemovePrefix, Value = "A_" };

            // "A_X" diventerebbe "X", ma "X" esiste gia' e non cambia nome.
            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(new[] { "A_X", "X" }, request);

            Assert.False(plan.IsValid);
        }

        [Fact]
        public void TrovaESostituisci_CambiaSoloLaParteIndicata()
        {
            var request = new BatchRenameRequest
            {
                Mode = BatchRenameMode.FindReplace,
                Value = "Tavola",
                ReplacementValue = "Foglio",
            };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(TreLayout, request);

            Assert.True(plan.IsValid);
            // "Pianta" non contiene "Tavola": resta fuori dai passi da eseguire.
            Assert.Equal(2, plan.Steps.Count);
            Assert.Equal(new[] { "Foglio1", "Foglio2" }, plan.Steps.Select(s => s.FinalName).ToArray());
        }

        [Fact]
        public void TrovaESostituisci_ConSostitutoVuoto_CancellaIlTesto()
        {
            var request = new BatchRenameRequest
            {
                Mode = BatchRenameMode.FindReplace,
                Value = "Tavola",
                ReplacementValue = string.Empty,
            };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(new[] { "TavolaA" }, request);

            Assert.True(plan.IsValid);
            Assert.Equal("A", plan.Steps.Single().FinalName);
        }

        [Fact]
        public void TrovaESostituisci_SostituisceTutteLeOccorrenze_SenzaCicliInfiniti()
        {
            var request = new BatchRenameRequest
            {
                Mode = BatchRenameMode.FindReplace,
                Value = "a",
                ReplacementValue = "aa",
            };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(new[] { "banana" }, request);

            Assert.True(plan.IsValid);
            Assert.Equal("baanaanaa", plan.Steps.Single().FinalName);
        }

        [Fact]
        public void Filtro_LimitaLAzioneAiSoliLayoutCheLoContengono()
        {
            var request = new BatchRenameRequest
            {
                Filter = "Tavola",
                Mode = BatchRenameMode.AddPrefix,
                Value = "X_",
            };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(TreLayout, request);

            Assert.True(plan.IsValid);
            Assert.Equal(2, plan.Steps.Count);
            Assert.DoesNotContain(plan.Steps, s => s.OriginalName == "Pianta");
        }

        [Fact]
        public void Filtro_IgnoraMaiuscoleEMinuscolePerDefault()
        {
            var request = new BatchRenameRequest
            {
                Filter = "tavola",
                Mode = BatchRenameMode.AddPrefix,
                Value = "X_",
            };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(TreLayout, request);

            Assert.Equal(2, plan.Steps.Count);
        }

        [Fact]
        public void Filtro_ConDistinzioneMaiuscole_NonTrovaNulla()
        {
            var request = new BatchRenameRequest
            {
                Filter = "tavola",
                Mode = BatchRenameMode.AddPrefix,
                Value = "X_",
                CaseSensitive = true,
            };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(TreLayout, request);

            Assert.True(plan.IsValid);
            Assert.True(plan.IsEmpty);
        }

        [Fact]
        public void SeIlNomeNonCambia_IlLayoutNonVieneToccato()
        {
            var request = new BatchRenameRequest
            {
                Mode = BatchRenameMode.FindReplace,
                Value = "ZZZ",
                ReplacementValue = "YYY",
            };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(TreLayout, request);

            Assert.True(plan.IsEmpty);
        }

        [Fact]
        public void NomeFinaleOccupatoDaUnAltroLayoutCheCambiaNome_RichiedeINomiTemporanei()
        {
            // Layout "A_X" e "X", si aggiunge il prefisso "A_":
            //   "A_X" -> "A_A_X"   e   "X" -> "A_X"
            // Rinominando in sequenza nell'ordine sbagliato, "X" -> "A_X" si scontrerebbe
            // con il layout "A_X" ancora esistente. Il piano deve accorgersene e chiedere
            // il passaggio intermedio con nomi temporanei.
            var request = new BatchRenameRequest { Mode = BatchRenameMode.AddPrefix, Value = "A_" };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(new[] { "A_X", "X" }, request);

            Assert.True(plan.IsValid);
            Assert.True(plan.RequiresTemporaryNames);
        }

        [Fact]
        public void RinominaSenzaSovrapposizioni_NonRichiedeNomiTemporanei()
        {
            var request = new BatchRenameRequest { Mode = BatchRenameMode.AddPrefix, Value = "Z_" };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(TreLayout, request);

            Assert.True(plan.IsValid);
            Assert.False(plan.RequiresTemporaryNames);
        }

        [Fact]
        public void RinominareSoloUnLayoutVersoUnNomeGiaOccupato_EUnErrore()
        {
            var request = new BatchRenameRequest
            {
                Mode = BatchRenameMode.FindReplace,
                Value = "1",
                ReplacementValue = "2",
            };

            // "T1" diventerebbe "T2", ma "T2" esiste e non cambia nome: si deve fermare.
            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(new[] { "T1", "T2" }, request);

            Assert.False(plan.IsValid);
        }

        [Fact]
        public void NomiFinaliCheSiScontranoFraLoro_SonoUnErrore()
        {
            // "AB" e "CB" con "trova A o C" ... qui: entrambi diventano "B".
            var request = new BatchRenameRequest
            {
                Mode = BatchRenameMode.FindReplace,
                Value = "Tavola",
                ReplacementValue = "X",
            };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(new[] { "Tavola", "TavolaTavola" }, request);

            // "Tavola" -> "X" e "TavolaTavola" -> "XX": nessuna collisione, deve passare.
            Assert.True(plan.IsValid);

            var collisione = new BatchRenameRequest
            {
                Mode = BatchRenameMode.FindReplace,
                Value = "1",
                ReplacementValue = string.Empty,
            };

            BatchRenamePlan planCollisione = BatchRenamePlanner.CreatePlan(new[] { "T1", "T11" }, collisione);

            Assert.False(planCollisione.IsValid);
            Assert.Contains(planCollisione.Errors, e => e.IndexOf("stesso nome", StringComparison.Ordinal) >= 0);
        }

        [Fact]
        public void NomeFinaleCheSiScontraConUnLayoutNonCoinvolto_EUnErrore()
        {
            var request = new BatchRenameRequest
            {
                Filter = "Tavola1",
                Mode = BatchRenameMode.FindReplace,
                Value = "1",
                ReplacementValue = "2",
            };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(TreLayout, request);

            Assert.False(plan.IsValid);
            Assert.Contains(plan.Errors, e => e.IndexOf("esiste gia'", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        [Fact]
        public void NomeFinaleNonValido_EUnErroreEBloccaTutto()
        {
            var request = new BatchRenameRequest { Mode = BatchRenameMode.AddPrefix, Value = "A/" };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(TreLayout, request);

            Assert.False(plan.IsValid);
            Assert.Empty(plan.Steps);
        }

        [Fact]
        public void ValoreVuoto_EUnErrore()
        {
            var request = new BatchRenameRequest { Mode = BatchRenameMode.AddPrefix, Value = string.Empty };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(TreLayout, request);

            Assert.False(plan.IsValid);
            Assert.Single(plan.Errors);
        }

        [Fact]
        public void SenzaLayout_IlPianoEVuotoMaValido()
        {
            var request = new BatchRenameRequest { Mode = BatchRenameMode.AddPrefix, Value = "A_" };

            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(Array.Empty<string>(), request);

            Assert.True(plan.IsEmpty);
        }

        [Fact]
        public void NomiTemporanei_SonoSempreDiversiFraLoro()
        {
            var generati = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < 50; i++)
            {
                Assert.True(generati.Add(BatchRenamePlanner.CreateTemporaryName(i)));
            }
        }

        [Fact]
        public void ParametriNulli_SollevanoEccezioniChiare()
        {
            Assert.Throws<ArgumentNullException>(
                () => BatchRenamePlanner.CreatePlan(null, new BatchRenameRequest()));
            Assert.Throws<ArgumentNullException>(
                () => BatchRenamePlanner.CreatePlan(Array.Empty<string>(), null));
        }
    }
}
