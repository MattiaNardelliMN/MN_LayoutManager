using System;
using Autodesk.AutoCAD.Windows;
using MN_LayoutManager.Infrastructure;
using MN_LayoutManager.Services;

namespace MN_LayoutManager.UI
{
    /// <summary>
    /// Crea e mostra la finestra agganciabile (palette) di AutoCAD che ospita l'interfaccia.
    /// Ne esiste una sola per sessione: riaprirla non ne crea un'altra.
    /// </summary>
    public static class LayoutPaletteHost
    {
        /// <summary>
        /// Identificativo fisso della palette. NON va mai cambiato: e' la chiave con cui
        /// AutoCAD ricorda posizione, dimensione e aggancio scelti dall'utente.
        /// </summary>
        private static readonly Guid PaletteSetId = new Guid("6C1F0A54-2D7B-4A31-9F0E-5C3B8E7A1D42");

        private const string PaletteTitle = "Gestione Layout";
        private const string PaletteName = "Layout";
        private const int MinimumWidth = 260;
        private const int MinimumHeight = 320;

        private static PaletteSet _paletteSet;
        private static LayoutPaletteViewModel _viewModel;
        private static PaletteShortcutInterceptor _shortcuts;

        /// <summary>Apre la palette se e' chiusa, la chiude se e' aperta.</summary>
        public static void Toggle()
        {
            if (!AcadContext.TryRun("Apertura palette", ToggleCore, out string error))
            {
                AcadContext.WriteMessage(error);
            }
        }

        /// <summary>
        /// Chiude la palette e libera le risorse. Va chiamata quando il plugin viene scaricato.
        /// </summary>
        public static void Shutdown()
        {
            AcadContext.TryRun("Chiusura palette", () =>
            {
                _shortcuts?.Dispose();
                _shortcuts = null;

                if (_paletteSet != null)
                {
                    _paletteSet.Visible = false;
                    _paletteSet.Dispose();
                    _paletteSet = null;
                }

                _viewModel?.Dispose();
                _viewModel = null;
            }, out _);
        }

        private static void ToggleCore()
        {
            if (_paletteSet == null)
            {
                Create();
                return;
            }

            _paletteSet.Visible = !_paletteSet.Visible;

            if (_paletteSet.Visible)
            {
                _viewModel?.Refresh();
            }
        }

        private static void Create()
        {
            _viewModel = new LayoutPaletteViewModel();

            var view = new LayoutPaletteView
            {
                DataContext = _viewModel,
            };

            _paletteSet = new PaletteSet(PaletteTitle, PaletteSetId)
            {
                Style = PaletteSetStyles.ShowAutoHideButton
                    | PaletteSetStyles.ShowCloseButton
                    | PaletteSetStyles.ShowPropertiesMenu
                    | PaletteSetStyles.Snappable,
                MinimumSize = new System.Drawing.Size(MinimumWidth, MinimumHeight),
                DockEnabled = DockSides.Left | DockSides.Right,

                // Senza questo la palette perde il fuoco a ogni clic e i tasti F2, Canc,
                // Ctrl+C e le frecce non arriverebbero all'elenco.
                KeepFocus = true,
            };

            _paletteSet.AddVisual(PaletteName, view);
            _paletteSet.Visible = true;

            // Va creato dopo che la palette e' visibile: prima l'elenco non esiste ancora
            // come elemento vero e non potrebbe ricevere il fuoco della tastiera.
            _shortcuts = new PaletteShortcutInterceptor(view.ShortcutTarget);

            PluginLog.Info("Apertura palette", "Palette Gestione Layout aperta.");
        }
    }
}
