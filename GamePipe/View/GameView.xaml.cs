/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace GamePipe.View
{
    /// <summary>
    /// Interaction logic for GameView.xaml
    /// </summary>
    public partial class GameView : UserControl
    {
        public GameView()
        {
            InitializeComponent();
            GameMenuButton.Click += GameMenuButton_Click;
        }

        private void GameMenuButton_Click(object sender, RoutedEventArgs e)
        {
            GameMenuButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Left;
            GameMenuButton.ContextMenu.PlacementTarget = GameMenuButton;
            GameMenuButton.ContextMenu.IsOpen = true;
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            e.Handled = true;

            var img = sender as BitmapImage;
            string source = "";
            if (sender is CachedImage.Image)
                source = ((CachedImage.Image)sender).ImageUrl;
            GamePipeLib.Utils.Logging.Logger.Error($"Image {source} failed due to exception:", e.ErrorException);
        }
    }
}
