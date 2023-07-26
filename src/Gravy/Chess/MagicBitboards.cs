using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Gravy.GravyChess
{
    internal static class MagicBitboards
    {
        public static ulong[] rookMovemasks;
        public static ulong[][] rookBlockerBitmasks;
        public static Dictionary<(int, ulong), ulong> rookLookup;

        static MagicBitboards()
        {
            InitRookMoveGeneration();
        }

        private static void InitRookMoveGeneration()
        {
            rookMovemasks = GenerateRookMovemasks();
            rookBlockerBitmasks = GenerateRookBlockerBitmasks();
            rookLookup = GenerateRookLookup();
        }

        static Dictionary<(int, ulong), ulong> GenerateRookLookup()
        {
            Dictionary<(int, ulong), ulong> rookLookup = new();

            for (int i = 0; i < 64; i++)
            {
                foreach (ulong blockerMask in rookBlockerBitmasks[i])
                {
                    rookLookup.Add((i, blockerMask), GenerateRookLegalBitboard(blockerMask, i));
                }
            }

            return rookLookup;
        }

        private static ulong GenerateRookLegalBitboard(ulong blockerBoard, int square)
        {
            List<int> squares = new();

            bool CheckAndAdd(int i)
            {
                squares.Add(i);
                if ((blockerBoard >> i & 1) == 1)
                {
                    return true;
                }

                return false;
            } 

            for (int i = square + 1; i < (square / 8) * 8 + 8; i++)
            {
                if (CheckAndAdd(i)) break;
            }

            for (int i = square - 1; i >= (square / 8) * 8; i--)
            {
                if (CheckAndAdd(i)) break;
            }

            for (int i = square + 8; i <= 56 + square % 8; i += 8)
            {
                if (CheckAndAdd(i)) break;
            }

            for (int i = square - 8; i >= square % 8; i -= 8)
            {
                if (CheckAndAdd(i)) break;
            }

            ulong mask = 0;

            foreach (int squareIter in squares)
            {
                mask |= 1ul << squareIter;
            }

            return mask;
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

            ulong movemask = rowMask | columnMask;
            movemask ^= 1ul << squareIndex;
            movemask &= 0x7e7e7e7e7e7e00;   // exclude edges because they make no difference as blockers

            return movemask;
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
