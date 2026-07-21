using System;
using MN_LayoutManager.Core;
using Xunit;

namespace MN_LayoutManager.Core.Tests
{
    /// <summary>
    /// Verifica le regole sui nomi dei layout.
    /// In parole semplici: controlla che il plugin rifiuti i nomi che AutoCAD non accetta,
    /// spiegando il motivo, invece di provarci e andare in errore.
    /// </summary>
    public class LayoutNameValidatorTests
    {
        [Theory]
        [InlineData("Tavola 01")]
        [InlineData("A")]
        [InlineData("Pianta-P1_rev2")]
        [InlineData("Sezione (A-A)")]
        public void NomiNormali_SonoAccettati(string name)
        {
            Assert.True(LayoutNameValidator.TryValidate(name, out string error), error);
            Assert.Null(error);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void NomeVuoto_ERifiutato(string name)
        {
            Assert.False(LayoutNameValidator.TryValidate(name, out string error));
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Theory]
        [InlineData("Tavola<1")]
        [InlineData("Tavola/1")]
        [InlineData("Tavola\\1")]
        [InlineData("Tavola:1")]
        [InlineData("Tavola?1")]
        [InlineData("Tavola*1")]
        [InlineData("Tavola|1")]
        [InlineData("Tavola,1")]
        [InlineData("Tavola=1")]
        public void CaratteriNonAmmessi_SonoRifiutati(string name)
        {
            Assert.False(LayoutNameValidator.TryValidate(name, out string error));
            Assert.Contains("caratteri", error, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("Model")]
        [InlineData("model")]
        [InlineData("MODEL")]
        public void NomeModel_ERiservato_InQualsiasiCombinazioneDiMaiuscole(string name)
        {
            Assert.False(LayoutNameValidator.TryValidate(name, out string error));
            Assert.Contains("riservato", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void NomeTroppoLungo_ERifiutato()
        {
            string name = new string('x', LayoutNameValidator.MaxLength + 1);
            Assert.False(LayoutNameValidator.TryValidate(name, out string error));
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Fact]
        public void NomeConSpaziAiBordi_ERifiutato()
        {
            Assert.False(LayoutNameValidator.TryValidate(" Tavola", out _));
            Assert.False(LayoutNameValidator.TryValidate("Tavola ", out _));
        }

        [Fact]
        public void NomeGiaUsatoDaUnAltroLayout_ERifiutato()
        {
            string[] existing = { "Tavola1", "Tavola2" };

            Assert.False(LayoutNameValidator.TryValidateUnique("Tavola2", existing, "Tavola1", out string error));
            Assert.Contains("Tavola2", error, StringComparison.Ordinal);
        }

        [Fact]
        public void ConfrontoDiUnicita_IgnoraMaiuscoleEMinuscole()
        {
            string[] existing = { "Tavola1", "Tavola2" };

            Assert.False(LayoutNameValidator.TryValidateUnique("TAVOLA2", existing, "Tavola1", out _));
        }

        [Fact]
        public void RinominareUnLayoutCambiandoSoloLeMaiuscole_EPermesso()
        {
            // "Tavola1" -> "TAVOLA1": il layout si scontrerebbe con se stesso, e va bene.
            string[] existing = { "Tavola1", "Tavola2" };

            Assert.True(LayoutNameValidator.TryValidateUnique("TAVOLA1", existing, "Tavola1", out string error), error);
        }

        [Fact]
        public void NuovoLayout_ConNomeGiaEsistente_ERifiutato()
        {
            string[] existing = { "Tavola1" };

            // currentName null = sto creando un layout nuovo, non rinominandone uno.
            Assert.False(LayoutNameValidator.TryValidateUnique("Tavola1", existing, null, out _));
        }

        [Fact]
        public void ElencoEsistentiNullo_SollevaEccezioneChiara()
        {
            Assert.Throws<ArgumentNullException>(
                () => LayoutNameValidator.TryValidateUnique("Tavola1", null, null, out _));
        }
    }
}
