using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravy.GravyChess
{
    internal class Board
    {
        public ulong[] bitboards;
        public Colour turn;

        public Board()
        {
            bitboards = new ulong[12];
        }

        public void LoadFen(string fen)
        {
            Dictionary<char, int> pieceLookup = new Dictionary<char, int>{
                { 'P', 0 },
                { 'N', 1 },
                { 'B', 2 },
                { 'R', 3 },
                { 'Q', 4 },
                { 'K', 5 },
                { 'p', 6 },
                { 'n', 7 },
                { 'b', 8 },
                { 'r', 9 },
                { 'q', 10 },
                { 'k', 11 },
            };

            int squareIndex = 63;

            foreach (string rank in fen.Split(" ")[0].Split("/"))
            {
                foreach (char piece in rank)
                {
                    if (char.IsDigit(piece))
                    {
                        squareIndex -= piece - '0';
                    }
                    else
                    {
                        bitboards[pieceLookup[piece]] |= 1ul << squareIndex;
                        squareIndex--;
                    }
                }
            }
        }

        public void PrintBoard()
        {
            Dictionary<int, char> pieceLookup = new Dictionary<int, char> {
                { 0, 'P' },
                { 1, 'N' },
                { 2, 'B' },
                { 3, 'R' },
                { 4, 'Q' },
                { 5, 'K' },
                { 6, 'p' },
                { 7, 'n' },
                { 8, 'b' },
                { 9, 'r' },
                { 10, 'q' },
                { 11, 'k' },
            };

            for (int i = 63; i >= 0; i--)
            {
                bool foundPiece = false;

                for (int j = 0; j < 12; j++)
                {
                    if ((bitboards[j] & (1ul << i)) == (1ul << i))
                    {
                        Console.Write(pieceLookup[j]);
                        foundPiece = true;
                        break;
                    }
                }

                if (!foundPiece)
                {
                    Console.Write(" ");
                }

                if (i % 8 == 0)
                {
                    Console.WriteLine("");
                }
            }
        }
    }
}
