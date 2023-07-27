using Chess;
using System.Numerics;

namespace Gravy.GravyChess
{
    internal class Board
    {
        public ulong[] bitboards;
        public Piece[] mailbox;

        public Stack<Move> moves;
        public bool whiteToMove;

        public Colour turn { get => whiteToMove ? Colour.White : Colour.Black; }

        private ulong white { get => bitboards[0] | bitboards[1] | bitboards[2] | bitboards[3] | bitboards[4] | bitboards[5]; }
        private ulong black { get => bitboards[6] | bitboards[7] | bitboards[8] | bitboards[9] | bitboards[10] | bitboards[11]; }

        public ulong friendly { get => whiteToMove ? white : black; }
        public ulong enemy { get => whiteToMove ? black : white; }

        public Board()
        {
            bitboards = new ulong[12];
            mailbox = new Piece[64];
            moves = new Stack<Move>();
            whiteToMove = true;

            // This code ensures that the constructor of MagicBitboards is called before it is first used.
            Type type = typeof(MagicBitboards);
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        }

        public Move[] GenerateMoves()   // TODO: change to span https://youtu.be/_vqlIPDR2TU?t=2396
        {
            Move[] moves = new Move[218];   // Maximum number of moves in any legal positon
            int movesOffset = 0;

            for (int i = (whiteToMove ? 0 : 6); i < (whiteToMove ? 6 : 12); i++)
            {
                Move[] generatedMoves = GenerateMovesPieceType(i);

                Array.Copy(generatedMoves, 0, moves, movesOffset, generatedMoves.Length);
                movesOffset += generatedMoves.Length;
            }

            return moves[..movesOffset];
        }

        private Move[] GenerateMovesPieceType(int bitboardIndex)
        {
            Move[] moves = new Move[128];
            int movesOffset = 0;

            ulong bitboard = bitboards[bitboardIndex];

            while (bitboard != 0)
            {
                int square = BitOperations.TrailingZeroCount(bitboard);
                bitboard ^= 1ul << square;

                Move[] generatedMoves = GenerateMovesPieceTypeOnSquare(bitboardIndex, square);

                Array.Copy(generatedMoves, 0, moves, movesOffset, generatedMoves.Length);
                movesOffset += generatedMoves.Length;
            }

            return moves[..movesOffset];
        }

        private Move[] GenerateMovesPieceTypeOnSquare(int piece, int fromSquare)
        {
            switch ((PieceType)(piece % 6))
            {
                case PieceType.Pawn:
                    break;
                case PieceType.Knight:
                    break;
                case PieceType.Bishop:
                    break;
                case PieceType.Rook:
                    return GenerateMovesRookOnSquare(piece, fromSquare);
                case PieceType.Queen:
                    break;
                case PieceType.King:
                    break;
            }

            return Array.Empty<Move>();
        }

        private Move[] GenerateMovesRookOnSquare(int piece, int fromSquare)
        {
            ulong pieceBitboard = friendly | enemy;
            ulong blockerBitboard = pieceBitboard & MagicBitboards.rookMovemasks[fromSquare];

            (int, ulong) key = (fromSquare, blockerBitboard);
            ulong movemask = MagicBitboards.rookLookup[key];
            movemask &= ~friendly;

            return GenerateMovesFromBitboard(piece, fromSquare, movemask);
        }

        private Move[] GenerateMovesFromBitboard(int piece, int fromSquare, ulong movemask)
        {
            Move[] moves = new Move[64];
            int movesGenerated = 0;

            while (movemask != 0)
            {
                int toSquare = BitOperations.TrailingZeroCount(movemask);
                movemask ^= 1ul << toSquare;

                moves[movesGenerated] = new Move(fromSquare, toSquare, new Piece(piece), this);
                movesGenerated++;
            }

            return moves[..movesGenerated];
        }

        public void MakeMove(Move move, bool push = true)
        {
            if (push)
            {
                moves.Push(move);
                whiteToMove ^= true;    // this is here because this parameter is only used in unmake move to undo moves FIXME: TODO: refactor?, makemove 2 args private and makemove one arg public calling this
            }

            bitboards[move.Piece.BitboardIndex] ^= 1ul << move.TargetSquare | 1ul << move.StartSquare;    // toggle start and target squares

            if (move.IsCapture && !move.IsEnPassant)
            {
                bitboards[((Piece)move.CapturedPiece).BitboardIndex] ^= 1ul << move.TargetSquare;  // if capture remove captured piece
            }

            if (move.IsEnPassant)
            {
                bitboards[((Piece)move.CapturedPiece).BitboardIndex] ^= 1ul << move.EnPassantSquare;
            }

            if (move.IsCastling)
            {
                int rookOldSquare;
                int rookNewSquare;

                if (move.CastleType == CastlingType.Short)
                {
                    rookOldSquare = move.TargetSquare - 1;
                    rookNewSquare = move.TargetSquare + 1;
                }
                else
                {
                    rookOldSquare = move.TargetSquare + 2;
                    rookNewSquare = move.TargetSquare - 1;
                }

                bitboards[(int)PieceType.Rook + (int)move.Piece.Colour * 6] ^= 1ul << rookOldSquare | 1ul << rookNewSquare;    // toggle start and target squares
            }

            if (move.IsPromotion)
            {
                bitboards[move.Piece.BitboardIndex] ^= 1ul << move.TargetSquare;  // Remove piece
                bitboards[((Piece)move.PromotionPiece).BitboardIndex] ^= 1ul << move.TargetSquare;  // Add new piece
            }
        }

        public void UnmakeMove()
        {
            Move move = moves.Pop();
            MakeMove(move, false);
            whiteToMove ^= true;
        }

        public int FindPieceType(int square)
        {
            for (int i = 0; i < 12; i++)
            {
                if (((bitboards[i] >> square) & 1) == 1)
                {
                    return i;
                }
            }

            return -1;
        }

        public void LoadFen(string fen)
        {
            bitboards = new ulong[12];
            mailbox = new Piece[64];
            string[] fenSplit = fen.Split(" ");

            Dictionary<char, Piece> pieceLookup = new Dictionary<char, Piece> {
                { 'P', new(Colour.White, PieceType.Pawn) },
                { 'N', new(Colour.White, PieceType.Knight) },
                { 'B', new(Colour.White, PieceType.Bishop) },
                { 'R', new(Colour.White, PieceType.Rook) },
                { 'Q', new(Colour.White, PieceType.Queen) },
                { 'K', new(Colour.White, PieceType.King) },
                { 'p', new(Colour.Black, PieceType.Pawn) },
                { 'n', new(Colour.Black, PieceType.Knight) },
                { 'b', new(Colour.Black, PieceType.Bishop) },
                { 'r', new(Colour.Black, PieceType.Rook) },
                { 'q', new(Colour.Black, PieceType.Queen) },
                { 'k', new(Colour.Black, PieceType.King) },
            };

            int squareIndex = 63;

            foreach (string rank in fenSplit[0].Split("/"))
            {
                foreach (char piece in rank.Reverse())
                {
                    if (char.IsDigit(piece))
                    {
                        squareIndex -= piece - '0';
                    }
                    else
                    {
                        bitboards[pieceLookup[piece].BitboardIndex] |= 1ul << squareIndex;
                        mailbox[squareIndex] = pieceLookup[piece];

                        squareIndex--;
                    }
                }
            }

            if (fenSplit[1] == "w")
            {
                whiteToMove = true;
            }
            else
            {
                whiteToMove = false;
            }
        }


        public void PrintBoard()
        {
            Dictionary<int, char> pieceLookup = new()
            {
                { -1, ' ' },
                { 0,  'P' },
                { 1,  'N' },
                { 2,  'B' },
                { 3,  'R' },
                { 4,  'Q' },
                { 5,  'K' },
                { 6,  'p' },
                { 7,  'n' },
                { 8,  'b' },
                { 9,  'r' },
                { 10, 'q' },
                { 11, 'k' },
            };

            for (int i = 7; i >= 0; i--)
            {
                string row = "";

                for (int j = 0; j < 8; j++)
                {
                    int pieceType = FindPieceType(i * 8 + j);
                    row += pieceLookup[pieceType];
                }

                Console.WriteLine(row);
            }
        }

        public static int ConvertNotationToSquare(string square)
        {
            int file = square[0] - 'a';
            int rank = square[1] - '0' - 1;

            return file + 8 * rank;
        }

        public static string ConvertSquareToNotation(int square)
        {
            char rank = (char)('a' + square % 8);
            char file = (char)('1' + square / 8);

            return new string(new char[] {rank, file});
        }
    }
}
