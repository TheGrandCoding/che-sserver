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
        [JsonRequired]
        public int ID;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PlayerColor color;
        public string white;
        public string black;
        public string whiteTime; // formatted as TimeSpan
        public string blackTime;
    }
}
