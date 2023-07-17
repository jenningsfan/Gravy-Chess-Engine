using System.ComponentModel;
using System.Diagnostics;
using Chess;

namespace EngineTester
{
    public enum Result
    {
        Engine1,
        Engine2,
        Draw
    }

    internal class Engine : IDisposable
    {
        private StreamReader stdout;
        private StreamWriter stdin;
        private string name;

        public Engine(StreamReader stdout, StreamWriter stdin)
        {
            this.stdout = stdout;
            this.stdin = stdin;
            
            SendCommand("uci");
            
            while (true)
            {
                name = RecieveResponse();
                if (name.StartsWith("id name "))
                {
                    name = name[8..];
                    break;
                }
            }
            while (RecieveResponse() != "uciok") { }

            SendCommand("ucinewgame");

            SendCommand("isready");
            while (RecieveResponse() != "readyok") { }

        }

        public void SendCommand(string command)
        {
            //Console.WriteLine($"<< {name} - {command}");
            stdin.WriteLine(command);
        }

        public string RecieveResponse()
        {
            string response = stdout.ReadLine();
            //Console.WriteLine($">> {name} - {response}");

            return response;
        }

        public void Dispose()
        {
            SendCommand("quit");
            GC.SuppressFinalize(this);
        }

        ~Engine()
        {
            Dispose();
        }
    }

    public class Tester
    {
        private string engine1Path;
        private string engine2Path;
        private string engine1WorkDir;
        private string engine2WorkDir;

        public Tester(string engine1Path, string engine2Path, string engine1WorkDir, string engine2WorkDir)
        {
            this.engine1Path = engine1Path;
            this.engine2Path = engine2Path;

            this.engine1WorkDir = engine1WorkDir;
            this.engine2WorkDir = engine2WorkDir;
        }

        public List<Result> PlayGames(int games, int moveTime)
        {
            List<Result> results = new List<Result>();

            /*for (int i = 0; i < games; i++)
            {
                results.Add(PlayGame(i % 2 == 0, moveTime));
            }*/

            Parallel.For(0, games, i => {
                //Thread.Sleep(10000);
                results.Add(PlayGame(i % 2 == 0, moveTime));
            });

            return results;
        }

        public Result PlayGame(bool white, int moveTime)
        {
            Engine engine1;
            Engine engine2;

            if (white)
            {
                engine1 = OpenEngine(engine1Path, engine1WorkDir);
                engine2 = OpenEngine(engine2Path, engine2WorkDir);
            }
            else
            {
                engine1 = OpenEngine(engine2Path, engine2WorkDir);
                engine2 = OpenEngine(engine1Path, engine1WorkDir);
            }

            ChessBoard board = new ChessBoard();
            board.AutoEndgameRules = AutoEndgameRules.All;

            Engine[] engines = new Engine[] { engine1, engine2 };

            try
            {
                List<string> moves = new();

                bool playing = true;

                while (playing)
                {
                    foreach (Engine engine in engines)
                    {
                        engine.SendCommand($"position startpos moves {string.Join(' ', moves)}");
                        engine.SendCommand($"go movetime {moveTime}");

                        while (true)
                        {
                            string move = engine.RecieveResponse();
                            if (move.StartsWith("bestmove "))
                            {
                                move = move[9..];

                                DoMove(move, board);
                                moves.Add(move);

                                if (board.EndGame != null)
                                {
                                    if (board.EndGame.WonSide is null) return Result.Draw;
                                    if (board.EndGame.WonSide == PieceColor.White && white) return Result.Engine1;
                                    if (board.EndGame.WonSide == PieceColor.White && !white) return Result.Engine2;
                                    if (board.EndGame.WonSide == PieceColor.Black && white) return Result.Engine2;
                                    if (board.EndGame.WonSide == PieceColor.Black && !white) return Result.Engine1;
                                }

                                break;
                            }
                        }
                    }
                }
            }
            catch { }
            finally
            {
                engine1.Dispose();
                engine2.Dispose();
            }        

            return Result.Draw;
        }

        public void DoMove(string move, ChessBoard board)
        {
            if (move == "") { return; }

            PromotionType promotion = PromotionType.Default;

            switch (move.ToLower().Last())
            {
                case 'q':
                    promotion = PromotionType.ToQueen;
                    break;
                case 'r':
                    promotion = PromotionType.ToRook;
                    break;
                case 'b':
                    promotion = PromotionType.ToBishop;
                    break;
                case 'n':
                    promotion = PromotionType.ToKnight;
                    break;
            }

            board.OnPromotePawn += (sender, e) => e.PromotionResult = promotion;

            board.Move(new Move(move[0..2], move[2..4]));
        }

        private static Engine OpenEngine(string path, string workingDirectory)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(path);

            startInfo.WorkingDirectory = workingDirectory;

            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;

            startInfo.UseShellExecute = false;

            Process engine = new Process();
            engine.StartInfo = startInfo;
            engine.Start();

            return new Engine(engine.StandardOutput, engine.StandardInput);
        }
    }
}
