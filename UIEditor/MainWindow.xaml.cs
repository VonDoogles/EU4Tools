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
        double CurrentZoom = 100.0;
        bool bProcessZoomSelectionChanged = true;

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
            SelectedRect.Fill = Brushes.Transparent;
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

            FileGUI.Element GfxElement = Win.FindFirst( Elem => Elem.Name == "spriteType" );

            if ( GfxDoc != null && GfxElement != null )
            {
                char[] ToTrim = { '"' };
                string SpriteName = GfxElement.Value;//.Trim( ToTrim );

                FileGUI.Element Sprite = GfxDoc.FindFirst( Elem => Elem.Name == "spriteType" && Elem.Field<string>( "name" ) == SpriteName );
                if ( Sprite != null )
                {
                    string SpriteFileName = Sprite.Field<string>( "texturefile" ).Trim( ToTrim );
                    SpriteFileName = System.IO.Path.Combine( Utils.FindGamePath(), SpriteFileName );

                    DDSImage dds = new DDSImage( File.Open( SpriteFileName, FileMode.Open ) );
                    TestImage.Source = dds.Bitmap;
                    TestImage.Visibility = System.Windows.Visibility.Visible;
                }
            }
            else
            {
                TestImage.Visibility = System.Windows.Visibility.Hidden;
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

        bool ContainsDigitsOnly( string Text )
        {
            bool Result = true;
            foreach (char Ch in Text)
            {
                Result &= Char.IsDigit( Ch );
            }
            return Result;
        }

        private void TextResX_PreviewTextInput( object sender, TextCompositionEventArgs e )
        {
            e.Handled = !ContainsDigitsOnly( e.Text );
            base.OnPreviewTextInput( e );
        }

        private void TextResY_PreviewTextInput( object sender, TextCompositionEventArgs e )
        {
            e.Handled = !ContainsDigitsOnly( e.Text );
            base.OnPreviewTextInput( e );
        }

        private void TextResX_Pasting( object sender, DataObjectPastingEventArgs e )
        {
            if ( e.DataObject.GetDataPresent( typeof( string ) ) )
            {
                string Text = (string)e.DataObject.GetData( typeof( string ) );
                if ( !ContainsDigitsOnly( Text ) )
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void TextResY_Pasting( object sender, DataObjectPastingEventArgs e )
        {
            if ( e.DataObject.GetDataPresent( typeof( string ) ) )
            {
                string Text = (string)e.DataObject.GetData( typeof( string ) );
                if ( !ContainsDigitsOnly( Text ) )
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        void SetZoom( double NewZoom )
        {
            NewZoom = Math.Min( 6400.0, Math.Max( NewZoom, 3.13 ) );

            if ( NewZoom != CurrentZoom )
            {
                double ResX, ResY;
                double.TryParse( TextResX.Text, out ResX );
                double.TryParse( TextResY.Text, out ResY );

                if ( ResX != 0 && ResY != 0 )
                {
                    CurrentZoom = NewZoom;
                    MainCanvas.Width = Math.Floor( ResX * CurrentZoom * 0.01 );
                    MainCanvas.Height = Math.Floor( ResY * CurrentZoom * 0.01 );

                    string ZoomText = string.Format( "{0:#####.##}", NewZoom ) + "%";

                    bProcessZoomSelectionChanged = false;
                    ZoomCombo.SelectedItem = (ComboBoxItem)ZoomCombo.Items
                        .OfType<ComboBoxItem>()
                        .Where( Item => Item.Content.ToString() == ZoomText ).FirstOrDefault();

                    ZoomCombo.Text = ZoomText;
                    bProcessZoomSelectionChanged = true;
                }
            }
        }

        void ParseZoom(string Text, out double NewZoom)
        {
            NewZoom = CurrentZoom;

            char[] ToTrim = { '%' };
            string ZoomText = Text.Trim( ToTrim );

            double ResX, ResY;
            double.TryParse( TextResX.Text, out ResX );
            double.TryParse( TextResY.Text, out ResY );

            if ( ResX != 0 && ResY != 0 )
            {
                bool bZoomFitAll = ZoomText == "Fit All";
                if ( bZoomFitAll )
                {
                    double WidthScale = ScrollView.ActualWidth / ResX;
                    double HeightScale = ScrollView.ActualHeight / ResY;
                    NewZoom = (float)Math.Min( HeightScale, WidthScale ) * 100.0f;
                }
                else if ( !double.TryParse( ZoomText, out NewZoom ) )
                {
                    NewZoom = CurrentZoom;
                }
            }
        }

        private void ZoomCombo_KeyUp( object sender, KeyEventArgs e )
        {
            base.OnKeyUp( e );

            if ( e.Key == Key.Return || e.Key == Key.Enter )
            {
                double NewZoom;
                ParseZoom( ZoomCombo.Text, out NewZoom );
                SetZoom( NewZoom );
                Keyboard.ClearFocus();
            }
            else if ( e.Key == Key.Escape )
            {
                Keyboard.ClearFocus();
            }
        }

        double GetNextZoomLevel( double Zoom, int Direction )
        {
            double NextZoom = 100.0;
            int Steps = 0;

            Action<bool> GetPrev = (bool bCheckEqual) =>
            {
                double[] ZoomMult = new double[ 2 ] { 2.0 / 3.0, 0.75 };
                while ( NextZoom > Zoom || ( bCheckEqual && NextZoom == Zoom ) )
                {
                    bool bPreInc = Zoom > 100.0;
                    if ( bPreInc )
                    {
                        Steps--;
                    }
                    NextZoom = NextZoom * ZoomMult[ Steps & 1 ];
                    if ( !bPreInc )
                    {
                        Steps--;
                    }
                }
            };

            Action<bool> GetNext = (bool bCheckEqual) =>
            {
                double[] ZoomMult = new double[ 2 ] { 1.5, 1.0 + 1.0 / 3.0 };
                while ( NextZoom < Zoom || ( bCheckEqual && NextZoom == Zoom ) )
                {
                    bool bPreInc = Zoom < 100.0;
                    if ( bPreInc )
                    {
                        Steps++;
                    }
                    NextZoom = NextZoom * ZoomMult[ Steps & 1 ];
                    if ( !bPreInc )
                    {
                        Steps++;
                    }
                }
            };

            if ( Direction > 0 )
            {
                GetNext( false );
                GetPrev( false );
                GetNext( true );
            }
            else
            {
                GetPrev( false );
                GetNext( false );
                GetPrev( true );
            }

            return NextZoom;
        }

        private void ScrollView_MouseWheel( object sender, MouseWheelEventArgs e )
        {
            if ( ( Keyboard.Modifiers & ModifierKeys.Control ) == ModifierKeys.Control )
            {
                double NewZoom = GetNextZoomLevel( CurrentZoom, Math.Sign( e.Delta ) );
                SetZoom( NewZoom );

                e.Handled = true;
            }
        }

        private void ZoomCombo_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            if ( bProcessZoomSelectionChanged )
            {
                ComboBoxItem Selected = e.AddedItems.OfType<ComboBoxItem>().FirstOrDefault();
                if ( Selected != null )
                {
                    double NewZoom;
                    ParseZoom( Selected.Content.ToString(), out NewZoom );
                    SetZoom( NewZoom );
                }
                Keyboard.ClearFocus();
            }
        }

        private void ZoomCombo_LostKeyboardFocus( object sender, KeyboardFocusChangedEventArgs e )
        {
            string ZoomText = string.Format( "{0:#####.##}", CurrentZoom ) + "%";
            ZoomCombo.Text = ZoomText;

            Action DelayUpdateText = () =>
            {
                ZoomCombo.Text = ZoomText;
            };
            ZoomCombo.Dispatcher.BeginInvoke( DelayUpdateText, DispatcherPriority.Send );
        }
    }
}
