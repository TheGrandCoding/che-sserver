using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Che_ssServer.Classes;

namespace Che_ssServer.EndConditions
{
    public class EndAfterTime : EndConditition, IEndConditition
    {
        /// <summary>
        /// Number of seconds for each player to have until they lose
        /// </summary>
        public readonly int SecondTimeout;
        public EndAfterTime(int time) : base("End After Time", false, $"Player loses after {time}s")
        {
            SecondTimeout = time;
        }

        /// <inheritdoc/>
        public override (bool ended, WinType playerWon) HasEnded(ChessGame game)
        {
            TimeSpan time;
            if(game.CurrentlyWaitingFor == game.White)
            {
                time = game.WhiteTime;
            } else
            {
                time = game.BlackTime;
            }
            return (time.TotalSeconds > SecondTimeout, game.CurrentlyWaitingFor == game.White ? WinType.Black : WinType.White);
        }
    }
}
