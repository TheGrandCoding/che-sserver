using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Che_ssServer.Classes
{
    public class Player : Connection
    {
        public PlayerColor Color;
        public override void HandleDisconnectLogic(bool kicked)
        {
        }

        public override void LogSendMessage(string message)
        {
            Program.Log(message, Services.LogSeverity.Debug, $"{this.Name}/Send");
        }
    }

    public enum PlayerColor
    {
        White,
        Black
    }
}
