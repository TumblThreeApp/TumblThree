using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Waf.Applications;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Queue;
using TumblThree.Presentation.Controls;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    ///     Interaction logic for ManagerView.xaml.
    /// </summary>
    [Export(typeof(IManagerView))]
    public partial class ManagerView : IManagerView
    {
        private readonly Lazy<ManagerViewModel> viewModel;
        private List<IBlog> _selected = new List<IBlog>();
        private bool handledLeftMouseButton;

        public ManagerView()
        {
            InitializeComponent();
            viewModel = new Lazy<ManagerViewModel>(() => ViewHelper.GetViewModel<ManagerViewModel>(this));

            Loaded += LoadedHandler;
            blogFilesGrid.Sorting += BlogFilesGridSorting;
            blogFilesGrid.SelectionChanged += SelectionChanged;
            blogFilesGrid.PreviewMouseLeftButtonDown += DataGridPreviewMouseLeftButtonDown;
            blogFilesGrid.MouseLeftButtonUp += DataGridMouseLeftButtonUp;
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectionService.RemoveRange(e.RemovedItems.Cast<IBlog>());
            ViewModel.SelectionService.AddRange(e.AddedItems.Cast<IBlog>());
        }

        private ManagerViewModel ViewModel
        {
            get { return viewModel.Value; }
        }

        public Dictionary<object, Tuple<int, double, Visibility>> DataGridColumnRestore
        {
            get
            {
                var columnSettings = new Dictionary<object, Tuple<int, double, Visibility>>();
                foreach (DataGridColumn column in blogFilesGrid.Columns)
                {
                    var width = Math.Round(column.ActualWidth);
                    columnSettings.Add(GetName(column), Tuple.Create(column.DisplayIndex, width, column.Visibility));
                }

                return columnSettings;
            }

            set
            {
                foreach (var column in blogFilesGrid.Columns)
                {
                    value.TryGetValue(GetName(column), out var entry);

                    column.DisplayIndex = entry.Item1;
                    column.Width = new DataGridLength(entry.Item2, DataGridLengthUnitType.Pixel);
                    column.Visibility = entry.Item3;
                }

                DataGridHideColumns.LoadColumnChecks(blogFilesGrid);
            }
        }

        private string GetName(DataGridColumn column)
        {
            var findMatch = from field in this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                            let fieldValue = field.GetValue(this)
                            where fieldValue == column
                            select field.Name;
            return findMatch.FirstOrDefault();
        }

        private void LoadedHandler(object sender, RoutedEventArgs e)
        {
            FocusBlogFilesGrid();
        }

        private void BlogFilesGridSorting(object sender, DataGridSortingEventArgs e)
        {
            var collectionView = CollectionViewSource.GetDefaultView(blogFilesGrid.ItemsSource) as ListCollectionView;
            if (collectionView == null)
            {
                return;
            }

            if (collectionView.IsEditingItem && collectionView.CanCancelEdit)
            {
                collectionView.CancelEdit();
            }
            if (collectionView.IsAddingNew)
            {
                collectionView.CancelNew();
            }

            //ListSortDirection newDirection = e.Column.SortDirection == ListSortDirection.Ascending
            //    ? ListSortDirection.Descending
            //    : ListSortDirection.Ascending;
        }

        private void FocusBlogFilesGrid()
        {
            blogFilesGrid.Focus();
            blogFilesGrid.CurrentCell = new DataGridCellInfo(blogFilesGrid.SelectedItem, blogFilesGrid.Columns[0]);
        }

        private void DataGridRowContextMenuOpening(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).ContextMenu.DataContext = ViewModel;
        }

        private void DataGridRowMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.CrawlerService.EnqueueSelectedCommand.CanExecute(null))
            {
                ViewModel.CrawlerService.EnqueueSelectedCommand.Execute(null);
            }
        }

        private static DataGridRow GetClickedRow(object originalSource)
        {
            DependencyObject dep = (DependencyObject)originalSource;

            while ((dep != null) && !(dep is DataGridCell) && !(dep is DataGridColumnHeader))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null) return null;

            if (dep is DataGridCell)
            {
                DataGridCell cell = dep as DataGridCell;
                while ((dep != null) && !(dep is DataGridRow))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }
                DataGridRow row = dep as DataGridRow;
                return row;
            }
            return null;
        }

        private static DataGridCell GetClickedCell(object originalSource)
        {
            DependencyObject dep = (DependencyObject)originalSource;

            while ((dep != null) && !(dep is DataGridCell) && !(dep is DataGridColumnHeader))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null) return null;

            if (dep is DataGridCell)
            {
                return dep as DataGridCell;
            }
            return null;
        }

        private void DataGridPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.None) return;
            var blogFilesGrid = sender as DataGrid;
            if (blogFilesGrid == null) return;
            _selected.Clear();

            var row = GetClickedRow(e.OriginalSource);
            if (row != null && row.IsSelected)
                _selected.AddRange(blogFilesGrid.SelectedItems.Cast<IBlog>());
            if (_selected.Count > 1 && !handledLeftMouseButton)
            {
                handledLeftMouseButton = true;
                e.Handled = true;
            }
        }

        private void DataGridMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _selected.Clear();
            if (handledLeftMouseButton)
            {
                var blogFilesGrid = sender as DataGrid;
                if (blogFilesGrid == null) return;

                var row = GetClickedRow(e.OriginalSource);
                if (row == null) return;
                blogFilesGrid.SelectedItems.Clear();
                blogFilesGrid.SelectedItems.Add(row.DataContext);

                var cell = GetClickedCell(e.OriginalSource);
                cell.Focus();

                handledLeftMouseButton = false;
            }
        }

        private void DataGridRowMouseMove(object sender, MouseEventArgs e)
        {
            if (!ViewModel.ManagerService.IsDragOperationActive && e.LeftButton == MouseButtonState.Pressed)
            {
                var draggedItem = (DataGridRow)sender;

                if (draggedItem.IsEditing)
                {
                    return;
                }

                if (_selected.Count > 0) blogFilesGrid.SelectedItems.Clear();
                foreach (var blog in _selected)
                {
                    if (!blogFilesGrid.SelectedItems.Contains(blog))
                    {
                        blogFilesGrid.SelectedItems.Add(blog);
                    }
                }

                var collectionView = CollectionViewSource.GetDefaultView(blogFilesGrid.ItemsSource) as ListCollectionView;
                List<QueueListItem> items = collectionView.Cast<IBlog>().Select(x => new QueueListItem(x)).ToList();
                IEnumerable<QueueListItem> selectedItems = blogFilesGrid.SelectedItems
                    .Cast<IBlog>()
                    .OrderBy(x => items.FindIndex(f => f.Blog.ChildId == x.ChildId))
                    .Select(x => new QueueListItem(x));
                ViewModel.ManagerService.IsDragOperationActive = true;
                DragDrop.AddQueryContinueDragHandler(draggedItem, DataGridRowQueryContinueDrag);
                DragDrop.DoDragDrop(draggedItem, selectedItems.Select(x => x.Blog).ToArray(), DragDropEffects.Copy);
            }
        }

        private void DataGridRowQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            if (e.KeyStates == DragDropKeyStates.None)
            {
                ViewModel.ManagerService.IsDragOperationActive = false;
            }
        }
    }
}
