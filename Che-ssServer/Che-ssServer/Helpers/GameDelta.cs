using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Che_ssServer.Classes;
using Newtonsoft.Json;

namespace Che_ssServer.Helpers
{
    public class GameDelta
    {
        private static int _id = 0;
        public readonly int ID;
        public ChessGame Game;
        public GameDelta LastDelta;

        public GameDelta(ChessGame game, GameDelta delta)
        {
            ID = System.Threading.Interlocked.Increment(ref _id);
            Game = game;
            LastDelta = delta;
            CurrentColor = game.CurrentlyWaitingFor.Color;
            WhiteName = game.White.Name;
            BlackName = game.Black.Name;
            WhiteTime = game.WhiteTime.ToString();
            BlackTime = game.BlackTime.ToString();
        }
        // Now for the game information
        public PlayerColor CurrentColor;
        public string WhiteName;
        public string BlackName;
        public string WhiteTime;
        public string BlackTime;

        // Now the comparison
        public string GetDelta()
        {
            var delta = new JsonGameDelta();
            if(this.CurrentColor != this.LastDelta?.CurrentColor)
            {
                delta.color = this.CurrentColor;
            }
            if(this.WhiteName != this.LastDelta?.WhiteName)
            {
                delta.white = this.WhiteName;
            }
            if(this.BlackName != this.LastDelta?.BlackName)
            {
                delta.black = this.BlackName;
            }
            if(this.WhiteTime != this.LastDelta?.WhiteTime)
            {
                delta.whiteTime = this.WhiteTime.ToString();
            }
            if(this.BlackTime != this.LastDelta?.BlackTime)
            {
                delta.blackTime = this.BlackTime.ToString();
            }
            delta.ID = this.ID;
            return JsonConvert.SerializeObject(delta);
        }
    }
}
