/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GamePipe.ViewModel;

namespace GamePipe.View
{
    /// <summary>
    /// Interaction logic for SteamLibraryView.xaml
    /// </summary>
    public partial class SteamLibraryView : UserControl
    {
        public SteamLibraryView()
        {
            InitializeComponent();
            DisplayList.PreviewMouseLeftButtonDown += DisplayList_PreviewMouseLeftButtonDown;
            DisplayList.PreviewMouseMove += DisplayList_PreviewMouseMove;

            this.DragEnter += DisplayList_DragEnter;
            this.Drop += DisplayList_Drop;
        }
        private class Transfer
        {
            public readonly SteamLibraryViewModel SourceLibrary;
            public readonly GameViewModel SourceGame;
            public Transfer(SteamLibraryViewModel list, GameViewModel item)
            {
                SourceLibrary = list;
                SourceGame = item;
            }
        }

        public const string DRAG_DATA_NAME = "LocalGame";
        private Point _startPoint;
        private object _mousePressOrigSource;
        private void DisplayList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
            _mousePressOrigSource = e.OriginalSource;
        }


        private void DisplayList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            //if the left mouse button is not pressed, then we can't be dragging anything
            if (e.LeftButton != MouseButtonState.Pressed || !(sender is ListBox) || !(_mousePressOrigSource is DependencyObject))
            {
                return;
            }

            var mousePos = e.GetPosition(null);
            var diff = _startPoint - mousePos;

            //If we travel far enough, begin the drag
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                //The sender is always the list box
                var listBox = (ListBox)sender;

                //Find a ListBoxItem ancestor of the mouse press original source (the top hit-detection item, not the item that handled the event)
                var listBoxItem = FindAncestor<ListBoxItem>((DependencyObject)_mousePressOrigSource);
                if (listBoxItem != null && listBoxItem.DataContext is GameViewModel)
                {
                    var item = listBoxItem.DataContext;
                    Transfer transferData = new Transfer((SteamLibraryViewModel)listBox.DataContext, (GameViewModel)listBoxItem.DataContext);
                    DataObject dragData = new DataObject(DRAG_DATA_NAME, transferData);

                    DragDrop.DoDragDrop(this, dragData, DragDropEffects.Move);

                }
            }

        }


        private void DisplayList_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DRAG_DATA_NAME) && !object.ReferenceEquals(sender, e.Source))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void DisplayList_Drop(object sender, DragEventArgs e)
        {
            var destination = (sender as FrameworkElement)?.DataContext as SteamLibraryViewModel;
            var sourceInfo = e.Data.GetData(DRAG_DATA_NAME) as Transfer;
            var source = sourceInfo.SourceLibrary;
            if (!object.ReferenceEquals(source, destination))
            {
                var shouldCopy = ((source is SteamArchiveViewModel && ((SteamArchiveViewModel)source).CopyInOut) ||
                                  (destination is SteamArchiveViewModel && ((SteamArchiveViewModel)destination).CopyInOut));

                var transfer = shouldCopy
                                ? new GamePipeLib.Model.LocalCopy(source.Model, destination.Model, sourceInfo.SourceGame.Model)
                                : new GamePipeLib.Model.LocalMove(source.Model, destination.Model, sourceInfo.SourceGame.Model);

                transfer.QueueTransfer();
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            if (current != null)
            {
                do
                {
                    //Search up the tree until we find a parent of type T, or run out of parents
                    if (current is T)
                    {
                        return (T)current;
                    }
                    current = System.Windows.Media.VisualTreeHelper.GetParent(current);
                } while (current != null);
            }
            return null;
        }
    }
}
