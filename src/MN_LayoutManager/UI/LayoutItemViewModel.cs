using System;

namespace MN_LayoutManager.UI
{
    /// <summary>
    /// Una riga dell'elenco: il layout come lo vede l'interfaccia.
    /// </summary>
    public sealed class LayoutItemViewModel : ObservableObject
    {
        private string _name;
        private string _editingName;
        private bool _isSelected;
        private bool _isEditing;
        private bool _isCurrent;

        /// <summary>Crea la riga.</summary>
        /// <param name="name">Nome del layout.</param>
        /// <param name="isCurrent">true se e' il layout attivo nel disegno.</param>
        public LayoutItemViewModel(string name, bool isCurrent)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _isCurrent = isCurrent;
        }

        /// <summary>Nome del layout come e' nel disegno.</summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// Testo scritto dall'utente durante la rinomina inline.
        /// Resta separato da <see cref="Name"/> finche' non si conferma: se l'utente
        /// annulla con Esc, il nome vero non viene toccato.
        /// </summary>
        public string EditingName
        {
            get => _editingName;
            set => SetProperty(ref _editingName, value);
        }

        /// <summary>true se la riga e' selezionata.</summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>true mentre l'utente sta scrivendo il nuovo nome dentro la riga.</summary>
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        /// <summary>true se e' il layout attualmente visualizzato nel disegno (mostrato in grassetto).</summary>
        public bool IsCurrent
        {
            get => _isCurrent;
            set => SetProperty(ref _isCurrent, value);
        }

        /// <summary>Prepara la rinomina inline partendo dal nome attuale.</summary>
        public void BeginEdit()
        {
            EditingName = Name;
            IsEditing = true;
        }

        /// <summary>Chiude la rinomina inline senza applicare nulla.</summary>
        public void CancelEdit()
        {
            IsEditing = false;
            EditingName = Name;
        }
    }
}
