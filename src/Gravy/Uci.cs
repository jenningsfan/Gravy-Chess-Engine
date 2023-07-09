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

            if (command == null)
            {
                return 0;
            }

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
                case "bench":
                    DoBenchmark(command.Split(" ")[1..]);
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
            long maxTime = 300000;    // 5 minutes

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

        private void DoBenchmark(string[] args)
        {
            //"7K/8/8/2P1P2P/2p1P2p/4p3/7k/8 w - - 0 1"

            string fen = "";

            if (args.Length == 0)
            {
                fen = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";
            }
            else
            {
                for (int i = 0; i < args.Length; i++)
                {
                    fen += args[i] + " ";
                }
                fen = fen[..^1];
            }

            engine.SetPosition(fen, new string[0] { });
            
            int maxTime = int.MaxValue;
            int maxDepth = 5;

            int totalNodes = 0;
            int totalPruned = 0;
            int totalTranspositionHits = 0;
            long totalTime = 0;

            string bestMove = "0000";

            for (int i = 0; i < 5; i++)
            {
                int depth = 0;
                while (depth < maxDepth)
                {
                    depth++;

                    Stopwatch timer = new Stopwatch();
                    timer.Start();

                    Tuple<bool, bool, bool, string> task = engine.ChooseMove(depth, maxTime - timer.ElapsedMilliseconds);

                    timer.Stop();

                    if (task.Item4 == "0000") break;
                    if (!task.Item1) bestMove = task.Item4;

                    if (task.Item2) SendCommand("info book");

                    if (timer.ElapsedMilliseconds > maxTime || task.Item2 || task.Item3)
                    {
                        break;
                    }

                    if (!task.Item1) SendCommand($"info depth {depth} {bestMove}");

                    SendCommand($"info nodes searched {engine.nodesSearched}");
                    SendCommand($"info nodes pruned {engine.nodesPruned}");
                    SendCommand($"info nodes transposition hits {engine.transpositionHits}");
                    SendCommand($"info nodes time {timer.ElapsedMilliseconds}");
                    SendCommand($"info nodes time {engine.nodesSearched / timer.ElapsedMilliseconds}K nodes/s\n");

                    totalNodes += engine.nodesSearched;
                    totalPruned += engine.nodesPruned;
                    totalTranspositionHits += engine.transpositionHits;
                    totalTime += timer.ElapsedMilliseconds;
                }
            }
            

            SendCommand($"info depth total");
            SendCommand($"info nodes searched {totalNodes}");
            SendCommand($"info nodes pruned {totalPruned}");
            SendCommand($"info nodes transposition hits {totalTranspositionHits}");
            SendCommand($"info nodes time {totalTime}");
            SendCommand($"info nodes time {totalNodes / totalTime}K nodes/s\n");
        }

        private void DoStop()
        {
            // When gravy is runs in a different task kill. for now do nothing.
        }
    }
}
