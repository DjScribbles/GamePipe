/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GamePipe.ViewModel;
using System.Collections.Generic;
using System.Linq;

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
            DisplayList.PreviewMouseLeftButtonUp += DisplayList_PreviewMouseLeftButtonUp;
            DisplayList.SelectionChanged += DisplayList_SelectionChanged;
            DisplayList.PreviewMouseMove += DisplayList_PreviewMouseMove;
        }


        public const string DRAG_DATA_NAME = "FriendGame";
        private Point _startPoint;
        private List<object> _selItems = new List<object>();
        private RemoteSteamApp _clickedGame = null;
        private void DisplayList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
            _clickedGame = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource)?.DataContext as RemoteSteamApp;
            if (_selItems.Any() && ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == 0))
            {
                if (!_selItems.Contains(_clickedGame))
                    _selItems.Clear();
            }
        }

        private void DisplayList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _clickedGame = null;
            _selItems.Clear();
            _selItems.AddRange(DisplayList.SelectedItems.Cast<object>());
        }

        private void DisplayList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_clickedGame != null)
            {
                if (e.AddedItems.Count > 0)
                {

                    var removals = new List<object>();
                    foreach (var item in e.AddedItems)
                    {
                        if (item != _clickedGame)
                        {
                            removals.Add(item);
                        }
                    }
                    foreach (var item in removals)
                        DisplayList.SelectedItems.Remove(item);
                }
                if (e.RemovedItems.Contains(_clickedGame))
                    DisplayList.SelectedItems.Add(_clickedGame); 

            }
        }
        private void DisplayList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            //if the left mouse button is not pressed, then we can't be dragging anything
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            if (!(sender is ListBox))
            {
                return;
            }

            var mousePos = e.GetPosition(null);
            var diff = _startPoint - mousePos;

            //If we travel far enough, begin the drag
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                _clickedGame = null;
                foreach (object selItem in _selItems)
                {
                    if (!DisplayList.SelectedItems.Contains(selItem))
                        DisplayList.SelectedItems.Add(selItem);
                }
                //The sender is always the list box
                var listBox = (ListBox)sender;
                var items = listBox.SelectedItems.OfType<RemoteSteamApp>().ToArray();

                FriendTransfer transferData = new FriendTransfer((FriendViewModel)listBox.DataContext, items);
                DataObject dragData = new DataObject(DRAG_DATA_NAME, transferData);
                DragDrop.DoDragDrop(this, dragData, DragDropEffects.Move);
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


        public class FriendTransfer
        {
            public readonly FriendViewModel SourceLibrary;
            public readonly RemoteSteamApp[] SourceGames;
            public FriendTransfer(FriendViewModel list, RemoteSteamApp[] items)
            {
                SourceLibrary = list;
                SourceGames = items;
            }
        }
    }
}
