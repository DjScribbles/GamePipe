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
    /// Interaction logic for FriendView.xaml
    /// </summary>
    public partial class FriendView : UserControl
    {
        public FriendView()
        {
            InitializeComponent();
            DisplayList.PreviewMouseLeftButtonDown += DisplayList_PreviewMouseLeftButtonDown;
            DisplayList.PreviewMouseMove += DisplayList_PreviewMouseMove;
        }

        public class FriendTransfer
        {
            public readonly FriendViewModel SourceLibrary;
            public readonly RemoteSteamApp SourceGame;
            public FriendTransfer(FriendViewModel list, RemoteSteamApp item)
            {
                SourceLibrary = list;
                SourceGame = item;
            }
        }


        public const string DRAG_DATA_NAME = "FriendGame";
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
            if (e.LeftButton != MouseButtonState.Pressed || !(_mousePressOrigSource is DependencyObject))
            {
                return;
            }

            if (!(sender is ListBox) )
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
                if (listBoxItem != null && listBoxItem.DataContext is RemoteSteamApp)
                {
                    var item = listBoxItem.DataContext;

                    FriendTransfer transferData = new FriendTransfer((FriendViewModel)listBox.DataContext, (RemoteSteamApp)listBoxItem.DataContext);
                    DataObject dragData = new DataObject(DRAG_DATA_NAME, transferData);
                    DragDrop.DoDragDrop(listBoxItem, dragData, DragDropEffects.Move);

                }
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
