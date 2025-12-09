using System;
using System.Diagnostics;

namespace Hextris.Core
{
    public struct ClearedType
    {
        public int Row { get; set; }
        public bool IsAlternate { get; set; }
    };

    public enum GameState
    {
        Playing,
        GameOver,
        Paused,
        Menu,
    };

    public enum GameType
    {
        Classic,     //progress through the levels
        Challenge,   //garbage at start of each level, must get 10 lines to progress then the level resets with new garbage 
        Patterns,    //Pre-defined preset block patterns ... 50ish??
        Speed,       //like classic but garbage comes up from the bottom of the play field
        Ultra,       //accumulate as many points as possible in 3 minutes
        Clear40,     //clear 40 lines as quickly as possible
        count,
    };

    public enum HexType
    {
        GamePiece,
        Erased,
        Blank,
    };

    public class GameBoard
    {
        public event EventHandler  OnLevelChanged;

        public static int GAME_WIDTH => 10;
        public static int GAME_HEIGHT => 21;
        readonly static int NUM_PIECE_TYPES = (int)PieceType.count;
        readonly static int NUM_PREVIEWS = 3;
        readonly static int MAX_LINES = 8;

        public GameState GameState { get; private set; }
        public bool ShowGhost { get; set; } = true;
        GameType gameType = GameType.Classic;

        readonly GameHexagon[,] gameField = new GameHexagon[GAME_WIDTH, GAME_HEIGHT];//our game field

        readonly ClearedType[] clearedLines = new ClearedType[MAX_LINES];

        public GamePiece GhostPiece { get; private set; }
        public GamePiece CurrentPiece { get; private set; }
        readonly GamePiece[] piecePreviews = new GamePiece[NUM_PIECE_TYPES];
        GamePiece savedPiece;

        readonly int rows = GAME_HEIGHT;

        public int HighScore { get; set; }
        public int Score { get; private set; }
        public int RowsCleared { get; private set; }

        public int Level
        {
            get => _level;
            set
            {
                _level = value;
                OnLevelChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        private int _level;

        readonly int startingLevel = 1;

        readonly int[] stats = new int[NUM_PIECE_TYPES];

        bool newHighScore;

        public GameBoard()
        {
            CurrentPiece = new GamePiece();
            savedPiece = new GamePiece();
            GhostPiece = new GamePiece();

            for (int i = 0; i < NUM_PREVIEWS; i++)
            {
                piecePreviews[i] = new GamePiece();
                piecePreviews[i].SetRandomPieceType();
                piecePreviews[i].SetPosition(3, GAME_HEIGHT + 1);
            }

            NewGame();
        }

        public void OnTimerTick() //from rendering engine
        {
            if (GameState != GameState.Playing)
                return;

            CurrentPiece.MoveDown();

            if (CheckCollision(CurrentPiece) == true)
            {
                Debug.WriteLine("Collision");
                CurrentPiece.MoveUp();
                SetPieceToBoard(CurrentPiece);
                NewPiece();
                CheckForCompleteLines();
                CalcGhost();
            }
        }

        public GameHexagon GetBoardHex(int x, int y)
        {
            return gameField[x, y];
        }

        void ResetGame()
        {
            Score = 0;
            RowsCleared = 0;
            Level = startingLevel;
            newHighScore = false;

            for (int i = 0; i < NUM_PIECE_TYPES; i++)
                stats[i] = 0;

            savedPiece.SetRandomPieceType();
            savedPiece.SetPosition(3, GAME_HEIGHT + 1);
            CurrentPiece.SetPosition(3, GAME_HEIGHT + 1);

            //reset the preview pieces
            for (int i = 0; i < NUM_PREVIEWS; i++)
                piecePreviews[i].SetRandomPieceType();

            NewPiece();
            CalcGhost();

            GameState = GameState.Playing;
        }

        void ResetBoard()
        {
            for (int i = 0; i < GAME_WIDTH; i++)
            {
                for (int j = 0; j < GAME_HEIGHT; j++)
                {
                    if (gameField[i, j] == null)
                        gameField[i, j] = new GameHexagon();
                    gameField[i, j].ePiece = HexType.Blank;
                }
            }
        }

        void NewPiece()
        {
            CurrentPiece = piecePreviews[0];

            for (int i = 0; i < NUM_PREVIEWS - 1; i++)
                piecePreviews[i] = piecePreviews[i + 1];

            var newPiece = new GamePiece();
            newPiece.SetRandomPieceType();
            newPiece.SetPosition(3, GAME_HEIGHT + 1);
            piecePreviews[NUM_PREVIEWS - 1] = newPiece;

            stats[(int)CurrentPiece.PieceType]++;
        }

        public void NewGame()
        {
            ResetBoard();
            ResetGame();

            switch (gameType)
            {
                case GameType.Classic:
                    break;
                case GameType.Challenge:
                    break;
                case GameType.Patterns:
                    break;
                case GameType.Ultra:
                    break;
                case GameType.Clear40:
                    break;
                default:
                    break;
            }
            GameState = GameState.Playing;
        }

        public void OnUp()
        {
            CurrentPiece.MoveUp();
        }

        public void OnDown()
        {
            CurrentPiece.MoveDown();

            if (CheckCollision(CurrentPiece))
            {
                CurrentPiece.MoveUp();
            }
        }

        public void OnLeft()
        {
            CurrentPiece.MoveLeft();

            if (CheckCollision(CurrentPiece))
            {
                CurrentPiece.MoveRight();
                return;
            }

            CalcGhost();
        }

        public void OnRight()
        {
            CurrentPiece.MoveRight();

            if (CheckCollision(CurrentPiece))
            {
                CurrentPiece.MoveLeft();
                return;
            }

            CalcGhost();
        }

        public void OnPause()
        {
            if (GameState == GameState.Playing)
                GameState = GameState.Paused;
            else if (GameState == GameState.Paused)
                GameState = GameState.Playing;
        }


        public void OnRotate()
        {
            CurrentPiece.Rotate();

            if (CheckCollision(CurrentPiece))
            {
                CurrentPiece.MoveLeft();
                if (CheckCollision(CurrentPiece))
                {
                    CurrentPiece.MoveRight();
                    CurrentPiece.MoveRight();

                    if (CheckCollision(CurrentPiece))
                    {
                        CurrentPiece.MoveLeft();
                        CurrentPiece.RotateCCW();
                        return;
                    }
                }
            }

            CalcGhost();
        }

        public void OnDrop()
        {
            while (CheckCollision(CurrentPiece) == false)
                CurrentPiece.MoveDown();
            CurrentPiece.MoveUp();
        }

        public void OnSwitchPiece()
        {
            var piece = CurrentPiece;
            CurrentPiece = savedPiece;
            savedPiece = piece;
        }

        private void SetPieceToBoard(GamePiece gamePiece)
        {
            int yOffset = 0;

            for (int i = 0; i < 5; i++)
            {
                if ((i + CurrentPiece.GetX()) % 2 == 0)
                    yOffset++;

                for (int j = 0; j < 5; j++)
                {
                    if (CurrentPiece.GetHex(i, j).ePiece == HexType.GamePiece)
                    {
                        int x2 = CurrentPiece.GetX() + i;
                        int y2 = CurrentPiece.GetY() - j - yOffset;

                        gameField[x2, y2].ePiece = HexType.GamePiece;
                        gameField[x2, y2].indexColor = (int)CurrentPiece.PieceType;
                    }
                }
            }
            CheckForEndGameState();
        }

        private void CheckForEndGameState()
        {
            for (int i = 0; i < GAME_WIDTH; i++)
            {
                if (gameField[i, rows - 1].ePiece == HexType.GamePiece)
                {   //game over
                    GameState = GameState.GameOver;
                    return;
                }
            }
        }

        public GamePiece GetPreviewPiece(int index)
        {
            if (index < 0 || index > NUM_PREVIEWS)
                throw new IndexOutOfRangeException();

            return piecePreviews[index];
        }

        private bool CheckCollision(GamePiece gamePiece)
        {
            if (gamePiece == null)
                return false;

            int yOffset = 0;
            for (int x = 0; x < 5; x++)
            {
                if ((x + gamePiece.GetX()) % 2 == 0)
                    yOffset++;

                for (int y = 0; y < 5; y++)
                {
                    if (gamePiece.GetHex(x,y).ePiece == HexType.GamePiece)
                    {
                        int x2 = gamePiece.GetX() + x;
                        int y2 = gamePiece.GetY() - y - yOffset;

                        if (y2 >= GAME_HEIGHT)
                            continue;
                        if (x2 < 0 || x2 >= GAME_WIDTH || y2 < 0)
                            return true;
                        if (gameField[x2,y2].ePiece != HexType.Blank)
                            return true;
                    }
                }
            }
            return false;
        }

        private void CalcGhost ()
        {
            GhostPiece.CopyPieceState(CurrentPiece);

            if (CheckCollision(GhostPiece))
                return;

            do
            {
                GhostPiece.MoveDown();
            }
            while (CheckCollision(GhostPiece) == false);

            GhostPiece.MoveUp();
        }

        private void CheckForCompleteLines ()
        {
            int clearedLineCount = 0;

            bool completeLineFound;

            for (int y = 0; y < rows -1; y++)
            {
                completeLineFound = true;

                for (int x = 0; x < GAME_WIDTH; x++)
                {
                    if (gameField[x,y].ePiece != HexType.GamePiece)
                    {
                        completeLineFound = false;
                        break;
                    }
                }

                if (completeLineFound)
                {
                    Debug.WriteLine("Normal complete line found at " + y);
                    clearedLines[clearedLineCount].IsAlternate = false;
                    clearedLines[clearedLineCount].Row = y;
                    SetLineToCleared(clearedLines[clearedLineCount].Row, clearedLines[clearedLineCount].IsAlternate);
                    clearedLineCount++;
                }

                //check for alt style
                completeLineFound = true;

                for (int x = 0; x < GAME_WIDTH; x++)
                {
                    if (gameField[x, y + x % 2].ePiece != HexType.GamePiece)
                    {
                        completeLineFound = false;
                        break;
                    }
                }

                if (completeLineFound)
                {
                    Debug.WriteLine("Alt complete line found at " + y);
                    clearedLines[clearedLineCount].IsAlternate = true;
                    clearedLines[clearedLineCount].Row = y;
                    SetLineToCleared(clearedLines[clearedLineCount].Row, clearedLines[clearedLineCount].IsAlternate);
                    clearedLineCount++;
                }
            }

            if (clearedLineCount > 0)
            {
                //Array.Copy(gameField, prevField, gameField.Length);
                //and finally, copy and move everything down
                DropBoardAfterRowsCleared();
                ScoreClearedLines(clearedLineCount);
                Debug.WriteLine($"Scored {clearedLineCount} lines");
            }
        }

        private void SetLineToCleared(int y, bool isAlternate)
        {
            int yLow = y;

            //to make this easy we'll do this one column at a time
            for (int i = 0; i < GAME_WIDTH; i++)
            {
                if (isAlternate)
                    yLow = y + i % 2;
                else
                    yLow = y;

                gameField[i, yLow].ePiece = HexType.Erased;
            }
        }

        private void ScoreClearedLines(int count = 1)
        {
            int baseScore = ShowGhost ? 50 : 60;

            if (count == 2)
                baseScore = 125;
            else if (count == 3)
                baseScore = 250;
            else if (count > 3)
                baseScore = 400;

            Score += baseScore * Level;

            if(Score > HighScore)
            {
                HighScore = Score;
                newHighScore = true;
            }

            //loop so we can catch the % 10
            for (int i = 0; i < count; i++)
            {
                RowsCleared++;
                if (RowsCleared % 10 == 0)
                    Level++;
            }
        }

        void DropBoardAfterRowsCleared()
        {
            //to make this really easy we're going to do this one column at a time
            for (int i = 0; i < GAME_WIDTH; i++)
            {
                for (int j = GAME_HEIGHT - 1; j > -1; j--)
                {
                    if (gameField[i,j].ePiece == HexType.Erased)
                    {
                        for (int k = j; k < GAME_HEIGHT - 2; k++)
                        {
                            gameField[i, k].ePiece = gameField[i, k + 1].ePiece;
                            gameField[i, k].indexColor = gameField[i, k + 1].indexColor;
                        }

                        gameField[i, GAME_HEIGHT - 1].ePiece = HexType.Blank;
                    }
                }
            }
        }
    }
}