using System;

namespace Hextris.Core
{
    //ToDo needs renaming
    public struct ClearedType
    {
        public int iRow { get; set; }
        public bool bAlt { get; set; }
    };

    enum GameState
    {
        Ingame,
        GameOver,
        Paused,
        Menu,
    };

    enum GameType
    {
        Classic,     //progress through the levels
        Challenge,   //garbage at start of each level, must get 10 lines to progress then the level resets with new garbage 
        Patterns,    //Pre-defined preset block patterns ... 50ish??
        Speed,       //	GT_Speed,		//like classic but garbage comes up from the bottom of the play field
        Ultra,       //accumulate as many points as possible in 3 minutes
        Clear40,          //clear 40 lines as quickly as possible
        count,
    };

    public enum HexType
    {
        Wall,
        GamePiece,
        Erased,
        Blank,
    };

    public class GameBoard
    {
        public static int GAME_WIDTH = 10;
        public static int GAME_HEIGHT = 20;
        static int NUM_PIECE_TYPES = (int)PieceType.count;
        static int NUM_PREVIEWS = 3;
        static int MAX_LINES = 8;

        GameState gameState;
        GameType gameType = GameType.Classic;

        GameHexagon[,] gameField = new GameHexagon[GAME_WIDTH, GAME_HEIGHT];//our game field
	    GameHexagon[,] prevField = new GameHexagon[GAME_WIDTH, GAME_HEIGHT];//before a line remove
	    GameHexagon[,] preDropField = new GameHexagon[GAME_WIDTH, GAME_HEIGHT];//lines erased without dropping

        ClearedType[] clearedLines = new ClearedType[MAX_LINES];

        GamePiece savedPiece;
        GamePiece ghostPiece;
        GamePiece currentPiece;
        GamePiece[] piecePreviews = new GamePiece[NUM_PIECE_TYPES];

        int[] iPieceStats = new int[(int)PieceType.count]; 
        int rows = GAME_HEIGHT;
        int rowsCleared;
        int startingLevel;
        int time;
        int score;
        int level;

        int[] stats = new int[NUM_PIECE_TYPES];

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

        public void OnTimerTick () //from rendering engine
        {
            currentPiece.MoveDown();

            if(CheckCollision(currentPiece) == true)
            {
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

        void ResetGame ()
        {
            score = 0;
            rowsCleared = 0;
            level = startingLevel;
            time = 0;
            newHighScore = false;

            for (int i = 0; i < NUM_PIECE_TYPES; i++)
                stats[i] = 0;

            savedPiece.SetRandomPieceType();
            currentPiece.SetPosition(4, GAME_HEIGHT);

            //reset the preview pieces
            for (int i = 0; i < NUM_PREVIEWS; i++)
                piecePreviews[i].SetRandomPieceType();

            NewPiece();
            CalcGhost();
        }

        public void ResetBoard ()
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

        void NewPiece ()
        {
            var oldPiece = currentPiece;
            currentPiece = piecePreviews[0];

            for (int i = 0; i < NUM_PREVIEWS - 1; i++)
                piecePreviews[i] = piecePreviews[i + 1];

            oldPiece.SetRandomPieceType();
            oldPiece.SetPosition(4, GAME_HEIGHT);
            piecePreviews[NUM_PREVIEWS - 1] = oldPiece;

            stats[(int)currentPiece.PieceType]++;
        }

        void NewGame ()
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
            gameState = GameState.Ingame;
        }

        public bool OnUp ()
        {
            currentPiece.MoveUp();
            return true;
        }

        public bool OnDown ()
        {
            currentPiece.MoveDown();

            if(CheckCollision(currentPiece))
            {
                currentPiece.MoveUp();
                return false;
            }
            return true;
        }

        public bool OnLeft ()
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

        public bool OnRight ()
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

        public bool OnRotate ()
        {
            currentPiece.Rotate();

            if(CheckCollision(currentPiece))
            {
                currentPiece.MoveLeft();
                if(CheckCollision(currentPiece))
                {
                    currentPiece.MoveRight();
                    currentPiece.MoveRight();

                    if(CheckCollision(currentPiece))
                    {
                        //give up
                        //TODO
                        currentPiece.MoveLeft();
                        currentPiece.MoveUp();
                        return false;
                    }
                }
            }

            CalcGhost();

            return true;
        }

        public bool OnDrop ()
        {
            return currentPiece.CopyPieceState(ghostPiece);
        }

        public bool OnSwitchPiece ()
        {

            return false;
        }

        private void SetPieceToBoard(GamePiece gamePiece)
        {
            int iYOffset = 0;

            for (int i = 0; i < 5; i++)
            {
                if ((i + currentPiece.GetX()) % 2 == 0)
                    iYOffset++;

                for (int j = 0; j < 5; j++)
                {
                    if (currentPiece.GetHex(i, j).ePiece == HexType.GamePiece)
                    {
                        int x2 = currentPiece.GetX() + i;
                        int y2 = currentPiece.GetY() - j - iYOffset;

                        gameField[x2,y2].ePiece = HexType.GamePiece;
                        gameField[x2,y2].indexColor = (int)currentPiece.PieceType;
                    }
                }
            }
            CheckForEndGameState();
        }

        private void CheckForEndGameState()
        {
            for (int i = 0; i < GAME_WIDTH; i++)
            {
                if (gameField[i,rows - 1].ePiece == HexType.GamePiece)
                {   //game over
                    gameState = GameState.GameOver;
                    return;
                }
            }
        }

        private bool CheckCollision(GamePiece gamePiece)
        {
            if (gamePiece == null)
                return false;

            int iYOffset = 0;//I'm not happy about this either ....
            for (int x = 0; x < 5; x++)
            {
                if ((x + gamePiece.GetX()) % 2 == 0)
                    iYOffset++;

                for (int y = 0; y < 5; y++)
                {
                    if (gamePiece.GetHex(x,y).ePiece == HexType.GamePiece)
                    {
                        int x2 = gamePiece.GetX() + x;
                        int y2 = gamePiece.GetY() - y - iYOffset;

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
            var iClearedLines = 0;

            bool bCompLine;
            bool bCompAlt;

            //ToDo check if the -1 is needed (wasn't in C++ code)
            for (int y = 0; y < rows - 1; y++)
            {
                bCompLine = true;

                for (int x = 0; x < GAME_WIDTH; x++)
                {
                    if (gameField[x,y].ePiece != HexType.GamePiece)
                    {
                        bCompLine = false;
                    }
                }

                if (bCompLine)
                {
                    clearedLines[iClearedLines].bAlt = false;
                    clearedLines[iClearedLines].iRow = y;
                    iClearedLines++;
                }

                bCompAlt = true;

                for (int x = 0; x < GAME_WIDTH; x++)
                {
                    if (gameField[x, y + x % 2].ePiece != HexType.GamePiece)
                    {
                        bCompAlt = false;
                    }
                }

                if (bCompAlt)
                {
                    clearedLines[iClearedLines].bAlt = true;
                    clearedLines[iClearedLines].iRow = y;
                    iClearedLines++;
                }
            }

            if (iClearedLines > 0)
            {
                //now we set our alt structures
                Array.Copy(gameField, prevField, gameField.Length);
                //memcpy(prevField, gameField, sizeof(GameHexagon) * GAME_WIDTH * MAX_GAME_HEIGHT);

                for (int i = 0; i < iClearedLines; i++)
                {
                    //now we'll set the hexes to cleared
                    SetLineToCleared(clearedLines[i].iRow, clearedLines[i].bAlt);
                    Array.Copy(gameField, preDropField, gameField.Length);
                  //  memcpy(preDropField, gameField, sizeof(GameHexagon) * GAME_WIDTH * GAME_HEIGHT);
                }
                //and finally, copy and move everything down
                DropPieces();
            }
        }

        private void SetLineToCleared(int iY, bool bAlt)
        {
            int iYLow = iY;

            //to make this really easy we're going to do this one column at a time
            for (int i = 0; i < GAME_WIDTH; i++)
            {
                if (bAlt)
                    iYLow = iY + i % 2;

                gameField[i, iYLow].ePiece = HexType.Erased;
            }
            ScoreClearedLine();
        }

        private void ScoreClearedLine()
        {

        }

        void DropPieces()
        {
            //to make this really easy we're going to do this one column at a time
            for (int i = 0; i < GAME_WIDTH; i++)
            {
                for (int j = rows - 1; j > -1; j--)
                {
                    if (gameField[i,j].ePiece == HexType.Erased)
                    {
                        for (int k = j; k < rows - 2; k++)
                        {
                            gameField[i,k] = gameField[i,k + 1];
                        }
                        //and no matter what, we clear the top
                        gameField[i, rows - 1].ePiece = HexType.Blank;
                    }
                }
            }
        }

        public GamePiece GetCurrentPiece ()
        {
            return currentPiece;
        }

        public void SetNumRows(int iRows)
        {
            this.rows = iRows;
        }

        public int GetNumRows() { return rows; }
    }
}
