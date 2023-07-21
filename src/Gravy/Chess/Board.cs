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
                if ((bitboards[i] & (1ul << square)) == (1ul << square))
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
                foreach (char piece in rank)
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

            for (int i = 63; i >= 0; i--)
            {
                int pieceType = FindPieceType(i);
                Console.Write(pieceLookup[pieceType]);

                if (i % 8 == 0)
                {
                    Console.WriteLine("");
                }
            }
        }

        public static int ConvertNotationSquare(string square)
        {
            int file = 8 - (square[0] - 'a') - 1;
            int rank = square[1] - '0' - 1;

            return file + 8 * rank;
        }
    }
}
