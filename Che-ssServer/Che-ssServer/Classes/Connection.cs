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
    {//
        public string Name;
        public bool Connected { get; internal set; }
        public int disconnected = 0;
        public TcpClient Client;
        private bool disconnectedfunctioncalled = false;
        public Thread recicivedataThread;

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
            string data;
            NetworkStream RecieveDataStream = Client.GetStream();
            byte[] bytes = new byte[256];
            int i;
            try
            {
                while (Connected)
                {
                    if ((i = RecieveDataStream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        RecievedMessage?.Invoke(this, data);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Disconnect(false);
            }
        }

        public abstract void LogSendMessage(string message);

        public virtual void Send(string message)
        {
            return;
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
    }
}
