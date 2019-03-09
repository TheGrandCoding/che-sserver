using Che_ssServer.Services;
using Che_ssServer.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Che_ssServer
{
    public class Program
    {
        public const string MAIN_PATH = "";
#if DEBUG
        public const bool BOT_DEBUG = true;
#else
        public const bool BOT_DEBUG = false;
#endif
        public static ChessGame Game; // only one game to start with.

        public static TcpListener Listener;

        public static MasterlistDLL.MasterlistServer Masterlist;
        public static bool MasterlistEnabled { get; protected set; } 

        static void Main(string[] args)
        {
            Listener = new TcpListener(IPAddress.Any, 9993);
            Listener.Start();
            var thread = new Thread(HandleNewConnections);
            thread.Start();
            Game = new ChessGame();
            Log("Listening on: " + Listener.LocalEndpoint.ToString(), LogSeverity.Debug);
            try
            {
                Masterlist = new MasterlistDLL.MasterlistServer(false, "chess#4008");
                Masterlist.LogMessage += (sender, msg) =>
                {
                    Log(msg, LogSeverity.Debug, "Masterlist");
                };
                Masterlist.RecieveMessage += (sender, msg) =>
                {
                    Log(msg.Message, LogSeverity.Info, "ML-" + msg.LastOperation);
                };
                Masterlist.HostServer("Alex's Server", Guid.NewGuid(), false);
            } catch (Exception ex)
            {
                MasterlistEnabled = false;
                Log("MLStart", ex);
            }
            while(true)
                Console.ReadLine();
        }

        static void HandleNewConnections()
        {
            TcpClient clientSocket = null;
            while(Listener != null)
            {
                clientSocket = Listener.AcceptTcpClient();
                Byte[] bytesFrom = new Byte[clientSocket.ReceiveBufferSize];
                string dataFromClient;
                NetworkStream netStream = clientSocket.GetStream();
                try
                {
                    netStream.Read(bytesFrom, 0, Convert.ToInt32(clientSocket.ReceiveBufferSize));
                }
                catch (Exception ex)
                {
                    Log("NewConRead", ex);
                    continue;
                }
                dataFromClient = Encoding.UTF8.GetString(bytesFrom).Trim().Replace("\0", "");
                IPEndPoint ipEnd = clientSocket.Client.RemoteEndPoint as IPEndPoint;
                Log("Connection from " + ipEnd.ToString(), LogSeverity.Debug, "Server");
                if (string.IsNullOrWhiteSpace(dataFromClient))
                {
                    clientSocket.Close();
                    continue;
                }
                dataFromClient = dataFromClient.Substring(1, dataFromClient.LastIndexOf("`") - 1);
                Log("New Player " + dataFromClient + " @ " + ipEnd.ToString(), LogSeverity.Info, "Server");
                Player user = new Player(dataFromClient, clientSocket);
                if(Game.White ==null)
                {
                    Game.White = user;
                    Log($"{user.Name} is White", LogSeverity.Info, "Game");
                } else
                {
                    Game.Black = user;
                    Log($"{user.Name} is Black, game starting", LogSeverity.Info, "Game");
                    Game.StartUp();
                }
            }
        }

        public static TakableBy TakeFor(PlayerColor colour)
        {
            if (colour == PlayerColor.Black)
                return TakableBy.Black;
            if (colour == PlayerColor.White)
                return TakableBy.White;
            return TakableBy.None;
        }

        public static string XtoStr(int x)
        {
            // todo: use ASCII?
            switch(x)
            {
                case 1:
                    return "A";
                case 2:
                    return "B";
                case 3:
                    return "C";
                case 4:
                    return "D";
                case 5:
                    return "E";
                case 6:
                    return "F";
                case 7:
                    return "G";
                case 8:
                    return "H";
                default:
                    return "N/A";
            }
        }
        public static int StrToX(string str)
        {
            switch(str)
            {
                case "A":
                    return 1;
                case "B":
                    return 2;
                case "C":
                    return 3;
                case "D":
                    return 4;
                case "E":
                    return 5;
                case "F":
                    return 6;
                case "G":
                    return 7;
                case "H":
                    return 8;
                default:
                    return 0;
            }
        }

        public static void Log(LogMessage msg) => Services.LoggingService.OnLogAsync(msg);

        public static void Log(string message, LogSeverity sev, string source = "App") => LoggingService.OnLogAsync(new LogMessage(sev, source, message));

        public static void Log(string source, Exception ex) => LoggingService.OnLogAsync(new LogMessage(LogSeverity.Error, source, "", ex));

    }

    public class ChessPositionEquality : IEqualityComparer<ChessPosition>
    {
        public bool Equals(ChessPosition x, ChessPosition y)
        {
            return x.X == y.X && x.Y == y.Y;
        }

        public int GetHashCode(ChessPosition obj)
        {
            return obj.GetHashCode();
        }
    }
}
