using Che_ssServer.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Che_ssServer.Classes
{
    public class ChessPiece
    {
        public PieceType Type;
        public ChessPosition Location;
        public PlayerColor Color;

        public ChessPiece(PieceType type, ChessPosition pos, PlayerColor color)
        {
            Type = type; Location = pos; Color = color;
        }

        public MoveResult Move(ChessPosition to)
        {
            return MoveResult.FromSuccess(this.Location, to, "");
        }
    }
}
