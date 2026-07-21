using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace MN_LayoutManager.Core.Tests
{
    /// <summary>
    /// Controlla i file dell'interfaccia (XAML) senza aprire AutoCAD.
    /// In parole semplici: verifica che ogni colore o stile richiamato nell'interfaccia
    /// esista davvero nel tema. Un nome scritto male non darebbe errore in compilazione,
    /// ma farebbe crashare la palette nel momento in cui la apri: questo test lo blocca prima.
    /// </summary>
    public class XamlResourceKeysTests
    {
        private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

        private static string ThemePath => Path.Combine(GetSourceRoot(), "MN_LayoutManager", "Themes", "DarkTheme.xaml");

        private static string ViewPath => Path.Combine(GetSourceRoot(), "MN_LayoutManager", "UI", "LayoutPaletteView.xaml");

        [Fact]
        public void IFileDellInterfacciaEsistono()
        {
            Assert.True(File.Exists(ThemePath), "Manca il tema scuro: " + ThemePath);
            Assert.True(File.Exists(ViewPath), "Manca la vista della palette: " + ViewPath);
        }

        [Fact]
        public void IFileDellInterfacciaSonoXmlValido()
        {
            // Se lo XAML e' malformato (un tag non chiuso) questo test fallisce subito,
            // con un messaggio che dice riga e colonna.
            XDocument.Load(ThemePath);
            XDocument.Load(ViewPath);
        }

        [Fact]
        public void OgniStileRichiamatoNelTemaEsisteNelTema()
        {
            IReadOnlyCollection<string> defined = ReadDefinedKeys(ThemePath);
            IReadOnlyCollection<string> used = ReadUsedKeys(ThemePath);

            var missing = used.Where(key => !defined.Contains(key)).Distinct().ToList();

            Assert.True(
                missing.Count == 0,
                "Nel tema si richiamano risorse che non esistono: " + string.Join(", ", missing));
        }

        [Fact]
        public void OgniStileRichiamatoNellaPaletteEsisteNelTemaONellaPaletteStessa()
        {
            var defined = new HashSet<string>(ReadDefinedKeys(ThemePath), StringComparer.Ordinal);
            foreach (string localKey in ReadDefinedKeys(ViewPath))
            {
                defined.Add(localKey);
            }

            var missing = ReadUsedKeys(ViewPath).Where(key => !defined.Contains(key)).Distinct().ToList();

            Assert.True(
                missing.Count == 0,
                "Nella palette si richiamano risorse che non esistono: " + string.Join(", ", missing));
        }

        [Fact]
        public void IlTemaDefinisceIColoriDiBaseDelloStileScuro()
        {
            var defined = new HashSet<string>(ReadDefinedKeys(ThemePath), StringComparer.Ordinal);

            foreach (string required in new[]
            {
                "BackgroundBrush", "PanelBrush", "SurfaceBrush", "BorderBrush",
                "TextPrimaryBrush", "TextSecondaryBrush", "AccentBrush",
            })
            {
                Assert.True(defined.Contains(required), "Il tema scuro non definisce " + required);
            }
        }

        private static IReadOnlyCollection<string> ReadDefinedKeys(string path)
        {
            XDocument document = XDocument.Load(path);

            return document.Descendants()
                .Select(element => (string)element.Attribute(XamlNamespace + "Key"))
                .Where(key => !string.IsNullOrEmpty(key))
                .ToList();
        }

        private static IReadOnlyCollection<string> ReadUsedKeys(string path)
        {
            string text = File.ReadAllText(path);

            return Regex.Matches(text, @"\{(?:Static|Dynamic)Resource\s+([A-Za-z0-9_.]+)\s*\}")
                .Cast<Match>()
                .Select(match => match.Groups[1].Value)
                .ToList();
        }

        /// <summary>
        /// Risale dalla cartella dei test fino alla radice del repository, per trovare
        /// i file XAML senza dipendere da percorsi fissi della macchina.
        /// </summary>
        private static string GetSourceRoot()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "src");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Non trovo la cartella 'src' del progetto partendo dai test.");
        }
    }
}
