using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Che_ssServer.Classes
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ChessPosition : Helpers.ChessEntity
    {
        /// <summary>
        /// X as per the Board[,] variable in <see cref="Program"/>
        /// </summary>
        public int X { get; }

        /// <summary>
        /// Y as per the Board[,] variable in <see cref="Program"/>
        /// </summary>
        public int Y { get; }

        public TakableBy Takable;

        [JsonProperty]
        public string Pos => Program.XtoStr(X) + Y.ToString(); // eg, A4

        /// <summary>
        /// Relative X for the Color, where the Color is at the bottom.
        /// </summary>
        public int ColX => Controller == PlayerColor.White ? X : 9 - X;
        /// <summary>
        /// Relative Y for the Color, where the Color is at the bottom.
        /// </summary>
        public int ColY => Controller == PlayerColor.White ? Y : 9 - Y;

        public ChessPosition GetRelative(int x, int y)
        {
            x += this.ColX; // 1 + 1 == 2
            y += this.ColY;
            x = Controller == PlayerColor.White ? x : 9 - x;
            y = Controller == PlayerColor.White ? y : 9 - y;
            return Game.GetLocation(x, y);
        }

        [JsonProperty]
        public ChessPiece PieceHere;
        public PlayerColor Controller => PieceHere?.Color ?? PlayerColor.NotControlled;

        public ChessPosition(int x, int y, ChessGame game, ChessPiece piece = null) : base(game)
        {
            X = x;
            Y = y;
            PieceHere = piece;
        }


        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is ChessPosition che)
            {
                if(che.Pos == this.Pos)
                {
                    if(this.PieceHere != null)
                    {
                        return this.PieceHere.Equals(che.PieceHere);
                    } else
                    {
                        return che.PieceHere == null;
                    }
                }
            } 
            return false;
        }

        public override int GetHashCode()
        {
            var hashCode = 58288899;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<ChessPiece>.Default.GetHashCode(PieceHere);
            return hashCode;
        }

        public override string ToString()
        {
            return $"{Pos} ({X}, {Y}) [{ColX}, {ColY}] {PieceHere}";
        }

    }

    [Flags]
    public enum TakableBy
    {
        /// <summary>
        /// Location cannot be took by either player
        /// </summary>
        None = 0b00,

        /// <summary>
        /// Location could be taken by a white player
        /// </summary>
        White= 0b01,
        /// <summary>
        /// Location could be taken by a black player
        /// </summary>
        Black= 0b11,
        /// <summary>
        /// Location could be taken by either player
        /// </summary>
        Both = White | Black

    }

}
