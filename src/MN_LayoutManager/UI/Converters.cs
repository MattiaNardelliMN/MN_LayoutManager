using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MN_LayoutManager.UI
{
    /// <summary>
    /// Traduce un vero/falso in "mostrato"/"nascosto" per l'interfaccia.
    /// Con <see cref="Invert"/> a true fa il contrario.
    /// </summary>
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>Se true, inverte il significato: vero diventa "nascosto".</summary>
        public bool Invert { get; set; }

        /// <summary>Da vero/falso a visibilita'.</summary>
        /// <param name="value">Valore booleano.</param>
        /// <param name="targetType">Non usato.</param>
        /// <param name="parameter">Non usato.</param>
        /// <param name="culture">Non usato.</param>
        /// <returns>Visible oppure Collapsed.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool booleanValue && booleanValue;
            if (Invert)
            {
                flag = !flag;
            }

            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>Non serve: il collegamento e' a senso unico.</summary>
        /// <param name="value">Non usato.</param>
        /// <param name="targetType">Non usato.</param>
        /// <param name="parameter">Non usato.</param>
        /// <param name="culture">Non usato.</param>
        /// <returns>Sempre <see cref="Binding.DoNothing"/>.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
