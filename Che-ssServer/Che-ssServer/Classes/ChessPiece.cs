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

        public bool WasPromoted;

        public ChessPosition Location;
        public PlayerColor Color;
        public PinType Pinned;
        public List<Pins> AllPins = new List<Pins>();
        public TakableBy Takable => Location.Takable;
        private bool atStartingPosition = true;

        public bool Taken => Location == null;

        public ChessPiece(PieceType type, ChessPosition pos, PlayerColor color, ChessGame game) : base(game)
        {
            Type = type; Location = pos; Color = color;
        }

        /// <summary>
        /// Returns the <see cref="AllPins"/> as a readable string
        /// </summary>
        /// <returns></returns>
        public string GetPinsAsString()
        {
            string str = "Pins: ";
            foreach(var pin in AllPins)
            {
                str += $"[{pin.By.Location.Pos}, {pin.Type}], ";
            }
            str = str.Substring(0, str.Length - 2); // strips last ", "
            return str;
        }

        public AttemptMoveResult GetMoveablePositions(bool dontRemoveOwnColor = false)
        {
            List<ChessPosition> pos = new List<ChessPosition>();
            string errMessage = "";
            try
            {
                if (Type == PieceType.Pawn)
                {
                    if(this.Pinned != PinType.NotPinned)
                    {
                        if(this.Pinned == PinType.Vertical)
                        { // only pinned vertically, and pawns can only move vertically
                            // so it should move?
                        } else
                        {
                            errMessage = "Pawn pinned: " + Program.PinString(Pinned);
                        }
                    }
                    if (Location.ColY == 8)
                    {
                        errMessage = "Pawn at end, can only promote";
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
                    if(Pinned != PinType.NotPinned)
                    {
                        errMessage = "Bishop pinned: " + Program.PinString(Pinned);
                    }
                    pos = GetDiagonalPositions();
                }
                else if (Type == PieceType.Rook)
                {
                    if (Pinned != PinType.NotPinned)
                    {
                        errMessage = "Bishop pinned: " + Program.PinString(Pinned);
                    }
                    pos = GetCrossPositions();
                }
                else if (Type == PieceType.Queen)
                {
                    if (Pinned != PinType.NotPinned)
                    {
                        errMessage = "Bishop pinned: " + Program.PinString(Pinned);
                    }
                    pos = GetCrossPositions();
                    pos.AddRange(GetDiagonalPositions());
                }
                else if (Type == PieceType.King)
                {
                    pos.Add(Location.GetRelative(-1, 1) ?? Location);  // Top left
                    pos.Add(Location.GetRelative(0, 1) ?? Location);  // Top centre
                    pos.Add(Location.GetRelative(1, 1) ?? Location); // Top right
                    pos.Add(Location.GetRelative(1, -1) ?? Location); // Bottom right
                    pos.Add(Location.GetRelative(0, -1) ?? Location); // Bottom centre
                    pos.Add(Location.GetRelative(-1, -1) ?? Location); // Bottom left
                    pos.Add(Location.GetRelative(-1, 0) ?? Location); // Middle left
                    pos.Add(Location.GetRelative(1, 0) ?? Location); // Middle right 
                    // removes any references to current position
                    pos = pos.Where(x => x.Pos != Location.Pos).ToList();
                    // removes any places that can be taken by the opposition
                    if (this.Color == PlayerColor.White)
                        pos = pos.Where(x => !x.Takable.HasFlag(TakableBy.Black)).ToList();
                    else
                        pos = pos.Where(x => !x.Takable.HasFlag(TakableBy.White)).ToList();
                }
                else if(Type == PieceType.Knight)
                { // see issue https://github.com/TheGrandCoding/che-sserver/issues/1 for image
                    if (Pinned != PinType.NotPinned)
                    {
                        errMessage = "Knight pinned: " + Program.PinString(Pinned);
                    }
                    int[] up = new int[2] { 0, 2 };
                    int[] down = new int[2] { 0, -2 };
                    int[] left = new int[2] { -2, 0 };
                    int[] right = new int[2] { 2, 0 };
                    foreach(int[] direction in new List<int[]> { up, down,left,right})
                    {
                        if(direction[0] == 0)
                        { // we havnt changed x-axis, so we look left/right
                            var leftpos = Location.GetRelative(-1, direction[1]);
                            var rightpos = Location.GetRelative(1, direction[1]);
                            if(leftpos != null)
                                pos.Add(leftpos);
                            if(rightpos != null)
                                pos.Add(rightpos);
                        } else
                        {
                            var leftpos = Location.GetRelative(direction[0], 1);
                            var rightpos = Location.GetRelative(direction[0], -1);
                            if (leftpos != null)
                                pos.Add(leftpos);
                            if (rightpos != null)
                                pos.Add(rightpos);
                        }
                    }
                } else if(Type == PieceType.Barrier)
                {
                    pos.Add(Location.GetRelative(0, 1) ?? Location);  // up
                    pos.Add(Location.GetRelative(0, -1) ?? Location); // down
                    pos.Add(Location.GetRelative(-1, 0) ?? Location); // left
                    pos.Add(Location.GetRelative(1, 0) ?? Location); // right 
                    // removes any references to current position
                    pos = pos.Where(x => x.Pos != Location.Pos).ToList();
                }
                else
                { // technically shouldnt get here?
                    throw new NotImplementedException();
                }
            } catch (NotImplementedException){
                throw;
            } catch (Exception ex)
            {
                Program.Log($"Move:{this.Location.Pos}", ex);
            }
            if(errMessage.Contains("pinned"))
            {
                errMessage += "\r\n" + GetPinsAsString();
            }
            return new AttemptMoveResult(dontRemoveOwnColor ? pos : ReturnValidLocations(pos), string.IsNullOrWhiteSpace(errMessage), errMessage);
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
            // ---------------------------------------------------------------------------- //
            // Slightly copy-paste from client code                                         //
            // So any confusion = check comments in there                                   //
            // For how the |= and ^= things work, google or check the PinType enum itself   //
            // ---------------------------------------------------------------------------- //

            var left = new int[2] { 0, -1 };
            var right = new int[2] { 0, 1 };
            var up = new int[2] { 1, 0 };
            var down = new int[2] { -1, 0 };
            var dirList = new List<int[]>() { left, right, up, down };

            ChessPosition lastValidLocation = null;
            bool blocked = false;
            int amountInWay = 0;

            foreach(int[] direction in dirList) {
                // reset variables
                lastValidLocation = null;
                blocked = false;
                amountInWay = 0;
                PinType wePinWith = direction[0] == 0 ? PinType.Vertical : PinType.Horizontal;
                for (int i = 1; i <= 8; i++)
                {
                    var target = Location.GetRelative(i * direction[0], i * direction[1]);
                    if (target == null)
                        continue;
                    if (target.Pos == Location.Pos)
                        continue; // refers to *this* location
                    if (target.Controller != this.Color)
                    {
                        if (!blocked && (
                                this.Pinned == PinType.NotPinned ||
                                this.Pinned.HasFlag(wePinWith)
                                // if we're not pinned OR ... we are pinned? i am confusion
                            ))
                        {
                            // could move here.
                            pos.Add(target);
                            target.Takable = Program.TakeFor(this.Color);
                            lastValidLocation = target;
                        }

                        if(target.Controller != this.Color)
                        {
                            if(target.PieceHere?.Type == PieceType.King)
                            {
                                if (lastValidLocation != null && lastValidLocation.PieceHere != null)
                                {
                                    //                           This  V  is a bitwise operator, for use in the [Flags] thing
                                    lastValidLocation.PieceHere.Pinned |= wePinWith;
                                    lastValidLocation.PieceHere.AllPins.Add(new Pins(this, wePinWith));
                                }
                                break;
                            } else
                            {
                                if(blocked)
                                {
                                    if(lastValidLocation != null && lastValidLocation.PieceHere != null)
                                    {
                                        lastValidLocation.PieceHere.Pinned &= ~wePinWith; // removes ONLY our pin
                                        lastValidLocation.PieceHere.AllPins.RemoveAll(x => x.By == this && x.Type == wePinWith);
                                    }
                                }
                            }
                        }
                        if(target.PieceHere != null)
                        {
                            amountInWay += 1;
                            blocked = true;
                        }
                    } else
                    {
                        break;
                    }
                }
                if(lastValidLocation != null && amountInWay > 2)
                {
                    if (lastValidLocation.PieceHere != null)
                    {
                        lastValidLocation.PieceHere.Pinned ^= wePinWith; // removes ONLY our pin
                        lastValidLocation.PieceHere.AllPins.RemoveAll(x => x.By == this && x.Type == wePinWith);
                    }
                }
            }
            return ReturnValidLocations(pos);
        }

        private List<ChessPosition> GetDiagonalPositions()
        {
            var pos = new List<ChessPosition>();
            ChessPosition lastValidPosition = null;
            bool blocked = false;
            int amountInWay = 0;

            int[] rightUp = new int[2] { 1, 1 }; // /
            int[] rightDown = new int[2] { -1, 1 }; // \
            int[] leftDown = new int[2] { -1, -1 }; // /
            int[] leftUp = new int[2] { 1, -1 };

            foreach(int[] direction in new List<int[]>() { rightUp, rightDown, leftDown, leftUp})
            {
                lastValidPosition = null;
                blocked = false;
                amountInWay = 0;
                PinType wePinWith = direction[0] - direction[1] == 0 ? PinType.DiagonalRight : PinType.DiagonalLeft;

                for (int i = 1; i<=8; i++)
                {
                    var target = Location.GetRelative(i * direction[0], i * direction[1]);
                    if (target == null)
                        continue;
                    if (target.Pos == this.Location.Pos)
                        continue;
                    if(target.Controller != this.Color)
                    {
                        if(!blocked && 
                            (
                                this.Pinned == PinType.NotPinned ||
                                this.Pinned == wePinWith
                            ))
                        {
                            pos.Add(target);
                            lastValidPosition = target;
                        }
                        if(target.Controller != this.Color)
                        {
                            try
                            {
                                if(target.PieceHere != null && target.PieceHere.Type == PieceType.King)
                                {
                                    if(lastValidPosition != null && lastValidPosition.PieceHere != null)
                                    {
                                        lastValidPosition.PieceHere.Pinned = wePinWith;
                                        lastValidPosition.PieceHere.AllPins.Add(new Pins(this, wePinWith));
                                        break;
                                    } else
                                    {
                                        if(blocked)
                                        {
                                            lastValidPosition.PieceHere.Pinned &= ~wePinWith;
                                            lastValidPosition.PieceHere.AllPins.RemoveAll(x => x.By == this && x.Type == wePinWith);
                                        }
                                    }
                                }
                            } catch { }
                            if (target.PieceHere != null)
                            {
                                amountInWay += 1;
                                blocked = true;
                            }
                        } else
                        {
                            break;
                        }
                    }
                }
                if(lastValidPosition != null)
                {
                    if(amountInWay >= 2)
                    {
                        lastValidPosition.PieceHere.Pinned &= ~wePinWith;
                        lastValidPosition.PieceHere.AllPins.RemoveAll(x => x.By == this && x.Type == wePinWith);
                    }
                }
            }
            return pos;
        }

        public MoveResult Move(ChessPosition to)
        {
            var positions = this.GetMoveablePositions();
            if (positions.IsSuccess && positions.Locations.Contains(to, new ChessPositionEquality()))
            {
                if (to.PieceHere != null)
                {
                    if(to.PieceHere.Type == PieceType.Barrier)
                        return MoveResult.FromError(this.Location, to, $"{this.Type} is unable to take Barrier piece.");
                    if (this.Type == PieceType.Barrier)
                        return MoveResult.FromError(this.Location, to, $"Barrier is unable to take any pieces");
                }
                var result = MoveResult.FromSuccess(this.Location, to, $"{Location.Pos} to {to.Pos}");
                //                       |
                // needs to be before as V cannot be null
                this.Location.PieceHere = null; // remove from old place
                to.PieceHere = this; // update new place's knowledge of this
                this.Location = to; // update our knowledge of the new place
                return result;
            } else
            {
                if(positions.IsSuccess == false)
                { // We didnt attempt to find any positions (maybe because we are pinned)
                    return MoveResult.FromError(this.Location, to, $"{this.Type} prevented from moving: {positions.Message}");
                } else
                { // We did find locations, but the given wasnt in the list
                    return MoveResult.FromError(this.Location, to, $"{this.Type} is unable to move to {to.Pos}: ");
                }
            }
        }

        public override string ToString()
        {
            return $"{Color} {Type}";
        }
    }

    public class AttemptMoveResult : IResult
    {
        public bool IsSuccess { get; set; }

        public string Message { get; set; }

        public Exception Error => null;

        public IEnumerable<ChessPosition> Locations;

        public AttemptMoveResult(IEnumerable<ChessPosition> locations, bool success, string message)
        {
            IsSuccess = success;
            Message = message;
            Locations = locations;
        }
    }

    public class Pins
    {
        public PinType Type;
        public ChessPiece By;
        public Pins(ChessPiece by, PinType type)
        {
            Type = type;
            By = by;
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
    // So how does all this work? well basically:
    // If a piece is pinned horizontally, then its 'Pinned' variable would have the binary: 0001
    // If it was *also* pinned diagonally to the right, then its binary would be:           0101
    // As such, we can use the | operator to 'OR' the binary together, eg:
    //                  0001
    //                   OR
    //                  0100
    //                  ====
    //                  0101
    // The |= is the same as +=, in that you perform the OR, then assign the value to the variable
    //
    // The XOR operator ^= is exactly like an XOR operation, in that:
    //                  0101
    //                  XOR
    //                  0001
    //                  ====
    //                  0100
    // Thus, one can use the XOR (^=) to remove a piece from being pinned.

    // To check if a piece is pinned horizontally, could should simply use: PieceHere.Pinned.HasFlag(PinType.Horizontal)
    //                                          This will check to see if the binary 'flag' is present
    // If you use == PinType.Horizontal, it will probably not work
}
