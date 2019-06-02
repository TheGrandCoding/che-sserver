using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Che_ssServer.Classes
{
    /// <summary>
    /// Contains the shared functions/items that are shared
    /// </summary>
    public abstract class Connection
    {
        private Connection() { }
        public Connection(string name)
        {
            Name = name;
        }
        public string Name { get; private set; }
        public bool Connected { get; protected set; }
        public int disconnected = 0;
        public TcpClient Client;
        private bool disconnectedfunctioncalled = false;
        public Thread recicivedataThread;

        /// <summary>
        /// Transfers the Connection to a different type
        /// </summary>
        /// <typeparam name="T"><see cref="Player"/> or <seealso cref="Spectator"/></typeparam>
        public T ReturnAs<T>() where T : Connection
        {
            T newCon = default(T);
            newCon.Name = Name;
            newCon.disconnected = this.disconnected;
            newCon.Client = this.Client;
            newCon.disconnectedfunctioncalled = this.disconnectedfunctioncalled;
            newCon.recicivedataThread = this.recicivedataThread;
            newCon.GameIn = this.GameIn;
            newCon.RecievedMessage = this.RecievedMessage;
            newCon.Connected = this.Connected;
            this.RecievedMessage = null;
            return newCon;
        }

        public ChessGame GameIn;

        public Connection(string name, TcpClient client)
        {
            Name = name;
            Client = client;
            StartCheckLogic();
        }

        /// <summary>
        /// Removes any functions that are listening to messages recieved by this Connection
        /// </summary>
        public void ClearListeners()
        {
            RecievedMessage = null;
        }

        public event EventHandler<string> RecievedMessage;

        private bool logicStarted = false;
        public void StartCheckLogic()
        {
            if (logicStarted) return;
            logicStarted = true;
            Connected = true;
            recicivedataThread = new Thread(this.handleRecieveMessage);
            recicivedataThread.Start();
        }

        private void handleRecieveMessage()
        {
#if DEBUG
            if (Client == null)
                return; // messages handled via the DEBUG function
#endif
            string data;
            NetworkStream RecieveDataStream = Client.GetStream();
            byte[] bytes = new byte[256];
            int i;
            try
            {
                while (Connected && Client != null)
                {
                    if ((i = RecieveDataStream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        foreach(var msg in data.Split('%'))
                        {
                            if (string.IsNullOrWhiteSpace(msg))
                                continue;
                            var message = msg.Substring(0, msg.IndexOf("`"));
                            Program.Log(message, Services.LogSeverity.Debug, $"{Name}/Rec");
                            RecievedMessage?.Invoke(this, message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Log($"{Name}/RecM", ex);
                this.Disconnect(false);
            }
        }

        public abstract void LogSendMessage(string message);

        public virtual void Send(string message)
        {
            if (Client == null)
            {
                LogSendMessage(message);
                return;
            }
            try
            {
                message = $"%{message}`";
                NetworkStream SendDataStream = Client.GetStream();
                byte[] msg = System.Text.Encoding.ASCII.GetBytes(message);
                SendDataStream.Write(msg, 0, msg.Length);
                try
                {
                    LogSendMessage(message);
                }
                catch (Exception) { }
            }
            catch (Exception)
            {
                Disconnect(false);
            }
        }

        public void DEBUG_RaiseMessage(string message)
        {
#if DEBUG
            RecievedMessage?.Invoke(this, message);
#endif
        }

        public void Disconnect(bool kicked)
        {
            if (!Connected)
                return;
            Connected = false;
            recicivedataThread.Abort();
            try
            {
                Client.Client.Disconnect(false);
                Client = null;
            }
            catch { }
        }
        public abstract void HandleDisconnectLogic(bool kicked);

        public override string ToString()
        {
            return Name;
        }
    }
}
