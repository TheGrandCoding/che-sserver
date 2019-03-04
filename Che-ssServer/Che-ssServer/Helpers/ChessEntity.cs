using Che_ssServer.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Che_ssServer.Helpers
{
    public class ChessEntity
    {
        public ChessGame Game { get; protected set; }
        public ChessEntity(ChessGame game)
        {
            Game = game;
        }
    }
}
