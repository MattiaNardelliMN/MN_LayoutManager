using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using MN_LayoutManager.Core;
using MN_LayoutManager.Infrastructure;
using MN_LayoutManager.Services;

namespace MN_LayoutManager.UI
{
    /// <summary>
    /// Il "cervello" della palette: tiene l'elenco dei layout, sa cosa fare quando
    /// l'utente clicca, e non conosce nulla dell'aspetto grafico.
    /// La parte visiva (XAML) si limita a mostrare cio' che c'e' qui dentro.
    /// </summary>
    public sealed class LayoutPaletteViewModel : ObservableObject, IDisposable
    {
        /// <summary>Quante copie si possono chiedere in una volta sola.</summary>
        /// <remarks>
        /// Un tetto serve: senza, un errore di battitura ("500" invece di "5") creerebbe
        /// centinaia di layout, con una lunga attesa e nessun modo comodo di annullare.
        /// </remarks>
        public const int MaxCopiesPerOperation = 100;

        private const int MaxLayoutsListedInConfirm = 8;

        private readonly List<RelayCommand> _commands = new List<RelayCommand>();

        private string _statusMessage = string.Empty;
        private string _documentName = string.Empty;
        private string _copiedLayoutName;
        private bool _isRefreshing;
        private bool _suspendItemNotifications;
        private bool _disposed;

        private string _outputFolder = string.Empty;
        private PublishOutputKind _publishFormat = PublishOutputKind.Pdf;

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
            DuplicateCommand = Register(new RelayCommand(DuplicateSelected, () => SelectedCount == 1));
            DeleteCommand = Register(new RelayCommand(DeleteSelected, () => SelectedCount > 0));
            SelectAllCommand = Register(new RelayCommand(SelectAll, () => Layouts.Count > 0));
            CheckAllCommand = Register(new RelayCommand(() => SetAllChecked(true), () => Layouts.Count > 0));
            UncheckAllCommand = Register(new RelayCommand(() => SetAllChecked(false), () => CheckedCount > 0));
            PageSetupCommand = Register(new RelayCommand(OpenPageSetup, () => SelectedCount == 1));
            PrintCommand = Register(new RelayCommand(PrintSelected, () => SelectedCount == 1));
            PublishSelectedCommand = Register(new RelayCommand(() => Publish(onlySelected: true), () => SelectedCount > 0));
            PublishAllCommand = Register(new RelayCommand(() => Publish(onlySelected: false), () => Layouts.Count > 0));
            ApplyBatchRenameCommand = Register(new RelayCommand(ApplyBatchRename, () => CheckedCount > 0));
            BrowseOutputFolderCommand = Register(new RelayCommand(BrowseOutputFolder));

            LayoutChangeNotifier.LayoutsChanged += OnLayoutsChanged;
            Refresh();
        }

        /// <summary>
        /// Chiede all'utente quante copie fare. E' un "innesto" sostituibile: nei test si
        /// puo' mettere una funzione finta, senza aprire nessuna finestra.
        /// </summary>
        /// <remarks>Riceve il nome del layout e una funzione che mostra l'anteprima dei nomi.</remarks>
        public Func<string, Func<int, string>, int?> AskCopyCount { get; set; } =
            (sourceName, preview) => DuplicateLayoutDialog.AskCount(sourceName, preview, MaxCopiesPerOperation);

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

        /// <summary>
        /// Cartella in cui finiscono le stampe e le pubblicazioni.
        /// Si puo' scrivere a mano o scegliere col bottone "Sfoglia".
        /// </summary>
        public string OutputFolder
        {
            get => _outputFolder;
            set => SetProperty(ref _outputFolder, value);
        }

        /// <summary>Formato PDF selezionato per la pubblicazione.</summary>
        public bool IsPdfFormat
        {
            get => _publishFormat == PublishOutputKind.Pdf;
            set => SetPublishFormat(value, PublishOutputKind.Pdf);
        }

        /// <summary>Formato DWF selezionato per la pubblicazione.</summary>
        public bool IsDwfFormat
        {
            get => _publishFormat == PublishOutputKind.Dwf;
            set => SetPublishFormat(value, PublishOutputKind.Dwf);
        }

        /// <summary>Formato DWFx selezionato per la pubblicazione.</summary>
        public bool IsDwfxFormat
        {
            get => _publishFormat == PublishOutputKind.Dwfx;
            set => SetPublishFormat(value, PublishOutputKind.Dwfx);
        }

        /// <summary>Apre la finestra di Windows per scegliere la cartella di destinazione.</summary>
        public RelayCommand BrowseOutputFolderCommand { get; }

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

        /// <summary>Modalita' "rimuovi prefisso" selezionata.</summary>
        public bool IsRemovePrefixMode
        {
            get => _batchMode == BatchRenameMode.RemovePrefix;
            set => SetBatchMode(value, BatchRenameMode.RemovePrefix);
        }

        /// <summary>Modalita' "rimuovi suffisso" selezionata.</summary>
        public bool IsRemoveSuffixMode
        {
            get => _batchMode == BatchRenameMode.RemoveSuffix;
            set => SetBatchMode(value, BatchRenameMode.RemoveSuffix);
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
                    case BatchRenameMode.RemovePrefix:
                    case BatchRenameMode.RemoveSuffix:
                        return "Da togliere:";
                    default:
                        return "Trova:";
                }
            }
        }

        /// <summary>
        /// Spiegazione, sotto i pulsanti, di su quali layout agira' la rinomina.
        /// E' scritta in chiaro perche' la rinomina multipla non si puo' annullare.
        /// </summary>
        public string BatchScopeHint
        {
            get
            {
                switch (CheckedCount)
                {
                    case 0:
                        return "Spunta nell'elenco i layout da rinominare.";
                    case 1:
                        return "Agira' su 1 layout spuntato.";
                    default:
                        return Msg("Agira' sui {0} layout spuntati.", CheckedCount);
                }
            }
        }

        /// <summary>Spiegazione della modalita' scelta.</summary>
        public string BatchModeHint
        {
            get
            {
                switch (_batchMode)
                {
                    case BatchRenameMode.RemovePrefix:
                        return "Toglie il testo solo ai layout spuntati che iniziano davvero cosi'.";
                    case BatchRenameMode.RemoveSuffix:
                        return "Toglie il testo solo ai layout spuntati che finiscono davvero cosi'.";
                    default:
                        return null;
                }
            }
        }

        /// <summary>Testo del bottone di stampa, che nomina il layout su cui agira'.</summary>
        public string PrintLabel
        {
            get
            {
                LayoutItemViewModel single = GetSingleSelected();
                return single == null
                    ? "Stampa (seleziona un layout)"
                    : Msg("Stampa \"{0}\"", single.Name);
            }
        }

        /// <summary>Testo del bottone "pubblica i selezionati", con quanti sono.</summary>
        public string PublishSelectedLabel => Msg("Pubblica selezionati ({0})", SelectedCount);

        /// <summary>Testo del bottone "pubblica tutti", con quanti sono.</summary>
        public string PublishAllLabel => Msg("Pubblica tutti ({0})", Layouts.Count);

        /// <summary>true se c'e' un layout copiato pronto da incollare.</summary>
        public bool CanPaste => !string.IsNullOrEmpty(_copiedLayoutName);

        /// <summary>Quanti layout sono selezionati (evidenziati).</summary>
        public int SelectedCount => Layouts.Count(item => item.IsSelected);

        /// <summary>Quanti layout hanno la casella spuntata.</summary>
        public int CheckedCount => Layouts.Count(item => item.IsChecked);

        /// <summary>Attiva il layout selezionato nel disegno.</summary>
        public RelayCommand ActivateCommand { get; }

        /// <summary>Avvia la rinomina inline del layout selezionato.</summary>
        public RelayCommand BeginRenameCommand { get; }

        /// <summary>Crea un layout nuovo, proponendo il nome successivo della serie.</summary>
        public RelayCommand NewLayoutCommand { get; }

        /// <summary>Mette il layout selezionato negli appunti interni.</summary>
        public RelayCommand CopyCommand { get; }

        /// <summary>Crea una copia del layout negli appunti interni. Si puo' ripetere.</summary>
        public RelayCommand PasteCommand { get; }

        /// <summary>Chiede quante copie fare del layout selezionato e le crea.</summary>
        public RelayCommand DuplicateCommand { get; }

        /// <summary>Elimina i layout selezionati, previa conferma.</summary>
        public RelayCommand DeleteCommand { get; }

        /// <summary>Seleziona (evidenzia) tutti i layout dell'elenco.</summary>
        public RelayCommand SelectAllCommand { get; }

        /// <summary>Spunta tutti i layout, per la rinomina multipla.</summary>
        public RelayCommand CheckAllCommand { get; }

        /// <summary>Toglie la spunta a tutti i layout.</summary>
        public RelayCommand UncheckAllCommand { get; }

        /// <summary>Apre la finestra nativa delle impostazioni di pagina.</summary>
        public RelayCommand PageSetupCommand { get; }

        /// <summary>Stampa il layout selezionato sul plotter delle sue impostazioni di pagina.</summary>
        public RelayCommand PrintCommand { get; }

        /// <summary>Pubblica in PDF/DWF i layout selezionati.</summary>
        public RelayCommand PublishSelectedCommand { get; }

        /// <summary>Pubblica in PDF/DWF tutti i layout.</summary>
        public RelayCommand PublishAllCommand { get; }

        /// <summary>Applica la rinomina multipla ai layout spuntati.</summary>
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
                    ClearList();
                    DocumentName = string.Empty;
                    StatusMessage = error;
                    return;
                }

                DocumentName = Path.GetFileName(document.Name);

                // Alla prima apertura si propone la cartella del disegno: nella maggior
                // parte dei casi e' quella giusta e l'utente non deve toccare niente.
                // Se l'ha gia' scelta lui, non viene mai sovrascritta.
                if (string.IsNullOrWhiteSpace(OutputFolder))
                {
                    OutputFolder = PublishService.SuggestOutputFolder(document);
                }

                IReadOnlyList<LayoutInfo> layouts = null;
                if (!AcadContext.TryRun("Lettura layout", () => layouts = LayoutService.GetLayouts(document), out string readError))
                {
                    StatusMessage = readError;
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

        /// <summary>Aggiorna lo stato attivo/grigio di tutti i comandi e le etichette che contano.</summary>
        public void UpdateCommandStates()
        {
            foreach (RelayCommand command in _commands)
            {
                command.RaiseCanExecuteChanged();
            }

            RaisePropertyChanged(nameof(SelectedCount));
            RaisePropertyChanged(nameof(CheckedCount));
            RaisePropertyChanged(nameof(CanPaste));
            RaisePropertyChanged(nameof(PrintLabel));
            RaisePropertyChanged(nameof(PublishSelectedLabel));
            RaisePropertyChanged(nameof(PublishAllLabel));
            RaisePropertyChanged(nameof(BatchScopeHint));
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
            ClearList();
        }

        private RelayCommand Register(RelayCommand command)
        {
            _commands.Add(command);
            return command;
        }

        private void OnLayoutsChanged(object sender, EventArgs e) => Refresh();

        /// <summary>
        /// Le righe avvisano quando cambiano selezione o spunta, cosi' i bottoni si
        /// accendono e si spengono da soli e i conteggi restano veri.
        /// </summary>
        private void OnItemChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_suspendItemNotifications)
            {
                return;
            }

            if (e.PropertyName == nameof(LayoutItemViewModel.IsSelected)
                || e.PropertyName == nameof(LayoutItemViewModel.IsChecked))
            {
                UpdateCommandStates();
            }
        }

        private void SetBatchMode(bool isChecked, BatchRenameMode mode)
        {
            if (!isChecked || _batchMode == mode)
            {
                return;
            }

            _batchMode = mode;
            RaisePropertyChanged(nameof(IsPrefixMode));
            RaisePropertyChanged(nameof(IsSuffixMode));
            RaisePropertyChanged(nameof(IsRemovePrefixMode));
            RaisePropertyChanged(nameof(IsRemoveSuffixMode));
            RaisePropertyChanged(nameof(IsFindReplaceMode));
            RaisePropertyChanged(nameof(BatchValueLabel));
            RaisePropertyChanged(nameof(BatchModeHint));
        }

        private void SetPublishFormat(bool isChecked, PublishOutputKind format)
        {
            if (!isChecked || _publishFormat == format)
            {
                return;
            }

            _publishFormat = format;
            RaisePropertyChanged(nameof(IsPdfFormat));
            RaisePropertyChanged(nameof(IsDwfFormat));
            RaisePropertyChanged(nameof(IsDwfxFormat));
        }

        private void BrowseOutputFolder()
        {
            string startingFolder = string.IsNullOrWhiteSpace(OutputFolder)
                ? SuggestFolderFromDocument()
                : OutputFolder;

            if (FolderPicker.TryPickFolder("Dove devono finire le stampe?", startingFolder, out string chosen))
            {
                OutputFolder = chosen;
            }
        }

        private string SuggestFolderFromDocument()
        {
            return AcadContext.TryGetActiveDocument(out Document document, out _)
                ? PublishService.SuggestOutputFolder(document)
                : string.Empty;
        }

        /// <summary>
        /// Allinea l'elenco a video a quello letto dal disegno mantenendo selezione, spunte
        /// e rinomina in corso: ricostruire tutto da zero a ogni evento farebbe "saltare"
        /// le scelte dell'utente e chiuderebbe la casella di rinomina mentre scrive.
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

            var checkedNames = new HashSet<string>(
                Layouts.Where(item => item.IsChecked).Select(item => item.Name),
                StringComparer.Ordinal);

            string editingName = Layouts.FirstOrDefault(item => item.IsEditing)?.Name;
            string editingText = Layouts.FirstOrDefault(item => item.IsEditing)?.EditingName;

            _suspendItemNotifications = true;
            try
            {
                ClearList();

                foreach (LayoutInfo info in layouts)
                {
                    var item = new LayoutItemViewModel(info.Name, info.IsCurrent)
                    {
                        IsSelected = selectedNames.Contains(info.Name),
                        IsChecked = checkedNames.Contains(info.Name),
                    };

                    if (string.Equals(info.Name, editingName, StringComparison.Ordinal))
                    {
                        item.BeginEdit();
                        item.EditingName = editingText;
                    }

                    AddItem(item);
                }
            }
            finally
            {
                _suspendItemNotifications = false;
            }
        }

        private void AddItem(LayoutItemViewModel item)
        {
            item.PropertyChanged += OnItemChanged;
            Layouts.Add(item);
        }

        /// <summary>
        /// Svuota l'elenco staccando gli ascoltatori: senza questo le righe vecchie
        /// resterebbero agganciate e continuerebbero a far ricalcolare i conteggi.
        /// </summary>
        private void ClearList()
        {
            foreach (LayoutItemViewModel item in Layouts)
            {
                item.PropertyChanged -= OnItemChanged;
            }

            Layouts.Clear();
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

        /// <summary>
        /// Crea un layout nuovo. Se fra i layout esistenti c'e' una progressione numerica
        /// (D_T_01, D_T_02...) il nome successivo viene PROPOSTO nella casella di rinomina:
        /// l'utente conferma con Invio o scrive quello che vuole. Non viene mai imposto.
        /// </summary>
        private void CreateNewLayout()
        {
            if (!AcadContext.TryGetEditableDocument(out Document document, out string error))
            {
                StatusMessage = error;
                return;
            }

            var existingNames = Layouts.Select(item => item.Name).ToList();
            bool hasSeries = LayoutNumbering.TryProposeNextInSeries(existingNames, out string proposedName);

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

            LayoutItemViewModel created = Layouts.FirstOrDefault(
                item => string.Equals(item.Name, createdName, StringComparison.Ordinal));

            if (created == null)
            {
                return;
            }

            SelectOnly(createdName);
            created.BeginEdit();

            if (hasSeries)
            {
                created.EditingName = proposedName;
                StatusMessage = Msg("Nome proposto \"{0}\": Invio per confermare, o scrivine un altro.", proposedName);
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
            StatusMessage = Msg("Layout \"{0}\" copiato: Ctrl+V lo duplica, anche piu' volte di seguito.", item.Name);
            UpdateCommandStates();
        }

        /// <summary>
        /// Incolla una copia del layout negli appunti. Si puo' ripetere quante volte si
        /// vuole: ogni Ctrl+V prosegue la numerazione (D_T_02, D_T_03, D_T_04...).
        /// </summary>
        private void PasteCopiedLayout() => CreateCopies(_copiedLayoutName, 1);

        /// <summary>Chiede quante copie fare del layout selezionato e le crea tutte insieme.</summary>
        private void DuplicateSelected()
        {
            LayoutItemViewModel item = GetSingleSelected();
            if (item == null)
            {
                return;
            }

            string source = item.Name;
            var existingNames = Layouts.Select(layout => layout.Name).ToList();

            int? requested = AskCopyCount(source, count => BuildCopyPreview(existingNames, source, count));
            if (requested == null)
            {
                return;
            }

            CreateCopies(source, requested.Value);
        }

        /// <summary>Anteprima dei nomi che verrebbero creati, mostrata nella finestra "Duplica".</summary>
        private static string BuildCopyPreview(IReadOnlyList<string> existingNames, string sourceName, int count)
        {
            if (count <= 0)
            {
                return "Indica quante copie servono.";
            }

            IReadOnlyList<string> names = LayoutNumbering.BuildCopyNames(existingNames, sourceName, count);

            return names.Count <= 3
                ? "Creera': " + string.Join(", ", names)
                : Msg("Creera': {0}, {1} ... {2}", names[0], names[1], names[names.Count - 1]);
        }

        /// <summary>
        /// Percorso unico per l'incolla e per la duplicazione multipla: cambia solo quante
        /// copie servono, tutto il resto (controlli, nomi, selezione finale) e' identico.
        /// </summary>
        private void CreateCopies(string sourceName, int count)
        {
            if (string.IsNullOrEmpty(sourceName) || count <= 0)
            {
                return;
            }

            if (count > MaxCopiesPerOperation)
            {
                StatusMessage = Msg("Si possono creare al massimo {0} copie per volta.", MaxCopiesPerOperation);
                return;
            }

            if (!AcadContext.TryGetEditableDocument(out Document document, out string error))
            {
                StatusMessage = error;
                return;
            }

            if (!LayoutService.LayoutExists(document, sourceName))
            {
                StatusMessage = Msg("Il layout \"{0}\" non esiste piu' in questo disegno.", sourceName);
                if (string.Equals(sourceName, _copiedLayoutName, StringComparison.Ordinal))
                {
                    _copiedLayoutName = null;
                }

                UpdateCommandStates();
                return;
            }

            var existingNames = Layouts.Select(item => item.Name).ToList();
            IReadOnlyList<string> newNames = LayoutNumbering.BuildCopyNames(existingNames, sourceName, count);

            var created = new List<string>(newNames.Count);
            string lastError = null;

            foreach (string newName in newNames)
            {
                if (AcadContext.TryRun(
                    "Copia layout",
                    () => LayoutService.CopyLayout(document, sourceName, newName),
                    out string copyError))
                {
                    created.Add(newName);
                }
                else
                {
                    // Ci si ferma al primo errore: insistere creerebbe una serie con buchi.
                    lastError = copyError;
                    break;
                }
            }

            Refresh();

            if (created.Count == 0)
            {
                StatusMessage = lastError ?? "Nessuna copia creata.";
                return;
            }

            StatusMessage = created.Count == 1
                ? Msg("Creata la copia \"{0}\".", created[0])
                : Msg("Create {0} copie, da \"{1}\" a \"{2}\".", created.Count, created[0], created[created.Count - 1]);

            if (lastError != null)
            {
                StatusMessage += " " + lastError;
            }

            // Restano selezionate le copie appena create: sono quelle su cui l'utente
            // vorra' agire subito (spostarle, rinominarle, stamparle).
            SelectOnly(created);
        }

        /// <summary>Lascia selezionato solo il layout indicato, se esiste ancora.</summary>
        private void SelectOnly(string layoutName) => SelectOnly(new[] { layoutName });

        /// <summary>Lascia selezionati solo i layout indicati, fra quelli ancora esistenti.</summary>
        private void SelectOnly(IReadOnlyList<string> layoutNames)
        {
            var wanted = new HashSet<string>(layoutNames, StringComparer.Ordinal);

            _suspendItemNotifications = true;
            try
            {
                foreach (LayoutItemViewModel item in Layouts)
                {
                    item.IsSelected = wanted.Contains(item.Name);
                }
            }
            finally
            {
                _suspendItemNotifications = false;
            }

            UpdateCommandStates();
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

        private void SelectAll() => SetAllSelected(true);

        private void SetAllSelected(bool selected)
        {
            _suspendItemNotifications = true;
            try
            {
                foreach (LayoutItemViewModel item in Layouts)
                {
                    item.IsSelected = selected;
                }
            }
            finally
            {
                _suspendItemNotifications = false;
            }

            UpdateCommandStates();
        }

        private void SetAllChecked(bool isChecked)
        {
            _suspendItemNotifications = true;
            try
            {
                foreach (LayoutItemViewModel item in Layouts)
                {
                    item.IsChecked = isChecked;
                }
            }
            finally
            {
                _suspendItemNotifications = false;
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

        /// <summary>
        /// Stampa il singolo layout selezionato. La stampa in blocco non passa di qui:
        /// per quella c'e' "Pubblica", che e' lo strumento pensato per i lotti di fogli.
        /// </summary>
        private void PrintSelected()
        {
            LayoutItemViewModel item = GetSingleSelected();
            if (item == null)
            {
                return;
            }

            StartPublish(new[] { item.Name }, PublishOutputKind.PageSetupPlotter);
        }

        private void Publish(bool onlySelected) => StartPublish(GetTargetNames(onlySelected), _publishFormat);

        /// <summary>
        /// Percorso comune di stampa e pubblicazione: cambiano solo il formato e il testo
        /// della conferma, tutto il resto (controlli, cartella, avvio) e' identico.
        /// </summary>
        private void StartPublish(IReadOnlyList<string> names, PublishOutputKind outputKind)
        {
            bool isPrint = outputKind == PublishOutputKind.PageSetupPlotter;
            string verb = isPrint ? "stampare" : "pubblicare";

            if (names.Count == 0)
            {
                StatusMessage = "Nessun layout da " + verb + ".";
                return;
            }

            if (!TryPrepareForPublish(out Document document, out string error))
            {
                StatusMessage = error;
                UserDialogs.Warn(error);
                return;
            }

            if (!PublishService.TryPrepareOutputFolder(OutputFolder, out string folderError))
            {
                StatusMessage = folderError;
                UserDialogs.Warn(folderError);
                return;
            }

            if (!UserDialogs.Confirm(BuildPublishConfirmation(document, names, outputKind, isPrint)))
            {
                return;
            }

            if (!PublishService.TryPublish(document, names, outputKind, OutputFolder, out string publishError))
            {
                StatusMessage = publishError;
                UserDialogs.Warn(publishError);
                return;
            }

            StatusMessage = isPrint
                ? Msg("Stampa di \"{0}\" avviata.", names[0])
                : Msg("Pubblicazione di {0} layout avviata in background.", names.Count);
        }

        /// <summary>
        /// Costruisce la domanda di conferma elencando i file che verranno creati,
        /// cosi' l'utente vede prima di confermare cosa otterra' e dove.
        /// </summary>
        private string BuildPublishConfirmation(
            Document document,
            IReadOnlyList<string> names,
            PublishOutputKind outputKind,
            bool isPrint)
        {
            var message = new StringBuilder();

            if (isPrint)
            {
                message.AppendLine(Msg(
                    "Stampare il layout \"{0}\" sul plotter indicato nelle sue impostazioni di pagina?",
                    names[0]));
                message.AppendLine();
                message.AppendLine("Se il plotter stampa su file, il file finira' in:");
                message.AppendLine(OutputFolder);
                return message.ToString();
            }

            var request = new PublishRequest(
                PublishService.GetSavedDrawingPath(document),
                names,
                outputKind,
                OutputFolder);

            message.AppendLine(Msg("Creare {0} file separati, uno per layout, in:", names.Count));
            message.AppendLine(OutputFolder);
            message.AppendLine();

            foreach (string name in names.Take(MaxLayoutsListedInConfirm))
            {
                message.AppendLine("  " + DsdFileBuilder.GetOutputFileName(request, name));
            }

            if (names.Count > MaxLayoutsListedInConfirm)
            {
                message.AppendLine(Msg("  ... e altri {0}.", names.Count - MaxLayoutsListedInConfirm));
            }

            message.AppendLine();
            message.AppendLine("I file gia' esistenti con lo stesso nome verranno sovrascritti.");
            return message.ToString();
        }

        private void ApplyBatchRename()
        {
            var targetNames = Layouts.Where(item => item.IsChecked).Select(item => item.Name).ToList();
            if (targetNames.Count == 0)
            {
                StatusMessage = "Spunta prima i layout da rinominare.";
                return;
            }

            if (!AcadContext.TryGetEditableDocument(out Document document, out string error))
            {
                StatusMessage = error;
                return;
            }

            var request = new BatchRenameRequest
            {
                TargetNames = targetNames,
                Mode = _batchMode,
                Value = BatchValue,
                ReplacementValue = BatchReplacement,
            };

            // Il piano riceve TUTTI i nomi, non solo quelli spuntati: serve per accorgersi
            // che un nome nuovo si scontrerebbe con un layout che non viene toccato.
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
                StatusMessage = "Nessuno dei layout spuntati cambierebbe nome.";
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
    }
}
