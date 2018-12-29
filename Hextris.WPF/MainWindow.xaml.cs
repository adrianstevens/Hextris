using System.Windows;
using System.Windows.Shapes;
using System.Windows.Threading;
using System;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hextris.Core;

namespace Hextris.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //we'll add rendering code here for now but it should be moved into a custom class or control
        int HEX_SIZE = 14; 
        int HEX_RATIO = 900; //of a 1000 for aspect ratio

        int HEX_COS30;

        int BOTTOM_INDENT = 30;

        DispatcherTimer gameTimer;

        Point[,] gameField;

        GameBoard oGame = new GameBoard();

        WriteableBitmap gameboardBmp;

        public MainWindow()
        {
            InitializeComponent();
            gameboardBmp = BitmapFactory.New((int)imageContainer.Width, (int)imageContainer.Height);
            imageGame.Source = gameboardBmp;

            gameField = new Point[GameBoard.GAME_WIDTH, GameBoard.GAME_HEIGHT];

            HEX_COS30 = HEX_SIZE * 900 / 1000;

            gameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            gameTimer.Tick += GameTimerTick;
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
                case Key.Space:
                    oGame.OnDrop();
                    break;
                default:
                    return;
            }
            OnDraw();
        }

        private void GameTimerTick(object sender, EventArgs e)
        {
            oGame.OnTimerTick();
            OnDraw();
        }

        void OnDraw ()
        {
            using (gameboardBmp.GetBitmapContext())
            {
                gameboardBmp.Clear();
                DrawBackGround();
                DrawGameBoard();
                DrawGhost();
                DrawCurrentPiece();
                DrawPiecePreview();
                DrawSavedPiece();
            }
        }

        void DrawBackGround ()
        {
            int j = (int)imageGame.ActualHeight - BOTTOM_INDENT;

            int iRow = 0;

            while (j > 0 && iRow < GameBoard.GAME_HEIGHT)
            {
                for (int i = 0; i < GameBoard.GAME_WIDTH / 2; i++)
                {
                    gameField[i * 2, iRow].X = i * HEX_SIZE * 3 + HEX_SIZE / 2;
                    gameField[i * 2, iRow].Y = j;

                    DrawHexagon(gameField[i*2, iRow], Color.FromRgb(20, 80, 255), Color.FromRgb(30, 30, 40), HEX_SIZE, false);

                    gameField[i * 2 + 1, iRow].X = i * HEX_SIZE * 3 + 2 * HEX_SIZE;
                    gameField[i * 2 + 1, iRow].Y = HEX_COS30 + gameField[i * 2, iRow].Y;

                    DrawHexagon(gameField[i * 2 + 1, iRow], Color.FromRgb(20, 80, 255), Color.FromRgb(30, 30, 40), HEX_SIZE, false);
                }
                iRow++;

                j -= 2 * HEX_COS30;
            }
        }

        void DrawGameBoard ()
        {
            for (int i = 0; i < GameBoard.GAME_WIDTH; i++)
            {
                for (int j = 0; j < oGame.GetNumRows(); j++)
                {
                    if (oGame.GetBoardHex(i, j).ePiece == HexType.GamePiece)
                    {
                        DrawHexagon(gameField[i,j],
                            GetColor((PieceType)oGame.GetBoardHex(i, j).indexColor, false),
                            GetColor((PieceType)oGame.GetBoardHex(i, j).indexColor, true),
                            HEX_SIZE, false);
                    }
                }
            }
        }

        void DrawCurrentPiece ()
        {
            int iYOffset = 0;

            var currentPiece = oGame.GetCurrentPiece();

            for (int i = 0; i < 5; i++)
            {
                if ((i + currentPiece.GetX()) % 2 == 0)
                    iYOffset++;

                for (int j = 0; j < 5; j++)
                {
                    if (currentPiece.GetHex(i, j).ePiece == HexType.GamePiece)
                    {
                        //so ... the current piece stores its position relative to the grid (bottom left being 0,0 ... this are grid points not screen points
                        //gameField is an array of screen points the size of the current grid
                        //so we get the piece position (which is top left
                        int x = currentPiece.GetX();
                        int y = currentPiece.GetY();
                        x += i;
                        y -= (j + iYOffset);

                        if (y < oGame.GetNumRows())
                        {
                            DrawHexagon(gameField[x, y], 
                                GetColor(currentPiece.PieceType, false),
                                GetColor(currentPiece.PieceType, true),
                                HEX_SIZE, false);
                        }
                    }
                }
            }
        }

        Color[,] pieceColors = new Color[,]
        {
            {Color.FromRgb(191,0,0),      Color.FromRgb(255,0,0),       Color.FromRgb(127,0,0)},			//Red
	        {Color.FromRgb(191,95,0),     Color.FromRgb(255,127,0),     Color.FromRgb(127,64,0)},			//orange
	        {Color.FromRgb(191,0,191),    Color.FromRgb(240,0,255),     Color.FromRgb(127,0,127)},				//fuschia
	        {Color.FromRgb(0,191,191),    Color.FromRgb(0,255,255),     Color.FromRgb(0,127,127)},				//cyan
	        {Color.FromRgb(0,95,191),     Color.FromRgb(0,127,255),     Color.FromRgb(0,64,127)},			//blue
	        {Color.FromRgb(127,0,191),    Color.FromRgb(191,0,255),     Color.FromRgb(92,0,127)},				//purple
	        {Color.FromRgb(0,191,0),      Color.FromRgb(0,255,0),       Color.FromRgb(0,127,0)},			//green
	        {Color.FromRgb(191,191,0),    Color.FromRgb(255,255,0),     Color.FromRgb(127,127,0)},				//yellow
	        {Color.FromRgb(191,191,191),  Color.FromRgb(255,255,255),   Color.FromRgb(127,127,127)},		//white
	        {Color.FromRgb(127,127,127),  Color.FromRgb(191,191,191),   Color.FromRgb(64,64,64)},
        };

        Color GetColor(PieceType pieceType, bool isFill, bool isGhost = false)
        {
            var clr = isFill ? pieceColors[(int)pieceType, 0] : pieceColors[(int)pieceType, 2];

            if (isGhost) clr.A = 30;

            return clr;
        }
            
        void DrawGhost ()
        {
            var ghostPiece = oGame.GetGhost();

            int iYOffset = 0;

            for (int i = 0; i < 5; i++)
            {
                if ((i + ghostPiece.GetX()) % 2 == 0)
                    iYOffset++;

                for (int j = 0; j < 5; j++)
                {
                    if (ghostPiece.GetHex(i, j).ePiece == HexType.GamePiece)
                    {
                        var x = ghostPiece.GetX() + i;
                        var y = ghostPiece.GetY() - j - iYOffset;

                        if (y < oGame.GetNumRows())
                        {
                            DrawHexagon(gameField[x,y],
                                GetColor(ghostPiece.PieceType, true),
                                Colors.Transparent,
                                HEX_SIZE, false);
                        }
                    }
                }
            }
        }

        void DrawPiecePreview ()
        {

        }

        void DrawSavedPiece ()
        {

        }

        void DrawHexagon(Point location, Color outline, Color fill, int size, bool highlight)
        {
            int[] ptHex = new int[14];

            ptHex[0] = (int)location.X;
            ptHex[1] = (int)location.Y;

            ptHex[2] = ptHex[0] + size;
            ptHex[3] = ptHex[1];

            ptHex[4] = ptHex[2] + size / 2;
            ptHex[5] = ptHex[3] + size * HEX_RATIO / 1000;

            ptHex[6] = ptHex[2];
            ptHex[7] = ptHex[5] + size * HEX_RATIO / 1000;

            ptHex[8] = ptHex[0];
            ptHex[9] = ptHex[7];

            ptHex[10] = ptHex[8] - size / 2;
            ptHex[11] = ptHex[5];

            ptHex[12] = ptHex[0];
            ptHex[13] = ptHex[1];

            gameboardBmp.FillPolygon(ptHex, fill);
            gameboardBmp.DrawPolylineAa(ptHex, outline);
        }

        void DrawHexagonShapes (Point location, Color outline, Color fill, int size, bool highlight)
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

           // canvasGame.Children.Add(polygon);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OnDraw();
        }
    }
}