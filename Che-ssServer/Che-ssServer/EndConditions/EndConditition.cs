using Che_ssServer.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Che_ssServer.EndConditions
{
    /// <summary>
    /// The base end condition class
    /// </summary>
    public abstract class EndConditition : IEndConditition
    {
        protected EndConditition(string name, bool isturnonly, string display)
        {
            Name = name;
            IsTurnOnly = isturnonly;
            Display = display;
        }
        /// <summary>
        /// The unique name of this condition
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Whether the condition only checks whether the **turn** has ended
        /// Thus, it would not affect whether the game should end
        /// </summary>
        public bool IsTurnOnly { get; }

        /// <summary>
        /// The string message displayed to players to indicate how the Torunament works.
        /// </summary>
        public string Display { get; }

        /// <summary>
        /// Checks whether the game (or turn - <see cref="IsTurnOnly"/>) should end
        /// </summary>
        /// <param name="game">The game to check</param>
        /// <returns>True if it should end, false otherwise</returns>
        public abstract (bool ended, WinType playerWon) HasEnded(ChessGame game);
    }
}
