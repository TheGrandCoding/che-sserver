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
        public PinType Pinned;
        public TakableBy Takable => Location.Takable;
        private bool atStartingPosition = true;

        public bool Taken => Location == null;


        public ChessPiece(PieceType type, ChessPosition pos, PlayerColor color, ChessGame game) : base(game)
        {
            Type = type; Location = pos; Color = color;
        }

        public List<ChessPosition> GetMoveablePositions()
        {
            List<ChessPosition> pos = new List<ChessPosition>();
            if (Type == PieceType.Pawn)
            {
                if (Location.ColY == 8)
                {
                    // can't move at all
                }
                else if (Location.ColY == 7)
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
                    var leftDiag = this.Location.GetRelative(-1, 1);
                    var rightDiag = this.Location.GetRelative(1, 1);
                    if (leftDiag != null && leftDiag.PieceHere != null)
                        pos.Add(leftDiag);
                    if (rightDiag != null && rightDiag.PieceHere != null)
                        pos.Add(rightDiag);
                }
            }
            else if (Type == PieceType.Bishop)
            {
                pos = GetDiagonalPositions();
            }
            else if (Type == PieceType.Rook)
            {
                pos = GetCrossPositions();
            }
            else if (Type == PieceType.Queen)
            {
                pos = GetCrossPositions();
                pos.AddRange(GetDiagonalPositions());
            }
            else if (Type == PieceType.King)
            {
                pos.Add(Location.GetRelative(-1, 1) ?? Location);  // Top left
                pos.Add(Location.GetRelative(0, 1) ?? Location);  // Top centre
                pos.Add(Location.GetRelative(1, 1) ?? Location); // Top right
                pos.Add(Location.GetRelative(0, 1) ?? Location); // Middle right 
                pos.Add(Location.GetRelative(1, -1) ?? Location); // Bottom right
                pos.Add(Location.GetRelative(0, -1) ?? Location); // Bottom centre
                pos.Add(Location.GetRelative(-1, -1) ?? Location); // Bottom left
                pos.Add(Location.GetRelative(-1, 0) ?? Location); // Middle left
                // removes any references to current position
                pos = pos.Where(x => x.Pos != Location.Pos).ToList();
                // removes any places that can be taken by the opposition
                if (this.Color == PlayerColor.White)
                    pos = pos.Where(x => !x.Takable.HasFlag(TakableBy.Black)).ToList();
                else
                    pos = pos.Where(x => !x.Takable.HasFlag(TakableBy.White)).ToList();
            }
            else
            {
                throw new NotImplementedException();
            }
            return ReturnValidLocations(pos);
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
                //                       |
                // needs to be before as V cannot be null
                this.Location.PieceHere = null; // remove from old place
                to.PieceHere = this; // update new place's knowledge of this
                this.Location = to; // update our knowledge of the new place
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



    [Flags]
    public enum PinType
    {
        /// <summary>
        /// Piece is not pinned at all
        /// </summary>
        NotPinned = 0b0000,
        /// <summary>
        /// Piece is pinned horizontally: --
        /// </summary>
        Horizontal = 0b0001,
        /// <summary>
        /// Piece is pinned vertically: |
        /// </summary>
        Vertical = 0b0010,
        /// <summary>
        /// Piece is pinned diagonally to right: /
        /// </summary>
        DiagonalRight = 0b0100,
        /// <summary>
        /// Piece is pinned diagonally to left: \
        /// </summary>
        DiagonalLeft = 0b1000,
        FullDiagonal = DiagonalLeft | DiagonalRight,
        FullCross = Horizontal | Vertical,
        /// <summary>
        /// Piece is fully pinned and cannot move at all
        /// </summary>
        Fully = Horizontal | Vertical | DiagonalLeft | DiagonalRight
    }

}
