using System;

namespace Hextris.Core
{
    public enum PieceType
    {
        Line,
        Square,
        Bend1,
        Bend2,
        Club1,
        Club2,
        ZigZag1,
        ZigZag2,
        U,
        Tri,
        count,
    }

    public class GamePiece
    {
        Random rand = new Random();

        static byte[,] pieces;
        static byte[] piecesData;

        GameHexagon[,] data = new GameHexagon[5, 5];

        PieceType pieceType;
        int xPos, yPos;
        
        public GamePiece ()
        {
            pieceType = GetRandomType();
            Init ();

            SetPiece(pieceType); //for now
        }

        PieceType GetRandomType ()
        {
            var value = rand.Next((int)PieceType.count);

            return (PieceType)value;
        }

        public void Init()
        {
            piecesData = new byte[]  { 0,0,0,0,0,
                                        0,0,0,0,0,
                                        1,1,1,1,0,
                                        0,0,0,0,0,
                                        0,0,0,0,0 ,//line

                                        0,0,0,0,0,
                                        0,1,1,0,0,
                                        0,1,1,0,0,
                                        0,0,0,0,0,
                                        0,0,0,0,0 , //Square

                                        0,0,0,0,0,		//'L' 1
                                        0,0,0,1,0,
                                        1,1,1,0,0,
                                        0,0,0,0,0,
                                        0,0,0,0,0 ,

                                        0,0,0,0,0,		//'L' 2
                                        0,1,0,0,0,
                                        0,1,1,1,0,
                                        0,0,0,0,0,
                                        0,0,0,0,0 ,

                                        0,0,0,0,0,		//Club 1
                                        0,0,1,0,0,
                                        0,1,1,1,0,
                                        0,0,0,0,0,
                                        0,0,0,0,0 ,

                                        0,0,0,0,0,		//Club 2
                                        0,0,0,1,0,
                                        0,1,1,1,0,
                                        0,0,0,0,0,
                                        0,0,0,0,0 ,

                                        0,0,0,0,0,		//Zig-zag 1
                                        0,1,1,0,0,
                                        0,0,1,1,0,
                                        0,0,0,0,0,
                                        0,0,0,0,0 ,

                                        0,0,0,0,0,		//Zig-zag 2
                                        0,0,1,1,0,
                                        1,1,0,0,0,
                                        0,0,0,0,0,
                                        0,0,0,0,0 ,

                                        0,0,0,0,0,		//'U'
                                        0,1,0,1,0,
                                        0,1,1,0,0,
                                        0,0,0,0,0,
                                        0,0,0,0,0 ,

                                        //Radiation
                                        0,0,0,0,0,
                                        0,0,0,1,0,
                                        0,1,1,0,0,
                                        0,0,1,0,0,
                                        0,0,0,0,0 };

            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    data[i, j] = new GameHexagon();
                }
            }
        }

        void SetPiece (PieceType ePiece)
        {
            int index = (int)ePiece;

            for (int y = 0; y < 5; ++y)
            {
                for (int x = 0; x < 5; ++x)
                {
                    if (piecesData[x + y * 5 + index * 25] == 1)
                        data[x, y].ePiece = HexType.GamePiece;
                    else
                        data[x, y].ePiece = HexType.Blank; 
                }
            }
        }

        public void Rotate ()
        {
            GameHexagon temp;

            temp = data[1,3];          //Inner loop
            data[1,3] = data[1,2];
            data[1,2] = data[2,1];
            data[2,1] = data[3,1];
            data[3,1] = data[3,2];
            data[3,2] = data[2,3];
            data[2,3] = temp;

            temp = data[0,4];          //One outer loop
            data[0,4] = data[0,2];
            data[0,2] = data[2,0];
            data[2,0] = data[4,0];
            data[4,0] = data[4,2];
            data[4,2] = data[2,4];
            data[2,4] = temp;

            temp = data[1,4];          //The other one
            data[1,4] = data[0,3];
            data[0,3] = data[1,1];
            data[1,1] = data[3,0];
            data[3,0] = data[4,1];
            data[4,1] = data[3,3];
            data[3,3] = temp;
        }

        public bool CopyPieceState (GamePiece piece)
        {
            if (piece == null)
                return false;

            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    data[i,j] = piece.GetHex(i, j);

                }
            }

            xPos = piece.GetX();
            yPos = piece.GetY();
            pieceType = piece.GetPieceType();

            return true;
        }

        public PieceType GetPieceType ()
        {
            return pieceType;
        }

        public void SetPosition(int x, int y)
        {
            xPos = x;
            yPos = y;
        }

        public int GetX ()
        {
            return xPos;
        }

        public int GetY ()
        {
            return yPos;
        }

        public GameHexagon GetHex (int x, int y)
        {
            return data[x,y];
        }

        public void MoveDown ()
        {
            yPos--;
        }

        public void MoveUp ()
        {
            yPos++;
        }

        public void MoveLeft ()
        {
            xPos--;
        }

        public void MoveRight ()
        {
            xPos++;
        }
        
    }
}
