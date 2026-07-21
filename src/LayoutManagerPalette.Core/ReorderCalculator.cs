using System;
using System.Collections.Generic;

namespace LayoutManagerPalette.Core
{
    /// <summary>
    /// Calcola il nuovo ordine di una lista dopo un trascinamento (drag and drop).
    /// Logica pura: nessun riferimento ad AutoCAD o alla UI, quindi testabile a comando.
    /// </summary>
    public static class ReorderCalculator
    {
        /// <summary>
        /// Sposta gli elementi selezionati in una nuova posizione, mantenendo il loro
        /// ordine relativo e l'ordine di tutti gli altri.
        /// </summary>
        /// <typeparam name="T">Tipo degli elementi (di solito il nome del layout).</typeparam>
        /// <param name="items">Lista attuale, nell'ordine corrente.</param>
        /// <param name="selectedIndexes">Posizioni degli elementi trascinati.</param>
        /// <param name="insertIndex">
        /// Posizione di inserimento riferita alla lista ORIGINALE: 0 = prima di tutto,
        /// items.Count = dopo tutto. E' il punto dove appare la barra di inserimento.
        /// </param>
        /// <returns>Una nuova lista nell'ordine risultante. La lista di partenza non viene modificata.</returns>
        public static IReadOnlyList<T> Move<T>(
            IReadOnlyList<T> items,
            IReadOnlyList<int> selectedIndexes,
            int insertIndex)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (selectedIndexes == null)
            {
                throw new ArgumentNullException(nameof(selectedIndexes));
            }

            if (insertIndex < 0 || insertIndex > items.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(insertIndex),
                    insertIndex,
                    "La posizione di inserimento e' fuori dai limiti della lista.");
            }

            var moving = new SortedSet<int>();
            foreach (int index in selectedIndexes)
            {
                if (index < 0 || index >= items.Count)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(selectedIndexes),
                        index,
                        "Indice selezionato fuori dai limiti della lista.");
                }

                moving.Add(index);
            }

            if (moving.Count == 0)
            {
                return new List<T>(items);
            }

            var before = new List<T>();
            var after = new List<T>();
            var dragged = new List<T>();

            for (int i = 0; i < items.Count; i++)
            {
                if (moving.Contains(i))
                {
                    dragged.Add(items[i]);
                }
                else if (i < insertIndex)
                {
                    before.Add(items[i]);
                }
                else
                {
                    after.Add(items[i]);
                }
            }

            var result = new List<T>(items.Count);
            result.AddRange(before);
            result.AddRange(dragged);
            result.AddRange(after);
            return result;
        }
    }
}
