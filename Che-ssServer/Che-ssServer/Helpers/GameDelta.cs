﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        public readonly ImmutableDictionary<string, ChessPosition> Board;

        public GameDelta(ChessGame game, GameDelta delta)
        {
            ID = System.Threading.Interlocked.Increment(ref _id);
            Game = game;
            LastDelta = delta;
            CurrentColor = game.CurrentlyWaitingFor.Color;
            WhiteName = game.White.Name;
            BlackName = game.Black.Name;
            var board = new Dictionary<string, ChessPosition>();
            foreach(var item in Game.Board)
            {
                board.Add(item.Pos, item);
            }
            Board = board.ToImmutableDictionary();
        }
        // Now for the game information
        public PlayerColor CurrentColor;
        public string WhiteName;
        public string BlackName;
        public string BoardStr => JsonConvert.SerializeObject(Board);
        public TimeSpan WhiteTime => Game.WhiteTime;
        public TimeSpan BlackTime => Game.BlackTime;

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
                delta.whiteTime = this.WhiteTime;
            }
            if(this.BlackTime != this.LastDelta?.BlackTime)
            {
                delta.blackTime = this.BlackTime;
            }
            var board = new Dictionary<string, ChessPosition>();
            foreach(var item in this.Board)
            {
                ChessPosition otherItem = null;
                if(this.LastDelta?.Board.TryGetValue(item.Key, out otherItem) ?? true)
                {
                    if(item.Value.Equals(otherItem))
                    { // no need to add, since it is equal to
                    } else
                    { // item has changed, so need to inform of that change
                        board.Add(item.Key, item.Value);
                    }
                }
            }
            delta.board = board;
            return JsonConvert.SerializeObject(delta);
        }
    }
}
