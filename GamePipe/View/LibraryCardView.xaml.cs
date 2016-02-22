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
    /// Interaction logic for LibraryCardView.xaml
    /// </summary>
    public partial class LibraryCardView : UserControl
    {
        public LibraryCardView()
        {
            InitializeComponent();
            this.DragEnter += LibraryCardView_DragEnter;
            this.Drop += LibraryCardView_Drop;
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
