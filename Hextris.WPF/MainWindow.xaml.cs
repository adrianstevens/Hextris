﻿using System.Windows;
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
        int HEX_SIZE = 14; 
        int HEX_RATIO = 900; //of a 1000 for aspect ratio
        int BOTTOM_INDENT = 50;
        int HEX_COS30;
      
        DispatcherTimer gameTimer;
        SimpleAudioPlayerWPF audioPlayer;

        Point[,] gameField;

        GameBoard hexGame;

        WriteableBitmap gameboardBmp;
        WriteableBitmap previewBmp;

        public MainWindow()
        {
            InitializeComponent();

            hexGame = new GameBoard();
            hexGame.OnLevelChanged += HexGame_OnLevelChanged;

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

            KeyUp += OnKeyUp;   
        }

        private void HexGame_OnLevelChanged(object sender, EventArgs e)
        {
            gameTimer.Interval = TimeSpan.FromSeconds(Math.Max(0.05, 1 - hexGame.Level * 0.05));
        }

        private void OnKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Left:
                    hexGame.OnLeft();
                    break;
                case Key.Right:
                    hexGame.OnRight();
                    break;
                case Key.Down:
                    hexGame.OnDown();
                    break;
                case Key.Up:
                    hexGame.OnRotate();
                    break;
                case Key.P:
                    hexGame.OnPause();
                    break;
                case Key.R:
                    hexGame.NewGame();
                    break;
                case Key.Space:
                    hexGame.OnDrop();
                    break;
                default:
                    return;
            }
            OnDraw();
        }

        private void GameTimerTick(object sender, EventArgs e)
        {
            hexGame.OnTimerTick();
            OnDraw();
            UpdateText();
        }

        void UpdateText()
        {
            lblHighScore.Content = hexGame.HighScore;
            lblScore.Content = hexGame.Score;
            lblLevel.Content = hexGame.Level;
            lblRowsCleared.Content = hexGame.RowsCleared;

            if (hexGame.GameState != GameState.Playing)
                lblGameState.Content = hexGame.GameState.ToString();
            else
                lblGameState.Content = string.Empty;
        }

        void OnDraw ()
        {
            using (gameboardBmp.GetBitmapContext())
            {
                gameboardBmp.Clear();
                DrawBackground();
                DrawGameBoard();

                DrawPiece(hexGame.GhostPiece,
                    GetPieceColor(hexGame.CurrentPiece.PieceType, true),
                    GetPieceColor(hexGame.CurrentPiece.PieceType, true, true));

                DrawPiece(hexGame.CurrentPiece,
                    GetPieceColor(hexGame.CurrentPiece.PieceType, false),
                    GetPieceColor(hexGame.CurrentPiece.PieceType, true));
            }

            using (previewBmp.GetBitmapContext())
            {
                previewBmp.Clear();
                DrawPiecePreview();
            }
        }

        void DrawBackground ()
        {
            int j = (int)imageContainer.Height - BOTTOM_INDENT;

            int row = 0;

            var hexColor = Color.FromRgb(10, 10, 36); //was 30, 30, 40
            var outlineColor = Color.FromRgb(120, 120, 160); //was 20, 80, 255

            while (j > 0 && row < GameBoard.GAME_HEIGHT)
            {
                for (int i = 0; i < GameBoard.GAME_WIDTH / 2; i++)
                {
                    gameField[i * 2, row].X = i * HEX_SIZE * 3 + HEX_SIZE / 2;
                    gameField[i * 2, row].Y = j;

                    DrawHexagon(gameboardBmp, gameField[i*2, row], outlineColor, hexColor, HEX_SIZE, false);

                    gameField[i * 2 + 1, row].X = i * HEX_SIZE * 3 + 2 * HEX_SIZE;
                    gameField[i * 2 + 1, row].Y = HEX_COS30 + gameField[i * 2, row].Y;

                    DrawHexagon(gameboardBmp, gameField[i * 2 + 1, row], outlineColor, hexColor, HEX_SIZE, false);
                }
                row++;

                j -= 2 * HEX_COS30;
            }
        }

        void DrawGameBoard ()
        {
            for (int i = 0; i < GameBoard.GAME_WIDTH; i++)
            {
                for (int j = 0; j < GameBoard.GAME_HEIGHT; j++)
                {
                    if (hexGame.GetBoardHex(i, j).ePiece == HexType.GamePiece)
                    {
                        DrawHexagon(gameboardBmp, gameField[i,j],
                            GetPieceColor((PieceType)hexGame.GetBoardHex(i, j).indexColor, false),
                            GetPieceColor((PieceType)hexGame.GetBoardHex(i, j).indexColor, true),
                            HEX_SIZE, false);
                    }
                }
            }
        }

        Color[,] pieceColors = new Color[,]
        {
            {Color.FromRgb(191,0,0),      Color.FromRgb(255,0,0),       Color.FromRgb(127,0,0)},			//Red
	        {Color.FromRgb(191,95,0),     Color.FromRgb(255,127,0),     Color.FromRgb(127,64,0)},			//orange
	        {Color.FromRgb(191,0,191),    Color.FromRgb(240,0,255),     Color.FromRgb(127,0,127)},			//fuschia
	        {Color.FromRgb(0,191,191),    Color.FromRgb(0,255,255),     Color.FromRgb(0,127,127)},			//cyan
	        {Color.FromRgb(0,95,191),     Color.FromRgb(0,127,255),     Color.FromRgb(0,64,127)},			//blue
	        {Color.FromRgb(127,0,191),    Color.FromRgb(191,0,255),     Color.FromRgb(92,0,127)},			//purple
	        {Color.FromRgb(0,191,0),      Color.FromRgb(0,255,0),       Color.FromRgb(0,127,0)},			//green
	        {Color.FromRgb(191,191,0),    Color.FromRgb(255,255,0),     Color.FromRgb(127,127,0)},			//yellow
	        {Color.FromRgb(191,191,191),  Color.FromRgb(255,255,255),   Color.FromRgb(127,127,127)},		//white
	        {Color.FromRgb(127,127,127),  Color.FromRgb(191,191,191),   Color.FromRgb(64,64,64)},           //gray
        };

        Color GetPieceColor(PieceType pieceType, bool isFill, bool isGhost = false)
        {
            var color = isFill ? pieceColors[(int)pieceType, 0] : pieceColors[(int)pieceType, 2];

            if (isGhost) color.A = 60;

            return color;
        }

        void DrawPiece(GamePiece piece, Color outline, Color fill)
        {
            int yOffset = 0;

            for (int i = 0; i < 5; i++)
            {
                if ((i + piece.GetX()) % 2 == 0)
                    yOffset++;

                for (int j = 0; j < 5; j++)
                {
                    if (piece.GetHex(i, j).ePiece == HexType.GamePiece)
                    {
                        var x = piece.GetX() + i;
                        var y = piece.GetY() - j - yOffset;

                        if (y < GameBoard.GAME_HEIGHT)
                        {
                            DrawHexagon(gameboardBmp, gameField[x, y],
                                outline, fill,
                                HEX_SIZE, false);
                        }
                    }
                }
            }
        }

        void DrawPiecePreview ()
        {
            int yOffset = 0;

            for (int i = 0; i < 4; i++)
            {
                if (i % 2 == 0)
                    yOffset++;

                for (int j = 1; j < 5; j++)
                {
                    int x = i + 10;
                    int y = GameBoard.GAME_HEIGHT - j - yOffset;

                    var previewPiece = hexGame.GetPreviewPiece(0);

                    if (previewPiece.GetHex(i, j).ePiece == HexType.GamePiece)
                    {
                        DrawHexagon(previewBmp, gameField[i, y],
                                GetPieceColor(previewPiece.PieceType, false),
                                GetPieceColor(previewPiece.PieceType, true),
                                HEX_SIZE, false);
                    }
                }
            }
        }

        void DrawHexagon(WriteableBitmap bitmap, Point location, Color outline, Color fill, int size, bool highlight)
        {
            var ptHex = new int[14];

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
    }
}