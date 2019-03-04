using Che_ssServer.Services;
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
        static void Main(string[] args)
        {
            StartUp();
        }

        /// <summary>
        ///    X1 X2 X3 X4 X5 X6 X7 X8
        /// Y8
        /// Y7
        /// Y6
        /// Y5
        /// Y4
        /// Y3
        /// Y2
        /// Y1
        ///    X1 X2 X3 X4 X5 X6 X7 X8
        /// </summary>
        public static ChessPosition[,] Board;

        public static void StartUp()
        {
            Board = new ChessPosition[8, 8];
            for (int x = 1; x <= 8; x++) {
                // x goes across bottom
                for(int y = 1; y <= 8; y++)
                {
                    // y goes vertical
                    var newPosition = new ChessPosition(x, y);
                    var player = y > 6 ? PlayerColor.Black : PlayerColor.White;
                    ChessPiece piece = null;
                    // Set types.
                    if(y == 2 || y == 7)
                    {
                        piece = new ChessPiece(PieceType.Pawn, newPosition, player);
                    }
                    if(x == 1 || x == 8)
                    {
                        if(y == 1 || y == 8)
                        {
                            piece = new ChessPiece(PieceType.Rook, newPosition, player);
                        }
                    }
                    if(x == 2 || x == 7)
                    {
                        if(y == 1 || y == 8)
                        {
                            piece = new ChessPiece(PieceType.Knight, newPosition, player);
                        }
                    }
                }
            }
            Console.ReadLine();
        }


        public static void Log(LogMessage msg) => Services.LoggingService.OnLogAsync(msg);

        public static void Log(string message, LogSeverity sev, string source = "App") => LoggingService.OnLogAsync(new LogMessage(sev, source, message));

    }
}
