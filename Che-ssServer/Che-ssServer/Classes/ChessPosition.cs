using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Che_ssServer.Classes
{
    public class ChessPosition
    {
        public int X { get; }
        public int Y { get; }
        public ChessPiece PieceHere;
        public PlayerColor Controller => PieceHere.Color;

        public ChessPosition(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
