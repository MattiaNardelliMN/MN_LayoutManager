using System;

namespace MN_LayoutManager.Services
{
    /// <summary>
    /// Fotografia di un layout letta dal disegno: solo dati, nessun comportamento.
    /// Serve a passare le informazioni dal disegno alla palette senza tenere aperti
    /// oggetti del database AutoCAD (che scadono appena la transazione si chiude).
    /// </summary>
    public sealed class LayoutInfo
    {
        /// <summary>Crea la fotografia di un layout.</summary>
        /// <param name="name">Nome del layout.</param>
        /// <param name="tabOrder">Posizione nella barra delle schede in basso.</param>
        /// <param name="isCurrent">true se e' il layout attualmente visualizzato.</param>
        public LayoutInfo(string name, int tabOrder, bool isCurrent)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            TabOrder = tabOrder;
            IsCurrent = isCurrent;
        }

        /// <summary>Nome del layout.</summary>
        public string Name { get; }

        /// <summary>Posizione nella barra delle schede (Model = 0, layout carta da 1 in poi).</summary>
        public int TabOrder { get; }

        /// <summary>true se e' il layout attivo nel disegno.</summary>
        public bool IsCurrent { get; }
    }
}
