using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Threading;
using Microsoft.Win32;
using EU4Tools;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace UIEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Image TestImage = null;
        Rectangle SelectedRect = null;
        FileGUI.Element GfxDoc = null;

        public MainWindow()
        {
            InitializeComponent();

            TestImage = new Image();
            MainCanvas.Children.Add( TestImage );
        }

        private void OnClick_FileExit( object sender, RoutedEventArgs e )
        {
            App.Current.Shutdown();
        }

        private void OnClick_FileOpen( object sender, RoutedEventArgs e )
        {
            OpenFileDialog Dialog = new OpenFileDialog();
            Dialog.InitialDirectory = Utils.FindInterfacePath();

            bool? Result = Dialog.ShowDialog();
            if ( Result == true )
            {
                GfxDoc = FileGUI.ReadGFX( Dialog.FileName );

                TextView.Text = File.ReadAllText( Dialog.FileName );
                FileGUI.Element GuiDoc = FileGUI.Read( Dialog.FileName );
                LoadElements( GuiDoc );
            }
        }

        private void LoadElements( FileGUI.Element Doc )
        {
            List<FileGUI.Element> WindowList = ( from Item in Doc.ItemList[ 0 ].ItemList
                                                 where Item.Name == "windowType"
                                                 select Item ).ToList();

            ItemTree.Items.Clear();

            foreach ( FileGUI.Element Win in WindowList )
            {
                TreeViewItem ViewItem = new TreeViewItem();
                ViewItem.Header = Win.Field<string>( "name" );
                ViewItem.Tag = Win;
                ItemTree.Items.Add( ViewItem );
            }
        }

        private void OnSelectedItemChanged_ItemTree( object sender, RoutedPropertyChangedEventArgs<object> e )
        {
            TreeViewItem ViewItem = e.NewValue as TreeViewItem;
            if ( ViewItem == null )
            {
                return;
            }

            if ( SelectedRect == null )
            {
                SelectedRect = new Rectangle();
                MainCanvas.Children.Add( SelectedRect );
            }

            FileGUI.Element Win = ViewItem.Tag as FileGUI.Element;
            FileGUI.Element Size = Win[ "size" ];
            FileGUI.Element Pos = Win[ "position" ];

            SelectedRect.Width = Size.Field<double>( "x" );
            SelectedRect.Height = Size.Field<double>( "y" );
            Canvas.SetLeft( SelectedRect, Pos.Field<double>( "x" ) );
            Canvas.SetTop( SelectedRect, Pos.Field<double>( "x" ) );
            SelectedRect.Fill = Brushes.Yellow;
            SelectedRect.Stroke = Brushes.Black;

            PropGrid.ItemsSource = Win.ItemList;

            TextView.Focus();
            TextView.SelectionBrush = Brushes.Red;

            int Index = TextView.Text.IndexOf( Win.OuterText );
            if ( Index != -1 )
            {
                TextView.CaretIndex = Index;
                TextView.Select( Index, Win.OuterText.Length );
                TextView.Focus();
            }
            else
            {
                TextView.Select( 0, 0 );
            }
        }

        private void TextView_PreviewMouseWheel( object sender, MouseWheelEventArgs e )
        {
            var OldFocus = FocusManager.GetFocusedElement( this );
            if ( OldFocus != null )
            {
                Action DelayFocus = new Action( () =>
                {
                    TextView.Focus();
                    OldFocus.Focus();
                });
                TextView.Dispatcher.BeginInvoke( DelayFocus, DispatcherPriority.Input );
            }
        }
    }
}
