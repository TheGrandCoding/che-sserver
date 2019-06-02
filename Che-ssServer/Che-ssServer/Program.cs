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
using System.Reflection;
using System.Net.Http;
using Newtonsoft.Json.Linq;

[assembly: AssemblyVersion("0.2.0")]
// Naming scheme - Major.Minor.Build
// Server should refuse connection if Major OR Minor are out of sync
// Build should not be considered
namespace Che_ssServer
{
    public class Program
    {
        public const string MAIN_PATH = "";
#if DEBUG
        public static bool CHS_DEBUG = true;
#else
        public static bool CHS_DEBUG = false;
#endif

        public static int VER_MAJOR => Assembly.GetEntryAssembly().GetName().Version.Major;
        public static int VER_MINOR => Assembly.GetEntryAssembly().GetName().Version.Minor;
        public static int VER_BUILD => Assembly.GetEntryAssembly().GetName().Version.Build;

        public static bool IsPreRelease => VER_MAJOR == 0;

        [Obsolete]
        public static ChessGame Game; // only one game to start with.

        public static dynamic Tournament;

        public static TcpListener Listener;

        public static MasterlistDLL.MasterlistServer Masterlist;
        public static bool MasterlistEnabled { get; protected set; } = false;

        public static EventHandler<string> ConsoleInput;

        enum Compare
        {
            Less = -1,
            Equal = 0,
            More = 1
        }

        /// <summary>
        /// Returns from perspective of the FIRST version
        /// </summary>
        static Compare CompareVersion(Version first, Version second)
        {
            if(first.Major == second.Major)
            {
                if(first.Minor == second.Minor)
                {
                    if (first.Build == second.Build)
                        return Compare.Equal;
                    return (Compare)first.Build.CompareTo(second.Build);
                }
                return (Compare)first.Minor.CompareTo(second.Minor);
            }
            return (Compare)first.Major.CompareTo(second.Major);
        }

        /// <summary>
        /// Probes github to test if we are running the latest version
        /// </summary>
        /// <returns>True if latest, or if github was unreachable, False if not the latest</returns>
        public static bool IsLatestVersion()
        {
            string endpoint = "https://api.github.com/repos/TheGrandCoding/che-sserver/releases/latest";
            bool isLatest = true; // we default to true, and only say 'False' if we are sure its out of date
            try
            {
                using(HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(endpoint);
                    client.DefaultRequestHeaders.Add("User-Agent", "chesserver CheAle14");
                    var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                    var response = client.SendAsync(request).Result;
                    var asString = response.Content.ReadAsStringAsync().Result;
                    if(response.IsSuccessStatusCode)
                    {
                        var parsed = JObject.Parse(asString);
                        var tag = parsed["tag_name"];
                        var tagstr = tag.ToString();

                        Program.Log("-------- Latest Version: --------", LogSeverity.Info, "Github");
                        Program.Log("Tag: " + tagstr, LogSeverity.Debug, "Github");
                        Program.Log("Name: " + parsed["name"].ToString(), LogSeverity.Debug, "Github");
                        Program.Log("By: " + parsed["author"]["login"].ToString(), LogSeverity.Debug, "GitHub");
                        Program.Log("At: " + parsed["published_at"].ToString(), LogSeverity.Debug, "GitHub");
                        Program.Log("-------- Latest Version  --------", LogSeverity.Info , "GitHub");

                        if (Version.TryParse(tagstr.Replace("v", ""), out var version))
                        {
                            return CompareVersion(Assembly.GetEntryAssembly().GetName().Version, version) != Compare.Less;
                        }
                    } else
                    {
                        Program.Log($"{response.StatusCode}: {asString}", LogSeverity.Error, "GithubVersion");
                    }
                }
            } catch (Exception ex)
            {
                Log("VersionUpdater", ex);
            }
            return isLatest;
        }

        static void HandleConsoleInput(object sender, string input)
        {
            if (input.StartsWith("/"))
                input = input.Substring(1);

            if(input.StartsWith("help"))
            {
                Program.Log("Use `/condition [name]` to set the end condition, then use `/start` when all players are in", LogSeverity.Warning, "Console");
            } else if(input.StartsWith("condition ") || input.StartsWith("cond "))
            {
                var q = from t in Assembly.GetExecutingAssembly().GetTypes()
                        where t.IsClass && t.Namespace == "Che_ssServer.EndConditions" && t.BaseType == typeof(EndConditions.EndConditition)
                        select t;
                var name = input.Substring(input.IndexOf(" ")).Trim();
                var item = q.FirstOrDefault(x => x.Name == name);
                if(item == null)
                {
                    Program.Log("Unknown condition - you muse use the type, allowed: " + string.Join(", ", q.Select(x => x.Name)), LogSeverity.Warning, "Console");
                } else
                {
                    if(item == typeof(EndConditions.EndAfterTime))
                    {
                        Tournament = new Tournament<EndConditions.EndAfterTime>(new EndConditions.EndAfterTime(60 * 5));
                        Program.Log("Tournament set: " + Tournament.EndConditition.Display, LogSeverity.Warning, "Console");
                    }
                }

            } else if(input.StartsWith("save "))
            {
                input = input.Replace("save ", "");
                if(int.TryParse(input, out int value))
                {
                    List<ChessGame> games = Tournament.Games;
                    var game = games.FirstOrDefault(x => x.Id == value);
                    if (game == null)
                        throw new ArgumentException("Game with id does not exist");
                    var str = game.ToSave();
                    System.IO.File.WriteAllText("savedgame.txt", str);
                    Program.Log(str, LogSeverity.Info, "SaveGame");
                }
            } else if (input.StartsWith("load "))
            {
                input = input.Replace("load ", "");
                if(int.TryParse(input, out int value))
                {
                    var str = System.IO.File.ReadAllText("savedgame.txt");
                    List<ChessGame> games = Tournament.Games;
                    var game = games.FirstOrDefault(x => x.Id == value);
                    if(game == null)
                        throw new ArgumentException("Game with id does not exist");
                    game.UpdateFromString(str);
                }
            } else if (input == "start")
            {
                Program.Log("Starting!", LogSeverity.Warning, "Console");
                Tournament.StartAll();
            }
        }

        static void Main(string[] args)
        {
            Log("Running version: " + Assembly.GetEntryAssembly().GetName().Version.ToString(), LogSeverity.Info);
            if(IsLatestVersion())
            {
                Log("Running latest", LogSeverity.Debug, "VersionChecker");
            } else
            {
                Log("Version if out of date, please check Github releases for update", LogSeverity.Critical, "VersionChecker");
                Console.ReadLine();
                Log("Console will close on next input, please pull the latest version from GitHub.", LogSeverity.Critical, "UPDATE");
                Console.ReadKey();
                return;
            }
            Listener = new TcpListener(IPAddress.Any, 9993);
            Listener.Start();
            var thread = new Thread(HandleNewConnections);
            thread.Start();
            Log("Listening on: " + Listener.LocalEndpoint.ToString(), LogSeverity.Debug);
            Log("Server IP: " + GetLocalIPAddress(), LogSeverity.Info);
            try
            {
                if (!MasterlistEnabled)
                    throw new Exception("Masterlist disabled.");
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
            ConsoleInput += HandleConsoleInput;
            Log("-> Please see /help for more information", LogSeverity.Warning, "Console");
            Log("-> You need to set how the tournament will end via /condition [type]", LogSeverity.Warning, "Console");
            while(true)
            {
                var obj = Console.ReadLine();
                try
                {
                    ConsoleInput?.Invoke(null, obj);
                } catch(Exception ex)
                {
                    Program.Log("Console", ex);
                }
            }

        }

        public static string PinString(PinType pin, string seperator = ", ")
        {
            string str = "";
            if(pin.HasFlag(PinType.Fully))
            {
                str = "Fully pinned";
            } else if(pin == PinType.NotPinned)
            {
                str = "Not pinned";
            }
            else
            {
                if(pin.HasFlag(PinType.Horizontal))
                {
                    str += "Horizontally" + seperator;
                }
                if(pin.HasFlag(PinType.Vertical))
                {
                    str += "Vertically" + seperator;
                }
                if(pin.HasFlag(PinType.DiagonalLeft))
                {
                    str += "Diagonally left" + seperator;
                }
                if(pin.HasFlag(PinType.DiagonalRight))
                {
                    str += "Diagonally right" + seperator;
                }
            }
            return str;
        }

        static bool duplicateName(string testing)
        {
            foreach(Connection coc in Tournament.Connections)
            {
                if (coc.Name == testing)
                    return true;
            }
            return false;
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
                string userVersion = "";
                try
                {
                    userVersion = dataFromClient.Substring(dataFromClient.IndexOf(":") + 1);
                }
                catch 
                {
                }
                if(Version.TryParse(userVersion, out var vers))
                {
                    if(vers.Major != VER_MAJOR || vers.Minor != VER_MINOR)
                    {
                        Log($"{dataFromClient} has outdated client, refusing.", LogSeverity.Warning, "VersionCheck");
                        clientSocket.Close();
                        continue;
                    }
                } else
                {
                    Log($"{dataFromClient} has malformed, invalid or absent version, refusing..", LogSeverity.Warning, "VersionCheck");
                    clientSocket.Close();
                    continue;
                }
                dataFromClient = dataFromClient.Replace(":" + userVersion, "");
                string testName = dataFromClient;
                int count = 0;
                if(Tournament == null)
                {
                    Log("Tournament is not set - please see /help", LogSeverity.Critical, "NewUserNoTournament");
                    clientSocket.Close();
                    continue;
                }
                while(duplicateName(testName))
                {
                    count++;
                    testName = dataFromClient + count.ToString();
                }

                var player = new Player(testName, clientSocket);
                if (testName != dataFromClient)
                    player.Send("NAME:" + testName);
                Tournament.Transfer(player);
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

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    if(ip.ToString().StartsWith("192.") || ip.ToString().StartsWith("10."))
                    { // we want a local address, so don't pull one that isnt
                        return ip.ToString();
                    }
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        #region Tournament
        public static void HandleGameOver(object sender, ChessGameWonEventArgs e)
        {
            Log($"{e.Winner.Name} won vs {e.Loser.Name}", LogSeverity.Info);
            e.Winner.Send($"WIN:{e.Reason}");
            e.Loser.Send($"LOSE:{e.Reason}");
        }
        #endregion
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
