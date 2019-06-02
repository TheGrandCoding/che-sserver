using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Che_ssServer.Classes;

namespace Che_ssServer.EndConditions
{
    public class EndAfterTime : EndConditition
    {
        /// <summary>
        /// Number of seconds for each player to have until they lose
        /// </summary>
        public readonly int SecondTimeout;
        public EndAfterTime(int time) : base("End After Time", false)
        {
            SecondTimeout = time;
            Display = $"Player loses after {time}s";
        }

        /// <inheritdoc/>
        public override bool HasEnded(ChessGame game)
        {
            TimeSpan time;
            if(game.CurrentlyWaitingFor == game.White)
            {
                time = game.WhiteTime;
            } else
            {
                time = game.BlackTime;
            }
            return time.TotalSeconds > SecondTimeout;
        }
    }
}
