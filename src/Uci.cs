using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravy
{
    internal class Uci
    {
        public Uci()
        {
            // Nothing in init method yet
        }

        public int HandleCommand(string command)
        {
            switch (command)
            {
                case "uci":
                    DoUciCommand();
                    break;
                case "isready":
                    DoIsReadyCommand();
                    break;
                case "quit":
                    return -1;
                default:
                    Console.WriteLine($"Unknown command: {command}");
                    break;
            }

            return 0;
        }

        private void SendCommand(string command)
        {
            Console.WriteLine(command);
        }

        private void DoUciCommand()
        {
            SendCommand("id name Gravy");
            SendCommand("id author Sammy Humphreys");
            SendCommand("uciok");
        }

        private void DoIsReadyCommand()
        {
            SendCommand("readyok");
        }
    }
}
