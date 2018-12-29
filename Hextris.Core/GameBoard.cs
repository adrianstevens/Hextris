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

        GameState gameState;
        GameType gameType = GameType.Classic;

        GameHexagon[,] gameField = new GameHexagon[GAME_WIDTH, GAME_HEIGHT];//our game field
	    GameHexagon[,] prevField = new GameHexagon[GAME_WIDTH, GAME_HEIGHT];//before a line remove
	    GameHexagon[,] preDropField = new GameHexagon[GAME_WIDTH, GAME_HEIGHT];//lines erased without dropping

        GamePiece savedPiece;
        GamePiece ghostPiece;
        GamePiece currentPiece;

        int[] iPieceStats = new int[(int)PieceType.count]; 
        int iRows = GAME_HEIGHT;
        int iStartingLevel = 0;
        int iTime = 0;

        public GameBoard()
        {
            NewGame();
        }

        public void OnTimerTick () //from rendering engine
        {
        //    currentPiece.MoveDown();

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
            return false;
        }

        public bool OnDown ()
        {
            currentPiece.MoveDown();
            return true;
        }

        public bool OnLeft ()
        {
            currentPiece.MoveLeft();
            return true;
        }

        public bool OnRight ()
        {
            currentPiece.MoveRight();
            return true;
        }

        public bool OnRotate ()
        {
            currentPiece.Rotate();
            return true;
        }

        public bool OnSwitchPiece ()
        {

            return false;
        }

        private bool CheckCollision()
        {
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
            this.iRows = iRows;
        }

        public int GetNumRows() { return iRows; }
    }
}
