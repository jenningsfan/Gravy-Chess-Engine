namespace EngineTester
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string engine1Path = args[0];
            string engine2Path = args[1];

            string engine1WorkDir = args[2];
            string engine2WorkDir = args[3];

            int games = Int32.Parse(args[4]);
            int moveTime = Int32.Parse(args[5]);

            Tester tester = new Tester(engine1Path, engine2Path, engine1WorkDir, engine2WorkDir);
            List<Result> results = tester.PlayGames(games, moveTime);

            int engine1Wins = 0;
            int engine2Wins = 0;
            int draws = 0;

            foreach (Result result in results)
            {
                if (result == Result.Engine1) engine1Wins++;
                if (result == Result.Engine2) engine2Wins++;
                if (result == Result.Draw)    draws++;
            }

            Console.WriteLine($"Engine1 wins: {engine1Wins}");
            Console.WriteLine($"Engine2 wins: {engine2Wins}");
            Console.WriteLine($"Draws: {draws}");
        }
    }
}