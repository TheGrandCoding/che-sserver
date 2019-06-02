using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Che_ssServer.Classes;
using Che_ssServer.EndConditions;
using Che_ssServer.Helpers;

namespace Che_ssServer
{
    public class Tournament : Tournament<EndConditions.EndConditition>
    {
        public Tournament(EndConditition condition) : base(condition)
        {
        }
    }
    public class Tournament<T> where T : class, EndConditions.IEndConditition
    {
        public List<ChessGame> Games { get; set; } = new List<ChessGame>();
        public List<Player> Players { get
            {
                var player = new List<Player>();
                foreach (var game in Games)
                {
                    player.Add(game.White);
                    player.Add(game.Black);
                }
                player = player.Where(x => x != null).ToList();
                return player;
            } }
        public List<Spectator> Spectators { get
            {
                return Games.SelectMany(x => x.Spectators).ToList();
            } }
        public List<Connection> Connections
        {
            get
            {
                List<Connection> connections = new List<Connection>();
                foreach(var p in Players)
                    connections.Add(p as Connection);
                foreach(var p in Spectators)
                    connections.Add(p as Connection);
                if(waitingGame.White != null)
                    connections.Add(waitingGame.White);
                if (waitingGame.Black != null)
                    connections.Add(waitingGame.Black);
                connections.AddRange(waitingGame.Spectators);
                return connections;
            }
        }

        /// <summary>
        /// players who are waiting because their game has ended.
        /// </summary>
        public List<Player> gameEndedWaiting = new List<Player>();

        public T EndConditition { get; set; }
        ChessGame waitingGame; // we hold players in this until they are both in, then move them
        public Tournament(T condition)
        {
            EndConditition = condition;
            waitingGame = new ChessGame();
            MainTickTimer.Elapsed += MainTickTimer_Elapsed;
        }

        private void MainTickTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock(Games)
            {
                foreach(var game in Games)
                {
                    var end = EndConditition.HasEnded(game);
                    if(end.ended)
                    {
                        if(EndConditition.IsTurnOnly == true)
                        {
                            game.SwitchPlayers();
                        } else
                        {
                            game.TournamentEndDueTo = EndConditition;
                            game.TournamentEndWinner = end.playerWon;
                        }
                    }
                }
            }
        }

        public void StartAll()
        {
            if (MainTickTimer.Enabled)
                return;

            foreach(var game in Games)
            { // first we slowly transfer waiting people to the games actually in
                if(waitingGame.White != null)
                {
                    var spec = waitingGame.White.ReturnAs<Spectator>();
                    game.Spectators.Add(spec);
                } else if (waitingGame.Black != null)
                {
                    var spec = waitingGame.Black.ReturnAs<Spectator>();
                    game.Spectators.Add(spec);
                } else if (waitingGame.Spectators.Count > 0)
                {
                    var first = waitingGame.Spectators[0];
                    game.Spectators.Add(first);
                }
                game.Log($"{game.White.Name} vs {game.Black.Name}, {game.Spectators.Count} spectating");
                game.StartUp();
                game.GameOver += Game_GameOver;
            }
            MainTickTimer.Start();
        }

        private void Game_GameOver(object sender, ChessGameWonEventArgs e)
        {
            e.Game.Log($"{e.Winner.Name} won vs {e.Loser.Name} due to {e.Reason}");
            e.Game.TickTimer?.Stop(); e.Game.TickTimer = null;
            lock(Games)
            {
                Games.Remove(e.Game);
                if(Games.Count == 0)
                {
                    this.MainTickTimer.Stop();
                }
            }
            gameEndedWaiting.Add(e.Winner);
            e.Loser.Disconnect(true);
            foreach (var spec in e.Game.Spectators)
                spec.Disconnect(true);
            // easier to just kick them than try and move them around
            if(Games.Count == 0)
            {
                foreach (var player in gameEndedWaiting)
                    Transfer(player);
                StartAll();
            }
        }

        public void Transfer(Connection entree)
        {
            lock(waitingGame)
            {
                if(entree is Player player)
                {
                    Program.Log($"Player {player.Name} now waiting for game {waitingGame.Id}", Services.LogSeverity.Warning, "Transfer");
                    if (waitingGame.White == null)
                    {
                        waitingGame.White = player;
                    }
                    else
                    {
                        waitingGame.Black = player;
                        player.GameIn = waitingGame;
                        Games.Add(waitingGame);
                        waitingGame = new ChessGame();
                    }
                }
                else if (entree is Spectator spectator)
                {
                    Program.Log($"Spectator {spectator.Name} now waiting for game {waitingGame.Id}", Services.LogSeverity.Warning, "Transfer");
                    waitingGame.Spectators.Add(spectator);
                    spectator.GameIn = waitingGame;
                }
            }
        }

        public System.Timers.Timer MainTickTimer { get; set; } = new System.Timers.Timer(1000);
    }
}
