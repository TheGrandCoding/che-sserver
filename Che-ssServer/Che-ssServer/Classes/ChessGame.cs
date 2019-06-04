using Che_ssServer.Helpers;
using Che_ssServer.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        public ChessGame()
        {
            Id = System.Threading.Interlocked.Increment(ref _id);
        }
        int _id = 0;
        public int Id { get; private set; }
        public Player White;
        public Player Black;
        public TimeSpan WhiteTime;
        public TimeSpan BlackTime;
        public bool HasWhiteGoneFirst { get; protected set; }
        public System.Timers.Timer TickTimer;
        /// <summary>
        /// 0 = not loaded at all
        /// 1 = loaded, awaiting reply
        /// 2 = heard first reply
        /// 3 = heard second reply, all is good
        /// </summary>
        public int HasJustLoadedSavedGame { get; private set; } = 0;

        public void Log(string message, LogSeverity severity = LogSeverity.Info)
        {
            Program.Log(message, severity, $"GAME/{Id}");
        }

        /// <summary>
        /// If non-null, indicates the game has been ended by the tournament
        /// The type indicates what condition caused it to end
        /// </summary>
        public EndConditions.IEndConditition TournamentEndDueTo;
        public WinType TournamentEndWinner;

        public event EventHandler<ChessGameWonEventArgs> GameOver;


        public Player WinnerPlayer => Winner == PlayerColor.White ? White : Black;
        public PlayerColor Winner = PlayerColor.NotControlled; // no winner default

        public List<ChessPiece> TakenPieces = new List<ChessPiece>();

        public List<Spectator> Spectators = new List<Spectator>();

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
                    if(y == 1 || y == 8)
                    {
                        if (x == 1 || x == 8)
                        {
                            piece = new ChessPiece(PieceType.Rook, newPosition, player, this);
                        }
                        else if (x == 2 || x == 7)
                        {
                            piece = new ChessPiece(PieceType.Knight, newPosition, player, this);
                        }else if (x == 3 || x == 6)
                        {
                            piece = new ChessPiece(PieceType.Bishop, newPosition, player, this);
                        } else if(x == 4 && y == 1)
                        {
                            piece = new ChessPiece(PieceType.King, newPosition, player, this);
                        } else if(x == 4 && y == 8)
                        {
                            piece = new ChessPiece(PieceType.Queen, newPosition, player, this);
                        }else if (x == 5 && y == 1)
                        {
                            piece = new ChessPiece(PieceType.Queen, newPosition, player, this);
                        }
                        else if(x == 5 && y == 8)
                        {
                            piece = new ChessPiece(PieceType.King, newPosition, player, this);
                        }
                    }
                    var pos = new ChessPosition(x, y, this, piece);
                    //Program.Log(pos.ToString(), Services.LogSeverity.Debug, "Board");
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

        /// <summary>
        /// Sends both players a list of things that have changed since the last delta.
        /// This means that less stuff is sent as not all of it needs to be re-sent if
        /// it was sent in the last delta.
        /// </summary>
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
            if(Winner != PlayerColor.NotControlled)
            {
                TickTimer.Stop();
                timerTicks = 100; // forces one final sync
            }
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

        public void SwitchPlayers()
        {
            if (CurrentlyWaitingFor.Color == PlayerColor.White)
                CurrentlyWaitingFor = Black;
            else
                CurrentlyWaitingFor = White;
            BroadCastDeltas();
        }

        /// <summary>
        /// Execute actions that can be done by either player WHILE IT IS THEIR TURN
        /// </summary>
        /// <param name="player">Player who's turn it currently is</param>
        /// <param name="opposite">The other player who is currently out of turn</param>
        /// <param name="color">The colour of the player currently going</param>
        /// <param name="message">The message recieved</param>
        /// <returns></returns>
        private bool SharedActions(Player player, Player opposite, PlayerColor color, string message)
        {
            if(message.StartsWith("MOVE:") && Winner == PlayerColor.NotControlled)
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
                        if (result?.PieceTook?.Type == PieceType.King)
                        {
                            // took a king, so game ends.
                            Winner = result.PieceTook.Color;
                            GameOver?.Invoke(this, new ChessGameWonEventArgs(this, WinnerPlayer, White == WinnerPlayer ? Black : White, "NOKING"));
                            return true; // we dont/cant evaluate any further, since the game has ended.
                        }
                        if (to?.PieceHere?.Type == PieceType.Pawn && (to.Y == 1 || to.Y == 8))
                        { // they need to promote their pawn before we can switch
                        } else
                        {
                            SwitchPlayers();
                        }
                        EvaluateBoard(); // Only re-evaluate if significantly changed (ie: moved)
                    }
                    else
                    {
                        player.Send($"RES/MOVE/ERR:{from.Pos}:{to.Pos}:{result.Message}");
                    }
                } 
                else
                {
                    player.Send($"RES/MOVE/ERR:{from.Pos}:{to.Pos}:No piece on that square");
                }
            }
            else if (message.StartsWith("PROMOTE:"))
            {
                message = message.Substring("PROMOTE:".Length);
                string[] split = message.Split(';');
                var target = GetLocation(split[0]);
                if(target != null)
                {
                    if(target.Y == 8 || target.Y == 1)
                    {
                        if (int.TryParse(split[1], out int id))
                        {
                            PieceType piece = (PieceType)id;
                            if (target.PieceHere != null && target.PieceHere.Type == PieceType.Pawn)
                            {
                                if (piece != PieceType.King && piece != PieceType.Pawn)
                                {
                                    target.PieceHere.Type = piece;
                                    target.PieceHere.WasPromoted = true;
                                    player.Send($"RES/PROMOTE/SUC:Promoted {target.Pos} to {piece.ToString()}");
                                    opposite.Send($"OTH/PROMOTE:{target.Pos}:{(int)piece}");
                                    SwitchPlayers(); // since we were waiting for this
                                    EvaluateBoard(); // Only re-evaluate if significantly changed (ie: promoted)
                                }
                                else
                                {
                                    player.Send($"RES/PROMOTE/ERR:Unable to promote to: {piece.ToString()}");
                                }
                            }
                            else
                            {
                                player.Send($"RES/PROMOTE/ERR:No pawn at {target.Pos}");
                            }
                        }
                        else
                        {
                            player.Send($"RES/PROMOTE/ERR:Unknown piece type: {split[1]}");
                        }
                    } else
                    {
                        player.Send($"RES/PROMOTE/ERR:Invalid row, must be at final row of enemy line");
                    }
                } else
                {
                    player.Send($"RES/PROMOTE/ERR:Unknown or invalid location: {split[0]}");
                }
            }
            else
            { 
                // not handled
                return false;
            } 
            // handled
            return true;
        }

        /// <summary>
        /// Execute actions at any point in time, even if 'out of turn'
        /// </summary>
        /// <param name="player">Player who sent the message</param>
        /// <param name="opposite">Opposition player</param>
        /// <param name="colour">Colour of sender</param>
        /// <param name="message">Message that was recieved</param>
        /// <returns></returns>
        private bool SharedPermenantActions(Player player, Player opposite, PlayerColor colour, string message)
        {
            if (message == "RESIGN")
            {
                Winner = opposite.Color;
                GameOver?.Invoke(this, new ChessGameWonEventArgs(this, opposite, player, "RESIGN"));
            } else
            { // Unknown message, so wasn't handled here.
                if(HasJustLoadedSavedGame > 0)
                {
                    if(message == "LOADED")
                    {
                        HasJustLoadedSavedGame++;
                    }
                    if(HasJustLoadedSavedGame >= 3)
                    { // both players have responded, so we continue
                        HasJustLoadedSavedGame = 0;
                        BroadCastDeltas();
                    }
                }
                return false;
            }
            return true;
        }

        private void Black_RecievedMessage(object sender, string e)
        {
            if(SharedPermenantActions(Black, White, PlayerColor.Black, e))
            { // message was handled by this
            } else
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
        }

        private void White_RecievedMessage(object sender, string e)
        {
            if (SharedPermenantActions(White, Black, PlayerColor.White, e))
            { // handled
            }
            else
            {
                if (CurrentlyWaitingFor == White)
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
        }

        private WinType CheckWin()
        { // TODO refactor
            if (this.TournamentEndDueTo != null)
                return WinType.EndCondition | this.TournamentEndWinner;

            var whitePieces = AllPieces.Where(x => x.Color == PlayerColor.White);
            var blackPieces = AllPieces.Where(x => x.Color == PlayerColor.Black);

            var survivingWhite = whitePieces.Where(x => !x.Taken);
            var survivingBlack = blackPieces.Where(x => !x.Taken);

            var whiteKing = whitePieces.FirstOrDefault(x => x.Type == PieceType.King);
            var blackKing = blackPieces.FirstOrDefault(x => x.Type == PieceType.King);

            if (whiteKing == null || whiteKing.Taken)
                return WinType.Black | WinType.NoKing;
            if (blackKing == null || blackKing.Taken)
                return WinType.White | WinType.NoKing;

            // TODO: this probably won't check if the self-color pieces are pinned.
            var whiteKingMoves = whiteKing.GetMoveablePositions(true); // already checks takable
            if (!whiteKingMoves.IsSuccess || whiteKingMoves.Locations.Count() == 0)
                return WinType.Black | WinType.CheckMate;
            var blackKingMoves = blackKing.GetMoveablePositions(true);
            if (!blackKingMoves.IsSuccess || blackKingMoves.Locations.Count() == 0)
                return WinType.White | WinType.CheckMate;

            return WinType.NoWin;
        }

        /// <summary>
        /// Goes through each piece and determines what square it can take, and if anyone has won.
        /// </summary>
        public void EvaluateBoard()
        {
            foreach(var pos in Board)
            {
                pos.Takable = TakableBy.None; // reset
                if(pos.PieceHere != null)
                {
                    pos.PieceHere.Pinned = PinType.NotPinned;
                    pos.PieceHere.AllPins = new List<Pins>();
                }
            }

            foreach (var piece in AllPieces)
            {
                if (piece.Taken)
                    continue;
                var locations = piece.GetMoveablePositions();
                foreach(var loc in locations.Locations)
                {
                    loc.Takable |= Program.TakeFor(piece.Color);
                }
            }


            var win = CheckWin();
            if(win != WinType.NoWin && Winner == PlayerColor.NotControlled)
            {
                Player winner;
                Player opposite;
                if(win.HasFlag(WinType.White))
                {
                    winner = White; opposite = Black;
                } else
                {
                    winner = Black; opposite = White;
                }
                string reason = "";
                if (win.HasFlag(WinType.NoKing))
                    reason += "NOKING";
                else if (win.HasFlag(WinType.CheckMate))
                    reason += "CHECKMATE";
                else if (win.HasFlag(WinType.EndCondition))
                    reason += $"END:{TournamentEndDueTo.Display}";
                Winner = winner.Color;
                this.GameOver?.Invoke(this, new ChessGameWonEventArgs(this, winner, opposite, reason));
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

        public void WinFromDisconnect(Player disconnect)
        {
            if(disconnect.Name == White.Name)
            {
                Winner = PlayerColor.Black;
            } else
            {
                Winner = PlayerColor.White;
            }
            GameOver?.Invoke(this, new ChessGameWonEventArgs(this, WinnerPlayer, disconnect, "LEFT"));
        }

        /// <summary>
        /// Gets a string that represents a save of the game
        /// </summary>
        /// <returns>A JSON string</returns>
        public string ToSave()
        {
            var gameDelta = new JsonGameDelta();
            gameDelta.black = Black.Name;
            gameDelta.white = White.Name;
            gameDelta.whiteTime = WhiteTime.ToString();
            gameDelta.blackTime = BlackTime.ToString();
            gameDelta.color = CurrentlyWaitingFor.Color;
            gameDelta.ID = -1; // indicates a save
            var jsonObject = JObject.FromObject(gameDelta);
            Dictionary<string, string[]> board = new Dictionary<string, string[]>();
            foreach(var item in Board)
            {
                string[] array = new string[] { };
                if (item.PieceHere != null)
                    array = new string[] { item.PieceHere.Type.ToString(), item.PieceHere.Color.ToString() };
                board.Add(item.Pos, array);
            }
            jsonObject.Add("board", JToken.FromObject(board));
            return jsonObject.ToString();
        }
        public void UpdateFromString(string savedGame)
        {
            JObject jsonObj = JObject.Parse(savedGame);
            var delta = JsonConvert.DeserializeObject<JsonGameDelta>(savedGame);
            // Apply delta
            this.WhiteTime = TimeSpan.Parse(delta.whiteTime);
            this.BlackTime = TimeSpan.Parse(delta.blackTime);
            this.CurrentlyWaitingFor = delta.color == PlayerColor.White ? this.White : this.Black;
            GameDelta lastDelta;
            if (PastDeltas.Count > 0)
                lastDelta = PastDeltas[PastDeltas.Count];
            else
                lastDelta = null;
            var gameDelta = new GameDelta(this, lastDelta);
            var board = jsonObj["board"].ToObject<Dictionary<string, string[]>>();
            foreach(var piece in this.AllPieces)
            {
                piece.hasBeenLoaded = false;
            }
            foreach(var keypair in board)
            {
                var pos = this.GetLocation(keypair.Key);
                if(keypair.Value.Length > 0)
                {
                    var pieceType = keypair.Value[0]; // eg "Pawn" "King" etc
                    var pieceColour = keypair.Value[1]; // White / Black
                    var firstPiece = this.AllPieces.FirstOrDefault(x => x.Type.ToString() == pieceType
                                                                && x.Color.ToString() == pieceColour);
                    if(firstPiece != null)
                    {
                        firstPiece.Location.PieceHere = null;
                        firstPiece.Location = pos;
                        pos.PieceHere = firstPiece;
                        firstPiece.hasBeenLoaded = true;
                    }
                }
            }
            HasJustLoadedSavedGame = 1;
            this.White.Send("LOAD:" + savedGame);
            this.Black.Send("LOAD:" + savedGame);
            foreach (var spec in this.Spectators)
                spec.Send("LOAD:" + savedGame);
        }
    }

    public enum WinType
    {
        NoWin        = 0,
        White        = 0b00001,
        Black        = 0b00010,
        CheckMate    = 0b00100,
        NoKing       = 0b01000,
        EndCondition = 0b10000,
        Draw = White | Black,
    }

    public class ChessGameWonEventArgs : EventArgs
    {
        public ChessGame Game;
        public Player Winner;
        public Player Loser;
        public string Reason;
        public ChessGameWonEventArgs(ChessGame game, Player win, Player loss, string reason)
        {
            Game = game;
            Winner = win;
            Loser = loss;
            Reason = reason;
        }
    }
}
