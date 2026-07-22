using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using MN_LayoutManager.Core;
using MN_LayoutManager.Infrastructure;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using AcadLayoutManager = Autodesk.AutoCAD.DatabaseServices.LayoutManager;

namespace MN_LayoutManager.Services
{
    /// <summary>
    /// Costringe AutoCAD a ridisegnare la barra delle schede dei layout, in fondo alla
    /// finestra del disegno, quando l'ordine e' cambiato dalla palette.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Perche' serve un modulo apposta: cambiare <c>TabOrder</c> scrive l'ordine nuovo nel
    /// disegno ma non avvisa nessuno. AutoCAD ha gli eventi <c>LayoutsReordered</c> e
    /// <c>LayoutsRefresh</c>, ma sono eventi che si ASCOLTANO, non esiste nessun metodo
    /// pubblico per scatenarli: verificato ispezionando <c>acdbmgd.dll</c> di AutoCAD 2024.
    /// </para>
    /// <para>
    /// Percio' l'unica strada e' rifare quello che fa l'utente a mano quando la barra si
    /// sblocca: cambiare layout. Si passa un istante su un altro layout e si torna subito
    /// su quello di prima. Il tentativo precedente (rimettere il layout corrente su se
    /// stesso) non poteva funzionare: AutoCAD vede che l'identificativo non cambia e non
    /// fa niente.
    /// </para>
    /// <para>
    /// Costo: il passaggio momentaneo su un altro layout provoca una rigenerazione. Su
    /// disegni molto pesanti si puo' notare. E' il prezzo della correttezza finche'
    /// Autodesk non espone un metodo di aggiornamento.
    /// </para>
    /// </remarks>
    public static class LayoutTabRefresher
    {
        private const string OperationName = "Aggiornamento schede layout";

        /// <summary>
        /// Ridisegna la barra delle schede senza cambiare il layout che l'utente sta guardando.
        /// Non solleva mai eccezioni: se non riesce, lo scrive nel log e basta, perche' una
        /// barra non aggiornata e' un fastidio, non un motivo per far fallire il riordino.
        /// </summary>
        /// <param name="document">Disegno su cui agire.</param>
        public static void Refresh(Document document)
        {
            if (document == null)
            {
                return;
            }

            AcadContext.TryRun(OperationName, () => RefreshCore(document), out string error);

            if (error != null)
            {
                PluginLog.Warn(OperationName, "La barra delle schede potrebbe non essere aggiornata: " + error);
            }
        }

        private static void RefreshCore(Document document)
        {
            AcadLayoutManager manager = AcadLayoutManager.Current;
            if (manager == null)
            {
                return;
            }

            string currentName = manager.CurrentLayout;
            if (string.IsNullOrEmpty(currentName))
            {
                return;
            }

            ObjectId currentId = manager.GetLayoutId(currentName);
            if (currentId.IsNull)
            {
                return;
            }

            ObjectId otherId = FindAnyOtherLayoutId(document, currentName);
            if (otherId.IsNull)
            {
                // Un solo layout: non c'e' nessun ordine da mostrare, quindi niente da fare.
                return;
            }

            // Il cambio di layout modifica lo stato del documento: senza il blocco AutoCAD
            // solleverebbe un errore di violazione del blocco (eLockViolation).
            using (document.LockDocument())
            {
                manager.SetCurrentLayoutId(otherId);
                manager.SetCurrentLayoutId(currentId);
            }

            AcadApp.UpdateScreen();
        }

        /// <summary>
        /// Identificativo di un layout qualunque diverso da quello corrente, spazio Modello
        /// escluso. Restituisce l'identificativo nullo se non ce ne sono altri.
        /// </summary>
        private static ObjectId FindAnyOtherLayoutId(Document document, string currentName)
        {
            using (Transaction tr = document.Database.TransactionManager.StartTransaction())
            {
                var dictionary = (DBDictionary)tr.GetObject(document.Database.LayoutDictionaryId, OpenMode.ForRead);

                foreach (DBDictionaryEntry entry in dictionary)
                {
                    if (IsUsableAlternative(entry.Key, currentName))
                    {
                        ObjectId found = entry.Value;
                        tr.Commit();
                        return found;
                    }
                }

                tr.Commit();
            }

            return ObjectId.Null;
        }

        private static bool IsUsableAlternative(string layoutName, string currentName)
        {
            return !string.Equals(layoutName, currentName, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(layoutName, LayoutNameValidator.ModelSpaceName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
