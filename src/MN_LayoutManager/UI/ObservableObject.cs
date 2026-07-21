using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MN_LayoutManager.UI
{
    /// <summary>
    /// Base per gli oggetti che l'interfaccia osserva: quando una proprieta' cambia,
    /// avvisa la finestra cosi' il testo a video si aggiorna da solo.
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        /// <summary>Scatta quando una proprieta' e' cambiata.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Avvisa l'interfaccia che una proprieta' e' cambiata.</summary>
        /// <param name="propertyName">Nome della proprieta' (compilato in automatico).</param>
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Assegna un valore a un campo solo se e' davvero diverso, e in tal caso avvisa
        /// l'interfaccia. Evita aggiornamenti inutili dello schermo.
        /// </summary>
        /// <typeparam name="T">Tipo del valore.</typeparam>
        /// <param name="field">Campo da aggiornare.</param>
        /// <param name="value">Nuovo valore.</param>
        /// <param name="propertyName">Nome della proprieta' (compilato in automatico).</param>
        /// <returns>true se il valore e' cambiato.</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }
    }
}
