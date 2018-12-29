namespace Hextris.Core
{
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
        static int GAME_WIDTH = 10;
        static int GAME_HEIGHT = 40;
        static int NUM_PIECE_TYPES = (int)PieceType.count;
        static int NUM_PREVIEWS = 3;

        GameState gameState;
        GameType gameType = GameType.Classic;

        GameHexagon[,] gameField = new GameHexagon[GAME_WIDTH, GAME_HEIGHT];//our game field
	    GameHexagon[,] prevField = new GameHexagon[GAME_WIDTH, GAME_HEIGHT];//before a line remove
	    GameHexagon[,] preDropField = new GameHexagon[GAME_WIDTH, GAME_HEIGHT];//lines erased without dropping

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
            savedPiece = new GamePiece();
            ghostPiece = new GamePiece();

            for (int i = 0; i < NUM_PREVIEWS; i++)
            {
                piecePreviews[i] = new GamePiece();
                piecePreviews[i].Init();
                piecePreviews[i].SetPosition(4, 20);
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

        void Reset ()
        {
            score = 0;
            rowsCleared = 0;
            level = startingLevel;
            time = 0;
            newHighScore = false;

            for (int i = 0; i < NUM_PIECE_TYPES; i++)
                stats[i] = 0;

            ResetBoard();

            savedPiece.Init();

            //reset the preview pieces
            for (int i = 0; i < NUM_PREVIEWS; i++)
                piecePreviews[i].Init();

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

            oldPiece.Init();
            oldPiece.SetPosition(-5, rows + 2);
            piecePreviews[NUM_PREVIEWS - 1] = oldPiece;

            stats[(int)currentPiece.GetPieceType()]++;
        }

        void NewGame ()
        {
            savedPiece = new GamePiece();
            currentPiece = new GamePiece();
            currentPiece.SetPosition(2, 10);

            int iTime = 0;

            ResetBoard();

            CalcGhost();

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
                        gameField[x2,y2].indexColor = (int)currentPiece.GetPieceType();
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

        }

        private void CheckForCompleteLines ()
        {

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
