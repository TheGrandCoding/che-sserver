﻿using Che_ssServer.Services;
using Che_ssServer.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        static void Main(string[] args)
        {
            Game = new ChessGame();
            Game.StartUp();
            Console.ReadLine();
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

        public static void Log(LogMessage msg) => Services.LoggingService.OnLogAsync(msg);

        public static void Log(string message, LogSeverity sev, string source = "App") => LoggingService.OnLogAsync(new LogMessage(sev, source, message));

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
