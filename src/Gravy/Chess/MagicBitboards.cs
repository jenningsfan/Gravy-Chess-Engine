using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravy.GravyChess
{
    internal static class MagicBitboards
    {
        public static ulong[] rookMovemasks;
        public static ulong[][] rookBlockerBitmasks;

        static MagicBitboards()
        {
            InitRookMoveGeneration();
        }

        private static void InitRookMoveGeneration()
        {
            rookMovemasks = GenerateRookMovemasks();
            rookBlockerBitmasks = GenerateRookBlockerBitmasks();
        }

        private static ulong[] GenerateRookMovemasks()
        {
            ulong[] movemasks = new ulong[64];

            for (int i = 0; i < 64; i++)
            {
                movemasks[i] = GenerateRookMovemask(i);
            }

            return movemasks;
        }

        private static ulong GenerateRookMovemask(int squareIndex)
        {
            ulong rowMask = (ulong)0xFF << (squareIndex & ~0x7); // 1111 1111
            ulong columnMask = (ulong)0x0101010101010101 << (squareIndex % 8);  // 0000 0001  8 times

            ulong moveMask = rowMask | columnMask;
            moveMask ^= 1ul << squareIndex;

            return moveMask;
        }

        private static ulong[][] GenerateRookBlockerBitmasks()
        {
            ulong[][] bitmasks = new ulong[64][];

            for (int i = 0; i < 64; i++)
            {
                bitmasks[i] = GenerateBlockerBitmasks(rookMovemasks[i]);
            }

            return bitmasks;
        }

        private static ulong[] GenerateBlockerBitmasks(ulong movemask)
        {
            List<int> squares = new();

            for (int i = 0; i < 64; i++)
            {
                if ((movemask >> i & 1) == 1)
                {
                    squares.Add(i);
                }
            }

            int masksLen = 1 << squares.Count;
            ulong[] masks = new ulong[masksLen];

            for (int i = 0; i < masksLen; i++)
            {
                for (int j = 0; j < squares.Count; j++)
                {
                    int bit = i >> j & 1;
                    masks[i] |= (ulong)bit << squares[j];
                }
            }

            return masks;
        }
    }
}
