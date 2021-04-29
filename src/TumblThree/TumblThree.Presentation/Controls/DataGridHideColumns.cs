﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace TumblThree.Presentation.Controls
{
    public static class DataGridHideColumns
    {
        public static readonly DependencyProperty HideColumnsHeaderProperty = DependencyProperty.RegisterAttached("HideColumnsHeader", typeof(object), typeof(DataGridHideColumns));

        public static object GetHideColumnsHeader(DataGrid obj)
        {
            return obj.GetValue(HideColumnsHeaderProperty);
        }

        public static void SetHideColumnsHeader(DataGrid obj, object value)
        {
            obj.SetValue(HideColumnsHeaderProperty, value);
        }

        public static readonly DependencyProperty HideColumnsHeaderTemplateProperty = DependencyProperty.RegisterAttached("HideColumnsHeaderTemplate", typeof(DataTemplate), typeof(DataGridHideColumns));

        public static DataTemplate GetHideColumnsHeaderTemplate(DataGrid obj)
        {
            return (DataTemplate)obj.GetValue(HideColumnsHeaderTemplateProperty);
        }

        public static void SetHideColumnsHeaderTemplate(DataGrid obj, DataTemplate value)
        {
            obj.SetValue(HideColumnsHeaderTemplateProperty, value);
        }

        public static readonly DependencyProperty HideColumnsIconProperty = DependencyProperty.RegisterAttached("HideColumnsIcon", typeof(object), typeof(DataGridHideColumns));

        public static object GetHideColumnsIcon(DataGrid obj)
        {
            return obj.GetValue(HideColumnsIconProperty);
        }

        public static void SetHideColumnsIcon(DataGrid obj, object value)
        {
            obj.SetValue(HideColumnsIconProperty, value);
        }

        public static readonly DependencyProperty CanUserHideColumnsProperty = DependencyProperty.RegisterAttached("CanUserHideColumns", typeof(bool), typeof(DataGridHideColumns), new UIPropertyMetadata(false, OnCanUserHideColumnsChanged));

        public static bool GetCanUserHideColumns(DataGrid obj)
        {
            return (bool)obj.GetValue(CanUserHideColumnsProperty);
        }

        public static void SetCanUserHideColumns(DataGrid obj, bool value)
        {
            obj.SetValue(CanUserHideColumnsProperty, value);
        }

        private static void OnCanUserHideColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DataGrid dataGrid = d as DataGrid;
            if (dataGrid == null)
            {
                return;
            }

            if ((bool)e.NewValue == false)
            {
                dataGrid.Loaded -= dataGrid_Loaded;
                RemoveAllItems(dataGrid);
                return;
            }

            if (!dataGrid.IsLoaded)
            {
                dataGrid.Loaded -= dataGrid_Loaded;
                dataGrid.Loaded += dataGrid_Loaded;
            }
            else
            {
                SetupColumnHeaders(dataGrid);
            }
        }

        private static void dataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            if (dataGrid == null)
            {
                return;
            }

            if (BindingOperations.IsDataBound(dataGrid, ItemsControl.ItemsSourceProperty))
            {
                Binding b = BindingOperations.GetBinding(dataGrid, ItemsControl.ItemsSourceProperty);
                dataGrid.TargetUpdated += new EventHandler<DataTransferEventArgs>(dataGrid_TargetUpdated);

                string xaml = XamlWriter.Save(b);
                Binding b2 = XamlReader.Parse(xaml) as Binding;
                if (b2 != null)
                {
                    b2.NotifyOnTargetUpdated = true;
                    BindingOperations.ClearBinding(dataGrid, ItemsControl.ItemsSourceProperty);
                    BindingOperations.SetBinding(dataGrid, ItemsControl.ItemsSourceProperty, b2);
                }
            }
            else
            {
                SetupColumnHeaders(dataGrid);
            }
        }

        private static void dataGrid_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            if (e.Property != ItemsControl.ItemsSourceProperty)
            {
                return;
            }

            DataGrid dataGrid = sender as DataGrid;
            if (dataGrid == null)
            {
                return;
            }

            EventHandler handler = null;
            handler = delegate
            {
                RemoveAllItems(dataGrid);
                if (SetupColumnHeaders(dataGrid))
                {
                    dataGrid.LayoutUpdated -= handler;
                }
            };

            dataGrid.LayoutUpdated += handler;
        }

        private static DataGridColumnHeader[] GetColumnHeaders(DataGrid dataGrid)
        {
            if (dataGrid == null)
            {
                return null;
            }

            dataGrid.UpdateLayout();
            DataGridColumnHeader[] columnHeaders = CustomVisualTreeHelper<DataGridColumnHeader>.FindChildrenRecursive(dataGrid);


            return (from DataGridColumnHeader columnHeader in columnHeaders
                    where columnHeader != null && columnHeader.Column != null
                    select columnHeader).ToArray();
        }

        private static string GetColumnName(DataGridColumn column)
        {
            if (column == null)
            {
                return string.Empty;
            }

            return column.Header != null ? column.Header.ToString() : $"Column {column.DisplayIndex}";
        }

        private static MenuItem GenerateItem(DataGrid dataGrid, DataGridColumn column)
        {
            if (column == null)
            {
                return null;
            }

            MenuItem item = new MenuItem
            {
                Tag = column,

                Header = GetColumnName(column)
            };
            if (string.IsNullOrEmpty(item.Header as string))
            {
                return null;
            }

            item.ToolTip = string.Format("Toggle column '{0}' visibility.", item.Header);

            item.IsCheckable = true;
            item.IsChecked = column.Visibility == Visibility.Visible;

            item.Checked += delegate
            {
                SetItemIsChecked(dataGrid, column, true);
            };

            item.Unchecked += delegate
            {
                SetItemIsChecked(dataGrid, column, false);
            };

            return item;
        }

        public static MenuItem[] GetAttachedItems(DataGridColumnHeader columnHeader)
        {
            if (columnHeader == null || columnHeader.ContextMenu == null)
            {
                return null;
            }

            ItemsControl itemsContainer = (from object i in columnHeader.ContextMenu.Items
                                           where i is MenuItem && ((MenuItem)i).Tag != null && ((MenuItem)i).Tag.ToString() == "ItemsContainer"
                                           select i).FirstOrDefault() as MenuItem;

            if (itemsContainer == null)
            {
                itemsContainer = columnHeader.ContextMenu;
            }

            return (from object i in itemsContainer.Items
                    where i is MenuItem && ((MenuItem)i).Tag is DataGridColumn
                    select i).Cast<MenuItem>().ToArray();
        }

        private static DataGridColumn GetColumnFromName(DataGrid dataGrid, string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                return null;
            }

            foreach (DataGridColumn column in dataGrid.Columns)
            {
                if (GetColumnName(column) == columnName)
                {
                    return column;
                }
            }

            return null;
        }

        private static DataGridColumnHeader GetColumnHeaderFromColumn(DataGrid dataGrid, DataGridColumn column)
        {
            if (dataGrid == null || column == null)
            {
                return null;
            }

            DataGridColumnHeader[] columnHeaders = GetColumnHeaders(dataGrid);
            return (from DataGridColumnHeader columnHeader in columnHeaders
                    where columnHeader.Column == column
                    select columnHeader).FirstOrDefault();
        }

        public static void RemoveAllItems(DataGrid dataGrid)
        {
            if (dataGrid == null)
            {
                return;
            }

            foreach (DataGridColumn column in dataGrid.Columns)
            {
                RemoveAllItems(dataGrid, column);
            }
        }

        public static void RemoveAllItems(DataGrid dataGrid, DataGridColumn column)
        {
            if (dataGrid == null || column == null)
            {
                return;
            }

            DataGridColumnHeader columnHeader = GetColumnHeaderFromColumn(dataGrid, column);
            List<MenuItem> itemsToRemove = new List<MenuItem>();

            if (columnHeader == null)
            {
                return;
            }

            // Mark items and/or items container for removal.
            if (columnHeader.ContextMenu != null)
            {
                foreach (object item in columnHeader.ContextMenu.Items)
                {
                    if (item is MenuItem && ((MenuItem)item).Tag != null
                        && (((MenuItem)item).Tag.ToString() == "ItemsContainer" || ((MenuItem)item).Tag is DataGridColumn))
                    {
                        itemsToRemove.Add((MenuItem)item);
                    }
                }
            }

            // Remove items and/or items container.
            foreach (MenuItem item in itemsToRemove)
            {
                columnHeader.ContextMenu.Items.Remove(item);
            }
        }

        public static void ResetupColumnHeaders(DataGrid dataGrid)
        {
            RemoveAllItems(dataGrid);
            SetupColumnHeaders(dataGrid);
        }

        private static void SetItemIsChecked(DataGrid dataGrid, DataGridColumn column, bool isChecked)
        {
            if (dataGrid == null || column == null)
            {
                return;
            }

            // Deny request if there are no other columns visible. Otherwise,
            // they'd have no way of changing the visibility of any columns
            // again.
            //if (!isChecked && (from DataGridColumn c in dataGrid.Columns
            //                   where c.Visibility == Visibility.Visible
            //                   select c).Count() < 2)
            //    return;

            if (isChecked && column.Visibility != Visibility.Visible)
            {
                ShowColumn(dataGrid, column);
            }
            else if (!isChecked)
            {
                column.Visibility = Visibility.Hidden;
            }

            DataGridColumnHeader[] columnHeaders = GetColumnHeaders(dataGrid);
            ItemsControl itemsContainer = null;
            object containerHeader = GetHideColumnsHeader(dataGrid);

            foreach (DataGridColumnHeader columnHeader in columnHeaders)
            {
                itemsContainer = null;
                if (columnHeader != null)
                {
                    if (columnHeader.ContextMenu == null)
                    {
                        continue;
                    }

                    itemsContainer = (from object i in columnHeader.ContextMenu.Items
                                      where i is MenuItem && ((MenuItem)i).Header == containerHeader
                                      select i).FirstOrDefault() as MenuItem;
                }

                if (itemsContainer == null)
                {
                    itemsContainer = columnHeader.ContextMenu;
                }

                foreach (object item in itemsContainer.Items)
                {
                    if (item is MenuItem && ((MenuItem)item).Tag != null && ((MenuItem)item).Tag is DataGridColumn
                        && ((MenuItem)item).Header.ToString() == GetColumnName(column))
                    {
                        ((MenuItem)item).IsChecked = isChecked;
                    }
                }
            }
        }

        private static void SetupColumnHeader(DataGridColumnHeader columnHeader)
        {
            if (columnHeader == null)
            {
                return;
            }

            DataGrid dataGrid = CustomVisualTreeHelper<DataGrid>.FindAncestor(columnHeader);
            if (dataGrid == null)
            {
                return;
            }

            DataGridColumnHeader[] columnHeaders = GetColumnHeaders(dataGrid);
            if (columnHeaders == null)
            {
                return;
            }

            SetupColumnHeader(dataGrid, columnHeaders, columnHeader);
        }

        private static void SetupColumnHeader(DataGrid dataGrid, DataGridColumnHeader[] columnHeaders, DataGridColumnHeader columnHeader)
        {
            if (columnHeader.ContextMenu == null)
            {
                columnHeader.ContextMenu = new ContextMenu();
            }

            ItemsControl itemsContainer = null;
            itemsContainer = columnHeader.ContextMenu;

            object containerHeader = GetHideColumnsHeader(dataGrid);
            if (containerHeader != null)
            {
                MenuItem ic = (from object i in columnHeader.ContextMenu.Items
                               where i is MenuItem && ((MenuItem)i).Tag != null && ((MenuItem)i).Tag.ToString() == "ItemsContainer"
                               select i).FirstOrDefault() as MenuItem;

                if (ic == null)
                {
                    itemsContainer = new MenuItem()
                    {
                        Header = containerHeader,
                        HeaderTemplate = GetHideColumnsHeaderTemplate(dataGrid),
                        Icon = GetHideColumnsIcon(dataGrid),
                        Tag = "ItemsContainer"
                    };
                    columnHeader.ContextMenu.Items.Add(itemsContainer);
                }
                else
                {
                    return;
                }
            }

            foreach (DataGridColumnHeader columnHeader2 in columnHeaders)
            {
                if (columnHeader2 != columnHeader
                    && itemsContainer is ContextMenu
                    && columnHeader2.ContextMenu == itemsContainer)
                {
                    continue;
                }

                itemsContainer.Items.Add(GenerateItem(dataGrid, columnHeader2.Column));
            }
        }

        public static bool SetupColumnHeaders(DataGrid dataGrid)
        {
            DataGridColumnHeader[] columnHeaders = GetColumnHeaders(dataGrid);
            if (columnHeaders == null || columnHeaders.Length == 0)
            {
                return false;
            }

            RemoveAllItems(dataGrid);
            columnHeaders = GetColumnHeaders(dataGrid);
            foreach (DataGridColumnHeader columnHeader in columnHeaders)
            {
                SetupColumnHeader(dataGrid, columnHeaders, columnHeader);
            }

            return true;
        }

        public static void LoadColumnChecks(DataGrid dataGrid)
        {
            if (dataGrid == null)
            {
                return;
            }

            foreach (DataGridColumn column in dataGrid.Columns)
            {
                DataGridColumnHeader columnHeader = GetColumnHeaderFromColumn(dataGrid, column);
                if (columnHeader == null)
                {
                    continue;
                }

                SyncItemsOnColumnHeader(columnHeader);
            }
        }

        /// <summary>
        /// Shows a column within the datagrid, which is not straightforward
        /// because the datagrid not only hides a column when you tell it to
        /// do so, but it also completely destroys its associated column
        /// header. Meaning we need to set it up again. Before we can do
        /// so we have to turn all columns back on again so we can get a
        /// complete list of their column headers, then turn them back off
        /// again.
        /// </summary>
        /// <param name="dataGrid"></param>
        /// <param name="column"></param>
        private static void ShowColumn(DataGrid dataGrid, DataGridColumn column)
        {
            if (dataGrid == null || column == null)
            {
                return;
            }

            column.Visibility = Visibility.Visible;

            // Turn all columns on, but store their original visibility so we
            // can restore it after we're done.
            Dictionary<DataGridColumn, Visibility> vis = new Dictionary<DataGridColumn, Visibility>();
            foreach (DataGridColumn c in dataGrid.Columns)
            {
                vis.Add(c, c.Visibility);
                c.Visibility = Visibility.Visible;
            }

            dataGrid.UpdateLayout();

            DataGridColumnHeader columnHeader = GetColumnHeaderFromColumn(dataGrid, column);
            SetupColumnHeader(columnHeader);

            foreach (DataGridColumn c in vis.Keys)
            {
                if (vis[c] != Visibility.Visible)
                {
                    c.Visibility = vis[c];
                }
            }

            dataGrid.UpdateLayout();

            // Now we need to uncheck items that are associated with hidden
            // columns.
            SyncItemsOnColumnHeader(columnHeader);
        }

        private static void SyncItemsOnColumnHeader(DataGridColumnHeader columnHeader)
        {
            bool isVisible;
            foreach (MenuItem item in GetAttachedItems(columnHeader))
            {
                if (item.Tag is DataGridColumn)
                {
                    isVisible = ((DataGridColumn)item.Tag).Visibility == Visibility.Visible ? true : false;
                    if (item.IsChecked != isVisible)
                    {
                        item.IsChecked = isVisible;
                    }
                }
            }
        }

        private static class CustomVisualTreeHelper<TReturn> where TReturn : DependencyObject
        {
            public static TReturn FindAncestor(DependencyObject descendant)
            {
                DependencyObject parent = descendant;
                while (parent != null && !(parent is TReturn))
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }

                if (parent != null)
                {
                    return (TReturn)parent;
                }

                return default(TReturn);
            }

            public static TReturn FindChild(DependencyObject parent)
            {
                int childCount = VisualTreeHelper.GetChildrenCount(parent);
                DependencyObject child = null;

                for (int childIndex = 0; childIndex < childCount; childIndex++)
                {
                    child = VisualTreeHelper.GetChild(parent, childIndex);
                    if (child is TReturn)
                    {
                        return (TReturn)child;
                    }
                }

                return default(TReturn);
            }

            public static TReturn FindChildRecursive(DependencyObject parent)
            {
                int childCount = VisualTreeHelper.GetChildrenCount(parent);
                DependencyObject child = null;

                for (int childIndex = 0; childIndex < childCount; childIndex++)
                {
                    child = VisualTreeHelper.GetChild(parent, childIndex);

                    if (child is TReturn)
                    {
                        return (TReturn)child;
                    }

                    child = FindChildRecursive(child);

                    if (child is TReturn)
                    {
                        return (TReturn)child;
                    }
                }

                return default(TReturn);
            }

            public static TReturn[] FindChildren(DependencyObject parent)
            {
                int childCount = VisualTreeHelper.GetChildrenCount(parent);
                DependencyObject child = null;
                List<TReturn> children = new List<TReturn>(childCount);

                for (int childIndex = 0; childIndex < childCount; childIndex++)
                {
                    child = VisualTreeHelper.GetChild(parent, childIndex);
                    if (child is TReturn)
                    {
                        children[childIndex] = (TReturn)child;
                    }
                }

                return children.ToArray();
            }

            public static TReturn[] FindChildrenRecursive(DependencyObject parent)
            {
                int childCount = VisualTreeHelper.GetChildrenCount(parent);
                DependencyObject child = null;
                List<TReturn> children = new List<TReturn>();

                for (int childIndex = 0; childIndex < childCount; childIndex++)
                {
                    child = VisualTreeHelper.GetChild(parent, childIndex);
                    if (child is TReturn)
                    {
                        children.Add((TReturn)child);
                    }

                    children.AddRange(FindChildrenRecursive(child));
                }

                return children.ToArray();
            }
        }
    }
}
