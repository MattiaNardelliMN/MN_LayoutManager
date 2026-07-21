using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using LayoutManagerPalette.Core;
using LayoutManagerPalette.Infrastructure;
using LayoutManagerPalette.Services;
using Microsoft.Win32;

namespace LayoutManagerPalette.UI
{
    /// <summary>
    /// Il "cervello" della palette: tiene l'elenco dei layout, sa cosa fare quando
    /// l'utente clicca, e non conosce nulla dell'aspetto grafico.
    /// La parte visiva (XAML) si limita a mostrare cio' che c'e' qui dentro.
    /// </summary>
    public sealed class LayoutPaletteViewModel : ObservableObject, IDisposable
    {
        private const string PdfExtension = ".pdf";
        private const string DwfExtension = ".dwf";
        private const string DwfxExtension = ".dwfx";
        private const int MaxCopyNameAttempts = 1000;

        private readonly List<RelayCommand> _commands = new List<RelayCommand>();

        private string _statusMessage = string.Empty;
        private string _documentName = string.Empty;
        private string _copiedLayoutName;
        private bool _isRefreshing;
        private bool _disposed;

        private string _batchFilter = string.Empty;
        private string _batchValue = string.Empty;
        private string _batchReplacement = string.Empty;
        private BatchRenameMode _batchMode = BatchRenameMode.AddPrefix;

        /// <summary>Crea il cervello della palette e si mette in ascolto dei cambiamenti.</summary>
        public LayoutPaletteViewModel()
        {
            Layouts = new ObservableCollection<LayoutItemViewModel>();

            ActivateCommand = Register(new RelayCommand(ActivateSelected, () => SelectedCount == 1));
            BeginRenameCommand = Register(new RelayCommand(BeginRenameSelected, () => SelectedCount == 1));
            NewLayoutCommand = Register(new RelayCommand(CreateNewLayout));
            CopyCommand = Register(new RelayCommand(CopySelected, () => SelectedCount == 1));
            PasteCommand = Register(new RelayCommand(PasteCopiedLayout, () => CanPaste));
            DeleteCommand = Register(new RelayCommand(DeleteSelected, () => SelectedCount > 0));
            SelectAllCommand = Register(new RelayCommand(SelectAll, () => Layouts.Count > 0));
            PageSetupCommand = Register(new RelayCommand(OpenPageSetup, () => SelectedCount == 1));
            PrintSelectedCommand = Register(new RelayCommand(() => Print(onlySelected: true), () => SelectedCount > 0));
            PrintAllCommand = Register(new RelayCommand(() => Print(onlySelected: false), () => Layouts.Count > 0));
            PublishSelectedCommand = Register(new RelayCommand(() => Publish(onlySelected: true), () => SelectedCount > 0));
            PublishAllCommand = Register(new RelayCommand(() => Publish(onlySelected: false), () => Layouts.Count > 0));
            ApplyBatchRenameCommand = Register(new RelayCommand(ApplyBatchRename, () => Layouts.Count > 0));

            LayoutChangeNotifier.LayoutsChanged += OnLayoutsChanged;
            Refresh();
        }

        /// <summary>Le righe mostrate nell'elenco, nell'ordine delle schede del disegno.</summary>
        public ObservableCollection<LayoutItemViewModel> Layouts { get; }

        /// <summary>Messaggio informativo mostrato in fondo alla palette.</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>Nome del disegno attivo, mostrato in cima alla palette.</summary>
        public string DocumentName
        {
            get => _documentName;
            private set => SetProperty(ref _documentName, value);
        }

        /// <summary>Testo del filtro della rinomina multipla ("agisci solo sui nomi che contengono...").</summary>
        public string BatchFilter
        {
            get => _batchFilter;
            set => SetProperty(ref _batchFilter, value);
        }

        /// <summary>Prefisso, suffisso o testo da cercare, secondo la modalita' scelta.</summary>
        public string BatchValue
        {
            get => _batchValue;
            set => SetProperty(ref _batchValue, value);
        }

        /// <summary>Testo sostitutivo, usato solo in modalita' Trova e sostituisci.</summary>
        public string BatchReplacement
        {
            get => _batchReplacement;
            set => SetProperty(ref _batchReplacement, value);
        }

        /// <summary>Modalita' "aggiungi prefisso" selezionata.</summary>
        public bool IsPrefixMode
        {
            get => _batchMode == BatchRenameMode.AddPrefix;
            set => SetBatchMode(value, BatchRenameMode.AddPrefix);
        }

        /// <summary>Modalita' "aggiungi suffisso" selezionata.</summary>
        public bool IsSuffixMode
        {
            get => _batchMode == BatchRenameMode.AddSuffix;
            set => SetBatchMode(value, BatchRenameMode.AddSuffix);
        }

        /// <summary>Modalita' "trova e sostituisci" selezionata.</summary>
        public bool IsFindReplaceMode
        {
            get => _batchMode == BatchRenameMode.FindReplace;
            set => SetBatchMode(value, BatchRenameMode.FindReplace);
        }

        /// <summary>Etichetta del campo principale, che cambia con la modalita'.</summary>
        public string BatchValueLabel
        {
            get
            {
                switch (_batchMode)
                {
                    case BatchRenameMode.AddPrefix:
                        return "Prefisso:";
                    case BatchRenameMode.AddSuffix:
                        return "Suffisso:";
                    default:
                        return "Trova:";
                }
            }
        }

        /// <summary>true se c'e' un layout copiato pronto da incollare.</summary>
        public bool CanPaste => !string.IsNullOrEmpty(_copiedLayoutName);

        /// <summary>Quanti layout sono selezionati.</summary>
        public int SelectedCount => Layouts.Count(item => item.IsSelected);

        /// <summary>Attiva il layout selezionato nel disegno.</summary>
        public RelayCommand ActivateCommand { get; }

        /// <summary>Avvia la rinomina inline del layout selezionato.</summary>
        public RelayCommand BeginRenameCommand { get; }

        /// <summary>Crea un layout nuovo e vuoto.</summary>
        public RelayCommand NewLayoutCommand { get; }

        /// <summary>Mette il layout selezionato negli appunti interni.</summary>
        public RelayCommand CopyCommand { get; }

        /// <summary>Crea una copia del layout negli appunti interni.</summary>
        public RelayCommand PasteCommand { get; }

        /// <summary>Elimina i layout selezionati, previa conferma.</summary>
        public RelayCommand DeleteCommand { get; }

        /// <summary>Seleziona tutti i layout dell'elenco.</summary>
        public RelayCommand SelectAllCommand { get; }

        /// <summary>Apre la finestra nativa delle impostazioni di pagina.</summary>
        public RelayCommand PageSetupCommand { get; }

        /// <summary>Stampa i layout selezionati.</summary>
        public RelayCommand PrintSelectedCommand { get; }

        /// <summary>Stampa tutti i layout.</summary>
        public RelayCommand PrintAllCommand { get; }

        /// <summary>Pubblica in PDF/DWF i layout selezionati.</summary>
        public RelayCommand PublishSelectedCommand { get; }

        /// <summary>Pubblica in PDF/DWF tutti i layout.</summary>
        public RelayCommand PublishAllCommand { get; }

        /// <summary>Applica la rinomina multipla.</summary>
        public RelayCommand ApplyBatchRenameCommand { get; }

        /// <summary>
        /// Rilegge l'elenco dei layout dal disegno attivo.
        /// La lettura dal disegno e' sempre la verita': l'elenco a video non viene mai
        /// "indovinato" dopo un'operazione, viene ricostruito.
        /// </summary>
        public void Refresh()
        {
            if (_isRefreshing)
            {
                return;
            }

            _isRefreshing = true;
            try
            {
                if (!AcadContext.TryGetActiveDocument(out Document document, out string error))
                {
                    Layouts.Clear();
                    DocumentName = string.Empty;
                    StatusMessage = error;
                    UpdateCommandStates();
                    return;
                }

                DocumentName = Path.GetFileName(document.Name);

                IReadOnlyList<LayoutInfo> layouts = null;
                if (!AcadContext.TryRun("Lettura layout", () => layouts = LayoutService.GetLayouts(document), out string readError))
                {
                    StatusMessage = readError;
                    UpdateCommandStates();
                    return;
                }

                MergeIntoList(layouts);
                StatusMessage = BuildCountMessage(layouts.Count);
            }
            finally
            {
                _isRefreshing = false;
                UpdateCommandStates();
            }
        }

        /// <summary>
        /// Sposta i layout selezionati nella posizione indicata (trascinamento) e scrive
        /// il nuovo ordine nel disegno.
        /// </summary>
        /// <param name="selectedIndexes">Posizioni attuali dei layout trascinati.</param>
        /// <param name="insertIndex">Posizione di rilascio, riferita all'elenco attuale.</param>
        public void ReorderSelection(IReadOnlyList<int> selectedIndexes, int insertIndex)
        {
            if (selectedIndexes == null || selectedIndexes.Count == 0)
            {
                return;
            }

            if (!AcadContext.TryGetEditableDocument(out Document document, out string error))
            {
                StatusMessage = error;
                return;
            }

            var currentNames = Layouts.Select(item => item.Name).ToList();
            IReadOnlyList<string> newOrder;

            try
            {
                newOrder = ReorderCalculator.Move(currentNames, selectedIndexes, insertIndex);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // L'elenco e' cambiato fra il trascinamento e il rilascio: si rilegge e basta.
                PluginLog.Warn("Riordino layout", "Elenco cambiato durante il trascinamento: " + ex.Message);
                Refresh();
                return;
            }

            if (!AcadContext.TryRun(
                "Riordino layout",
                () => LayoutService.ReorderLayouts(document, newOrder),
                out string reorderError))
            {
                StatusMessage = reorderError;
            }

            Refresh();
        }

        /// <summary>
        /// Conferma la rinomina inline di una riga: valida il nome e, se va bene, lo scrive nel disegno.
        /// </summary>
        /// <param name="item">Riga in modifica.</param>
        public void CommitRename(LayoutItemViewModel item)
        {
            if (item == null || !item.IsEditing)
            {
                return;
            }

            string newName = (item.EditingName ?? string.Empty).Trim();
            item.IsEditing = false;

            if (string.Equals(newName, item.Name, StringComparison.Ordinal))
            {
                return;
            }

            if (!AcadContext.TryGetEditableDocument(out Document document, out string error))
            {
                StatusMessage = error;
                item.CancelEdit();
                return;
            }

            var existingNames = Layouts.Select(layout => layout.Name).ToList();
            if (!LayoutNameValidator.TryValidateUnique(newName, existingNames, item.Name, out string validationError))
            {
                StatusMessage = validationError;
                UserDialogs.Warn(validationError);
                item.CancelEdit();
                return;
            }

            string oldName = item.Name;
            if (!AcadContext.TryRun(
                "Rinomina layout",
                () => LayoutService.RenameLayout(document, oldName, newName),
                out string renameError))
            {
                StatusMessage = renameError;
            }
            else
            {
                PluginLog.Info("Rinomina layout", Msg("\"{0}\" rinominato in \"{1}\".", oldName, newName));
            }

            Refresh();
        }

        /// <summary>Aggiorna lo stato attivo/grigio di tutti i comandi.</summary>
        public void UpdateCommandStates()
        {
            foreach (RelayCommand command in _commands)
            {
                command.RaiseCanExecuteChanged();
            }

            RaisePropertyChanged(nameof(SelectedCount));
            RaisePropertyChanged(nameof(CanPaste));
        }

        /// <summary>Smette di ascoltare i cambiamenti dei layout.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            LayoutChangeNotifier.LayoutsChanged -= OnLayoutsChanged;
        }

        private RelayCommand Register(RelayCommand command)
        {
            _commands.Add(command);
            return command;
        }

        private void OnLayoutsChanged(object sender, EventArgs e) => Refresh();

        private void SetBatchMode(bool isChecked, BatchRenameMode mode)
        {
            if (!isChecked || _batchMode == mode)
            {
                return;
            }

            _batchMode = mode;
            RaisePropertyChanged(nameof(IsPrefixMode));
            RaisePropertyChanged(nameof(IsSuffixMode));
            RaisePropertyChanged(nameof(IsFindReplaceMode));
            RaisePropertyChanged(nameof(BatchValueLabel));
        }

        /// <summary>
        /// Allinea l'elenco a video a quello letto dal disegno mantenendo selezione e
        /// rinomina in corso: ricostruire tutto da zero a ogni evento farebbe "saltare"
        /// la selezione dell'utente e chiuderebbe la casella di rinomina mentre scrive.
        /// </summary>
        private void MergeIntoList(IReadOnlyList<LayoutInfo> layouts)
        {
            bool sameOrder = layouts.Count == Layouts.Count;
            if (sameOrder)
            {
                for (int i = 0; i < layouts.Count; i++)
                {
                    if (!string.Equals(layouts[i].Name, Layouts[i].Name, StringComparison.Ordinal))
                    {
                        sameOrder = false;
                        break;
                    }
                }
            }

            if (sameOrder)
            {
                for (int i = 0; i < layouts.Count; i++)
                {
                    Layouts[i].IsCurrent = layouts[i].IsCurrent;
                }

                return;
            }

            var selectedNames = new HashSet<string>(
                Layouts.Where(item => item.IsSelected).Select(item => item.Name),
                StringComparer.Ordinal);

            string editingName = Layouts.FirstOrDefault(item => item.IsEditing)?.Name;

            Layouts.Clear();
            foreach (LayoutInfo info in layouts)
            {
                var item = new LayoutItemViewModel(info.Name, info.IsCurrent)
                {
                    IsSelected = selectedNames.Contains(info.Name),
                };

                if (string.Equals(info.Name, editingName, StringComparison.Ordinal))
                {
                    item.BeginEdit();
                }

                Layouts.Add(item);
            }
        }

        private void ActivateSelected()
        {
            LayoutItemViewModel item = GetSingleSelected();
            if (item == null)
            {
                return;
            }

            if (!AcadContext.TryGetEditableDocument(out Document document, out string error))
            {
                StatusMessage = error;
                return;
            }

            string name = item.Name;
            if (!AcadContext.TryRun(
                "Attivazione layout",
                () => LayoutService.ActivateLayout(document, name),
                out string activateError))
            {
                StatusMessage = activateError;
            }

            Refresh();
        }

        private void BeginRenameSelected()
        {
            LayoutItemViewModel item = GetSingleSelected();
            item?.BeginEdit();
        }

        private void CreateNewLayout()
        {
            if (!AcadContext.TryGetEditableDocument(out Document document, out string error))
            {
                StatusMessage = error;
                return;
            }

            string createdName = null;
            if (!AcadContext.TryRun(
                "Nuovo layout",
                () => createdName = LayoutService.CreateLayout(document),
                out string createError))
            {
                StatusMessage = createError;
                return;
            }

            Refresh();

            // Il layout nuovo viene selezionato e messo subito in rinomina, cosi' l'utente
            // puo' dargli un nome senza altri clic.
            LayoutItemViewModel created = Layouts.FirstOrDefault(
                item => string.Equals(item.Name, createdName, StringComparison.Ordinal));

            if (created != null)
            {
                foreach (LayoutItemViewModel item in Layouts)
                {
                    item.IsSelected = false;
                }

                created.IsSelected = true;
                created.BeginEdit();
                UpdateCommandStates();
            }
        }

        private void CopySelected()
        {
            LayoutItemViewModel item = GetSingleSelected();
            if (item == null)
            {
                return;
            }

            _copiedLayoutName = item.Name;
            StatusMessage = Msg("Layout \"{0}\" copiato: usa Incolla per duplicarlo.", item.Name);
            UpdateCommandStates();
        }

        private void PasteCopiedLayout()
        {
            if (!CanPaste)
            {
                return;
            }

            if (!AcadContext.TryGetEditableDocument(out Document document, out string error))
            {
                StatusMessage = error;
                return;
            }

            string source = _copiedLayoutName;
            if (!LayoutService.LayoutExists(document, source))
            {
                StatusMessage = Msg("Il layout \"{0}\" non esiste piu' in questo disegno.", source);
                _copiedLayoutName = null;
                UpdateCommandStates();
                return;
            }

            string newName = BuildCopyName(source);

            if (!AcadContext.TryRun(
                "Incolla layout",
                () => AcadCommandRunner.CopyLayout(document, source, newName),
                out string copyError))
            {
                StatusMessage = copyError;
                return;
            }

            // Il comando nativo viene messo in coda: l'elenco si aggiorna da solo
            // appena AutoCAD lo esegue e segnala la creazione del layout.
            StatusMessage = Msg("Copia di \"{0}\" in corso...", source);
        }

        private void DeleteSelected()
        {
            var names = Layouts.Where(item => item.IsSelected).Select(item => item.Name).ToList();
            if (names.Count == 0)
            {
                return;
            }

            string question = names.Count == 1
                ? Msg("Eliminare il layout \"{0}\"?\n\nL'operazione non si puo' annullare.", names[0])
                : Msg("Eliminare {0} layout selezionati?\n\nL'operazione non si puo' annullare.", names.Count);

            if (!UserDialogs.Confirm(question))
            {
                return;
            }

            if (!AcadContext.TryGetEditableDocument(out Document document, out string error))
            {
                StatusMessage = error;
                return;
            }

            int deleted = 0;
            var failures = new List<string>();

            foreach (string name in names)
            {
                if (AcadContext.TryRun(
                    "Elimina layout",
                    () => LayoutService.DeleteLayout(document, name),
                    out string deleteError))
                {
                    deleted++;
                }
                else
                {
                    failures.Add(name + ": " + deleteError);
                }
            }

            StatusMessage = failures.Count == 0
                ? Msg("Eliminati {0} layout.", deleted)
                : Msg("Eliminati {0} layout, {1} non eliminati.", deleted, failures.Count);

            if (failures.Count > 0)
            {
                UserDialogs.Warn("Alcuni layout non sono stati eliminati:\n\n" + string.Join("\n", failures));
            }

            Refresh();
        }

        private void SelectAll()
        {
            foreach (LayoutItemViewModel item in Layouts)
            {
                item.IsSelected = true;
            }

            UpdateCommandStates();
        }

        private void OpenPageSetup()
        {
            LayoutItemViewModel item = GetSingleSelected();
            if (item == null)
            {
                return;
            }

            if (!AcadContext.TryGetEditableDocument(out Document document, out string error))
            {
                StatusMessage = error;
                return;
            }

            // La finestra nativa lavora sul layout corrente: prima lo si attiva.
            string name = item.Name;
            if (!AcadContext.TryRun(
                "Impostazioni di pagina",
                () =>
                {
                    LayoutService.ActivateLayout(document, name);
                    AcadCommandRunner.OpenPageSetupManager(document);
                },
                out string pageSetupError))
            {
                StatusMessage = pageSetupError;
                return;
            }

            StatusMessage = Msg("Impostazioni di pagina di \"{0}\".", name);
        }

        private void Print(bool onlySelected)
        {
            IReadOnlyList<string> names = GetTargetNames(onlySelected);
            if (names.Count == 0)
            {
                StatusMessage = "Nessun layout da stampare.";
                return;
            }

            if (!TryPrepareForPublish(out Document document, out string error))
            {
                StatusMessage = error;
                return;
            }

            string question = Msg(
                "Stampare {0} layout sul plotter indicato nelle rispettive impostazioni di pagina?",
                names.Count);

            if (!UserDialogs.Confirm(question))
            {
                return;
            }

            if (!PublishService.TryPublish(document, names, PublishOutputKind.PageSetupPlotter, null, out string publishError))
            {
                StatusMessage = publishError;
                UserDialogs.Warn(publishError);
                return;
            }

            StatusMessage = Msg("Stampa di {0} layout avviata.", names.Count);
        }

        private void Publish(bool onlySelected)
        {
            IReadOnlyList<string> names = GetTargetNames(onlySelected);
            if (names.Count == 0)
            {
                StatusMessage = "Nessun layout da pubblicare.";
                return;
            }

            if (!TryPrepareForPublish(out Document document, out string error))
            {
                StatusMessage = error;
                return;
            }

            string suggested = PublishService.SuggestOutputPath(document, PdfExtension);
            var dialog = new SaveFileDialog
            {
                Title = "Pubblica layout",
                Filter = "PDF (*.pdf)|*.pdf|DWF (*.dwf)|*.dwf|DWFx (*.dwfx)|*.dwfx",
                FileName = Path.GetFileName(suggested),
                InitialDirectory = Path.GetDirectoryName(suggested) ?? string.Empty,
                OverwritePrompt = true,
                AddExtension = true,
                DefaultExt = PdfExtension,
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            PublishOutputKind kind = GetOutputKind(dialog.FileName);

            if (!PublishService.TryPublish(document, names, kind, dialog.FileName, out string publishError))
            {
                StatusMessage = publishError;
                UserDialogs.Warn(publishError);
                return;
            }

            StatusMessage = Msg("Pubblicazione di {0} layout avviata.", names.Count);
        }

        private void ApplyBatchRename()
        {
            if (!AcadContext.TryGetEditableDocument(out Document document, out string error))
            {
                StatusMessage = error;
                return;
            }

            var request = new BatchRenameRequest
            {
                Filter = BatchFilter,
                Mode = _batchMode,
                Value = BatchValue,
                ReplacementValue = BatchReplacement,
            };

            // La rinomina multipla agisce su TUTTI i layout che passano il filtro,
            // non solo su quelli selezionati: e' il comportamento richiesto.
            var allNames = Layouts.Select(item => item.Name).ToList();
            BatchRenamePlan plan = BatchRenamePlanner.CreatePlan(allNames, request);

            if (!plan.IsValid)
            {
                string message = "La rinomina multipla non e' stata eseguita:\n\n"
                    + string.Join("\n", plan.Errors.Take(10));
                StatusMessage = "Rinomina multipla annullata: ci sono conflitti nei nomi.";
                UserDialogs.Warn(message);
                return;
            }

            if (plan.IsEmpty)
            {
                StatusMessage = "Nessun layout corrisponde al filtro: niente da rinominare.";
                return;
            }

            if (!UserDialogs.Confirm(Msg("Rinominare {0} layout?", plan.Steps.Count)))
            {
                return;
            }

            int renamed = 0;
            if (!AcadContext.TryRun(
                "Rinomina multipla",
                () => renamed = LayoutService.ApplyBatchRename(document, plan),
                out string renameError))
            {
                StatusMessage = renameError;
                UserDialogs.Warn(renameError);
            }
            else
            {
                StatusMessage = Msg("Rinominati {0} layout.", renamed);
                PluginLog.Info("Rinomina multipla", StatusMessage);
            }

            Refresh();
        }

        private bool TryPrepareForPublish(out Document document, out string error)
        {
            if (!AcadContext.TryGetEditableDocument(out document, out error))
            {
                return false;
            }

            if (string.IsNullOrEmpty(PublishService.GetSavedDrawingPath(document)))
            {
                error = "Il disegno non e' ancora stato salvato: salvalo prima di stampare o pubblicare.";
                return false;
            }

            if (PublishService.HasUnsavedChanges()
                && !UserDialogs.Confirm(
                    "Il disegno ha modifiche non salvate.\n\n"
                    + "La stampa legge il file su disco, quindi le modifiche non salvate NON verranno stampate.\n\n"
                    + "Continuare lo stesso?"))
            {
                document = null;
                error = "Stampa annullata: salva il disegno e riprova.";
                return false;
            }

            return true;
        }

        private IReadOnlyList<string> GetTargetNames(bool onlySelected)
        {
            IEnumerable<LayoutItemViewModel> source = onlySelected
                ? Layouts.Where(item => item.IsSelected)
                : Layouts;

            return source.Select(item => item.Name).ToList();
        }

        private LayoutItemViewModel GetSingleSelected()
        {
            var selected = Layouts.Where(item => item.IsSelected).Take(2).ToList();
            return selected.Count == 1 ? selected[0] : null;
        }

        private string BuildCopyName(string sourceName)
        {
            var taken = new HashSet<string>(Layouts.Select(item => item.Name), StringComparer.OrdinalIgnoreCase);

            for (int i = 2; i < MaxCopyNameAttempts; i++)
            {
                string candidate = MsgFixed("{0} ({1})", sourceName, i);
                if (candidate.Length <= LayoutNameValidator.MaxLength && !taken.Contains(candidate))
                {
                    return candidate;
                }
            }

            return MsgFixed("{0}_{1}", sourceName, Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture).Substring(0, 6));
        }

        private static PublishOutputKind GetOutputKind(string fileName)
        {
            string extension = Path.GetExtension(fileName);

            if (string.Equals(extension, DwfExtension, StringComparison.OrdinalIgnoreCase))
            {
                return PublishOutputKind.Dwf;
            }

            if (string.Equals(extension, DwfxExtension, StringComparison.OrdinalIgnoreCase))
            {
                return PublishOutputKind.Dwfx;
            }

            return PublishOutputKind.Pdf;
        }

        private static string BuildCountMessage(int count)
        {
            switch (count)
            {
                case 0:
                    return "Nessun layout carta in questo disegno.";
                case 1:
                    return "1 layout.";
                default:
                    return Msg("{0} layout.", count);
            }
        }

        private static string Msg(string format, params object[] args) =>
            string.Format(CultureInfo.CurrentCulture, format, args);

        private static string MsgFixed(string format, params object[] args) =>
            string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
