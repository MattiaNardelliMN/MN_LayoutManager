using System;
using System.Collections.Generic;
using System.Globalization;

namespace LayoutManagerPalette.Core
{
    /// <summary>
    /// Regole di validita' di un nome di layout AutoCAD.
    /// Nessuna dipendenza dalle API Autodesk: e' logica pura, testabile a comando.
    /// </summary>
    public static class LayoutNameValidator
    {
        /// <summary>Lunghezza massima ammessa per un nome di layout.</summary>
        public const int MaxLength = 255;

        /// <summary>Nome riservato: lo spazio Modello non e' un layout carta rinominabile.</summary>
        public const string ModelSpaceName = "Model";

        private const string InvalidCharacters = "<>/\\\":;?*|,=`";

        /// <summary>Caratteri non ammessi, in forma leggibile per i messaggi all'utente.</summary>
        public static string InvalidCharactersDisplay => InvalidCharacters;

        /// <summary>
        /// Controlla che il nome sia formalmente valido, senza guardare gli altri layout.
        /// </summary>
        /// <param name="name">Nome da controllare.</param>
        /// <param name="error">Messaggio in italiano che spiega il problema, o null se il nome va bene.</param>
        /// <returns>true se il nome e' valido.</returns>
        public static bool TryValidate(string name, out string error)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Il nome del layout non puo' essere vuoto.";
                return false;
            }

            if (name.Length > MaxLength)
            {
                error = string.Format(
                    CultureInfo.CurrentCulture,
                    "Il nome del layout non puo' superare {0} caratteri.",
                    MaxLength);
                return false;
            }

            if (name != name.Trim())
            {
                error = "Il nome del layout non puo' iniziare o finire con uno spazio.";
                return false;
            }

            if (name.IndexOfAny(InvalidCharacters.ToCharArray()) >= 0)
            {
                error = string.Format(
                    CultureInfo.CurrentCulture,
                    "Il nome del layout non puo' contenere questi caratteri: {0}",
                    InvalidCharacters);
                return false;
            }

            if (string.Equals(name, ModelSpaceName, StringComparison.OrdinalIgnoreCase))
            {
                error = string.Format(
                    CultureInfo.CurrentCulture,
                    "\"{0}\" e' un nome riservato allo spazio Modello.",
                    ModelSpaceName);
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Controlla nome formale + unicita' rispetto ai layout gia' presenti.
        /// Il confronto ignora maiuscole/minuscole, come fa AutoCAD.
        /// </summary>
        /// <param name="name">Nuovo nome proposto.</param>
        /// <param name="existingNames">Nomi di tutti i layout attuali del disegno.</param>
        /// <param name="currentName">
        /// Nome attuale del layout che stiamo rinominando: viene ignorato nel controllo di
        /// unicita' (rinominare "Tavola1" in "TAVOLA1" deve essere permesso).
        /// Passare null quando si sta creando un layout nuovo.
        /// </param>
        /// <param name="error">Messaggio in italiano che spiega il problema, o null se va bene.</param>
        /// <returns>true se il nome e' utilizzabile.</returns>
        public static bool TryValidateUnique(
            string name,
            IEnumerable<string> existingNames,
            string currentName,
            out string error)
        {
            if (existingNames == null)
            {
                throw new ArgumentNullException(nameof(existingNames));
            }

            if (!TryValidate(name, out error))
            {
                return false;
            }

            foreach (string existing in existingNames)
            {
                if (existing == null)
                {
                    continue;
                }

                bool isSelf = currentName != null
                    && string.Equals(existing, currentName, StringComparison.OrdinalIgnoreCase);

                if (!isSelf && string.Equals(existing, name, StringComparison.OrdinalIgnoreCase))
                {
                    error = string.Format(
                        CultureInfo.CurrentCulture,
                        "Esiste gia' un layout chiamato \"{0}\".",
                        existing);
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
