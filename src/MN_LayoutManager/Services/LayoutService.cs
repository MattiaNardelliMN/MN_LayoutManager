using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using MN_LayoutManager.Core;
using AcadLayoutManager = Autodesk.AutoCAD.DatabaseServices.LayoutManager;

namespace MN_LayoutManager.Services
{
    /// <summary>
    /// Tutte le operazioni sui layout che passano dalle API AutoCAD.
    /// E' l'unico modulo che parla con il database del disegno: se le API cambiano,
    /// si corregge qui e il resto del plugin non se ne accorge.
    /// </summary>
    /// <remarks>
    /// Regola fissa: ogni modifica al disegno avviene dentro
    /// <c>using (doc.LockDocument())</c> + <c>using (Transaction)</c> con Commit finale.
    /// Il blocco del documento serve perche' queste chiamate arrivano dalla palette,
    /// cioe' da fuori un comando AutoCAD, dove il documento non e' bloccato da solo.
    /// </remarks>
    public static class LayoutService
    {
        private const string DefaultLayoutBaseName = "Layout";
        private const int MaxAutoNameAttempts = 10000;

        /// <summary>
        /// Legge l'elenco dei layout carta nell'ordine delle schede.
        /// Lo spazio Modello viene escluso perche' non e' un layout stampabile gestibile qui.
        /// </summary>
        /// <param name="document">Disegno da leggere.</param>
        /// <returns>I layout ordinati come nelle schede in basso.</returns>
        public static IReadOnlyList<LayoutInfo> GetLayouts(Document document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            string currentName = GetCurrentLayoutName();
            var layouts = new List<LayoutInfo>();

            // La lettura non modifica niente: non serve bloccare il documento.
            using (Transaction tr = document.Database.TransactionManager.StartTransaction())
            {
                var dictionary = (DBDictionary)tr.GetObject(document.Database.LayoutDictionaryId, OpenMode.ForRead);

                foreach (DBDictionaryEntry entry in dictionary)
                {
                    var layout = (Layout)tr.GetObject(entry.Value, OpenMode.ForRead);

                    if (string.Equals(layout.LayoutName, LayoutNameValidator.ModelSpaceName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    bool isCurrent = string.Equals(layout.LayoutName, currentName, StringComparison.OrdinalIgnoreCase);
                    layouts.Add(new LayoutInfo(layout.LayoutName, layout.TabOrder, isCurrent));
                }

                tr.Commit();
            }

            layouts.Sort(CompareByTabOrder);
            return layouts;
        }

        /// <summary>Nome del layout attualmente visualizzato, o stringa vuota se non determinabile.</summary>
        /// <returns>Nome del layout corrente.</returns>
        public static string GetCurrentLayoutName()
        {
            AcadLayoutManager manager = AcadLayoutManager.Current;
            return manager == null ? string.Empty : manager.CurrentLayout ?? string.Empty;
        }

        /// <summary>Rende attivo un layout nel disegno (equivale a cliccare la sua scheda).</summary>
        /// <param name="document">Disegno su cui agire.</param>
        /// <param name="layoutName">Layout da attivare.</param>
        public static void ActivateLayout(Document document, string layoutName)
        {
            ValidateArguments(document, layoutName);

            using (document.LockDocument())
            {
                AcadLayoutManager.Current.CurrentLayout = layoutName;
            }
        }

        /// <summary>
        /// Rinomina un layout usando la funzione nativa di AutoCAD, che tiene allineati
        /// dizionario, ordine delle schede ed eventi. Impostare il nome a mano non lo farebbe.
        /// </summary>
        /// <param name="document">Disegno su cui agire.</param>
        /// <param name="oldName">Nome attuale.</param>
        /// <param name="newName">Nome nuovo.</param>
        public static void RenameLayout(Document document, string oldName, string newName)
        {
            ValidateArguments(document, oldName);

            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentException("Il nuovo nome non puo' essere vuoto.", nameof(newName));
            }

            using (document.LockDocument())
            {
                AcadLayoutManager.Current.RenameLayout(oldName, newName);
            }
        }

        /// <summary>
        /// Riscrive l'ordine delle schede in modo che corrisponda all'elenco ricevuto.
        /// Tutte le scritture avvengono in una sola transazione: o si applicano tutte, o nessuna.
        /// </summary>
        /// <param name="document">Disegno su cui agire.</param>
        /// <param name="orderedLayoutNames">Nomi dei layout nell'ordine desiderato.</param>
        public static void ReorderLayouts(Document document, IReadOnlyList<string> orderedLayoutNames)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (orderedLayoutNames == null)
            {
                throw new ArgumentNullException(nameof(orderedLayoutNames));
            }

            using (document.LockDocument())
            using (Transaction tr = document.Database.TransactionManager.StartTransaction())
            {
                var dictionary = (DBDictionary)tr.GetObject(document.Database.LayoutDictionaryId, OpenMode.ForRead);

                for (int i = 0; i < orderedLayoutNames.Count; i++)
                {
                    string name = orderedLayoutNames[i];
                    if (!dictionary.Contains(name))
                    {
                        // Il layout e' sparito nel frattempo (comando nativo, altro disegno):
                        // si salta, l'elenco verra' ricostruito subito dopo dagli eventi.
                        continue;
                    }

                    var layout = (Layout)tr.GetObject(dictionary.GetAt(name), OpenMode.ForWrite);

                    // Lo spazio Modello occupa sempre la posizione 0: i layout carta partono da 1.
                    layout.TabOrder = i + 1;
                }

                tr.Commit();
            }

            // Scrivere TabOrder non avvisa nessuno: senza questo la barra delle schede in
            // basso resterebbe nell'ordine vecchio. Il come sta tutto in un modulo a parte.
            LayoutTabRefresher.Refresh(document);
        }

        /// <summary>
        /// Duplica un layout con tutto il suo contenuto (finestre, entita', impostazioni
        /// di stampa) usando la funzione nativa di AutoCAD.
        /// </summary>
        /// <param name="document">Disegno su cui agire.</param>
        /// <param name="sourceLayoutName">Layout da copiare.</param>
        /// <param name="newLayoutName">Nome della copia.</param>
        public static void CopyLayout(Document document, string sourceLayoutName, string newLayoutName)
        {
            ValidateArguments(document, sourceLayoutName);

            if (string.IsNullOrWhiteSpace(newLayoutName))
            {
                throw new ArgumentException("Il nome della copia non puo' essere vuoto.", nameof(newLayoutName));
            }

            using (document.LockDocument())
            {
                AcadLayoutManager.Current.CopyLayout(sourceLayoutName, newLayoutName);
            }
        }

        /// <summary>
        /// Crea un layout nuovo e vuoto, con un nome libero del tipo "Layout3".
        /// </summary>
        /// <param name="document">Disegno su cui agire.</param>
        /// <returns>Il nome del layout appena creato.</returns>
        public static string CreateLayout(Document document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var existingNames = new List<string>();
            foreach (LayoutInfo info in GetLayouts(document))
            {
                existingNames.Add(info.Name);
            }

            string name = BuildAvailableName(existingNames);

            using (document.LockDocument())
            {
                AcadLayoutManager.Current.CreateLayout(name);
            }

            return name;
        }

        /// <summary>Elimina un layout. Operazione distruttiva: chiedere conferma prima di chiamarla.</summary>
        /// <param name="document">Disegno su cui agire.</param>
        /// <param name="layoutName">Layout da eliminare.</param>
        public static void DeleteLayout(Document document, string layoutName)
        {
            ValidateArguments(document, layoutName);

            using (document.LockDocument())
            {
                AcadLayoutManager.Current.DeleteLayout(layoutName);
            }
        }

        /// <summary>
        /// Esegue una rinomina multipla gia' calcolata e validata da
        /// <see cref="BatchRenamePlanner"/>.
        /// </summary>
        /// <param name="document">Disegno su cui agire.</param>
        /// <param name="plan">Piano valido da eseguire.</param>
        /// <returns>Quanti layout sono stati rinominati.</returns>
        public static int ApplyBatchRename(Document document, BatchRenamePlan plan)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (!plan.IsValid)
            {
                throw new InvalidOperationException("Il piano di rinomina contiene errori: non deve essere eseguito.");
            }

            if (plan.Steps.Count == 0)
            {
                return 0;
            }

            // Un solo blocco del documento per tutta l'operazione: cosi' nessun altro
            // comando puo' infilarsi a meta' lasciando i nomi in uno stato incoerente.
            using (document.LockDocument())
            {
                AcadLayoutManager manager = AcadLayoutManager.Current;

                if (plan.RequiresTemporaryNames)
                {
                    // Fase 1: tutti a nomi temporanei, cosi' nessun nome finale puo'
                    // scontrarsi con un nome ancora occupato.
                    var temporaryNames = new List<string>(plan.Steps.Count);
                    for (int i = 0; i < plan.Steps.Count; i++)
                    {
                        string temporary = BatchRenamePlanner.CreateTemporaryName(i);
                        manager.RenameLayout(plan.Steps[i].OriginalName, temporary);
                        temporaryNames.Add(temporary);
                    }

                    // Fase 2: dai nomi temporanei ai nomi definitivi.
                    for (int i = 0; i < plan.Steps.Count; i++)
                    {
                        manager.RenameLayout(temporaryNames[i], plan.Steps[i].FinalName);
                    }
                }
                else
                {
                    foreach (BatchRenameStep step in plan.Steps)
                    {
                        manager.RenameLayout(step.OriginalName, step.FinalName);
                    }
                }
            }

            return plan.Steps.Count;
        }

        /// <summary>Verifica se un layout esiste ancora nel disegno.</summary>
        /// <param name="document">Disegno da controllare.</param>
        /// <param name="layoutName">Nome da cercare.</param>
        /// <returns>true se il layout esiste.</returns>
        public static bool LayoutExists(Document document, string layoutName)
        {
            if (document == null || string.IsNullOrEmpty(layoutName))
            {
                return false;
            }

            using (Transaction tr = document.Database.TransactionManager.StartTransaction())
            {
                var dictionary = (DBDictionary)tr.GetObject(document.Database.LayoutDictionaryId, OpenMode.ForRead);
                bool exists = dictionary.Contains(layoutName);
                tr.Commit();
                return exists;
            }
        }

        private static string BuildAvailableName(IReadOnlyList<string> existingNames)
        {
            var taken = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);

            for (int i = 1; i <= MaxAutoNameAttempts; i++)
            {
                string candidate = DefaultLayoutBaseName + i.ToString(CultureInfo.InvariantCulture);
                if (!taken.Contains(candidate))
                {
                    return candidate;
                }
            }

            // Praticamente impossibile, ma meglio un nome buffo che un ciclo infinito.
            return DefaultLayoutBaseName + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture).Substring(0, 8);
        }

        private static int CompareByTabOrder(LayoutInfo left, LayoutInfo right)
        {
            int byOrder = left.TabOrder.CompareTo(right.TabOrder);

            // A parita' di posizione (disegni con TabOrder incoerenti) si ordina per nome,
            // cosi' l'elenco resta stabile invece di cambiare a ogni aggiornamento.
            return byOrder != 0
                ? byOrder
                : string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidateArguments(Document document, string layoutName)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (string.IsNullOrWhiteSpace(layoutName))
            {
                throw new ArgumentException("Il nome del layout non puo' essere vuoto.", nameof(layoutName));
            }
        }
    }
}
