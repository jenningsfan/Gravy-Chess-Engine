﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
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
            int maxTime = 10000;    // 10 seconds

            if (args[0] == "movetime")
            {
                maxTime = Convert.ToInt32(args[1]);
            }
            else if (args.Contains("wtime") || args.Contains("btime"))
            {
                if (engine.IsWhite) maxTime = Convert.ToInt32(args[Array.IndexOf(args, "wtime") + 1]);
                if (!engine.IsWhite) maxTime = Convert.ToInt32(args[Array.IndexOf(args, "btime") + 1]);

                maxTime /= 100;
            }

            var cts = new CancellationTokenSource();
            Stopwatch timer = new Stopwatch();
            timer.Start();

            int depth = 0;

            while (true)
            {
                depth++;

                var task = engine.ChooseMove(depth, cts.Token);
                if (timer.ElapsedMilliseconds > maxTime)
                {
                    cts.Cancel();
                    break;
                }
                try
                {
                    bestMove = task.Result;
                }
                catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is TaskCanceledException))
                {
                    Console.WriteLine("Calculation was cancelled");
                    break;
                }
            }
            timer.Stop();

            engine.DoMove(bestMove);

            Console.WriteLine($"bestmove {bestMove}");
        }


        private void DoStop()
        {
            // When gravy is runs in a different task kill. for now do nothing.
        }
    }
}
