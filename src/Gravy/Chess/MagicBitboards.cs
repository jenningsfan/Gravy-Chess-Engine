using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static void GenerateRookMagics()
        {
            bool IsUnique(ulong[] bitboards) { return bitboards.Distinct().Count() == bitboards.Count(); }

            ulong[][] rookLookup = new ulong[64][];

            ulong[] rookMagics = new ulong[64];
            int[] rookShifts = new int[64];

            Random random = new Random();

            for (int i = 0; i < 64; i++)
            {
                ulong magic = 0;
                int shiftFound = 0;

                while (shiftFound == 0)
                {
                    magic = (ulong)random.NextInt64();   // create instance

                    for (int shift = 64; shift > 47; shift--)
                    {
                        ulong[] lookup = rookBlockerBitmasks[i].Select(x => (x * magic) >> shift).ToArray();
                        if (IsUnique(lookup))
                        {
                            shiftFound = shift;
                        }
                    }
                }

                rookMagics[i] = magic;
                rookShifts[i] = shiftFound;

                rookLookup[i] = rookBlockerBitmasks[i].Select(x => (x * magic) >> shiftFound).ToArray();

                Console.WriteLine($"Found shift and magic for {i}: {shiftFound}, {magic}, {(rookBlockerBitmasks[i][1] * magic) >> shiftFound}");
            }
        }

        private static Dictionary<(int, ulong), ulong> GenerateRookLookup()
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
            ulong movemask = 0;

            for (int i = (squareIndex / 8 * 8) + 1; i < (squareIndex / 8 * 8) + 7; i++)
            {
                movemask |= 1ul << i;
            }

            for (int i = 8 + squareIndex % 8; i <= 48 + squareIndex % 8; i += 8)
            {
                movemask |= 1ul << i;
            }

            movemask &= ~(1ul << squareIndex);

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
