using Che_ssServer.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Che_ssServer.Classes
{
    public class ChessGame
    {
        public Player White;
        public Player Black;
        public TimeSpan WhiteTime;
        public TimeSpan BlackTime;
        public bool HasWhiteGoneFirst { get; protected set; }
        public System.Timers.Timer TickTimer;

        public List<ChessPiece> TakenPieces = new List<ChessPiece>();

        public Dictionary<int, GameDelta> PastDeltas = new Dictionary<int, GameDelta>();

        public List<ChessPiece> AllPieces = new List<ChessPiece>();

        public Player CurrentlyWaitingFor;

        /// <summary>
        /// --- Board is same as in client
        ///    A  B  C  D  E  F  G  H
        ///    X1 X2 X3 X4 X5 X6 X7 X8
        /// Y1
        /// Y2      === White ===
        /// Y3
        /// Y4
        /// Y5
        /// Y6
        /// Y7      === Black ===
        /// Y8
        ///    X1 X2 X3 X4 X5 X6 X7 X8
        ///    A  B  C  D  E  F  G  H
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
                    Program.Log(pos.ToString(), Services.LogSeverity.Debug, "Board");
                    pos.PieceHere = piece;
                    if(pos.PieceHere != null)
                    {
                        pos.PieceHere.Location = pos;
                        AllPieces.Add(piece);
                    }
                    Board[x - 1, y - 1] = pos;
                }
            }
            White.ClearListeners();
            Black.ClearListeners();
            White.RecievedMessage += White_RecievedMessage;
            Black.RecievedMessage += Black_RecievedMessage;
            White.Color = PlayerColor.White;
            Black.Color = PlayerColor.Black;
            CurrentlyWaitingFor = White;
            BroadCastDeltas();
        }

        public void BroadCastDeltas()
        {
            var lastDelta = PastDeltas.Count > 0 ? PastDeltas.Values.LastOrDefault() : null;
            var newDelta = new GameDelta(this, lastDelta);
            PastDeltas.Add(newDelta.ID, newDelta);
            string message = newDelta.GetDelta();
            White.Send("GAME:" + message);
            Black.Send("GAME:" + message);
        }

        int timerTicks = 0;
        private void timerTick(object sender, ElapsedEventArgs e)
        {
            timerTicks++;
            if(CurrentlyWaitingFor.Color == PlayerColor.White)
            {
                WhiteTime = WhiteTime.Add(TimeSpan.FromMilliseconds(TickTimer.Interval));
            }
            else
            {
                BlackTime = BlackTime.Add(TimeSpan.FromMilliseconds(TickTimer.Interval));
            }
            int delay = 1000 * 10; 
            // force a broadcast every 'delay' miliseconds
            if(timerTicks > (delay / TickTimer.Interval))
            {
                // we force a delta to sync back up...
                timerTicks = 0;
                BroadCastDeltas();
            }
        }

        private void SwitchPlayers()
        {
            if (CurrentlyWaitingFor.Color == PlayerColor.White)
                CurrentlyWaitingFor = Black;
            else
                CurrentlyWaitingFor = White;
            /*string message = $"PLY:{CurrentlyWaitingFor.Color};{WhiteTime};{BlackTime}";
            White.Send(message); // send both who's turn it is to go next
            Black.Send(message); // and also sync the timers across both clients*/
            BroadCastDeltas();
        }

        private bool SharedActions(Player player, Player opposite, PlayerColor color, string message)
        {
            if(message.StartsWith("MOVE:"))
            { // expected: "MOVE:[from]:[to]"
                var split = message.Split(':');
                ChessPosition from = GetLocation(split[1]);
                ChessPosition to = GetLocation(split[2]);
                if(from.PieceHere != null)
                {
                    if(from.PieceHere.Color != player.Color)
                    {
                        player.Send($"RES/MOVE/ERR:{from.Pos}:{to.Pos}:Piece not owned by player");
                    }
                    var result = from.PieceHere.Move(to);
                    if(result.IsSuccess)
                    {
                        player.Send($"RES/MOVE/SUC:{from.Pos}:{to.Pos}:Moved successfully");
                        if (player.Color == PlayerColor.White && !HasWhiteGoneFirst)
                        {
                            HasWhiteGoneFirst = true;
                            TickTimer = new System.Timers.Timer(250);
                            TickTimer.Elapsed += timerTick;
                            TickTimer.Start();
                        }
                        opposite.Send($"OTH/MOVE:{from.Pos}:{to.Pos}");
                        SwitchPlayers();
                    }
                    else
                    {
                        player.Send($"RES/MOVE/ERR:{from.Pos}:{to.Pos}:{result.Message}");
                    }
                } else
                {
                    player.Send($"RES/MOVE/ERR:{from.Pos}:{to.Pos}:No piece on that square");
                }
            }
            else if(message == "RESIGN")
            {
                player.Send("LOSE:RESIGN");
                opposite.Send("WIN:RESIGN");
            } else
            { 
                // not handled
                return false;
            } 
            // handled
            return true;
        }

        private void Black_RecievedMessage(object sender, string e)
        {
            if(CurrentlyWaitingFor == Black)
            {
                if(SharedActions(Black, White, PlayerColor.Black, e))
                { // it was a shared action, and was handled.
                } else
                {
                    // some other action
                }
                BroadCastDeltas();
            }
        }

        private void White_RecievedMessage(object sender, string e)
        {
            if(CurrentlyWaitingFor == White)
            {
                if (SharedActions(White, Black, PlayerColor.White, e))
                { // it was a shared action, and was handled.
                }
                else
                {
                    // some other action
                }
                BroadCastDeltas();
            }
        }

        /// <summary>
        /// Goes through each piece and determines what square it can take, and if anyone has won.
        /// </summary>
        public void EvaluateBoard()
        {
            foreach(var pos in Board)
            {
                pos.Takable = TakableBy.None; // reset
                pos.PieceHere.Pinned = PinType.NotPinned;
            }

            foreach(var piece in AllPieces)
            {
                if (piece.Taken)
                    continue;
                var locations = piece.GetMoveablePositions();
                foreach(var loc in locations)
                {
                    loc.Takable |= Program.TakeFor(piece.Color);
                }
            }

        }

        public ChessPosition GetLocation(int x, int y)
        { // need to offset x/y as the Board is zero-based, while x/y are 1-based
            ChessPosition pos = null;
            try
            {
                pos = Board[x - 1, y - 1];
            } catch
            {
            }
            return pos;
        }
        public ChessPosition GetLocation(string pos)
        {
            int x = Program.StrToX(pos.Substring(0, 1));
            int y = int.Parse(pos.Substring(1));
            return Board[x - 1, y - 1];
        }
    }
}
