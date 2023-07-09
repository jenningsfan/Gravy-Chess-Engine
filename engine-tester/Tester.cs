using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EngineTester
{
    public enum Result
    {
        White,
        Black,
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
            Console.WriteLine($"<< {name} - {command}");
            stdin.WriteLine(command);
        }

        public string RecieveResponse()
        {
            string response = stdout.ReadLine();
            Console.WriteLine($">> {name} - {response}");

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

            for (int i = 0; i < games; i++)
            {
                results.Add(PlayGame(i % 2 == 0, moveTime));
            }

            return results;
        }

        public Result PlayGame(bool white, int moveTime)
        {
            Engine engine1 = OpenEngine(engine1Path, engine1WorkDir);
            Engine engine2 = OpenEngine(engine2Path, engine2WorkDir);

            engine1.Dispose();
            engine2.Dispose();

            if (white)
            {
                return Result.White;
            }
            else
            {
                return Result.Black;
            }
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
