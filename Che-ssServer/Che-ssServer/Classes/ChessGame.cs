﻿using Che_ssServer.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Che_ssServer.Classes
{
    public class ChessGame
    {
        public Player White;
        public Player Black;
        public TimeSpan WhiteTime;
        public TimeSpan BlackTime;

        public Dictionary<int, GameDelta> PastDeltas = new Dictionary<int, GameDelta>();

        public Player CurrentlyWaitingFor;

        /// <summary>
        ///    X1 X2 X3 X4 X5 X6 X7 X8
        /// Y8
        /// Y7
        /// Y6
        /// Y5
        /// Y4
        /// Y3
        /// Y2
        /// Y1
        ///    X1 X2 X3 X4 X5 X6 X7 X8
        /// </summary>
        public ChessPosition[,] Board;

        public void StartUp()
        {
            Board = new ChessPosition[8, 8];
            for (int x = 1; x <= 8; x++)
            {
                // x goes across bottom
                for (int y = 1; y <= 8; y++)
                {
                    // y goes vertical
                    var newPosition = new ChessPosition(x, y, this);
                    var player = y > 6 ? PlayerColor.Black : PlayerColor.White;
                    ChessPiece piece = null;
                    // Set types.
                    if (y == 2 || y == 7)
                    {
                        piece = new ChessPiece(PieceType.Pawn, newPosition, player, this);
                    }
                    if (x == 1 || x == 8)
                    {
                        if (y == 1 || y == 8)
                        {
                            piece = new ChessPiece(PieceType.Rook, newPosition, player, this);
                        }
                    }
                    if (x == 2 || x == 7)
                    {
                        if (y == 1 || y == 8)
                        {
                            piece = new ChessPiece(PieceType.Knight, newPosition, player, this);
                        }
                    }
                    var pos = new ChessPosition(x, y, this, piece);
                    pos.PieceHere = piece;
                    if(pos.PieceHere != null)
                        pos.PieceHere.Location = pos;
                    Board[x - 1, y - 1] = pos;
                }
            }
            White.ClearListeners();
            Black.ClearListeners();
            White.RecievedMessage += White_RecievedMessage;
            Black.RecievedMessage += Black_RecievedMessage;
            CurrentlyWaitingFor = White;
            BroadCastDeltas();
        }

        public void BroadCastDeltas()
        {
            var lastDelta = PastDeltas.Count > 0 ? PastDeltas.Values.LastOrDefault() : null;
            var newDelta = new GameDelta(this, lastDelta);
            PastDeltas.Add(newDelta.ID, newDelta);
            string message = newDelta.GetDelta();
            White.Send(message);
            Black.Send(message);
        }

        private void SwitchPlayers()
        {
            if (CurrentlyWaitingFor.Color == PlayerColor.White)
                CurrentlyWaitingFor = Black;
            else
                CurrentlyWaitingFor = White;
            string message = $"PLY:{CurrentlyWaitingFor.Color};{WhiteTime};{BlackTime}";
            White.Send(message); // send both who's turn it is to go next
            Black.Send(message); // and also sync the timers across both clients
        }

        private void Black_RecievedMessage(object sender, string e)
        {
            throw new NotImplementedException();
        }

        private void White_RecievedMessage(object sender, string e)
        {
            throw new NotImplementedException();
        }

        public ChessPosition GetLocation(int x, int y)
        { // need to offset x/y as the Board is zero-based, while x/y are 1-based
            return Board[x-1, y-1];
        }
    }
}
