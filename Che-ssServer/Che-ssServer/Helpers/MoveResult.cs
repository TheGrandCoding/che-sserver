using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Che_ssServer.Classes;

namespace Che_ssServer.Helpers
{
    public class MoveResult : IResult
    {
        public ChessPosition From { get; }
        public ChessPosition To { get; }

        public bool IsSuccess { get; }

        public ChessPiece PieceTook { get; }

        public string Message { get; }

        public Exception Error { get; }

        internal MoveResult(bool isSuccess, string message, ChessPosition from, ChessPosition to, Exception error = null, ChessPiece took = null)
        {
            PieceTook = took;
            IsSuccess = isSuccess;
            Message = message;
            Error = error;
        }

        public static MoveResult FromSuccess(ChessPosition from, ChessPosition to, string message, ChessPiece took = null)
        {
            return new MoveResult(true, message, from, to, null, took);
        }
        public static MoveResult FromError(ChessPosition from, ChessPosition to, string message)
        {
            return new MoveResult(false, message, from, to);
        }
    }
}
