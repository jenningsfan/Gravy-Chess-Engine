namespace Gravy
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Uci uci = new Uci();

            while (true)
            {
                if (uci.HandleCommand(Console.ReadLine()) == -1) break;
            }
        }
    }
}