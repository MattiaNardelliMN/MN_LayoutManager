using System;
using System.Globalization;

namespace MN_LayoutManager.Core
{
    /// <summary>
    /// Un nome di layout letto come "testo + numero + testo".
    /// Esempio: "D_T_01" diventa prefisso "D_T_", numero 1 scritto con 2 cifre, suffisso vuoto.
    /// Serve a capire che "D_T_01" e "D_T_02" fanno parte della stessa serie.
    /// </summary>
    public sealed class NumberedLayoutName
    {
        /// <summary>Separatore usato nella chiave di raggruppamento.</summary>
        /// <remarks>
        /// E' il carattere nullo, che non puo' comparire in un nome di layout. Con un
        /// separatore normale (uno spazio) le serie "A " + "" e "A" + " " darebbero la
        /// stessa chiave pur essendo diverse.
        /// </remarks>
        private const string ShapeKeySeparator = "\0";

        /// <summary>Crea la scomposizione di un nome numerato.</summary>
        /// <param name="prefix">Testo prima del numero.</param>
        /// <param name="number">Valore del numero.</param>
        /// <param name="digits">Con quante cifre era scritto (2 per "01"), per non perdere gli zeri davanti.</param>
        /// <param name="suffix">Testo dopo il numero.</param>
        public NumberedLayoutName(string prefix, int number, int digits, string suffix)
        {
            if (digits < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(digits), digits, "Un numero ha almeno una cifra.");
            }

            Prefix = prefix ?? string.Empty;
            Number = number;
            Digits = digits;
            Suffix = suffix ?? string.Empty;
        }

        /// <summary>Testo che precede il numero ("D_T_" in "D_T_01").</summary>
        public string Prefix { get; }

        /// <summary>Valore del numero (1 in "D_T_01").</summary>
        public int Number { get; }

        /// <summary>Quante cifre aveva il numero scritto (2 in "D_T_01"), zeri davanti compresi.</summary>
        public int Digits { get; }

        /// <summary>Testo che segue il numero (vuoto in "D_T_01").</summary>
        public string Suffix { get; }

        /// <summary>
        /// Ricostruisce il nome sostituendo il numero, mantenendo gli zeri davanti.
        /// Con "D_T_01" come modello il numero 7 diventa "D_T_07", e il numero 123 diventa
        /// "D_T_123": il numero non viene mai troncato per rispettare le cifre.
        /// </summary>
        /// <param name="number">Numero da scrivere al posto di quello originale.</param>
        /// <returns>Il nome completo.</returns>
        public string Format(int number)
        {
            string text = number.ToString(CultureInfo.InvariantCulture);
            return Prefix + text.PadLeft(Digits, '0') + Suffix;
        }

        /// <summary>
        /// true se i due nomi appartengono alla stessa serie, cioe' hanno lo stesso testo
        /// prima e dopo il numero. Il numero di cifre NON conta, cosi' "Tav_9" e "Tav_10"
        /// restano parenti.
        /// </summary>
        /// <param name="other">L'altro nome scomposto.</param>
        /// <returns>true se sono della stessa serie.</returns>
        public bool HasSameShapeAs(NumberedLayoutName other)
        {
            return other != null
                && string.Equals(Prefix, other.Prefix, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Suffix, other.Suffix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Chiave che identifica la serie, per raggruppare i nomi parenti.</summary>
        /// <returns>Prefisso e suffisso uniti da un separatore impossibile in un nome vero.</returns>
        public string GetShapeKey() => Prefix + ShapeKeySeparator + Suffix;
    }
}
