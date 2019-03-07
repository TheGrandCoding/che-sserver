using Che_ssServer.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Che_ssServer.Classes
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ChessPiece : ChessEntity
    {
        [JsonProperty]
        public PieceType Type;
        public ChessPosition Location;
        public PlayerColor Color;
        private bool atStartingPosition = true;


        public ChessPiece(PieceType type, ChessPosition pos, PlayerColor color, ChessGame game) : base(game)
        {
            Type = type; Location = pos; Color = color;
        }

        public List<ChessPosition> GetMoveablePositions()
        {
            List<ChessPosition> pos = new List<ChessPosition>();
            if(Type == PieceType.Pawn)
            {
                if(Location.ColX == 8)
                {
                    // can't move at all
                } else if(Location.ColX == 7)
                { // can only move forward once.
                    pos.Add(this.Location.GetRelative(0, 1));
                }
                else 
                { // can move one ahead AND more.
                    pos.Add(this.Location.GetRelative(0, 1));
                    if (atStartingPosition)
                    {
                        pos.Add(this.Location.GetRelative(0, 2));
                    }
                }
            } else if(Type == PieceType.Bishop)
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new NotImplementedException();
            }
            return pos;
        }

        private List<ChessPosition> ReturnValidLocations(IEnumerable<ChessPosition> positions)
        {
            List<ChessPosition> pos = new List<ChessPosition>();
            foreach(var position in positions) {
                if(position.Controller != this.Location.Controller)
                { // cannot move to a position of our own piece
                    pos.Add(position);
                }
            }
            return pos;
        }

        private List<ChessPosition> GetCrossPositions()
        { // find horizontal and diagonal positions
            var pos = new List<ChessPosition>();
            for(int x = Location.ColX; x >= 1; x--)
            {
                pos.Add(this.Location.GetRelative(Location.ColX - x, 0));
            }
            for(int x = Location.ColX; x <= 8; x++)
            {
                pos.Add(this.Location.GetRelative(x - Location.ColX, 0));
            }
            for (int y = Location.ColY; y >= 1; y--)
            {
                pos.Add(this.Location.GetRelative(0, Location.ColY - y));
            }
            for (int y = Location.ColY; y <= 8; y++)
            {
                pos.Add(this.Location.GetRelative(0, y - Location.ColY));
            }
            return ReturnValidLocations(pos);
        }

        private List<ChessPosition> GetDiagonalPositions()
        {
            var pos = new List<ChessPosition>();
            return pos;
        }

        public MoveResult Move(ChessPosition to)
        {
            var positions = this.GetMoveablePositions();
            if (positions.Contains(to, new ChessPositionEquality()))
            {
                var result = MoveResult.FromSuccess(this.Location, to, $"{Location.Pos} to {to.Pos}");
                if(to.PieceHere != null)
                {
                    // took something
                    Game.TakenPieces.Add(to.PieceHere);
                }
                to.PieceHere = this;
                this.Location.PieceHere = null;
                return result;
            } else
            {
                return MoveResult.FromError(this.Location, to, $"{this.Type} is unable to move to {to.Pos}");
            }
        }

        public override string ToString()
        {
            return $"{Color} {Type}";
        }
    }
}
