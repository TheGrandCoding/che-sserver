using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Che_ssServer.Classes;
using Newtonsoft.Json;

namespace Che_ssServer.Helpers
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public struct JsonGameDelta
    {
        public PlayerColor color;
        public string white;
        public string black;
        public Dictionary<string, ChessPosition> board;
    }
}
