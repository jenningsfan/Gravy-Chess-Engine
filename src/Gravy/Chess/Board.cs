using Chess;
using System.Numerics;

namespace Gravy.GravyChess
{
    internal class Board
    {
        public ulong[] bitboards;
        public Piece[] mailbox;

        public Stack<Move> moves;
        public Colour turn;

        public Board()
        {
            bitboards = new ulong[12];
            mailbox = new Piece[64];
            moves = new Stack<Move>();

            // This code ensures that the constructor of MagicBitboards is called before it is first used.
            Type type = typeof(MagicBitboards);
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        }

        public Move[] GenerateMoves()
        {
            Move[] moves = new Move[4096];  // 64 pieces to 64 places
            int movesOffset = 0;

            for (int i = 0; i < bitboards.Length; i++)
            {
                Move[] generatedMoves = GenerateMovesPieceType(i);

                Array.Copy(generatedMoves, 0, moves, generatedMoves.Length, movesOffset);
                movesOffset += generatedMoves.Length;
            }

            return moves[..movesOffset];
        }

        private Move[] GenerateMovesPieceType(int bitboardIndex)
        {
            Move[] moves = new Move[13952];
            int movesOffset = 0;

            ulong bitboard = bitboards[bitboardIndex];
            for (int i = 0; i < 64; i++)
            {
                int square = BitOperations.LeadingZeroCount(bitboards[bitboardIndex]);
                bitboard ^= 1ul << square;

                Move[] generatedMoves = GenerateMovesPiece(bitboardIndex, i);

                Array.Copy(moves, 0, moves, moves.Length, movesOffset);
                movesOffset += generatedMoves.Length;
            }

            return moves[..movesOffset];
        }

        private Move[] GenerateMovesPiece(int piece, int fromSquare)
        {
            Move[] moves = new Move[218];
            int movesGenerated = 0;

            ulong movemask = GenerateMovesBitboard(piece, fromSquare);

            for (int i = 0; i < 64; i++)
            {
                int toSquare = BitOperations.LeadingZeroCount(movemask);
                movemask ^= 1ul << toSquare;

                moves[movesGenerated] = new Move(fromSquare, toSquare, new Piece(piece));
                movesGenerated++;
            }

            return moves[..movesGenerated];
        }

        private ulong GenerateMovesBitboard(int piece, int square)
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
                    return MagicBitboards.rookMovemasks[square];
                case PieceType.Queen:
                    break;
                case PieceType.King:
                    break;
            }

            return 0;
        }

        public void MakeMove(Move move, bool push = true)
        {
            if (push)
            {
                moves.Push(move);
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
                if (move.CastleType == CastlingType.Short)
                {
                    MakeMove(new Move(move.TargetSquare - 1, move.TargetSquare + 1, new Piece(move.Piece.Colour, PieceType.Rook)), false);
                }
                else
                {
                    MakeMove(new Move(move.TargetSquare + 2, move.TargetSquare - 1, new Piece(move.Piece.Colour, PieceType.Rook)), false);
                }
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

            foreach (string rank in fen.Split(" ")[0].Split("/"))
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

        public static int ConvertNotationSquare(string square)
        {
            int file = square[0] - 'a';
            int rank = square[1] - '0' - 1;

            return file + 8 * rank;
        }
    }
}
