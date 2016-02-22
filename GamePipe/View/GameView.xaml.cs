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
    }
}
