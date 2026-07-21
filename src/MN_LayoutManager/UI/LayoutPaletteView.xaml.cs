using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace MN_LayoutManager.UI
{
    /// <summary>
    /// La parte visiva della palette.
    /// Qui dentro sta SOLO quello che riguarda mouse e tastiera (trascinamento,
    /// doppio clic, casella di rinomina). Tutte le decisioni sui layout stanno nel
    /// "cervello" <see cref="LayoutPaletteViewModel"/>.
    /// </summary>
    public partial class LayoutPaletteView : UserControl
    {
        /// <summary>Formato usato per il trascinamento interno all'elenco.</summary>
        private const string DragFormat = "MN_LayoutManager.LayoutDrag";

        private Point _mouseDownPoint;
        private bool _mouseDownOnItem;

        /// <summary>Crea la palette.</summary>
        public LayoutPaletteView()
        {
            InitializeComponent();
        }

        private LayoutPaletteViewModel ViewModel => DataContext as LayoutPaletteViewModel;

        // ==================== Selezione e doppio clic ====================

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel?.UpdateCommandStates();
        }

        private void OnListDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Il doppio clic dentro la casella di rinomina serve a selezionare una parola:
            // non deve attivare il layout.
            if (FindAncestor<TextBox>(e.OriginalSource as DependencyObject) != null)
            {
                return;
            }

            if (FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject) == null)
            {
                return;
            }

            LayoutPaletteViewModel viewModel = ViewModel;
            if (viewModel != null && viewModel.ActivateCommand.CanExecute(null))
            {
                viewModel.ActivateCommand.Execute(null);
            }
        }

        // ==================== Rinomina dentro la riga ====================

        private void OnEditBoxVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(sender is TextBox textBox) || !(e.NewValue is bool visible) || !visible)
            {
                return;
            }

            // Il fuoco va dato DOPO che WPF ha finito di disegnare la casella,
            // altrimenti la richiesta si perde e l'utente deve cliccarci sopra.
            textBox.Dispatcher.BeginInvoke(
                DispatcherPriority.Input,
                new Action(() =>
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }));
        }

        private void OnEditBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (!(sender is TextBox textBox) || !(textBox.DataContext is LayoutItemViewModel item))
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Enter:
                    e.Handled = true;
                    ViewModel?.CommitRename(item);
                    FocusList();
                    break;

                case Key.Escape:
                    e.Handled = true;
                    item.CancelEdit();
                    FocusList();
                    break;

                case Key.F2:
                    // Si sta gia' rinominando: F2 non deve ripartire da capo.
                    e.Handled = true;
                    break;

                default:
                    break;
            }
        }

        private void OnEditBoxLostFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is LayoutItemViewModel item)
            {
                // Come in Esplora risorse: cliccare altrove conferma il nome scritto.
                ViewModel?.CommitRename(item);
            }
        }

        private void FocusList()
        {
            LayoutList.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => LayoutList.Focus()));
        }

        // ==================== Trascinamento per riordinare ====================

        private void OnListPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mouseDownPoint = e.GetPosition(LayoutList);
            _mouseDownOnItem = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject) != null
                && FindAncestor<TextBox>(e.OriginalSource as DependencyObject) == null;
        }

        private void OnListPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_mouseDownOnItem || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point current = e.GetPosition(LayoutList);
            if (Math.Abs(current.X - _mouseDownPoint.X) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(current.Y - _mouseDownPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            _mouseDownOnItem = false;

            if (LayoutList.SelectedItems.Count == 0)
            {
                return;
            }

            DragDrop.DoDragDrop(LayoutList, new DataObject(DragFormat, true), DragDropEffects.Move);
            HideDropIndicator();
        }

        private void OnListDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DragFormat))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            e.Effects = DragDropEffects.Move;
            e.Handled = true;
            ShowDropIndicator(GetInsertIndex(e.GetPosition(LayoutList)));
        }

        private void OnListDragLeave(object sender, DragEventArgs e) => HideDropIndicator();

        private void OnListDrop(object sender, DragEventArgs e)
        {
            HideDropIndicator();

            if (!e.Data.GetDataPresent(DragFormat))
            {
                return;
            }

            e.Handled = true;

            int insertIndex = GetInsertIndex(e.GetPosition(LayoutList));

            var selectedIndexes = new List<int>();
            for (int i = 0; i < LayoutList.Items.Count; i++)
            {
                if (LayoutList.Items[i] is LayoutItemViewModel item && item.IsSelected)
                {
                    selectedIndexes.Add(i);
                }
            }

            ViewModel?.ReorderSelection(selectedIndexes, insertIndex);
        }

        /// <summary>
        /// Traduce la posizione del mouse nella posizione di inserimento fra due righe.
        /// Sopra la meta' di una riga si inserisce prima, sotto si inserisce dopo.
        /// </summary>
        private int GetInsertIndex(Point positionInList)
        {
            for (int i = 0; i < LayoutList.Items.Count; i++)
            {
                if (!(LayoutList.ItemContainerGenerator.ContainerFromIndex(i) is ListBoxItem container)
                    || !container.IsVisible)
                {
                    continue;
                }

                Point topLeft = container.TranslatePoint(new Point(0, 0), LayoutList);
                double middle = topLeft.Y + (container.ActualHeight / 2);

                if (positionInList.Y < middle)
                {
                    return i;
                }
            }

            return LayoutList.Items.Count;
        }

        private void ShowDropIndicator(int insertIndex)
        {
            double y;

            if (LayoutList.Items.Count == 0)
            {
                y = 2;
            }
            else if (insertIndex >= LayoutList.Items.Count)
            {
                ListBoxItem last = GetContainer(LayoutList.Items.Count - 1);
                if (last == null)
                {
                    HideDropIndicator();
                    return;
                }

                y = last.TranslatePoint(new Point(0, 0), DropLayer).Y + last.ActualHeight;
            }
            else
            {
                ListBoxItem target = GetContainer(insertIndex);
                if (target == null)
                {
                    HideDropIndicator();
                    return;
                }

                y = target.TranslatePoint(new Point(0, 0), DropLayer).Y;
            }

            DropIndicator.Width = Math.Max(0, LayoutList.ActualWidth - 8);
            Canvas.SetLeft(DropIndicator, 4);
            Canvas.SetTop(DropIndicator, Math.Max(0, y - 1));
            DropIndicator.Visibility = Visibility.Visible;
        }

        private void HideDropIndicator() => DropIndicator.Visibility = Visibility.Collapsed;

        private ListBoxItem GetContainer(int index)
        {
            if (index < 0 || index >= LayoutList.Items.Count)
            {
                return null;
            }

            return LayoutList.ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem;
        }

        private static T FindAncestor<T>(DependencyObject start)
            where T : DependencyObject
        {
            DependencyObject current = start;
            while (current != null)
            {
                if (current is T match)
                {
                    return match;
                }

                current = current is Visual || current is Visual3D
                    ? VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}
