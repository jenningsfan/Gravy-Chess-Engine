using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Gravy.GravyChess
{
    internal static class MagicBitboards
    {
        public static ulong[] rookMovemasks;
        public static ulong[][] rookBlockerBitmasks;
        public static ulong[][] rookLookup;

        public static ulong[] rookMagics = new ulong[] { 6302813242384170221, 5742673882911820088, 8103471518102492298, 5734081564827947961, 8133547980671227035, 6093374702486619280, 4613095546263609464, 5679040094644847128, 4764642141007754339, 478193962044271057, 1620869994536952545, 1279740764825841811, 7247126610875058480, 6109102119361894428, 1094970162217252233, 46726669191978088, 7902599288076368093, 4847479427210040796, 3506932468554891120, 1020984679073937488, 6285920083561092756, 7670357973234742752, 5912008052659263128, 8430328281526010923, 7008723023808468578, 335205079719479138, 7263996334238680442, 1493101562418572851, 2090315856632185431, 8267251524589817568, 1450326257323491074, 3420245881310675913, 1972209083367818562, 6732747837839644300, 6469529381327867184, 3071094364859406026, 4760676533121531616, 7224694299914404235, 8066105464762415190, 6257418805721172325, 4357268100719362049, 842720436676245924, 5643457919961793035, 7594832747636790004, 6264772759994102346, 3257791587822993412, 3869032301546700808, 6135949450851516455, 3215292645937285632, 6583058775242815256, 7474291688865866240, 836141212232073728, 5063760697986002945, 4277595246031477531, 3036710063851414528, 1541895527254213778, 1754400546720073234, 5251479176361694722, 4307414596289421313, 635757485424132130, 6532723571648446838, 6586888392247611298, 2541049080768895516, 47902990073506370  };
        public static int[] rookShifts = new int[] { 50, 52, 51, 52, 51, 52, 52, 51, 52, 53, 53, 53, 53, 53, 53, 52, 52, 53, 53, 53, 53, 53, 54, 52, 52, 53, 53, 53, 53, 53, 54, 53, 52, 53, 53, 53, 53, 53, 54, 52, 52, 53, 53, 53, 53, 53, 54, 53, 53, 53, 54, 54, 53, 53, 54, 52, 52, 53, 53, 53, 52, 52, 53, 52  };

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

        public static void GenerateRookMagics(int max = 47)
        {
            bool IsUnique(IEnumerable<ulong> bitboards) {
                // https://stackoverflow.com/questions/18303897/test-if-all-values-in-a-list-are-unique
                HashSet<ulong> diffChecker = new();
                return bitboards.All(diffChecker.Add);
            }

            ulong[][] rookLookup = new ulong[64][];

            ulong[] rookMagics = new ulong[64];
            int[] rookShifts = new int[64];

            Random random = new Random();

            Console.CancelKeyPress += delegate {
                Console.WriteLine("Magics: {0}", string.Join(", ", rookMagics));
                Console.WriteLine("Shifts: {0}", string.Join(", ", rookShifts));
            };

            Parallel.For(0, 64, i =>
            {
                int maxLocal = max;

                while (true)
                {
                    ulong magic = 0;
                    int shiftFound = 0;

                    while (shiftFound == 0)
                    {
                        magic = (ulong)random.NextInt64();   // create instance

                        for (int shift = 63; shift > maxLocal; shift--)
                        {
                            if (IsUnique(rookBlockerBitmasks[i].Select(x => (x * magic) >> shift)))
                            {
                                shiftFound = shift;
                            }
                        }
                    }

                    rookMagics[i] = magic;
                    rookShifts[i] = shiftFound;

                    rookLookup[i] = rookBlockerBitmasks[i].Select(x => (x * magic) >> shiftFound).ToArray();

                    Console.WriteLine($"{i}, {shiftFound}, {magic}");
                    maxLocal += 1;
                }
            });
        }

        private static ulong[][] GenerateRookLookup()
        {
            ulong[][] rookLookup = new ulong[64][];

            for (int i = 0; i < 64; i++)
            {
                rookLookup[i] = new ulong[1 << rookShifts[i]];

                foreach (ulong blockerMask in rookBlockerBitmasks[i])
                {
                    ulong key = (blockerMask * rookMagics[i]) >> rookShifts[i];
                    rookLookup[i][key] = GenerateRookLegalBitboard(blockerMask, i);
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
