using System.Diagnostics;

namespace Gravy
{
    internal class Uci
    {
        Gravy engine;
        public Uci()
        {
            engine = new Gravy();
        }

        public int HandleCommand(string command)
        {         
            Trace.TraceInformation(command);

            switch (command.Split(" ")[0])
            {
                case "uci":
                    DoUciCommand();
                    break;
                case "isready":
                    DoIsReadyCommand();
                    break;
                case "ucinewgame":
                    DoUciNewGameCommand();
                    break;
                case "position":
                    DoSetPosition(command.Split(" ")[1..]);
                    break;
                case "go":
                    DoChooseMove(command.Split(" ")[1..]);
                    break;
                case "stop":
                    DoStop();
                    break;
                case "eval":
                    SendCommand($"{engine.EvaluateBoard()}");
                    break;
                case "print":
                    DoPrintBoard();
                    break;
                case "quit":
                    return -1;
                default:
                    SendCommand($"Unknown command: {command}");
                    break;
            }

            return 0;
        }

        private void SendCommand(string command)
        {
            Console.WriteLine(command);

            Trace.TraceInformation(command);
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

        private void DoUciNewGameCommand()
        {
            engine.StartNewGame();
        }

        private void DoSetPosition(string[] args)
        {
            string fen = "";

            if (args[0] == "startpos")
            {
                fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            }
            else
            {
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i] == "moves")
                    {
                        break;
                    }
                    else
                    {
                        fen += args[i] + " ";
                    }
                }
                fen = fen[..^1];
            }

            int movesIndex = Array.IndexOf(args, "moves");

            if (movesIndex != -1)
            {
                engine.SetPosition(fen, args[(movesIndex + 1)..]);
            }
            else
            {
                engine.SetPosition(fen, new string[0] { });
            }
        }

        private void DoChooseMove(string[] args)
        {
            string bestMove = "0000";
            long maxTime = 10000;    // 10 seconds

            if (args[0] == "movetime")
            {
                maxTime = (long)Convert.ToInt32(args[1]);
            }
            else if (args.Contains("wtime") || args.Contains("btime"))
            {
                if (engine.IsWhite) maxTime = Convert.ToInt32(args[Array.IndexOf(args, "wtime") + 1]);
                if (!engine.IsWhite) maxTime = Convert.ToInt32(args[Array.IndexOf(args, "btime") + 1]);

                maxTime /= 40;
            }

            Stopwatch timer = new Stopwatch();
            timer.Start();

            int depth = 0;
            int maxDepth = int.MaxValue;

            if (args.Contains("depth"))
            {
                maxDepth = Convert.ToInt32(args[Array.IndexOf(args, "depth") + 1]);
            }

            while (depth < maxDepth)
            {
                depth++;

                Tuple<bool, bool, bool, string> task = engine.ChooseMove(depth, maxTime - timer.ElapsedMilliseconds);

                if (task.Item4 == "0000") break;
                if (!task.Item1) bestMove = task.Item4;

                if (task.Item2) SendCommand("info book");

                if (timer.ElapsedMilliseconds > maxTime || task.Item2 || task.Item3)
                {
                    break;
                }

                if (!task.Item1) SendCommand($"info depth {depth} {task.Item4}");
            }
            timer.Stop();

            //engine.DoMove(bestMove);

            SendCommand($"bestmove {bestMove}");
        }

        private void DoPrintBoard()
        {
            engine.PrintBoard();
        }

        private void DoStop()
        {
            // When gravy is runs in a different task kill. for now do nothing.
        }
    }
}
