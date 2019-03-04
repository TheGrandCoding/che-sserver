using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Che_ssServer.Classes
{
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
            x += this.ColX;
            y += this.ColY;
            x = Controller == PlayerColor.White ? x : 9 - x;
            y = Controller == PlayerColor.White ? y : 9 - y;
            return Game.GetLocation(x, y);
        }

        public ChessPiece PieceHere;
        public PlayerColor Controller => PieceHere.Color;

        public ChessPosition(int x, int y, ChessGame game) : base(game)
        {
            X = x;
            Y = y;
        }
    }
}
