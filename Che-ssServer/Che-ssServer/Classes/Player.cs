using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Che_ssServer.Classes
{
    public class Player : Connection
    {
        public Player(string name) : base(name)
        {

        }
        public PlayerColor Color;

        public Player(string name, TcpClient client) : base(name, client)
        {
        }

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
        NotControlled = 0,
        White,
        Black
    }
}
