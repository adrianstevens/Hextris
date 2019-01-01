using System.Windows;
using System.Windows.Shapes;
using System.Windows.Threading;
using System;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hextris.Core;
using Plugin.SimpleAudioPlayer;

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

        int BOTTOM_INDENT = 50;

        int HEX_COS30;
      
        DispatcherTimer gameTimer;
        SimpleAudioPlayerWPF audioPlayer;

        Point[,] gameField;

        GameBoard oGame = new GameBoard();

        WriteableBitmap gameboardBmp;
        WriteableBitmap previewBmp;

        public MainWindow()
        {
            InitializeComponent();

            audioPlayer = new SimpleAudioPlayerWPF();

            gameboardBmp = BitmapFactory.New((int)imageContainer.Width, (int)imageContainer.Height);
            imageGame.Source = gameboardBmp;

            previewBmp = BitmapFactory.New((int)imagePreviewContainer.Width, (int)imagePreviewContainer.Height);
            imagePreview.Source = previewBmp;

            gameField = new Point[GameBoard.GAME_WIDTH, GameBoard.GAME_HEIGHT];

            HEX_COS30 = HEX_SIZE * HEX_RATIO / 1000;

            gameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            gameTimer.Tick += GameTimerTick;
            gameTimer.Start();

            this.KeyUp += OnKeyUp;   
        }

        private void OnKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Left:
                    oGame.OnLeft();
                    audioPlayer.Load(@"levelup.wav");
                    audioPlayer.Play();
                    break;
                case Key.Right:
                    oGame.OnRight();
                    audioPlayer.Load(@"lineclear.wav");
                    audioPlayer.Play();
                    break;
                case Key.Down:
                    oGame.OnDown();
                    audioPlayer.Load(@"down.wav");
                    audioPlayer.Play();
                    break;
                case Key.Up:
                    oGame.OnRotate();
                    audioPlayer.Load(@"move.wav");
                    audioPlayer.Play();
                    break;
                case Key.R:
                    oGame.ResetBoard();
                    break;
                case Key.S:
                    oGame.OnSwitchPiece();
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
            lblScore.Content = oGame.Score;
            lblLevel.Content = oGame.Level;
            lblRowsCleared.Content = oGame.RowsCleared;
        }

        void OnDraw ()
        {
            using (gameboardBmp.GetBitmapContext())
            {
                gameboardBmp.Clear();
                DrawBackground();
                DrawGameBoard();
                DrawGhost();
                DrawCurrentPiece();
            }

            using (previewBmp.GetBitmapContext())
            {
                previewBmp.Clear();

                DrawPiecePreview();
       //         DrawSavedPiece();
            }
        }

        void DrawBackground ()
        {
            int j = (int)imageContainer.Height - BOTTOM_INDENT;

            int iRow = 0;

            while (j > 0 && iRow < GameBoard.GAME_HEIGHT)
            {
                for (int i = 0; i < GameBoard.GAME_WIDTH / 2; i++)
                {
                    gameField[i * 2, iRow].X = i * HEX_SIZE * 3 + HEX_SIZE / 2;
                    gameField[i * 2, iRow].Y = j;

                    DrawHexagon(gameboardBmp, gameField[i*2, iRow], Color.FromRgb(20, 80, 255), Color.FromRgb(30, 30, 40), HEX_SIZE, false);

                    gameField[i * 2 + 1, iRow].X = i * HEX_SIZE * 3 + 2 * HEX_SIZE;
                    gameField[i * 2 + 1, iRow].Y = HEX_COS30 + gameField[i * 2, iRow].Y;

                    DrawHexagon(gameboardBmp, gameField[i * 2 + 1, iRow], Color.FromRgb(20, 80, 255), Color.FromRgb(30, 30, 40), HEX_SIZE, false);
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
                        DrawHexagon(gameboardBmp, gameField[i,j],
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
                            DrawHexagon(gameboardBmp, gameField[x, y], 
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
                            DrawHexagon(gameboardBmp, gameField[x,y],
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
            previewBmp.DrawRectangle(10, 30, 110, 130, Colors.Gray);

            GamePiece previewPiece;
            GamePiece savedPiece = oGame.GetSavedPiece();
            int yOffset = 0;

            for (int i = 0; i < 4; i++)
            {
                if (i % 2 == 0)
                    yOffset++;

                for (int j = 1; j < 5; j++)
                {
                    int x = i + 10;
                    int y = GameBoard.GAME_HEIGHT + 1 - j - yOffset;

                    if (savedPiece.GetHex(i, j).ePiece == HexType.GamePiece)
                    {
                        DrawHexagon(previewBmp, gameField[i, y],
                                GetColor(savedPiece.PieceType, false),
                                GetColor(savedPiece.PieceType, true),
                                HEX_SIZE, false);
                    }

                    for (int k = 0; k < 3; k++)
                    {
                        previewPiece = oGame.GetPreviewPiece(k);

                        y = GameBoard.GAME_HEIGHT - (4 * (k + 1)) - j - yOffset;

                        if (previewPiece.GetHex(i, j).ePiece == HexType.GamePiece)
                        {
                            DrawHexagon(previewBmp, gameField[i, y],
                                    GetColor(previewPiece.PieceType, false),
                                    GetColor(previewPiece.PieceType, true),
                                    HEX_SIZE, false);
                        }
                    }
                }
            }
        }

        void DrawSavedPiece ()
        {

        }

        void DrawHexagon(WriteableBitmap bitmap, Point location, Color outline, Color fill, int size, bool highlight)
        {
            int[] ptHex = new int[14];

            ptHex[0] = (int)location.X;
            ptHex[1] = (int)location.Y;

            ptHex[2] = ptHex[0] + size;
            ptHex[3] = ptHex[1];

            ptHex[4] = ptHex[2] + size / 2;
            ptHex[5] = ptHex[3] + HEX_COS30;

            ptHex[6] = ptHex[2];
            ptHex[7] = ptHex[5] + HEX_COS30;

            ptHex[8] = ptHex[0];
            ptHex[9] = ptHex[7];

            ptHex[10] = ptHex[8] - size / 2;
            ptHex[11] = ptHex[5];

            ptHex[12] = ptHex[0];
            ptHex[13] = ptHex[1];

            bitmap.FillPolygon(ptHex, fill);
            bitmap.DrawPolylineAa(ptHex, outline);
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
            ptHex[2].Y = ptHex[1].Y + HEX_COS30;
            ptHex[3].X = ptHex[1].X;
            ptHex[3].Y = ptHex[2].Y + HEX_COS30;
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