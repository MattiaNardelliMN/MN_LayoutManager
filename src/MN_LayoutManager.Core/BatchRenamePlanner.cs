using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MN_LayoutManager.Core
{
    /// <summary>
    /// Calcola cosa deve succedere in una rinomina multipla, senza toccare il disegno.
    /// Tutta la parte "che ragiona" sta qui, cosi' e' verificabile con i test.
    /// </summary>
    public static class BatchRenamePlanner
    {
        /// <summary>
        /// Prefisso dei nomi temporanei usati nella rinomina in due fasi.
        /// Non e' un nome che un utente sceglierebbe, quindi non collide con i suoi layout.
        /// </summary>
        public const string TemporaryNamePrefix = "~tmp_";

        /// <summary>
        /// Costruisce il piano di rinomina.
        /// </summary>
        /// <param name="layoutNames">Nomi di TUTTI i layout carta del disegno, nell'ordine dei tab.</param>
        /// <param name="request">Cosa ha chiesto l'utente.</param>
        /// <returns>Il piano: valido ed eseguibile, oppure con l'elenco dei problemi.</returns>
        public static BatchRenamePlan CreatePlan(IReadOnlyList<string> layoutNames, BatchRenameRequest request)
        {
            if (layoutNames == null)
            {
                throw new ArgumentNullException(nameof(layoutNames));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var errors = new List<string>();
            var steps = new List<BatchRenameStep>();

            if (string.IsNullOrEmpty(request.Value))
            {
                errors.Add(DescribeMissingValue(request.Mode));
                return new BatchRenamePlan(steps, errors, false);
            }

            // Layout che il filtro lascia passare: sono quelli su cui si agisce.
            var affected = new List<string>();
            var untouched = new List<string>();
            foreach (string name in layoutNames)
            {
                if (name == null)
                {
                    continue;
                }

                if (MatchesFilter(name, request))
                {
                    affected.Add(name);
                }
                else
                {
                    untouched.Add(name);
                }
            }

            // Nome finale proposto per ciascun layout coinvolto.
            var proposals = new List<BatchRenameStep>();
            foreach (string original in affected)
            {
                string final = ApplyMode(original, request);

                // Se il nome non cambia davvero, non serve toccare quel layout.
                if (string.Equals(final, original, StringComparison.Ordinal))
                {
                    untouched.Add(original);
                    continue;
                }

                proposals.Add(new BatchRenameStep(original, final));
            }

            ValidateProposals(proposals, untouched, errors);

            if (errors.Count > 0)
            {
                return new BatchRenamePlan(new List<BatchRenameStep>(), errors, false);
            }

            steps.AddRange(proposals);
            return new BatchRenamePlan(steps, errors, RequiresTemporaryNames(proposals));
        }

        /// <summary>
        /// Genera un nome temporaneo univoco per la fase 1 della rinomina in due fasi.
        /// </summary>
        /// <param name="index">Progressivo, per rendere leggibile il nome in caso di errore.</param>
        /// <returns>Nome temporaneo che non puo' collidere con nomi reali.</returns>
        public static string CreateTemporaryName(int index)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}_{2}",
                TemporaryNamePrefix,
                index.ToString(CultureInfo.InvariantCulture),
                Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture).Substring(0, 8));
        }

        private static bool MatchesFilter(string name, BatchRenameRequest request)
        {
            if (string.IsNullOrEmpty(request.Filter))
            {
                return true;
            }

            return name.IndexOf(request.Filter, request.Comparison) >= 0;
        }

        private static string DescribeMissingValue(BatchRenameMode mode)
        {
            switch (mode)
            {
                case BatchRenameMode.FindReplace:
                    return "Indica il testo da cercare.";
                case BatchRenameMode.RemovePrefix:
                case BatchRenameMode.RemoveSuffix:
                    return "Indica il testo da togliere.";
                default:
                    return "Indica il testo da aggiungere.";
            }
        }

        private static string ApplyMode(string original, BatchRenameRequest request)
        {
            switch (request.Mode)
            {
                case BatchRenameMode.AddPrefix:
                    return request.Value + original;

                case BatchRenameMode.AddSuffix:
                    return original + request.Value;

                // Nelle rimozioni si tocca il nome SOLO se inizia (o finisce) davvero con
                // il testo indicato: cosi' i layout che non c'entrano restano come sono.
                case BatchRenameMode.RemovePrefix:
                    return original.StartsWith(request.Value, request.Comparison)
                        ? original.Substring(request.Value.Length)
                        : original;

                case BatchRenameMode.RemoveSuffix:
                    return original.EndsWith(request.Value, request.Comparison)
                        ? original.Substring(0, original.Length - request.Value.Length)
                        : original;

                case BatchRenameMode.FindReplace:
                    return ReplaceAll(original, request.Value, request.ReplacementValue ?? string.Empty, request.Comparison);

                default:
                    throw new ArgumentOutOfRangeException(nameof(request), request.Mode, "Modalita' di rinomina non riconosciuta.");
            }
        }

        /// <summary>
        /// Sostituzione di tutte le occorrenze potendo scegliere se ignorare maiuscole/minuscole.
        /// .NET Framework 4.8 non offre un string.Replace con StringComparison, quindi la facciamo qui.
        /// </summary>
        private static string ReplaceAll(string source, string find, string replacement, StringComparison comparison)
        {
            if (string.IsNullOrEmpty(find))
            {
                return source;
            }

            var builder = new StringBuilder();
            int position = 0;

            while (position < source.Length)
            {
                int hit = source.IndexOf(find, position, comparison);
                if (hit < 0)
                {
                    break;
                }

                builder.Append(source, position, hit - position);
                builder.Append(replacement);

                // Si riparte DOPO il testo sostituito, non dopo il testo trovato:
                // evita di ripassare sopra la sostituzione appena fatta (ciclo infinito).
                position = hit + find.Length;
            }

            builder.Append(source, position, source.Length - position);
            return builder.ToString();
        }

        private static void ValidateProposals(
            IReadOnlyList<BatchRenameStep> proposals,
            IReadOnlyList<string> untouched,
            ICollection<string> errors)
        {
            var seenFinalNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (BatchRenameStep step in proposals)
            {
                if (!LayoutNameValidator.TryValidate(step.FinalName, out string error))
                {
                    errors.Add(string.Format(
                        CultureInfo.CurrentCulture,
                        "\"{0}\" diventerebbe \"{1}\": {2}",
                        step.OriginalName,
                        step.FinalName,
                        error));
                    continue;
                }

                if (!seenFinalNames.Add(step.FinalName))
                {
                    errors.Add(string.Format(
                        CultureInfo.CurrentCulture,
                        "Piu' layout finirebbero con lo stesso nome \"{0}\".",
                        step.FinalName));
                }
            }

            foreach (BatchRenameStep step in proposals)
            {
                foreach (string other in untouched)
                {
                    if (string.Equals(step.FinalName, other, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add(string.Format(
                            CultureInfo.CurrentCulture,
                            "\"{0}\" diventerebbe \"{1}\", ma un layout con quel nome esiste gia'.",
                            step.OriginalName,
                            step.FinalName));
                    }
                }
            }
        }

        /// <summary>
        /// Serve la fase intermedia con nomi temporanei? Si', se un nome finale e' oggi
        /// occupato da un ALTRO layout che sta anch'esso cambiando nome.
        /// </summary>
        private static bool RequiresTemporaryNames(IReadOnlyList<BatchRenameStep> proposals)
        {
            foreach (BatchRenameStep step in proposals)
            {
                foreach (BatchRenameStep other in proposals)
                {
                    if (ReferenceEquals(step, other))
                    {
                        continue;
                    }

                    if (string.Equals(step.FinalName, other.OriginalName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
