using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Che_ssServer.Classes
{
    public class Spectator : Connection
    {
        public Spectator(string name, TcpClient client) : base(name, client)
        {
        }

        public override void HandleDisconnectLogic(bool kicked)
        {
            try
            {
                this.GameIn.Spectators.RemoveAll(x => x.Name == this.Name);
            } catch { }
        }

        public override void LogSendMessage(string message)
        {
            Program.Log(message, Services.LogSeverity.Debug, $"Spec-{this.Name}");
        }
    }
}
