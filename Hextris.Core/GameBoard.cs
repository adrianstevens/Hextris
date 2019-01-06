using System;
using System.Diagnostics;

namespace Hextris.Core
{
    //ToDo needs renaming
    public struct ClearedType
    {
        public int row { get; set; }
        public bool isAlternate { get; set; }
    };

    enum GameState
    {
        Playing,
        GameOver,
        Paused,
        Menu,
    };

    enum GameType
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
        public static int GAME_WIDTH => 10;
        public static int GAME_HEIGHT => 20;
        readonly static int NUM_PIECE_TYPES = (int)PieceType.count;
        readonly static int NUM_PREVIEWS = 3;
        readonly static int MAX_LINES = 8;

        GameState gameState;
        GameType gameType = GameType.Classic;

        readonly GameHexagon[,] gameField = new GameHexagon[GAME_WIDTH, GAME_HEIGHT];//our game field
        readonly GameHexagon[,] prevField = new GameHexagon[GAME_WIDTH, GAME_HEIGHT];//before a line remove
        readonly GameHexagon[,] preDropField = new GameHexagon[GAME_WIDTH, GAME_HEIGHT];//lines erased without dropping

        readonly ClearedType[] clearedLines = new ClearedType[MAX_LINES];

        GamePiece savedPiece;
        GamePiece ghostPiece;
        GamePiece currentPiece;
        readonly GamePiece[] piecePreviews = new GamePiece[NUM_PIECE_TYPES];

        readonly int[] pieceCounts = new int[(int)PieceType.count];

        int rows = GAME_HEIGHT;

        public int Score { get; private set; }
        public int Level { get; private set; }
        public int RowsCleared { get; private set; }

        int startingLevel = 1;
        int time;
        

        readonly int[] stats = new int[NUM_PIECE_TYPES];

        bool newHighScore;

        public GameBoard()
        {
            currentPiece = new GamePiece();
            savedPiece = new GamePiece();
            ghostPiece = new GamePiece();

            for (int i = 0; i < NUM_PREVIEWS; i++)
            {
                piecePreviews[i] = new GamePiece();
                piecePreviews[i].SetRandomPieceType();
                piecePreviews[i].SetPosition(4, GAME_HEIGHT);
            }

            NewGame();
        }

        public void OnTimerTick() //from rendering engine
        {
            if (gameState != GameState.Playing)
                return;

            currentPiece.MoveDown();

            if (CheckCollision(currentPiece) == true)
            {
                Debug.WriteLine("Collision");
                currentPiece.MoveUp();
                SetPieceToBoard(currentPiece);
                NewPiece();
                CheckForCompleteLines();
                CalcGhost();
            }
        }

        public GameHexagon GetBoardHex(int x, int y)
        {
            return gameField[x, y];
        }

        public GamePiece GetGhost()
        {
            return ghostPiece;
        }

        void ResetGame()
        {
            Score = 0;
            RowsCleared = 0;
            Level = startingLevel;
            time = 0;
            newHighScore = false;

            for (int i = 0; i < NUM_PIECE_TYPES; i++)
                stats[i] = 0;

            savedPiece.SetRandomPieceType();
            savedPiece.SetPosition(4, GAME_HEIGHT);
            currentPiece.SetPosition(4, GAME_HEIGHT);

            //reset the preview pieces
            for (int i = 0; i < NUM_PREVIEWS; i++)
                piecePreviews[i].SetRandomPieceType();

            NewPiece();
            CalcGhost();
        }

        public void ResetBoard()
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
            currentPiece = piecePreviews[0];

            for (int i = 0; i < NUM_PREVIEWS - 1; i++)
                piecePreviews[i] = piecePreviews[i + 1];

            var newPiece = new GamePiece();
            newPiece.SetRandomPieceType();
            newPiece.SetPosition(4, GAME_HEIGHT + 1);
            piecePreviews[NUM_PREVIEWS - 1] = newPiece;

            stats[(int)currentPiece.PieceType]++;
        }

        void NewGame()
        {
            ResetBoard();
            ResetGame();

            switch (gameType)
            {
                case GameType.Classic:
                    break;
                case GameType.Challenge:
                    //AddGarageBlocks();//based on current level
                    break;
                case GameType.Patterns:
                    //SetPattern();
                    break;
                case GameType.Ultra:
                    //iTime = CHALLENGE_TIME;//we'll count backwards for this gamemode
                    break;
                case GameType.Clear40:
                    break;
                default:
                    break;
            }
            gameState = GameState.Playing;
        }

        public bool OnUp()
        {
            currentPiece.MoveUp();
            return true;
        }

        public bool OnDown()
        {
            currentPiece.MoveDown();

            if (CheckCollision(currentPiece))
            {
                currentPiece.MoveUp();
                return false;
            }
            return true;
        }

        public bool OnLeft()
        {
            currentPiece.MoveLeft();

            if (CheckCollision(currentPiece))
            {
                currentPiece.MoveRight();
                return false;
            }

            CalcGhost();

            return true;
        }

        public bool OnRight()
        {
            currentPiece.MoveRight();

            if (CheckCollision(currentPiece))
            {
                currentPiece.MoveLeft();
                return false;
            }

            CalcGhost();

            return true;
        }

        public void OnPause()
        {
            if (gameState == GameState.Playing)
                gameState = GameState.Paused;
            else if (gameState == GameState.Paused)
                gameState = GameState.Playing;
        }


        public bool OnRotate()
        {
            currentPiece.Rotate();

            if (CheckCollision(currentPiece))
            {
                currentPiece.MoveLeft();
                if (CheckCollision(currentPiece))
                {
                    currentPiece.MoveRight();
                    currentPiece.MoveRight();

                    if (CheckCollision(currentPiece))
                    {
                        currentPiece.MoveLeft();
                        currentPiece.RotateCCW();
                        return false;
                    }
                }
            }

            CalcGhost();

            return true;
        }

        public bool OnDrop()
        {
            return currentPiece.CopyPieceState(ghostPiece);
        }

        public bool OnSwitchPiece()
        {
            var piece = currentPiece;
            currentPiece = savedPiece;
            savedPiece = piece;
            return true;
        }

        private void SetPieceToBoard(GamePiece gamePiece)
        {
            int yOffset = 0;

            for (int i = 0; i < 5; i++)
            {
                if ((i + currentPiece.GetX()) % 2 == 0)
                    yOffset++;

                for (int j = 0; j < 5; j++)
                {
                    if (currentPiece.GetHex(i, j).ePiece == HexType.GamePiece)
                    {
                        int x2 = currentPiece.GetX() + i;
                        int y2 = currentPiece.GetY() - j - yOffset;

                        gameField[x2, y2].ePiece = HexType.GamePiece;
                        gameField[x2, y2].indexColor = (int)currentPiece.PieceType;
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
                    gameState = GameState.GameOver;
                    return;
                }
            }
        }

        public GamePiece GetSavedPiece ()
        {
            return savedPiece;
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
            ghostPiece.CopyPieceState(currentPiece);

            if (CheckCollision(ghostPiece))
                return;

            do
            {
                ghostPiece.MoveDown();
            }
            while (CheckCollision(ghostPiece) == false);

            ghostPiece.MoveUp();
        }

        private void CheckForCompleteLines ()
        {
            int clearedLineCount = 0;

            bool completeLineFound;

            //ToDo check if the -1 is needed (wasn't in C++ code)
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
                    clearedLines[clearedLineCount].isAlternate = false;
                    clearedLines[clearedLineCount].row = y;
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
                    clearedLines[clearedLineCount].isAlternate = true;
                    clearedLines[clearedLineCount].row = y;
                    clearedLineCount++;
                }
            }

            if (clearedLineCount > 0)
            {
                //Array.Copy(gameField, prevField, gameField.Length);

                for (int i = 0; i < clearedLineCount; i++)
                {
                    //now we'll set the hexes to cleared
                    SetLineToCleared(clearedLines[i].row, clearedLines[i].isAlternate);
                    //Array.Copy(gameField, preDropField, gameField.Length);
                }
                //and finally, copy and move everything down
                DropBoardAfterRowsCleared();
            }
        }

        private void SetLineToCleared(int y, bool isAlternate)
        {
            int yLow = y;

            //to make this really easy we're going to do this one column at a time
            for (int i = 0; i < GAME_WIDTH; i++)
            {
                if (isAlternate)
                    yLow = y + i % 2;
                else
                    yLow = y;

                gameField[i, yLow].ePiece = HexType.Erased;
            }
            ScoreClearedLine();
        }

        private void ScoreClearedLine()
        {
            Score += 100;
            RowsCleared++;

            if (RowsCleared % 10 == 0)
                Level++;
        }

        void DropBoardAfterRowsClearedNew()
        {
            for (int i = 0; i < GAME_WIDTH; i++)
            {
                for(int j = 0; j < GAME_HEIGHT - 1; j++)
                {
                    if(gameField[i,j].ePiece == HexType.Erased)
                    {   //move everything down
                        for (int y = j; y < GAME_HEIGHT -1; y++)
                        {
                            gameField[i, y].ePiece = gameField[i, y + 1].ePiece;
                            gameField[i, y].indexColor = gameField[i, y + 1].indexColor;
                        }

                    }


                }
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
                            //cough ... refrence types not value types ....
                            gameField[i, k].ePiece = gameField[i, k + 1].ePiece;
                            gameField[i, k].indexColor = gameField[i, k + 1].indexColor;
                        }

                        //and 
                        gameField[i, GAME_HEIGHT - 1].ePiece = HexType.Blank;
                    }
                }
            }
        }

        public GamePiece GetCurrentPiece ()
        {
            return currentPiece;
        }
    }
}