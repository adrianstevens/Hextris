using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Hextris.Core;
using System.Windows.Threading;
using System;
using System.Windows.Input;

namespace Hextris.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //we'll add rendering code here for now but it should be moved into a custom class or control
        int HEX_SIZE = 10; 
        int HEX_RATIO = 900; //of a 1000 for aspect ratio

        int HEX_COS30;

        int BOTTOM_INDENT = 10;
        static int GAME_WIDTH = 10;
        static int GAME_HEIGHT = 40;

        DispatcherTimer gameTimer;

        Point[,] gameField = new Point[GAME_WIDTH, GAME_HEIGHT];

        GameBoard oGame = new GameBoard();

        public MainWindow()
        {
            InitializeComponent();

            HEX_COS30 = HEX_SIZE * 900 / 1000;

            gameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            gameTimer.Tick += GameTimer_Tick;
            gameTimer.Start();

            this.KeyUp += MainWindow_KeyUp;   
        }

        private void MainWindow_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Left:
                    oGame.OnLeft();
                    break;
                case Key.Right:
                    oGame.OnRight();
                    break;
                case Key.Down:
                    oGame.OnDown();
                    break;
                case Key.Up:
                    oGame.OnRotate();
                    break;
                case Key.R:
                    oGame.ResetBoard();
                    break;
                default:
                    return;
            }
            OnDraw();
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            oGame.OnTimerTick();
            OnDraw();
        }

        void OnDraw ()
        {
            DrawBackGround();
            DrawGameBoard();
            DrawGhost();
            DrawCurrentPiece();
            DrawPiecePreview();
            DrawSavedPiece();
        }

        void DrawBackGround ()
        {
            int j = (int)canvasGame.ActualHeight - BOTTOM_INDENT;

            int iRow = 0;

            while (j > 0)
            {
                for (int i = 0; i < GAME_WIDTH / 2; i++)
                {
                    gameField[i * 2, iRow].X = i * HEX_SIZE * 3 + HEX_SIZE / 2;
                    gameField[i * 2, iRow].Y = j;

                    DrawHexagon(canvasGame, gameField[i*2, iRow], Color.FromRgb(20, 80, 255), Color.FromRgb(30, 30, 40), HEX_SIZE, false);

                    gameField[i * 2 + 1, iRow].X = i * HEX_SIZE * 3 + 2 * HEX_SIZE;
                    gameField[i * 2 + 1, iRow].Y = HEX_COS30 + gameField[i * 2, iRow].Y;

                    DrawHexagon(canvasGame, gameField[i * 2 + 1, iRow], Color.FromRgb(20, 80, 255), Color.FromRgb(30, 30, 40), HEX_SIZE, false);
                }
                iRow++;

                j -= 2 * HEX_COS30;

            }
        }

        void DrawGameBoard ()
        {

        }

        void DrawCurrentPiece ()
        {
            int iYOffset = 0;

            var oPiece = oGame.GetCurrentPiece();

            for (int i = 0; i < 5; i++)
            {
                if ((i + oPiece.GetX()) % 2 == 0)
                    iYOffset++;

                for (int j = 0; j < 5; j++)
                {
                    if (oPiece.GetHex(i, j).ePiece == HexType.GamePiece)
                    {
                        //so ... the current piece stores its position relative to the grid (bottom left being 0,0 ... this are grid points not screen points
                        //m_ptGameField is an array of screen points the size of the current grid
                        //so we get the piece position (which is top left
                        int X = oPiece.GetX();
                        int Y = oPiece.GetY();
                        X += i;
                        Y -= (j + iYOffset);

                        if (Y < oGame.GetNumRows())
                        {
                            DrawHexagon(canvasGame, gameField[X, Y], Colors.LimeGreen, Colors.GreenYellow, HEX_SIZE, false);
                        }
                    }
                }
            }
        }

        void DrawGhost ()
        {

        }

        void DrawPiecePreview ()
        {

        }

        void DrawSavedPiece ()
        {

        }

        void DrawHexagon (Canvas canvas, Point location, Color outline, Color fill, int size, bool highlight)
        {
            Point[] ptHex = new Point[6];

            var brushOutline = new SolidColorBrush(outline);
            var brushFill = new SolidColorBrush(fill);

            ptHex[0].X = location.X;
            ptHex[0].Y = location.Y;
            ptHex[1].X = ptHex[0].X + size;
            ptHex[1].Y = ptHex[0].Y;
            ptHex[2].X = ptHex[1].X + size / 2;
            ptHex[2].Y = ptHex[1].Y + size * HEX_RATIO / 1000;
            ptHex[3].X = ptHex[1].X;
            ptHex[3].Y = ptHex[2].Y + size * HEX_RATIO / 1000;
            ptHex[4].X = ptHex[0].X;
            ptHex[4].Y = ptHex[3].Y;
            ptHex[5].X = ptHex[0].X - size / 2;
            ptHex[5].Y = ptHex[2].Y;

            var polygon = new Polygon();
            var collection = new PointCollection();
            for (int i = 0; i < ptHex.Length; i++)
                collection.Add(ptHex[i]);

            polygon.Points = collection;
            polygon.Fill = brushFill;
            polygon.Stroke = brushOutline;

            canvas.Children.Add(polygon);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OnDraw();
        }
    }
}