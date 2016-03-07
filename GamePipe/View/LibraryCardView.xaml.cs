﻿/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GamePipe.ViewModel;

namespace GamePipe.View
{
    /// <summary>
    /// Interaction logic for LibraryCardView.xaml
    /// </summary>
    public partial class LibraryCardView : UserControl
    {
        public LibraryCardView()
        {
            InitializeComponent();
            this.DragEnter += LibraryCardView_DragEnter;
            this.Drop += LibraryCardView_Drop;
            LibraryMenuButton.Click += LibraryMenuButton_Click;
        }

        private void LibraryMenuButton_Click(object sender, RoutedEventArgs e)
        {
            LibraryMenuButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Left;
            LibraryMenuButton.ContextMenu.PlacementTarget = LibraryMenuButton;
            LibraryMenuButton.ContextMenu.IsOpen = true;
        }

        private void LibraryCardView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var vm = (SteamLibraryViewModel)DataContext;
            vm.OpenLibraryCommand.Execute(null);
        }

        private void LibraryCardView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(FriendView.DRAG_DATA_NAME))
            {
                e.Effects = DragDropEffects.Move;
                //DirectCast(sender,ListView).AllowDrop=True
            }
            else
            {
                //DirectCast(sender, ListView).AllowDrop = False
                e.Effects = DragDropEffects.None;
            }
        }


        private void LibraryCardView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(FriendView.DRAG_DATA_NAME) && sender is FrameworkElement)
            {
                var destination = ((FrameworkElement)sender).DataContext as SteamLibraryViewModel;
                var sourceInfo = e.Data.GetData(FriendView.DRAG_DATA_NAME) as FriendView.FriendTransfer;
                if (!object.ReferenceEquals(sourceInfo.SourceLibrary, destination))
                {
                    //var transferViewModel = new StreamCopyViewModel(sourceInfo.SourceLibrary.Client, destination, sourceInfo.SourceGame, this.Dispatcher);
                    //RootSteamViewModel.Transfers.Add(transferViewModel);
                    //transferViewModel.StartTransfer();
                    var transfer = new GamePipeLib.Model.NetworkCopy(sourceInfo.SourceLibrary._provider, destination.Model, sourceInfo.SourceGame);
                    transfer.QueueTransfer();
                }
            }
        }
    }
}
