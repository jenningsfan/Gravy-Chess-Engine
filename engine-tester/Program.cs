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
            tester.PlayGames(games, moveTime);
        }
    }
}