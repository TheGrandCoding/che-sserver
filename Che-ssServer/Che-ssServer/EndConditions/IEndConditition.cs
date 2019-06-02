using Che_ssServer.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Che_ssServer.EndConditions
{
    public interface IEndConditition
    {
        (bool ended, WinType playerWon) HasEnded(ChessGame game);
        bool IsTurnOnly { get; }
        string Name { get; }
        string Display { get; }
    }
}
