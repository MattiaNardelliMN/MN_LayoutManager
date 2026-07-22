using System;
using System.Collections.Generic;
using System.Globalization;

namespace MN_LayoutManager.Core
{
    /// <summary>
    /// Riconosce le progressioni numeriche nei nomi dei layout e propone il nome successivo,
    /// come quando si trascina una cella in Excel: da "D_T_01" si arriva a "D_T_02", "D_T_03"...
    /// </summary>
    /// <remarks>
    /// Logica pura: nessuna dipendenza da AutoCAD, quindi verificabile con i test automatici.
    /// </remarks>
    public static class LayoutNumbering
    {
        /// <summary>
        /// Quanti numeri al massimo si provano prima di arrendersi.
        /// Serve solo a garantire che nessun ciclo possa girare all'infinito.
        /// </summary>
        public const int MaxSearchAttempts = 10000;

        /// <summary>
        /// Quanti layout servono per parlare di "progressione".
        /// Con un solo nome numerato non si sa se e' una serie o un caso isolato.
        /// </summary>
        public const int MinimumSeriesLength = 2;

        private const int FirstCopySuffix = 2;

        /// <summary>
        /// Scompone un nome in "testo + numero + testo" guardando l'ULTIMO gruppo di cifre.
        /// "D_T_01" da prefisso "D_T_", numero 1, 2 cifre. Un nome senza cifre non si scompone.
        /// </summary>
        /// <param name="name">Nome da leggere.</param>
        /// <param name="parsed">La scomposizione, se il nome contiene un numero.</param>
        /// <returns>true se nel nome c'e' un numero utilizzabile.</returns>
        public static bool TryParse(string name, out NumberedLayoutName parsed)
        {
            parsed = null;

            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            int end = -1;
            for (int i = name.Length - 1; i >= 0; i--)
            {
                if (IsAsciiDigit(name[i]))
                {
                    end = i;
                    break;
                }
            }

            if (end < 0)
            {
                return false;
            }

            int start = end;
            while (start > 0 && IsAsciiDigit(name[start - 1]))
            {
                start--;
            }

            string digits = name.Substring(start, end - start + 1);

            // Un numero che non entra in un int non e' un progressivo, e' un codice:
            // meglio non toccarlo che produrre nomi assurdi.
            if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int number))
            {
                return false;
            }

            parsed = new NumberedLayoutName(
                name.Substring(0, start),
                number,
                digits.Length,
                name.Substring(end + 1));

            return true;
        }

        /// <summary>
        /// Calcola i nomi delle copie di un layout.
        /// Se il nome di partenza fa parte di una serie numerata si prosegue la serie
        /// ("D_T_01" con 3 copie da "D_T_02", "D_T_03", "D_T_04"); altrimenti si ripiega
        /// sul classico "Nome (2)", "Nome (3)".
        /// </summary>
        /// <param name="existingNames">Nomi dei layout gia' presenti nel disegno.</param>
        /// <param name="sourceName">Layout che viene copiato.</param>
        /// <param name="count">Quante copie servono.</param>
        /// <returns>I nomi da usare, tutti liberi e diversi fra loro.</returns>
        public static IReadOnlyList<string> BuildCopyNames(
            IReadOnlyList<string> existingNames,
            string sourceName,
            int count)
        {
            if (existingNames == null)
            {
                throw new ArgumentNullException(nameof(existingNames));
            }

            if (string.IsNullOrWhiteSpace(sourceName))
            {
                throw new ArgumentException("Il layout di partenza deve avere un nome.", nameof(sourceName));
            }

            if (count <= 0)
            {
                return new string[0];
            }

            var taken = BuildNameSet(existingNames);
            var names = new List<string>(count);

            for (int i = 0; i < count; i++)
            {
                // Ogni nome appena deciso entra subito fra quelli occupati: cosi' la copia
                // successiva non puo' ricevere lo stesso nome.
                string next = BuildSingleCopyName(taken, sourceName);
                names.Add(next);
                taken.Add(next);
            }

            return names;
        }

        /// <summary>
        /// Nome della singola copia di un layout: e' <see cref="BuildCopyNames"/> con una copia sola.
        /// </summary>
        /// <param name="existingNames">Nomi dei layout gia' presenti nel disegno.</param>
        /// <param name="sourceName">Layout che viene copiato.</param>
        /// <returns>Il nome libero da usare per la copia.</returns>
        public static string BuildCopyName(IReadOnlyList<string> existingNames, string sourceName)
        {
            return BuildCopyNames(existingNames, sourceName, 1)[0];
        }

        /// <summary>
        /// Cerca una progressione numerica fra i layout esistenti e propone il nome
        /// successivo per un layout nuovo.
        /// </summary>
        /// <remarks>
        /// Se ci sono piu' serie (per esempio "D_T_xx" e "Dettaglio_xx") vince quella con
        /// piu' layout; a parita' di numero vince quella comparsa piu' in basso nell'elenco,
        /// cioe' quella su cui l'utente stava presumibilmente lavorando.
        /// </remarks>
        /// <param name="existingNames">Nomi dei layout gia' presenti, nell'ordine delle schede.</param>
        /// <param name="proposedName">Nome proposto, se e' stata riconosciuta una serie.</param>
        /// <returns>true se e' stata riconosciuta una progressione.</returns>
        public static bool TryProposeNextInSeries(IReadOnlyList<string> existingNames, out string proposedName)
        {
            proposedName = null;

            if (existingNames == null)
            {
                throw new ArgumentNullException(nameof(existingNames));
            }

            NumberedLayoutName bestSeries = FindDominantSeries(existingNames);
            if (bestSeries == null)
            {
                return false;
            }

            var taken = BuildNameSet(existingNames);
            proposedName = FindFreeNameInSeries(taken, bestSeries);

            return proposedName != null;
        }

        /// <summary>
        /// La serie numerata piu' rappresentata fra i nomi dati, oppure null se non ce n'e'
        /// nessuna con almeno <see cref="MinimumSeriesLength"/> layout.
        /// </summary>
        private static NumberedLayoutName FindDominantSeries(IReadOnlyList<string> names)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastPosition = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var samples = new Dictionary<string, NumberedLayoutName>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < names.Count; i++)
            {
                if (!TryParse(names[i], out NumberedLayoutName parsed))
                {
                    continue;
                }

                string key = parsed.GetShapeKey();

                counts.TryGetValue(key, out int seen);
                counts[key] = seen + 1;
                lastPosition[key] = i;

                // Si conserva il campione col numero piu' alto: e' quello che detta il
                // formato (quante cifre) da cui proseguire.
                if (!samples.TryGetValue(key, out NumberedLayoutName sample) || parsed.Number > sample.Number)
                {
                    samples[key] = parsed;
                }
            }

            string bestKey = null;
            int bestCount = 0;
            int bestPosition = -1;

            foreach (KeyValuePair<string, int> entry in counts)
            {
                if (entry.Value < MinimumSeriesLength)
                {
                    continue;
                }

                int position = lastPosition[entry.Key];

                if (entry.Value > bestCount || (entry.Value == bestCount && position > bestPosition))
                {
                    bestKey = entry.Key;
                    bestCount = entry.Value;
                    bestPosition = position;
                }
            }

            return bestKey == null ? null : samples[bestKey];
        }

        private static string BuildSingleCopyName(HashSet<string> taken, string sourceName)
        {
            if (TryParse(sourceName, out NumberedLayoutName pattern))
            {
                // Si riparte dal numero piu' alto gia' presente nella serie, non da quello
                // del layout copiato: copiando "D_T_01" quando esiste gia' "D_T_05" il nome
                // giusto e' "D_T_06", altrimenti si litigherebbe con i nomi occupati.
                NumberedLayoutName highest = FindHighestInSeries(taken, pattern);

                string candidate = FindFreeNameInSeries(taken, highest);
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return BuildParenthesisName(taken, sourceName);
        }

        /// <summary>Il membro della serie col numero piu' alto, fra i nomi occupati.</summary>
        private static NumberedLayoutName FindHighestInSeries(HashSet<string> taken, NumberedLayoutName pattern)
        {
            NumberedLayoutName highest = pattern;

            foreach (string name in taken)
            {
                if (TryParse(name, out NumberedLayoutName other)
                    && other.HasSameShapeAs(pattern)
                    && other.Number > highest.Number)
                {
                    highest = other;
                }
            }

            return highest;
        }

        /// <summary>
        /// Primo nome libero della serie partendo dal numero successivo a quello dato.
        /// Restituisce null se non se ne trova uno valido: chi chiama deve ripiegare.
        /// </summary>
        private static string FindFreeNameInSeries(HashSet<string> taken, NumberedLayoutName series)
        {
            for (int attempt = 1; attempt <= MaxSearchAttempts; attempt++)
            {
                // Il controllo di overflow evita che, con numeri vicini al massimo,
                // il contatore diventi negativo e produca nomi tipo "D_T_-2147483648".
                long next = (long)series.Number + attempt;
                if (next > int.MaxValue)
                {
                    return null;
                }

                string candidate = series.Format((int)next);

                if (candidate.Length <= LayoutNameValidator.MaxLength
                    && !taken.Contains(candidate)
                    && LayoutNameValidator.TryValidate(candidate, out _))
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>Ripiego classico quando non c'e' nessuna serie: "Nome (2)", "Nome (3)"...</summary>
        private static string BuildParenthesisName(HashSet<string> taken, string sourceName)
        {
            for (int i = FirstCopySuffix; i < MaxSearchAttempts; i++)
            {
                string candidate = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} ({1})",
                    sourceName,
                    i);

                if (candidate.Length <= LayoutNameValidator.MaxLength && !taken.Contains(candidate))
                {
                    return candidate;
                }
            }

            // Praticamente impossibile, ma meglio un nome buffo che un errore.
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}_{1}",
                sourceName,
                Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture).Substring(0, 6));
        }

        private static HashSet<string> BuildNameSet(IReadOnlyList<string> names)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string name in names)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    set.Add(name);
                }
            }

            return set;
        }

        /// <summary>
        /// Solo le cifre 0-9 contano come numero.
        /// <c>char.IsDigit</c> accetterebbe anche le cifre di altri alfabeti, che poi
        /// <c>int.TryParse</c> non sa leggere: meglio essere espliciti.
        /// </summary>
        private static bool IsAsciiDigit(char character) => character >= '0' && character <= '9';
    }
}
